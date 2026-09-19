using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class TrailerCatalogEntry
    {
        public string manufacturer;
        public string model;
        public UltimateTruckEmpire.Gameplay.TrailerType type;
        public float price;
        public float capacityTons;
        public int requiredCompanyLevel;

        public TrailerCatalogEntry(string manufacturer, string model,
            UltimateTruckEmpire.Gameplay.TrailerType type, float price, float capacityTons, int requiredCompanyLevel)
        {
            this.manufacturer = manufacturer;
            this.model = model;
            this.type = type;
            this.price = Mathf.Max(0f, price);
            this.capacityTons = Mathf.Max(1f, capacityTons);
            this.requiredCompanyLevel = Mathf.Max(1, requiredCompanyLevel);
        }
    }

    public sealed class TrailerDealer : MonoBehaviour
    {
        public static TrailerDealer Instance { get; private set; }
        public IReadOnlyList<TrailerCatalogEntry> Catalog => catalog;
        private readonly List<TrailerCatalogEntry> catalog = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCatalog();
        }

        private void BuildCatalog()
        {
            catalog.Clear();
            catalog.Add(new TrailerCatalogEntry("UTE Trailers", "Box 30T", UltimateTruckEmpire.Gameplay.TrailerType.Box, 110000f, 30f, 1));
            catalog.Add(new TrailerCatalogEntry("UTE Trailers", "Curtainsider 30T", UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider, 95000f, 30f, 1));
            catalog.Add(new TrailerCatalogEntry("UTE Trailers", "Refrigerated 28T", UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated, 165000f, 28f, 2));
            catalog.Add(new TrailerCatalogEntry("UTE Trailers", "Flatbed 32T", UltimateTruckEmpire.Gameplay.TrailerType.Flatbed, 125000f, 32f, 2));
            catalog.Add(new TrailerCatalogEntry("UTE Trailers", "Tanker 30T", UltimateTruckEmpire.Gameplay.TrailerType.Tanker, 180000f, 30f, 3));
            catalog.Add(new TrailerCatalogEntry("UTE Heavy Haul", "Lowboy 40T", UltimateTruckEmpire.Gameplay.TrailerType.Lowboy, 210000f, 40f, 4));
        }

        public bool IsUnlocked(TrailerCatalogEntry entry)
        {
            return entry != null && (CompanyManager.Instance?.Data?.level ?? 1) >= entry.requiredCompanyLevel;
        }

        public string GetUnlockReason(TrailerCatalogEntry entry)
        {
            if (entry == null) return "Trailer unavailable.";
            int level = CompanyManager.Instance?.Data?.level ?? 1;
            return IsUnlocked(entry) ? "Unlocked" : $"Requires HQ Level {entry.requiredCompanyLevel}. Current: {level}.";
        }

        public bool Purchase(TrailerCatalogEntry entry)
        {
            if (entry == null || !IsUnlocked(entry) || TrailerFleetManager.Instance == null) return false;
            return TrailerFleetManager.Instance.Purchase(
                entry.manufacturer + " " + entry.model, entry.type, entry.price);
        }
    }
}