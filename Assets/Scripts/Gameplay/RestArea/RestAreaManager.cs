using System;
using UnityEngine;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Save;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.Gameplay.RestArea
{
    [Serializable]
    public sealed class RestAreaSaveState
    {
        public bool active;
        public string areaId = "";
        public string driverId = "";
        public string truckId = "";
        public int bayIndex = -1;
        public float remainingHours;
        public float totalHours;
        public float cost;
    }

    public sealed class RestAreaManager : MonoBehaviour
    {
        public static RestAreaManager Instance { get; private set; }

        public bool IsResting => active;
        public RestAreaZone CurrentArea { get; private set; }
        public DriverData CurrentDriver { get; private set; }
        public float RemainingHours => remainingHours;
        public float TotalHours => totalHours;
        public float CurrentCost => cost;
        public string StatusMessage { get; private set; } = "Drive into a rest area to park.";

        private bool active;
        private float remainingHours;
        private float totalHours;
        private float cost;
        private int bayIndex = -1;
        private Rigidbody playerBody;
        private TruckController playerTruck;
        private RestAreaSaveState pendingRestore;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!active && pendingRestore != null)
                TryRestorePending();

            if (!active) return;

            // TimeWeatherManager.timeScale is expressed as in-game hours per real second.
            float gameHours = Time.deltaTime * (TimeWeatherManager.Instance != null
                ? Mathf.Max(0f, TimeWeatherManager.Instance.timeScale) : 0.08f);
            if (gameHours <= 0f) return;

            remainingHours = Mathf.Max(0f, remainingHours - gameHours);
            DriverManager.Instance?.RecoverFatigue(CurrentDriver, gameHours);

            if (remainingHours <= 0f)
                FinishRest();
        }

        public bool CanInteract => !active && CurrentArea != null && CurrentArea.CanUse;

        public void SetNearbyArea(RestAreaZone area)
        {
            if (active) return;
            CurrentArea = area;
            StatusMessage = area == null ? "Drive into a rest area to park." :
                area.CanUse ? area.DisplayName + " — press E to park and rest." :
                area.DisplayName + " is currently unavailable.";
        }

        public void ClearNearbyArea(RestAreaZone area)
        {
            if (active || CurrentArea != area) return;
            CurrentArea = null;
            StatusMessage = "Drive into a rest area to park.";
        }

        public bool BeginRest(float hours = -1f)
        {
            if (active || CurrentArea == null || !CurrentArea.CanUse) return false;
            playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck == null) { StatusMessage = "No player truck found."; return false; }
            playerBody = playerTruck.GetComponent<Rigidbody>();
            if (playerBody == null) { StatusMessage = "Truck physics is unavailable."; return false; }

            if (playerBody.linearVelocity.magnitude > CurrentArea.MaxParkingSpeedKph / 3.6f)
            {
                StatusMessage = "Slow down and stop inside a parking bay.";
                return false;
            }

            CurrentDriver = ResolvePlayerDriver();
            if (CurrentDriver == null) { StatusMessage = "No available player driver."; return false; }

            float requestedHours = hours > 0f ? hours : CurrentArea.DefaultRestHours;
            requestedHours = Mathf.Clamp(requestedHours, CurrentArea.MinRestHours, CurrentArea.MaxRestHours);
            cost = CurrentArea.GetCost(requestedHours);

            if (cost > 0f && (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(cost)))
            {
                StatusMessage = "Not enough cash for this rest stop.";
                return false;
            }

            if (!CurrentArea.TryReserveBay(out bayIndex))
            {
                if (cost > 0f) GameManager.Instance?.AddMoney(cost);
                StatusMessage = "All parking bays are occupied.";
                return false;
            }

            Transform bay = CurrentArea.GetBay(bayIndex);
            if (bay != null)
            {
                playerTruck.transform.SetPositionAndRotation(bay.position, bay.rotation);
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }

            playerBody.isKinematic = true;
            totalHours = requestedHours;
            remainingHours = requestedHours;
            active = true;
            CurrentDriver.resting = true;
            CurrentDriver.available = false;

            if (cost > 0f) FinanceManager.Instance?.RecordExpense(cost);
            StatusMessage = "Resting — driver fatigue is recovering.";
            SaveManager.Instance?.Save();
            return true;
        }

        public void FinishRest()
        {
            if (!active) return;

            DriverManager.Instance?.RecoverFatigue(CurrentDriver, remainingHours + 0.001f);
            if (CurrentDriver != null)
            {
                CurrentDriver.resting = false;
                CurrentDriver.available = true;
            }

            if (playerBody != null) playerBody.isKinematic = false;
            CurrentArea?.ReleaseBay(bayIndex);
            active = false;
            remainingHours = 0f;
            bayIndex = -1;
            StatusMessage = "Rest complete. Driver is ready to continue.";
            SaveManager.Instance?.Save();
        }

        public void CancelRest()
        {
            if (!active) return;
            StatusMessage = remainingHours < totalHours
                ? "Rest ended early. Driver recovery is partial."
                : "Rest ended.";
            FinishRest();
        }

        public RestAreaSaveState CaptureState()
        {
            return new RestAreaSaveState {
                active = active,
                areaId = CurrentArea != null ? CurrentArea.AreaId : "",
                driverId = CurrentDriver != null ? CurrentDriver.id : "",
                truckId = FleetManager.Instance?.ActiveTruck?.id ?? "",
                bayIndex = bayIndex,
                remainingHours = remainingHours,
                totalHours = totalHours,
                cost = cost
            };
        }

        public void RestoreState(RestAreaSaveState saved)
        {
            if (saved == null || !saved.active) return;
            pendingRestore = saved;
            TryRestorePending();
        }

        private void TryRestorePending()
        {
            var saved = pendingRestore;
            if (saved == null || active) return;

            var area = RestAreaZone.Find(saved.areaId);
            var driver = DriverManager.Instance?.Find(saved.driverId);
            if (area == null || driver == null || !area.CanUse) return;

            playerTruck = FindFirstObjectByType<TruckController>();
            playerBody = playerTruck != null ? playerTruck.GetComponent<Rigidbody>() : null;
            if (playerBody == null) return;
            if (!area.TryReserveSpecificBay(saved.bayIndex)) return;

            CurrentArea = area;
            CurrentDriver = driver;
            bayIndex = saved.bayIndex;
            remainingHours = Mathf.Max(0f, saved.remainingHours);
            totalHours = Mathf.Max(remainingHours, saved.totalHours);
            cost = Mathf.Max(0f, saved.cost);
            active = remainingHours > 0f;
            driver.resting = active;
            driver.available = !active;
            playerBody.isKinematic = active;

            Transform bay = area.GetBay(bayIndex);
            if (active && bay != null)
            {
                playerTruck.transform.SetPositionAndRotation(bay.position, bay.rotation);
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }

            StatusMessage = active ? "Rest resumed from saved game." : "Rest complete.";
            pendingRestore = null;
        }

        private static DriverData ResolvePlayerDriver()
        {
            var dm = DriverManager.Instance;
            if (dm == null) return null;
            foreach (var d in dm.Drivers)
            {
                if (d != null && d.employed && string.IsNullOrEmpty(d.assignedContractId) && d.available)
                    return d;
            }
            return null;
        }
    }
}
