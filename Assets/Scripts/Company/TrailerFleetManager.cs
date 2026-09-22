using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FleetTrailerData
    {
        public string id;
        public string model;
        public string definitionId = "";
        public string skinId = "";
        public UltimateTruckEmpire.Gameplay.TrailerType type = UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider;
        public float purchasePrice;
        public float condition = 100f;
        public bool available = true;
        public string assignedTruckId = "";
        public string assignedContractId = "";

        public float ConditionPercent => Mathf.Clamp(condition, 0f, 100f);
    }

    /// <summary>
    /// Persistent trailer ownership and assignment. Contract trailer types use the
    /// Gameplay catalogue; this manager is the single mapping point to physical
    /// physical truck trailer enum values, avoiding the project's two UltimateTruckEmpire.Gameplay.TrailerType enums.
    /// </summary>
    public sealed class TrailerFleetManager : MonoBehaviour
    {
        public static TrailerFleetManager Instance { get; private set; }
        public IReadOnlyList<FleetTrailerData> Trailers => trailers;

        private readonly List<FleetTrailerData> trailers = new();
        private int nextId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public FleetTrailerData Find(string id) => trailers.Find(t => t != null && t.id == id);

        public FleetTrailerData FindByDefinitionId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var trailer in trailers)
                if (trailer != null && string.Equals(trailer.definitionId, id, StringComparison.OrdinalIgnoreCase))
                    return trailer;
            return null;
        }

        public bool OwnsDefinition(string definitionId) => FindByDefinitionId(definitionId) != null;

        public FleetTrailerData FindAvailableFor(UltimateTruckEmpire.Gameplay.TrailerType type, string forTruckId = null)
        {
            foreach (var trailer in trailers)
            {
                if (trailer == null || !trailer.available || trailer.type != type) continue;
                if (!string.IsNullOrWhiteSpace(trailer.assignedTruckId) &&
                    !string.Equals(trailer.assignedTruckId, forTruckId, StringComparison.OrdinalIgnoreCase))
                    continue;
                return trailer;
            }
            return null;
        }

        public FleetTrailerData FindAssignedToTruck(string truckId)
        {
            if (string.IsNullOrWhiteSpace(truckId)) return null;
            foreach (var trailer in trailers)
                if (trailer != null && string.Equals(trailer.assignedTruckId, truckId, StringComparison.OrdinalIgnoreCase))
                    return trailer;
            return null;
        }

        public FleetTrailerData EnsureStarterTrailer()
        {
            if (trailers.Count > 0) return trailers[0];
            var starter = AddTrailer("TRL-" + nextId++, "UTE Curtainsider 30T", UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider, 95000f, "dry-van");
            starter.skinId = "plain-white";
            return starter;
        }

        public void EnsureStarterFleet()
        {
            // New companies receive only the starter curtainsider. Existing saves are
            // preserved by Restore(), so previously owned trailers are never removed.
            EnsureStarterTrailer();
        }

        private void EnsureOwnedType(UltimateTruckEmpire.Gameplay.TrailerType type, string model, float price)
        {
            if (FindAny(type) != null) return;
            AddTrailer("TRL-" + nextId++, model, type, price);
        }

        private FleetTrailerData FindAny(UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            foreach (var trailer in trailers)
                if (trailer != null && trailer.type == type) return trailer;
            return null;
        }

        private FleetTrailerData AddTrailer(string id, string model, UltimateTruckEmpire.Gameplay.TrailerType type, float price, string definitionId = "")
        {
            var trailer = new FleetTrailerData {
                id = id, model = model, definitionId = definitionId ?? "", type = type, purchasePrice = Mathf.Max(0f, price),
                condition = 100f, available = true
            };
            trailers.Add(trailer);
            return trailer;
        }

        public bool AssignForContract(string truckId, string contractId, UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            var truck = FleetManager.Instance?.Find(truckId);
            if (truck == null) return false;

            var current = FindAssignedToTruck(truckId);
            if (current != null && current.type == type && current.available)
            {
                current.available = false;
                current.assignedContractId = contractId ?? "";
                return true;
            }

            if (current != null && !string.IsNullOrWhiteSpace(current.assignedContractId))
                return false;

            var trailer = FindAvailableFor(type, truckId);
            if (trailer == null) return false;

            if (current != null)
            {
                current.assignedTruckId = "";
                current.assignedContractId = "";
                current.available = true;
            }

            trailer.assignedTruckId = truckId;
            trailer.assignedContractId = contractId ?? "";
            trailer.available = false;
            return true;
        }

        public bool BindPlayerTrailer(string truckId, UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            if (string.IsNullOrWhiteSpace(truckId)) return false;
            var current = FindAssignedToTruck(truckId);
            if (current != null && current.type == type) return true;
            if (current != null && !string.IsNullOrWhiteSpace(current.assignedContractId)) return false;

            var trailer = FindAvailableFor(type);
            if (trailer == null) return false;
            if (current != null) Release(truckId);

            trailer.assignedTruckId = truckId;
            trailer.assignedContractId = "";
            trailer.available = true;
            return true;
        }

        public bool IsCompatible(string truckId, UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            var assigned = FindAssignedToTruck(truckId);
            return assigned != null && assigned.type == type;
        }

        public FleetTrailerData GetAssigned(string truckId) => FindAssignedToTruck(truckId);
        public void ReleaseContract(string truckId)
        {
            var trailer = FindAssignedToTruck(truckId);
            if (trailer == null) return;
            trailer.assignedContractId = "";
            trailer.available = true;
        }


        public void Release(string truckId)
        {
            var trailer = FindAssignedToTruck(truckId);
            if (trailer == null) return;
            trailer.assignedTruckId = "";
            trailer.assignedContractId = "";
            trailer.available = true;
        }

        public float GetRepairCostPerConditionPoint(FleetTrailerData trailer)
        {
            return Mathf.Max(100f, trailer?.purchasePrice ?? 0f) * 0.00055f;
        }

        public bool Repair(string trailerId, float targetCondition = 100f)
        {
            var trailer = Find(trailerId);
            var game = Core.GameManager.Instance;
            if (trailer == null || game == null || !trailer.available) return false;

            float target = Mathf.Clamp(targetCondition, 0f, 100f);
            float points = target - trailer.condition;
            if (points <= 0f) return false;

            float cost = points * GetRepairCostPerConditionPoint(trailer);
            if (!game.TrySpendMoney(cost)) return false;

            trailer.condition = target;
            FinanceManager.Instance?.RecordMaintenance(cost);
            return true;
        }

        public bool AssignToTruck(string trailerId, string truckId)
        {
            var trailer = Find(trailerId);
            var truck = FleetManager.Instance?.Find(truckId);
            if (trailer == null || truck == null || !trailer.available || !truck.available) return false;
            if (FindAssignedToTruck(truckId) != null) return false;

            trailer.assignedTruckId = truckId;
            trailer.assignedContractId = "";
            trailer.available = true;
            return true;
        }

        /// <summary>Purchase a trailer from the data-driven trailer definition.</summary>
        public bool Purchase(UltimateTruckEmpire.TrailerSystem.TrailerDefinition definition)
        {
            var company = CompanyManager.Instance;
            var game = Core.GameManager.Instance;
            if (definition == null || company == null || !company.IsCompanyCreated || game == null) return false;
            if (company.Data.level < definition.requiredCompanyLevel) return false;
            if (OwnsDefinition(definition.id)) return false;
            if (!game.TrySpendMoney(definition.purchasePrice)) return false;

            AddTrailer("TRL-" + nextId++, definition.displayName, FromCategory(definition.category),
                definition.purchasePrice, definition.id);
            FinanceManager.Instance?.RecordCapitalExpense(definition.purchasePrice);
            return true;
        }

        private static UltimateTruckEmpire.Gameplay.TrailerType FromCategory(
            UltimateTruckEmpire.TrailerSystem.TrailerCategory category)
        {
            switch (category)
            {
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.Refrigerated: return UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.Flatbed: return UltimateTruckEmpire.Gameplay.TrailerType.Flatbed;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.HeavyFlatbed: return UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.Lowboy: return UltimateTruckEmpire.Gameplay.TrailerType.Lowboy;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.ContainerChassis: return UltimateTruckEmpire.Gameplay.TrailerType.Container;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.GrainHopper: return UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.CementTanker: return UltimateTruckEmpire.Gameplay.TrailerType.CementTanker;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.DumpTrailer: return UltimateTruckEmpire.Gameplay.TrailerType.Dump;
                case UltimateTruckEmpire.TrailerSystem.TrailerCategory.AgriculturalBulk: return UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk;
                default: return UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider;
            }
        }

        public bool Purchase(string model, UltimateTruckEmpire.Gameplay.TrailerType type, float price)
        {
            var company = CompanyManager.Instance;
            var game = Core.GameManager.Instance;
            if (company == null || !company.IsCompanyCreated || game == null || price < 0f) return false;
            if (!game.TrySpendMoney(price)) return false;

            AddTrailer("TRL-" + nextId++, model, type, price);
            FinanceManager.Instance?.RecordCapitalExpense(price);
            return true;
        }

        public void Restore(FleetTrailerData[] saved)
        {
            trailers.Clear();
            nextId = 1;
            if (saved != null) trailers.AddRange(saved);
            foreach (var trailer in trailers)
            {
                if (trailer == null) continue;
                trailer.condition = Mathf.Clamp(trailer.condition, 0f, 100f);
                if (trailer.definitionId == null) trailer.definitionId = "";
                if (trailer.skinId == null) trailer.skinId = "";
                trailer.purchasePrice = Mathf.Max(0f, trailer.purchasePrice);
                if (int.TryParse(trailer.id?.Replace("TRL-", ""), out int n))
                    nextId = Mathf.Max(nextId, n + 1);
            }
            EnsureStarterFleet();
        }

        public TrailerController ApplyToPlayerTruck(TruckController truck)
        {
            if (truck == null) return null;

            var assigned = FindAssignedToTruck(truck.FleetTruckId);
            if (assigned == null) return null;

            var controller = truck.GetComponent<TrailerController>();
            if (controller == null) controller = truck.gameObject.AddComponent<TrailerController>();

            // Prefer the data-driven trailer definition when the owned record has one.
            // The catalog is intentionally loaded from one small Resources asset so this
            // remains deterministic on mobile and does not require a scene reference.
            TrailerDefinition definition = null;
            TrailerSkinDefinition skin = null;
            var catalog = Resources.Load<UltimateTruckEmpire.TrailerSystem.TrailerCatalogAsset>(
                "TrailerSystem/Catalog/TrailerCatalog");
            if (catalog != null && !string.IsNullOrWhiteSpace(assigned.definitionId))
            {
                definition = catalog.FindTrailer(assigned.definitionId);
                if (definition != null && !string.IsNullOrWhiteSpace(assigned.skinId))
                {
                    var availableSkins = definition.availableSkins;
                    if (availableSkins != null)
                    {
                        for (int i = 0; i < availableSkins.Length; i++)
                        {
                            var candidate = availableSkins[i];
                            if (candidate != null && string.Equals(candidate.id, assigned.skinId, StringComparison.OrdinalIgnoreCase))
                            {
                                skin = candidate;
                                break;
                            }
                        }
                    }
                }
                skin ??= definition.defaultSkin;
            }

            if (definition != null)
            {
                controller.ConfigureDefinition(definition, null, 0f, skin);
                ReplaceTrailerVisual(truck.transform, definition, skin);
            }
            else
            {
                controller.ConfigureGameplay(assigned.type);
            }

            return controller;
        }

        private static void ReplaceTrailerVisual(Transform truck, TrailerDefinition definition, TrailerSkinDefinition skin)
        {
            if (truck == null || definition == null || definition.prefab == null) return;

            // The bootstrap creates a lightweight fallback trailer. Replace only that
            // generated visual; authored trailer prefabs remain independent children.
            var fallback = truck.Find("Dry Van Trailer");
            if (fallback == null) fallback = truck.Find("Trailer Visual");
            Vector3 localPosition = fallback != null ? fallback.localPosition : new Vector3(0f, 1.35f, -2.35f);
            Quaternion localRotation = fallback != null ? fallback.localRotation : Quaternion.identity;
            if (fallback != null) UnityEngine.Object.Destroy(fallback.gameObject);

            var instance = UnityEngine.Object.Instantiate(definition.prefab, truck);
            instance.name = definition.displayName + " Trailer";
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;

            var loadedTrailer = instance.GetComponent<UltimateTruckEmpire.TrailerSystem.LoadedTrailer>();
            if (loadedTrailer != null)
                loadedTrailer.ConfigureEmpty(definition, skin);
        }

    }
}
