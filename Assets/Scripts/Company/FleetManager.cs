using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FleetTruckData
    {
        public string id;
        public string model;
        public float purchasePrice;
        public float fuelCapacity = 500f;
        public float fuel = 500f;
        public float capacityTons = 30f;
        public float condition = 100f;
        public bool available = true;
        public string assignedDriverId = "";
        public string assignedContractId = "";
    }

    public sealed class FleetManager : MonoBehaviour
    {
        public static FleetManager Instance { get; private set; }
        public IReadOnlyList<FleetTruckData> Trucks => trucks;
        private readonly List<FleetTruckData> trucks = new();
        private int nextId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public FleetTruckData BuyTruck(string model, float price, float capacityTons = 30f)
        {
            var company = CompanyManager.Instance;
            if (company == null || !company.IsCompanyCreated || trucks.Count >= company.Data.truckCapacity) return null;
            var game = Core.GameManager.Instance;
            if (game == null || !game.TrySpendMoney(price)) return null;
            var truck = new FleetTruckData { id = "TRK-" + nextId++, model = string.IsNullOrWhiteSpace(model) ? "Freightliner" : model.Trim(), purchasePrice = price, capacityTons = Mathf.Max(1f, capacityTons) };
            trucks.Add(truck);
            return truck;
        }

        public FleetTruckData Find(string id) => trucks.Find(t => t.id == id);

        public bool Assign(string truckId, string driverId, string contractId)
        {
            var truck = Find(truckId);
            var driver = DriverManager.Instance?.Find(driverId);
            if (truck == null || driver == null || !truck.available || !driver.available) return false;
            truck.assignedDriverId = driverId;
            truck.assignedContractId = contractId ?? "";
            truck.available = false;
            driver.assignedTruckId = truckId;
            driver.assignedContractId = contractId ?? "";
            driver.available = false;
            return true;
        }

        public void Release(string truckId)
        {
            var truck = Find(truckId);
            if (truck == null) return;
            var driver = DriverManager.Instance?.Find(truck.assignedDriverId);
            if (driver != null) { driver.assignedTruckId = ""; driver.assignedContractId = ""; driver.available = true; }
            truck.assignedDriverId = "";
            truck.assignedContractId = "";
            truck.available = true;
        }
    }
}
