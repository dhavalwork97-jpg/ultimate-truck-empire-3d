using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class CompanyData
    {
        public string companyName = "My Trucking Company";
        public string headquarters = "Ahmedabad";
        public string businessType = "General Freight";
        public int level = 1;
        public int truckCapacity = 2;
        public int driverCapacity = 2;
        public float reputation = 0f;
        public float companyValue = 0f;
        public List<string> branches = new();
    }

    public sealed class CompanyManager : MonoBehaviour
    {
        public static CompanyManager Instance { get; private set; }
        public CompanyData Data { get; private set; }
        public bool IsCompanyCreated => Data != null;
        public static readonly string[] BusinessTypes = { "General Freight", "Long Haul", "Construction", "Refrigerated", "Heavy Haul", "Luxury Transport" };
        private static readonly int[] UpgradeCosts = { 0, 250000, 750000, 2000000, 5000000 };
        private static readonly float[] UpgradeReputation = { 0f, 10f, 30f, 55f, 80f };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public bool CreateCompany(string name, string headquarters, string type = "General Freight")
        {
            if (IsCompanyCreated || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(headquarters)) return false;
            Data = new CompanyData { companyName = name.Trim(), headquarters = headquarters.Trim(), businessType = IsValidBusinessType(type) ? type.Trim() : BusinessTypes[0] };
            return true;
        }

        public void AddRevenue(float amount)
        {
            if (Data == null) return;
            float value = Mathf.Max(0f, amount);
            Data.companyValue = Mathf.Max(0f, Data.companyValue + value);
            Data.reputation = Mathf.Clamp(Data.reputation + value * 0.00001f, 0f, 100f);
        }

        public int GetHeadquartersUpgradeCost() => Data == null || Data.level >= 5 ? 0 : UpgradeCosts[Data.level];
        public float GetHeadquartersReputationRequirement() => Data == null || Data.level >= 5 ? 100f : UpgradeReputation[Data.level];

        public bool CanUpgradeHeadquarters(out string reason)
        {
            reason = string.Empty;
            if (Data == null) { reason = "Create a company first."; return false; }
            if (Data.level >= 5) { reason = "Maximum headquarters level reached."; return false; }
            int cost = GetHeadquartersUpgradeCost(); float rep = GetHeadquartersReputationRequirement();
            if (Data.reputation < rep) { reason = $"Need {rep:0} reputation."; return false; }
            if (GameManager.Instance == null || GameManager.Instance.Money < cost) { reason = $"Need ₹{cost:N0}."; return false; }
            return true;
        }

        public bool UpgradeHeadquarters()
        {
            if (!CanUpgradeHeadquarters(out _)) return false;
            int cost = GetHeadquartersUpgradeCost();
            if (!GameManager.Instance.TrySpendMoney(cost)) return false;
            Data.level++;
            Data.truckCapacity = Data.level == 2 ? 5 : Data.level == 3 ? 15 : Data.level == 4 ? 30 : 100;
            Data.driverCapacity = Data.truckCapacity;
            return true;
        }

        public int GetNextBranchCost()
        {
            if (Data == null) return 0;
            int n = Data.branches.Count;
            return 400000 + n * 350000;
        }

        public bool AddBranch(string city)
        {
            if (Data == null || string.IsNullOrWhiteSpace(city)) return false;
            city = city.Trim();
            if (Data.branches.Contains(city) || Data.level < 2 || Data.branches.Count >= Data.level - 1) return false;
            int cost = GetNextBranchCost();
            if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(cost)) return false;
            Data.branches.Add(city);
            Data.companyValue += cost * .5f;
            return true;
        }

        public bool SetBusinessType(string type)
        {
            if (Data == null || !IsValidBusinessType(type)) return false;
            Data.businessType = type.Trim(); return true;
        }

        public string GetRank()
        {
            if (Data == null) return "No Company";
            if (Data.level >= 5 && Data.reputation >= 85f) return "Global Logistics Empire";
            if (Data.level >= 4) return "National Logistics";
            if (Data.level >= 3) return "Regional Fleet";
            if (Data.level >= 2) return "Local Carrier";
            return "New Company";
        }

        private static bool IsValidBusinessType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;
            foreach (var value in BusinessTypes) if (string.Equals(value, type.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void Restore(CompanyData saved)
        {
            Data = saved;
            if (Data == null) return;
            if (Data.branches == null) Data.branches = new List<string>();
            if (!IsValidBusinessType(Data.businessType)) Data.businessType = BusinessTypes[0];
            Data.level = Mathf.Clamp(Data.level, 1, 5);
            Data.truckCapacity = Data.level == 1 ? 2 : Data.level == 2 ? 5 : Data.level == 3 ? 15 : Data.level == 4 ? 30 : 100;
            Data.driverCapacity = Data.truckCapacity;
            Data.reputation = Mathf.Clamp(Data.reputation, 0f, 100f);
        }
    }
}
