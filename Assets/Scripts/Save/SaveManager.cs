using System.IO;
using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "ute_save.json";

        [System.Serializable]
        private sealed class SaveData
        {
            public float money;
            public int xp;
            public CompanyData company;
            public DriverData[] drivers;
            public FleetTruckData[] trucks;
            public FinanceData finance;
        }

        public void Save()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            var data = new SaveData {
                money = game.Money,
                xp = game.PlayerXp,
                company = CompanyManager.Instance?.Data,
                drivers = DriverManager.Instance == null ? null : new System.Collections.Generic.List<DriverData>(DriverManager.Instance.Drivers).ToArray(),
                trucks = FleetManager.Instance == null ? null : new System.Collections.Generic.List<FleetTruckData>(FleetManager.Instance.Trucks).ToArray(),
                finance = FinanceManager.Instance?.Data
            };
            File.WriteAllText(Path.Combine(Application.persistentDataPath, FileName), JsonUtility.ToJson(data, true));
        }

        public void Load()
        {
            var game = GameManager.Instance;
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (game == null || !File.Exists(path)) return;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null) return;
            game.Restore(data.money, data.xp);
            if (CompanyManager.Instance != null && data.company != null) CompanyManager.Instance.Restore(data.company);
            if (DriverManager.Instance != null && data.drivers != null) DriverManager.Instance.Restore(data.drivers);
            if (FleetManager.Instance != null && data.trucks != null) FleetManager.Instance.Restore(data.trucks);
            if (FinanceManager.Instance != null && data.finance != null) FinanceManager.Instance.Restore(data.finance);
        }

        private void OnApplicationPause(bool pause) { if (pause) Save(); }
        private void OnApplicationQuit() => Save();
    }
}
