using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.TrailerSystem;

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
            if (trailers.Count > 0)
            {
                MigrateStarterDefinitionIfProductionCatalogIsReady(trailers[0]);
                return trailers[0];
            }

            // Prefer the production Batch 01 dry-van identity when the generated
            // catalog is present. Until Unity has generated the catalog, keep the
            // existing starter id so a headless/legacy build remains playable.
            string definitionId = ResolveProductionDefinitionId(
                "TRAILER_DRY_VAN_001",
                "dry-van");

            var starter = AddTrailer(
                "TRL-" + nextId++,
                "UTE Curtainsider 30T",
                UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider,
                95000f,
                definitionId);
            starter.skinId = "plain-white";
            return starter;
        }

        private static string ResolveProductionDefinitionId(string productionId, string fallbackId)
        {
            var catalog = TrailerCatalogRuntime.Catalog;
            if (catalog != null && catalog.FindTrailer(productionId) != null)
                return productionId;
            return fallbackId;
        }

        private static void MigrateStarterDefinitionIfProductionCatalogIsReady(FleetTrailerData trailer)
        {
            if (trailer == null || !string.Equals(trailer.definitionId, "dry-van", StringComparison.OrdinalIgnoreCase))
                return;

            string productionId = ResolveProductionDefinitionId("TRAILER_DRY_VAN_001", null);
            if (!string.IsNullOrWhiteSpace(productionId))
                trailer.definitionId = productionId;
        }

        public void EnsureStarterFleet() => EnsureStarterTrailer();

        private FleetTrailerData AddTrailer(string id, string model, UltimateTruckEmpire.Gameplay.TrailerType type, float price, string definitionId = "")
        {
            var trailer = new FleetTrailerData {
                id = id, model = model, definitionId = definitionId ?? "", type = type,
                purchasePrice = Mathf.Max(0f, price), condition = 100f, available = true
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
            if (current != null && !string.IsNullOrWhiteSpace(current.assignedContractId)) return false;

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

        public float GetRepairCostPerConditionPoint(FleetTrailerData trailer) =>
            Mathf.Max(100f, trailer?.purchasePrice ?? 0f) * 0.00055f;

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

        public bool Purchase(TrailerDefinition definition)
        {
            var company = CompanyManager.Instance;
            var game = Core.GameManager.Instance;
            if (definition == null || company == null || !company.IsCompanyCreated || game == null) return false;
            if (company.Data.level < definition.requiredCompanyLevel || OwnsDefinition(definition.id)) return false;
            if (!game.TrySpendMoney(definition.purchasePrice)) return false;

            AddTrailer("TRL-" + nextId++, definition.displayName, FromCategory(definition.category),
                definition.purchasePrice, definition.id);
            FinanceManager.Instance?.RecordCapitalExpense(definition.purchasePrice);
            return true;
        }

        private static UltimateTruckEmpire.Gameplay.TrailerType FromCategory(TrailerCategory category)
        {
            switch (category)
            {
                case TrailerCategory.Refrigerated: return UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated;
                case TrailerCategory.Flatbed: return UltimateTruckEmpire.Gameplay.TrailerType.Flatbed;
                case TrailerCategory.HeavyFlatbed: return UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed;
                case TrailerCategory.Lowboy: return UltimateTruckEmpire.Gameplay.TrailerType.Lowboy;
                case TrailerCategory.ContainerChassis: return UltimateTruckEmpire.Gameplay.TrailerType.Container;
                case TrailerCategory.GrainHopper: return UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper;
                case TrailerCategory.CementTanker: return UltimateTruckEmpire.Gameplay.TrailerType.CementTanker;
                case TrailerCategory.FuelTanker: return UltimateTruckEmpire.Gameplay.TrailerType.Tanker;
                case TrailerCategory.DumpTrailer: return UltimateTruckEmpire.Gameplay.TrailerType.Dump;
                case TrailerCategory.AgriculturalBulk: return UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk;
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
                trailer.definitionId ??= "";
                trailer.skinId ??= "";
                trailer.purchasePrice = Mathf.Max(0f, trailer.purchasePrice);
                if (int.TryParse(trailer.id?.Replace("TRL-", ""), out int n))
                    nextId = Mathf.Max(nextId, n + 1);
            }
            EnsureStarterFleet();
        }

        public bool TrySpawnTemporaryJobTrailer(TruckController truck, UltimateTruckEmpire.Gameplay.TrailerType jobType)
        {
            if (truck == null) return false;
            var definition = FindTemporaryJobTrailerDefinition(jobType);
            if (definition == null || definition.prefab == null) return false;

            var controller = truck.GetComponent<TrailerController>();
            if (controller == null) controller = truck.gameObject.AddComponent<TrailerController>();
            if (!controller.ConfigureDefinition(definition, null, 0f, definition.defaultSkin)) return false;
            controller.MarkTemporaryJobTrailer(true);
            ReplaceTrailerVisual(truck.transform, controller, definition, definition.defaultSkin);
            return controller.PhysicsAttachment != null && controller.PhysicsAttachment.IsAttached;
        }

        public void RemoveTemporaryJobTrailer(TruckController truck)
        {
            if (truck == null) return;
            var controller = truck.GetComponent<TrailerController>();
            if (controller == null || !controller.IsTemporaryJobTrailer) return;

            var attachment = controller.PhysicsAttachment;
            var instance = attachment != null ? attachment.AttachedTrailer : null;
            attachment?.Detach(false);
            if (instance != null) UnityEngine.Object.Destroy(instance);
            controller.MarkTemporaryJobTrailer(false);
            controller.Unload();
            controller.Definition = null;
            UnityEngine.Object.Destroy(controller);
        }

        public static TrailerDefinition FindTemporaryJobTrailerDefinition(UltimateTruckEmpire.Gameplay.TrailerType jobType)
        {
            var catalog = TrailerCatalogRuntime.Catalog;
            if (catalog == null || catalog.trailers == null) return null;

            TrailerCategory required;
            switch (jobType)
            {
                case UltimateTruckEmpire.Gameplay.TrailerType.Tanker:
                    required = TrailerCategory.FuelTanker;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated:
                    required = TrailerCategory.Refrigerated;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.Flatbed:
                    required = TrailerCategory.Flatbed;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed:
                    required = TrailerCategory.HeavyFlatbed;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.Container:
                    required = TrailerCategory.ContainerChassis;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.Lowboy:
                    required = TrailerCategory.Lowboy;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper:
                    required = TrailerCategory.GrainHopper;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.CementTanker:
                    required = TrailerCategory.CementTanker;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.Dump:
                    required = TrailerCategory.DumpTrailer;
                    break;
                case UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk:
                    required = TrailerCategory.AgriculturalBulk;
                    break;
                default:
                    required = TrailerCategory.DryVan;
                    break;
            }

            for (int i = 0; i < catalog.trailers.Count; i++)
            {
                var candidate = catalog.trailers[i];
                if (candidate != null && candidate.category == required) return candidate;
            }
            return null;
        }

        public TrailerController ApplyToPlayerTruck(TruckController truck)
        {
            if (truck == null) return null;
            var assigned = FindAssignedToTruck(truck.FleetTruckId);
            if (assigned == null) return null;

            var controller = truck.GetComponent<TrailerController>() ?? truck.gameObject.AddComponent<TrailerController>();
            TrailerDefinition definition = null;
            TrailerSkinDefinition skin = null;
            var catalog = TrailerCatalogRuntime.Catalog;

            if (catalog != null && !string.IsNullOrWhiteSpace(assigned.definitionId))
            {
                definition = catalog.FindTrailer(assigned.definitionId);
                if (definition != null && !string.IsNullOrWhiteSpace(assigned.skinId))
                {
                    var availableSkins = definition.availableSkins;
                    if (availableSkins != null)
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
                skin ??= definition.defaultSkin;
            }

            if (definition != null)
            {
                controller.ConfigureDefinition(definition, null, 0f, skin);
                ReplaceTrailerVisual(truck.transform, controller, definition, skin);
            }
            else
            {
                controller.ConfigureGameplay(assigned.type);
            }

            return controller;
        }

        private static void ReplaceTrailerVisual(Transform truck, TrailerController controller, TrailerDefinition definition, TrailerSkinDefinition skin)
        {
            if (truck == null || definition == null || definition.prefab == null) return;

            var attachment = truck.GetComponent<TrailerPhysicsAttachment>() ??
                             truck.gameObject.AddComponent<TrailerPhysicsAttachment>();
            var previous = attachment.AttachedTrailer;
            attachment.Detach(false);

            var fallback = truck.Find("Dry Van Trailer");
            if (fallback == null) fallback = truck.Find("Trailer Visual");

            Vector3 localPosition = fallback != null ? fallback.localPosition : new Vector3(0f, 1.35f, -2.35f);
            Quaternion localRotation = fallback != null ? fallback.localRotation : Quaternion.identity;
            if (fallback != null) UnityEngine.Object.Destroy(fallback.gameObject);
            if (previous != null) UnityEngine.Object.Destroy(previous);

            var instance = UnityEngine.Object.Instantiate(definition.prefab);
            instance.name = definition.displayName + " Trailer";
            instance.transform.SetParent(null, true);
            instance.transform.position = truck.TransformPoint(localPosition);
            instance.transform.rotation = truck.rotation * localRotation;

            var loadedTrailer = instance.GetComponentInChildren<LoadedTrailer>(true);
            if (loadedTrailer != null)
                loadedTrailer.ConfigureEmpty(definition, skin);

            var calibrator = instance.GetComponent<TrailerRuntimeCalibrator>();
            if (calibrator != null && !calibrator.ApplyCalibration())
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            Transform kingpin = FindKingpin(instance.transform);
            if (kingpin != null)
            {
                float mass = calibrator != null && calibrator.HasValidCalibration
                    ? calibrator.EmptyMassTons * 1000f
                    : definition.emptyWeightTons * 1000f;
                if (!attachment.Attach(instance, kingpin, mass))
                {
                    UnityEngine.Object.Destroy(instance);
                    return;
                }
                controller.SetAuthoredDockingPoint(instance.transform);
            }
            else
            {
                // Keep data-driven trailers usable even when an authored prefab is
                // missing its physical kingpin; it remains a visual-only trailer.
                instance.transform.SetParent(truck, true);
                controller.SetAuthoredDockingPoint(instance.transform);
            }
        }

        private static Transform FindKingpin(Transform root)
        {
            if (root == null) return null;
            var direct = root.Find("Sockets/Kingpin") ?? root.Find("Kingpin");
            if (direct != null) return direct;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child != root && string.Equals(child.name, "Kingpin", StringComparison.OrdinalIgnoreCase))
                    return child;
            return null;
        }
    }
}