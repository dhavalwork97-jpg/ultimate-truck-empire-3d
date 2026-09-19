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
        public TrailerType type = TrailerType.Curtainsider;
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
    /// Truck.TrailerType values, avoiding the project's two TrailerType enums.
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

        public FleetTrailerData FindAvailableFor(TrailerType type)
        {
            foreach (var trailer in trailers)
                if (trailer != null && trailer.available && trailer.type == type)
                    return trailer;
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
            return AddTrailer("TRL-" + nextId++, "UTE Curtainsider 30T", TrailerType.Curtainsider, 95000f);
        }

        public void EnsureStarterFleet()
        {
            EnsureStarterTrailer();
            // Provide a small, useful starting fleet so the contract system can
            // actually exercise trailer compatibility from the first session.
            EnsureOwnedType(TrailerType.Box, "UTE Box 30T", 110000f);
            EnsureOwnedType(TrailerType.Refrigerated, "UTE Reefer 28T", 165000f);
            EnsureOwnedType(TrailerType.Flatbed, "UTE Flatbed 32T", 125000f);
            EnsureOwnedType(TrailerType.Tanker, "UTE Tanker 30T", 180000f);
            EnsureOwnedType(TrailerType.Lowboy, "UTE Lowboy 40T", 210000f);
        }

        private void EnsureOwnedType(TrailerType type, string model, float price)
        {
            if (FindAny(type) != null) return;
            AddTrailer("TRL-" + nextId++, model, type, price);
        }

        private FleetTrailerData FindAny(TrailerType type)
        {
            foreach (var trailer in trailers)
                if (trailer != null && trailer.type == type) return trailer;
            return null;
        }

        private FleetTrailerData AddTrailer(string id, string model, TrailerType type, float price)
        {
            var trailer = new FleetTrailerData {
                id = id, model = model, type = type, purchasePrice = Mathf.Max(0f, price),
                condition = 100f, available = true
            };
            trailers.Add(trailer);
            return trailer;
        }

        public bool AssignForContract(string truckId, string contractId, TrailerType type)
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

            var trailer = FindAvailableFor(type);
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

        public bool BindPlayerTrailer(string truckId, TrailerType type)
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

        public bool IsCompatible(string truckId, TrailerType type)
        {
            var assigned = FindAssignedToTruck(truckId);
            return assigned != null && assigned.type == type;
        }

        public FleetTrailerData GetAssigned(string truckId) => FindAssignedToTruck(truckId);

        public void Release(string truckId)
        {
            var trailer = FindAssignedToTruck(truckId);
            if (trailer == null) return;
            trailer.assignedTruckId = "";
            trailer.assignedContractId = "";
            trailer.available = true;
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
            controller.Configure(ToPhysicalType(assigned.type));
            return controller;
        }

        public static UltimateTruckEmpire.Truck.TrailerType ToPhysicalType(TrailerType type)
        {
            switch (type)
            {
                case TrailerType.Refrigerated: return UltimateTruckEmpire.Truck.TrailerType.Refrigerated;
                case TrailerType.Flatbed: return UltimateTruckEmpire.Truck.TrailerType.Flatbed;
                case TrailerType.Tanker: return UltimateTruckEmpire.Truck.TrailerType.Tanker;
                case TrailerType.Lowboy: return UltimateTruckEmpire.Truck.TrailerType.HeavyHaul;
                case TrailerType.Box:
                case TrailerType.Curtainsider:
                default: return UltimateTruckEmpire.Truck.TrailerType.DryVan;
            }
        }
    }
}
