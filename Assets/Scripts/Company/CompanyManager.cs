using System;
using System.Collections.Generic;
using UnityEngine;

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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool CreateCompany(string name, string headquarters, string type = "General Freight")
        {
            if (IsCompanyCreated || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(headquarters)) return false;
            Data = new CompanyData { companyName = name.Trim(), headquarters = headquarters.Trim(), businessType = string.IsNullOrWhiteSpace(type) ? "General Freight" : type.Trim() };
            return true;
        }

        public void AddRevenue(float amount)
        {
            if (Data == null) return;
            Data.companyValue = Mathf.Max(0f, Data.companyValue + Mathf.Max(0f, amount));
            Data.reputation = Mathf.Clamp(Data.reputation + Mathf.Max(0f, amount) * 0.00001f, 0f, 100f);
        }

        public bool UpgradeHeadquarters()
        {
            if (Data == null || Data.level >= 5) return false;
            Data.level++;
            Data.truckCapacity = Data.level == 2 ? 5 : Data.level == 3 ? 15 : Data.level == 4 ? 30 : 100;
            Data.driverCapacity = Data.truckCapacity;
            return true;
        }

        public bool AddBranch(string city)
        {
            if (Data == null || string.IsNullOrWhiteSpace(city) || Data.branches.Contains(city.Trim())) return false;
            Data.branches.Add(city.Trim());
            return true;
        }

        public void Restore(CompanyData saved)
        {
            Data = saved;
            if (Data != null && Data.branches == null) Data.branches = new List<string>();
        }
    }
}
