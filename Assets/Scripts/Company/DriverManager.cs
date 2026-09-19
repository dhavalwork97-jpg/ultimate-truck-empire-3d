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
        public float fatigue;
        public int tripsCompleted;
        public float distanceDrivenKm;
        public bool resting;
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
            driver.experience += amount;
            driver.level = Mathf.Clamp(1 + driver.experience / 500, 1, 20);

            float growth = driver.level * 0.15f;
            driver.driving = Mathf.Min(100f, driver.driving + growth);
            driver.reliability = Mathf.Min(100f, driver.reliability + growth * .5f);
            driver.safety = Mathf.Min(100f, driver.safety + growth * .35f);
            driver.fuelEfficiency = Mathf.Min(100f, driver.fuelEfficiency + growth * .25f);
            driver.cargoHandling = Mathf.Min(100f, driver.cargoHandling + growth * .2f);
        }

        public bool CanDispatch(DriverData driver)
        {
            return driver != null && driver.employed && driver.available && !driver.resting && driver.fatigue < 90f;
        }

        public void BeginDelivery(DriverData driver)
        {
            if (driver == null) return;
            driver.available = false;
            driver.resting = false;
        }

        public void CompleteDelivery(DriverData driver, float distanceKm, int xp)
        {
            if (driver == null) return;

            driver.tripsCompleted++;
            driver.distanceDrivenKm += Mathf.Max(0f, distanceKm);
            driver.fatigue = Mathf.Clamp(driver.fatigue + Mathf.Clamp(distanceKm / 25f, 5f, 45f), 0f, 100f);
            AddExperience(driver, Mathf.Max(0, xp));

            driver.assignedTruckId = "";
            driver.assignedContractId = "";

            if (driver.fatigue >= 75f)
            {
                driver.resting = true;
                driver.available = false;
            }
            else
            {
                driver.resting = false;
                driver.available = true;
            }
        }

        public void RestDriver(DriverData driver)
        {
            if (driver == null || !driver.employed) return;
            driver.fatigue = 0f;
            driver.resting = false;
            driver.available = true;
            driver.assignedTruckId = "";
            driver.assignedContractId = "";
        }

        public void RecoverFatigue(DriverData driver, float hours)
        {
            if (driver == null || hours <= 0f) return;
            driver.fatigue = Mathf.Max(0f, driver.fatigue - hours * 12f);
            if (driver.fatigue < 75f && string.IsNullOrEmpty(driver.assignedContractId))
            {
                driver.resting = false;
                driver.available = true;
            }
        }

        public float GetEffectivePerformance(DriverData driver)
        {
            if (driver == null) return 0f;
            float fatiguePenalty = Mathf.Lerp(1f, .55f, driver.fatigue / 100f);
            return GetPerformance(driver) * fatiguePenalty;
        }

        public float GetDailyPayroll()
        {
            float total = 0f;
            foreach (var driver in drivers)
                if (driver != null && driver.employed)
                    total += Mathf.Max(0f, driver.salary);
            return total;
        }
        public float GetPerformance(DriverData driver) => driver == null ? 0f : (driver.driving + driver.fuelEfficiency + driver.reliability + driver.safety + driver.cargoHandling) / 5f;
        public void Restore(DriverData[] saved)
        {
            drivers.Clear(); if (saved == null) return; drivers.AddRange(saved);
            nextId = 1; foreach (var d in drivers) if (d != null && int.TryParse(d.id?.Replace("DRV-", ""), out int n)) nextId = Mathf.Max(nextId, n + 1);
        }
    }
}
