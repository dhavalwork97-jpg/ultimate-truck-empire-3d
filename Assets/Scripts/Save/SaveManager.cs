using System.IO;
using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;\nusing UltimateTruckEmpire.Gameplay.Toll;
using UltimateTruckEmpire.Truck;
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
        private const string BackupFileName = "ute_save.json.bak";
        private const int CurrentSaveVersion = 3;

        [System.Serializable]
        private sealed class SaveData
        {
            public int saveVersion = 1;
            public float money;
            public int xp;
            public CompanyData company;
            public DriverData[] drivers;
            public FleetTruckData[] trucks;
            public FleetTrailerData[] trailers;
            public FinanceData finance;
            public bool contractAccepted;
            public bool cargoLoaded;
            public ContractOffer activeContract;
            public int completedContracts;
            public string activeTruckId;
            public AutomatedDelivery[] automatedDeliveries;
            public SupplyChainSaveState[] supplyChain;
            public FuelPriceRegionState[] fuelPrices;\n            public TollSaveState tolls;
            public bool hasPlayerTransform;
            public Vector3 playerPosition;
            public Quaternion playerRotation;
        }

        public void Save()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            try
            {
                var data = new SaveData {
                    saveVersion = CurrentSaveVersion,
                    money = game.Money,
                    xp = game.PlayerXp,
                    company = CompanyManager.Instance?.Data,
                    drivers = DriverManager.Instance == null ? null : new System.Collections.Generic.List<DriverData>(DriverManager.Instance.Drivers).ToArray(),
                    trucks = FleetManager.Instance == null ? null : new System.Collections.Generic.List<FleetTruckData>(FleetManager.Instance.Trucks).ToArray(),
                    trailers = TrailerFleetManager.Instance == null ? null : new System.Collections.Generic.List<FleetTrailerData>(TrailerFleetManager.Instance.Trailers).ToArray(),
                    finance = FinanceManager.Instance?.Data,
                    contractAccepted = DeliveryManager.Instance != null && DeliveryManager.Instance.ContractAccepted,
                    cargoLoaded = DeliveryManager.Instance != null && DeliveryManager.Instance.CargoLoaded,
                    activeContract = DeliveryManager.Instance != null && DeliveryManager.Instance.ContractAccepted ? new ContractOffer {
                        id = DeliveryManager.Instance.ContractId,
                        cargo = DeliveryManager.Instance.CargoName,
                        pickup = DeliveryManager.Instance.Pickup,
                        destination = DeliveryManager.Instance.Destination,
                        weightTons = DeliveryManager.Instance.ContractWeightTons,
                        reward = DeliveryManager.Instance.Reward,
                        xp = DeliveryManager.Instance.RewardXp,
                        distanceKm = DeliveryManager.Instance.ContractDistanceKm,
                        difficulty = DeliveryManager.Instance.ContractDifficulty,
                        routeId = "PLAYER-AHM-VAD",
                        routeTier = RouteTier.Local,
                        cargoId = DeliveryManager.Instance.CargoId,
                        trailerType = DeliveryManager.Instance.Trailer,
                        modifier = DeliveryManager.Instance.ContractModifier,
                        qualityBonus = DeliveryManager.Instance.ContractQualityBonus,
                        qualityPenalty = DeliveryManager.Instance.ContractQualityPenalty,
                        originIndustryId = DeliveryManager.Instance.ActiveOriginIndustryId,
                        destinationIndustryId = DeliveryManager.Instance.ActiveDestinationIndustryId,
                        customerName = DeliveryManager.Instance.ActiveCustomerName,
                        supplyChainRouteId = ""
                    } : null,
                    completedContracts = DeliveryManager.Instance?.CompletedContracts ?? 0,
                    activeTruckId = FleetManager.Instance?.ActiveTruck?.id ?? "",
                    hasPlayerTransform = TryGetPlayerTransform(out Vector3 playerPosition, out Quaternion playerRotation),
                    playerPosition = playerPosition,
                    playerRotation = playerRotation,
                    automatedDeliveries = AutomatedDeliveryManager.Instance?.CaptureState(),
                    supplyChain = SupplyChainManager.Instance?.CaptureState(),
                    fuelPrices = FuelPriceManager.Instance?.CaptureState(),\n                    tolls = Toll.TollPlazaManager.Instance?.CaptureState()
                };
                string directory = Application.persistentDataPath;
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, FileName);
                string backupPath = Path.Combine(directory, BackupFileName);
                string tempPath = path + ".tmp";
                string json = JsonUtility.ToJson(data, true);

                File.WriteAllText(tempPath, json);
                if (File.Exists(path))
                    File.Copy(path, backupPath, true);

                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Save failed: {ex.Message}");
            }
        }

        public bool TryGetSavedPlayerTransform(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (!File.Exists(path)) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || !data.hasPlayerTransform) return false;
                position = data.playerPosition;
                rotation = data.playerRotation;
                return true;
            }
            catch { return false; }
        }

        private static SaveData TryReadSave(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;
                if (data.saveVersion < 1 || data.saveVersion > CurrentSaveVersion)
                {
                    Debug.LogWarning($"Unsupported save version: {data.saveVersion}");
                    return null;
                }
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Save read failed: {ex.Message}");
                return null;
            }
        }

        private static bool TryGetPlayerTransform(out Vector3 position, out Quaternion rotation)
        {
            var player = FindFirstObjectByType<TruckController>();
            if (player == null) { position = default; rotation = Quaternion.identity; return false; }
            position = player.transform.position;
            rotation = player.transform.rotation;
            return true;
        }

        public void Load()
        {
            var game = GameManager.Instance;
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (game == null || !File.Exists(path)) return;
            try
            {
                SaveData data = TryReadSave(path);
                if (data == null)
                {
                    string backupPath = Path.Combine(Application.persistentDataPath, BackupFileName);
                    data = TryReadSave(backupPath);
                    if (data != null)
                        Debug.LogWarning("Primary save was unreadable; restored from backup.");
                }
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
                if (TrailerFleetManager.Instance != null)
                {
                    TrailerFleetManager.Instance.Restore(data.trailers);
                    TrailerFleetManager.Instance.ApplyToPlayerTruck(FindFirstObjectByType<TruckController>());
                }
                DeliveryManager.Instance?.Restore(data.contractAccepted, data.cargoLoaded, data.activeContract, data.completedContracts);
                AutomatedDeliveryManager.Instance?.RestoreState(data.automatedDeliveries);
                SupplyChainManager.Instance?.RestoreState(data.supplyChain);
                FuelPriceManager.Instance?.RestoreState(data.fuelPrices);\n                Toll.TollPlazaManager.Instance?.RestoreState(data.tolls);
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
