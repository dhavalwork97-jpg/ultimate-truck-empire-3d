using System.IO;
using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;
using System.Collections.Generic;

namespace UltimateTruckEmpire.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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
            public bool contractAccepted;
            public bool cargoLoaded;
            public string activeTruckId;
            public AutomatedDelivery[] automatedDeliveries;
        }

        public void Save()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            try
            {
                var data = new SaveData {
                    money = game.Money,
                    xp = game.PlayerXp,
                    company = CompanyManager.Instance?.Data,
                    drivers = DriverManager.Instance == null ? null : new System.Collections.Generic.List<DriverData>(DriverManager.Instance.Drivers).ToArray(),
                    trucks = FleetManager.Instance == null ? null : new System.Collections.Generic.List<FleetTruckData>(FleetManager.Instance.Trucks).ToArray(),
                    finance = FinanceManager.Instance?.Data,
                    contractAccepted = DeliveryManager.Instance != null && DeliveryManager.Instance.ContractAccepted,
                    cargoLoaded = DeliveryManager.Instance != null && DeliveryManager.Instance.CargoLoaded,
                    activeTruckId = FleetManager.Instance?.ActiveTruck?.id ?? "",
                    automatedDeliveries = AutomatedDeliveryManager.Instance?.CaptureState()
                };
                string directory = Application.persistentDataPath;
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, FileName);
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Save failed: {ex.Message}");
            }
        }

        public void Load()
        {
            var game = GameManager.Instance;
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (game == null || !File.Exists(path)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) return;
                game.Restore(data.money, data.xp);
                if (CompanyManager.Instance != null && data.company != null) CompanyManager.Instance.Restore(data.company);
                if (DriverManager.Instance != null && data.drivers != null) DriverManager.Instance.Restore(data.drivers);
                if (FleetManager.Instance != null && data.trucks != null)
                {
                    FleetManager.Instance.Restore(data.trucks);
                    if (!string.IsNullOrWhiteSpace(data.activeTruckId))
                        FleetManager.Instance.SetActiveTruck(data.activeTruckId);
                    else
                        FleetManager.Instance.EnsureActiveTruck();
                }
                if (FinanceManager.Instance != null && data.finance != null) FinanceManager.Instance.Restore(data.finance);
                DeliveryManager.Instance?.Restore(data.contractAccepted, data.cargoLoaded);
                AutomatedDeliveryManager.Instance?.RestoreState(data.automatedDeliveries);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Load failed: {ex.Message}");
            }
        }

        private void OnApplicationPause(bool pause) { if (pause) Save(); }
        private void OnApplicationQuit() => Save();
    }
}
