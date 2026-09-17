using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class DriverData
    {
        public string id;
        public string name;
        public int level = 1;
        public int experience;
        public float driving = 55f;
        public float fuelEfficiency = 55f;
        public float reliability = 55f;
        public float safety = 55f;
        public float cargoHandling = 55f;
        public float salary = 18000f;
        public bool employed = true;
        public bool available = true;
        public string assignedTruckId = "";
        public string assignedContractId = "";
    }

    public sealed class DriverManager : MonoBehaviour
    {
        public static DriverManager Instance { get; private set; }
        public IReadOnlyList<DriverData> Drivers => drivers;
        private readonly List<DriverData> drivers = new();
        private int nextId = 1;

        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); }
        public DriverData HireDriver(string name, float salary = 18000f)
        {
            if (CompanyManager.Instance == null || !CompanyManager.Instance.IsCompanyCreated || drivers.Count >= CompanyManager.Instance.Data.driverCapacity) return null;
            var driver = new DriverData { id = "DRV-" + nextId++, name = string.IsNullOrWhiteSpace(name) ? "New Driver" : name.Trim(), salary = Mathf.Max(0f, salary) };
            drivers.Add(driver); return driver;
        }
        public DriverData Find(string id) => drivers.Find(d => d.id == id);
        public void AddExperience(DriverData driver, int amount)
        {
            if (driver == null || amount <= 0) return;
            driver.experience += amount; driver.level = Mathf.Clamp(1 + driver.experience / 500, 1, 20);
            float growth = driver.level * 0.15f;
            driver.driving = Mathf.Min(100f, driver.driving + growth); driver.reliability = Mathf.Min(100f, driver.reliability + growth * .5f); driver.safety = Mathf.Min(100f, driver.safety + growth * .35f);
        }
        public float GetPerformance(DriverData driver) => driver == null ? 0f : (driver.driving + driver.fuelEfficiency + driver.reliability + driver.safety + driver.cargoHandling) / 5f;
        public void Restore(DriverData[] saved)
        {
            drivers.Clear(); if (saved == null) return; drivers.AddRange(saved);
            nextId = 1; foreach (var d in drivers) if (d != null && int.TryParse(d.id?.Replace("DRV-", ""), out int n)) nextId = Mathf.Max(nextId, n + 1);
        }
    }
}
