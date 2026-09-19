using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Save;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.Gameplay
{
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }
        public bool ContractAccepted { get; private set; }
        public bool CargoLoaded { get; private set; }
        public string CargoName { get; private set; } = "Industrial Machinery";
        public float Reward { get; private set; } = 145000f;
        public int RewardXp { get; private set; } = 350;
        public string Pickup => "Ahmedabad Logistics Depot";
        public string Destination => "Vadodara Factory Warehouse";

        private Vector3 pickupWorldPosition;
        private bool hasPickupPosition;
        private float pickupFuelLitres;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AcceptStarterContract()
        {
            if (ContractAccepted) return;
            ContractAccepted = true;
            CargoLoaded = false;
            hasPickupPosition = false;
            pickupFuelLitres = -1f;
            SaveManager.Instance?.Save();
        }

        public void Restore(bool contractAccepted, bool cargoLoaded)
        {
            ContractAccepted = contractAccepted;
            CargoLoaded = contractAccepted && cargoLoaded;
            hasPickupPosition = false;
        }

        public void LoadCargo(Vector3 worldPosition)
        {
            if (!ContractAccepted || CargoLoaded) return;
            CargoLoaded = true;
            pickupWorldPosition = worldPosition;
            hasPickupPosition = true;
            var activeTruck = FleetManager.Instance?.ActiveTruck;
            pickupFuelLitres = activeTruck != null ? activeTruck.fuel : -1f;
            SaveManager.Instance?.Save();
        }

        public void LoadCargo()
        {
            LoadCargo(Vector3.zero);
        }

        public void CompleteDelivery(Vector3 worldPosition)
        {
            if (!ContractAccepted || !CargoLoaded) return;

            var fleet = FleetManager.Instance;
            var truck = fleet?.ActiveTruck;
            var playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck != null)
            {
                var binding = playerTruck.GetComponent<PlayerTruckFleetBinding>();
                if (binding != null && binding.Truck != null) truck = binding.Truck;
            }

            // Apply condition wear to the player truck without deducting fuel a second
            // time; TruckController already consumes fuel while driving.
            float routeMeters = hasPickupPosition
                ? Vector3.Distance(pickupWorldPosition, worldPosition)
                : 1000f;
            float distanceKm = Mathf.Max(1f, routeMeters / 1000f);
            float fuelUsed = pickupFuelLitres >= 0f && truck != null
                ? Mathf.Max(0f, pickupFuelLitres - truck.fuel)
                : 0f;
            if (truck != null)
                fleet?.ApplyTripWear(truck.id, distanceKm, 18f, false);

            FinanceManager.Instance?.RecordDelivery(Reward, fuelUsed * (fleet?.GetFuelPricePerLitre() ?? 95f), 0f);
            CompanyManager.Instance?.AddRevenue(Reward);
            GameManager.Instance?.AddXp(RewardXp);

            ContractAccepted = false;
            CargoLoaded = false;
            hasPickupPosition = false;
            SaveManager.Instance?.Save();
        }

        public void CompleteDelivery()
        {
            CompleteDelivery(Vector3.zero);
        }
    }
}