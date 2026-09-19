using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class TruckCatalogEntry
    {
        public string manufacturer;
        public string model;
        public float price;
        public float capacityTons;
        public float fuelCapacity;
        public float powerHp;
        public float torqueNm;
        public float maxSpeedKph;
        public float fuelEfficiency;
        public string drivetrain;
        public int requiredCompanyLevel;

        public TruckCatalogEntry(string manufacturer, string model, float price, float capacityTons, float fuelCapacity,
            float powerHp, float torqueNm, float maxSpeedKph, float fuelEfficiency, string drivetrain, int requiredCompanyLevel)
        {
            this.manufacturer = manufacturer; this.model = model; this.price = price; this.capacityTons = capacityTons;
            this.fuelCapacity = fuelCapacity; this.powerHp = powerHp; this.torqueNm = torqueNm; this.maxSpeedKph = maxSpeedKph;
            this.fuelEfficiency = fuelEfficiency; this.drivetrain = drivetrain;
            this.requiredCompanyLevel = Mathf.Max(1, requiredCompanyLevel);
        }
    }

    public sealed class TruckDealer : MonoBehaviour
    {
        public static TruckDealer Instance { get; private set; }
        private readonly List<TruckCatalogEntry> catalog = new();
        public IReadOnlyList<TruckCatalogEntry> Catalog => catalog;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); BuildCatalog();
        }

        private void BuildCatalog()
        {
            catalog.Clear();
            catalog.Add(new TruckCatalogEntry("UTE Motors", "Hauler 300", 180000f, 30f, 500f, 320f, 1450f, 110f, 7.2f, "6x4", 1));
            catalog.Add(new TruckCatalogEntry("UTE Motors", "Hauler 500", 285000f, 36f, 620f, 410f, 1900f, 115f, 6.8f, "6x4", 1));
            catalog.Add(new TruckCatalogEntry("Bharat Forge Trucks", "Atlas 420", 340000f, 40f, 650f, 420f, 2050f, 115f, 6.6f, "6x4", 2));
            catalog.Add(new TruckCatalogEntry("Bharat Forge Trucks", "Atlas X 520", 495000f, 45f, 760f, 520f, 2400f, 120f, 6.1f, "8x4", 3));
            catalog.Add(new TruckCatalogEntry("Western Starline", "Titan 600", 680000f, 50f, 850f, 600f, 2800f, 120f, 5.7f, "8x4", 4));
            catalog.Add(new TruckCatalogEntry("Western Starline", "Titan Heavy 750", 950000f, 65f, 1050f, 750f, 3400f, 105f, 4.9f, "8x8", 5));
        }

        public bool IsUnlocked(TruckCatalogEntry entry)
        {
            if (entry == null) return false;
            int level = CompanyManager.Instance?.Data?.level ?? 1;
            return level >= entry.requiredCompanyLevel;
        }

        public string GetUnlockReason(TruckCatalogEntry entry)
        {
            if (entry == null) return "Truck unavailable.";
            int level = CompanyManager.Instance?.Data?.level ?? 1;
            return level >= entry.requiredCompanyLevel
                ? "Unlocked"
                : $"Requires HQ Level {entry.requiredCompanyLevel}. Current: {level}.";
        }

        public FleetTruckData Purchase(TruckCatalogEntry entry)
        {
            if (entry == null || !IsUnlocked(entry) || FleetManager.Instance == null) return null;
            var truck = FleetManager.Instance.BuyTruck(entry.manufacturer + " " + entry.model, entry.price, entry.capacityTons);
            if (truck == null) return null;
            truck.fuelCapacity = entry.fuelCapacity;
            truck.fuel = entry.fuelCapacity;
            truck.enginePower = entry.powerHp;
            truck.maxSpeedKph = entry.maxSpeedKph;
            truck.fuelEfficiency = entry.fuelEfficiency;
            return truck;
        }
    }
}