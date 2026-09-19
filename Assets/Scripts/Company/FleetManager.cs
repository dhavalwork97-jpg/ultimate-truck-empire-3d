using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FleetTruckData
    {
        public string id;
        public string model;
        public string definitionId = "ute-starter";
        public float purchasePrice;
        public float fuelCapacity = 350f;
        public float fuel = 350f;
        public float capacityTons = 18f;
        public float condition = 100f;
        public float enginePower = 300f;
        public float maxSpeedKph = 90f;
        public float fuelEfficiency = 3.2f;
        public float reliability = 72f;
        public float maintenanceCostPerKm = 5.5f;
        public int engineUpgradeLevel;
        public int fuelUpgradeLevel;
        public int reliabilityUpgradeLevel;
        public bool available = true;
        public string assignedDriverId = "";
        public string assignedContractId = "";

        public float ConditionPercent => Mathf.Clamp(condition, 0f, 100f);
        public float FuelPercent => fuelCapacity <= 0f ? 0f : Mathf.Clamp01(fuel / fuelCapacity) * 100f;
    }

    public sealed class FleetManager : MonoBehaviour
    {
        public static FleetManager Instance { get; private set; }
        public IReadOnlyList<FleetTruckData> Trucks => trucks;
        public FleetTruckData ActiveTruck { get; private set; }

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
            var definition = TruckCatalog.FindByName(model);
            if (definition != null)
                return BuyTruck(definition.id);

            return BuyTruckInternal(string.Empty, string.IsNullOrWhiteSpace(model) ? "UTE Hauler" : model.Trim(),
                price, capacityTons, 350f, 300f, 90f, 3.2f, 70f, 5.5f);
        }

        public FleetTruckData BuyTruck(string definitionId)
        {
            var definition = TruckCatalog.Find(definitionId);
            if (definition == null) return null;

            return BuyTruckInternal(definition.id, definition.displayName, definition.purchasePrice,
                definition.capacityTons, definition.fuelCapacity, definition.enginePower, definition.maxSpeedKph,
                definition.fuelEfficiency, definition.reliability, definition.maintenanceCostPerKm);
        }

        private FleetTruckData BuyTruckInternal(string definitionId, string model, float price, float capacityTons,
            float fuelCapacity, float enginePower, float maxSpeedKph, float fuelEfficiency,
            float reliability, float maintenanceCostPerKm)
        {
            var company = CompanyManager.Instance;
            var game = Core.GameManager.Instance;
            if (company == null || !company.IsCompanyCreated ||
                trucks.Count >= company.Data.truckCapacity ||
                game == null || price < 0f || !game.TrySpendMoney(price))
                return null;

            var truck = new FleetTruckData
            {
                id = "TRK-" + nextId++,
                definitionId = definitionId,
                model = string.IsNullOrWhiteSpace(model) ? "UTE Hauler" : model.Trim(),
                purchasePrice = Mathf.Max(0f, price),
                capacityTons = Mathf.Max(1f, capacityTons),
                fuelCapacity = Mathf.Max(1f, fuelCapacity),
                fuel = Mathf.Max(1f, fuelCapacity),
                enginePower = Mathf.Max(1f, enginePower),
                maxSpeedKph = Mathf.Max(1f, maxSpeedKph),
                fuelEfficiency = Mathf.Max(0.1f, fuelEfficiency),
                reliability = Mathf.Clamp(reliability, 0f, 100f),
                maintenanceCostPerKm = Mathf.Max(0f, maintenanceCostPerKm)
            };

            trucks.Add(truck);
            FinanceManager.Instance?.RecordCapitalExpense(truck.purchasePrice);
            if (ActiveTruck == null) ActiveTruck = truck;
            return truck;
        }

        public FleetTruckData Find(string id) => trucks.Find(t => t != null && t.id == id);

        public bool SetActiveTruck(string truckId)
        {
            var truck = Find(truckId);
            if (truck == null || !truck.available) return false;
            ActiveTruck = truck;
            return true;
        }

        public FleetTruckData EnsureActiveTruck()
        {
            if (ActiveTruck != null && trucks.Contains(ActiveTruck) && ActiveTruck.available)
                return ActiveTruck;

            foreach (var truck in trucks)
            {
                if (truck != null && truck.available)
                {
                    ActiveTruck = truck;
                    return truck;
                }
            }

            ActiveTruck = null;
            return null;
        }

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
            if (driver != null)
            {
                driver.assignedTruckId = "";
                driver.assignedContractId = "";
                driver.available = true;
            }

            truck.assignedDriverId = "";
            truck.assignedContractId = "";
            truck.available = true;
        }

        public bool Refuel(string truckId, float amount)
        {
            var truck = Find(truckId);
            var game = Core.GameManager.Instance;
            if (truck == null || game == null || amount <= 0f) return false;

            float litres = Mathf.Min(amount, Mathf.Max(0f, truck.fuelCapacity - truck.fuel));
            if (litres <= 0f) return false;

            float cost = litres * GetFuelPricePerLitre();
            if (!game.TrySpendMoney(cost)) return false;

            truck.fuel += litres;
            FinanceManager.Instance?.RecordFuelExpense(cost);
            return true;
        }

        public bool Repair(string truckId, float targetCondition = 100f)
        {
            var truck = Find(truckId);
            var game = Core.GameManager.Instance;
            if (truck == null || game == null) return false;

            float target = Mathf.Clamp(targetCondition, 0f, 100f);
            float points = target - truck.condition;
            if (points <= 0f) return false;

            float cost = points * GetRepairCostPerConditionPoint(truck);
            if (!game.TrySpendMoney(cost)) return false;

            truck.condition = target;
            FinanceManager.Instance?.RecordMaintenance(cost);
            return true;
        }

        public bool UpgradeEngine(string truckId)
        {
            var truck = Find(truckId);
            if (truck == null || truck.engineUpgradeLevel >= 5) return false;
            if (CompanyManager.Instance?.Data == null || CompanyManager.Instance.Data.level < 2) return false;

            float cost = EconomyConfig.GetUpgradeCost(EconomyConfig.EngineUpgradeBaseCost, truck.engineUpgradeLevel);
            if (Core.GameManager.Instance == null || !Core.GameManager.Instance.TrySpendMoney(cost)) return false;

            truck.engineUpgradeLevel++;
            FinanceManager.Instance?.RecordCapitalExpense(cost);
            truck.enginePower += 35f;
            truck.maxSpeedKph += 2f;
            return true;
        }

        public bool UpgradeFuelTank(string truckId)
        {
            var truck = Find(truckId);
            if (truck == null || truck.fuelUpgradeLevel >= 3) return false;
            if (CompanyManager.Instance?.Data == null || CompanyManager.Instance.Data.level < 2) return false;

            float cost = EconomyConfig.GetUpgradeCost(EconomyConfig.FuelTankUpgradeBaseCost, truck.fuelUpgradeLevel);
            if (Core.GameManager.Instance == null || !Core.GameManager.Instance.TrySpendMoney(cost)) return false;

            float oldCapacity = truck.fuelCapacity;
            truck.fuelUpgradeLevel++;
            FinanceManager.Instance?.RecordCapitalExpense(cost);
            truck.fuelCapacity += 100f;
            truck.fuel += truck.fuelCapacity - oldCapacity;
            return true;
        }

        public bool UpgradeReliability(string truckId)
        {
            var truck = Find(truckId);
            if (truck == null || truck.reliabilityUpgradeLevel >= 3) return false;
            if (CompanyManager.Instance?.Data == null || CompanyManager.Instance.Data.level < 3) return false;

            float cost = EconomyConfig.GetUpgradeCost(EconomyConfig.ReliabilityUpgradeBaseCost, truck.reliabilityUpgradeLevel);
            if (Core.GameManager.Instance == null || !Core.GameManager.Instance.TrySpendMoney(cost)) return false;

            truck.reliabilityUpgradeLevel++;
            FinanceManager.Instance?.RecordCapitalExpense(cost);
            truck.reliability = Mathf.Min(100f, truck.reliability + 7.5f);
            truck.maintenanceCostPerKm = Mathf.Max(1f, truck.maintenanceCostPerKm - 0.75f);
            return true;
        }

        public float GetUpgradeCost(FleetTruckData truck, float baseCost, int currentLevel)
        {
            if (truck == null) return 0f;
            return EconomyConfig.GetUpgradeCost(baseCost, currentLevel);
        }

        public float GetRepairCostPerConditionPoint(FleetTruckData truck)
        {
            return EconomyConfig.GetRepairCostPerConditionPoint(truck?.maintenanceCostPerKm ?? 0f);
        }

        public float GetFuelPricePerLitre() => EconomyConfig.FuelPricePerLitre;

        public void ApplyTripWear(string truckId, float distanceKm, float cargoWeightTons, bool consumeFuel = true)
        {
            var truck = Find(truckId);
            if (truck == null || distanceKm <= 0f) return;

            float weightFactor = 1f + Mathf.Clamp(cargoWeightTons, 0f, 60f) / 100f;
            if (consumeFuel)
            {
                float fuelUsed = distanceKm / Mathf.Max(0.1f, truck.fuelEfficiency) * weightFactor;
                truck.fuel = Mathf.Max(0f, truck.fuel - fuelUsed);
            }

            float wear = distanceKm * (0.006f + weightFactor * 0.002f);
            truck.condition = Mathf.Max(0f, truck.condition - wear);
        }

        public void Restore(FleetTruckData[] saved)
        {
            trucks.Clear();
            if (saved == null) return;

            trucks.AddRange(saved);
            ActiveTruck = null;
            nextId = 1;

            foreach (var truck in trucks)
            {
                if (truck == null) continue;

                var definition = TruckCatalog.Find(truck.definitionId) ?? TruckCatalog.FindByName(truck.model);
                if (definition != null)
                {
                    if (truck.fuelCapacity <= 0f) truck.fuelCapacity = definition.fuelCapacity;
                    if (truck.enginePower <= 0f) truck.enginePower = definition.enginePower;
                    if (truck.maxSpeedKph <= 0f) truck.maxSpeedKph = definition.maxSpeedKph;
                    if (truck.fuelEfficiency <= 0f) truck.fuelEfficiency = definition.fuelEfficiency;
                    if (truck.reliability <= 0f) truck.reliability = definition.reliability;
                    if (truck.maintenanceCostPerKm <= 0f) truck.maintenanceCostPerKm = definition.maintenanceCostPerKm;
                    if (string.IsNullOrWhiteSpace(truck.definitionId)) truck.definitionId = definition.id;
                }

                truck.fuelCapacity = Mathf.Max(1f, truck.fuelCapacity);
                truck.fuel = Mathf.Clamp(truck.fuel, 0f, truck.fuelCapacity);
                truck.condition = Mathf.Clamp(truck.condition, 0f, 100f);
                truck.capacityTons = Mathf.Max(1f, truck.capacityTons);

                if (int.TryParse(truck.id?.Replace("TRK-", ""), out int n))
                    nextId = Mathf.Max(nextId, n + 1);
            }

            EnsureActiveTruck();
        }
    }
}