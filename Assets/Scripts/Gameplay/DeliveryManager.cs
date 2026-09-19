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
        public bool ContractAccepted { get; private set; } public bool CargoLoaded { get; private set; }
        public string ContractId { get; private set; } = "PLAYER-001"; public string CargoName { get; private set; } = "Industrial Machinery";
        public float Reward { get; private set; } = 145000f; public int RewardXp { get; private set; } = 350;
        public float ContractDistanceKm { get; private set; } = 180f; public float ContractWeightTons { get; private set; } = 18f;
        public int ContractDifficulty { get; private set; } = 2; public int CompletedContracts { get; private set; }
        public string Pickup { get; private set; } = "Ahmedabad Logistics Depot"; public string Destination { get; private set; } = "Vadodara Factory Warehouse";
        private Vector3 pickupWorldPosition; private bool hasPickupPosition; private float pickupFuelLitres;

        private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); }

        public void AcceptStarterContract()
        {
            if (ContractAccepted) return;
            var offer = ContractMarket.Instance?.CreateNextPlayerContract(CompletedContracts);
            if (offer != null) AcceptContract(offer);
        }

        public bool AcceptContract(ContractOffer offer)
        {
            if (ContractAccepted || offer == null) return false;
            ContractId = string.IsNullOrWhiteSpace(offer.id) ? "PLAYER-" + (CompletedContracts + 1).ToString("000") : offer.id;
            CargoName = offer.cargo; Pickup = offer.pickup; Destination = offer.destination; Reward = Mathf.Max(0f, offer.reward);
            RewardXp = Mathf.Max(1, offer.xp); ContractDistanceKm = Mathf.Max(1f, offer.distanceKm); ContractWeightTons = Mathf.Max(0f, offer.weightTons); ContractDifficulty = Mathf.Clamp(offer.difficulty, 1, 5);
            ContractAccepted = true; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f; SaveManager.Instance?.Save(); return true;
        }

        public void Restore(bool contractAccepted, bool cargoLoaded, ContractOffer savedContract, int completedContracts)
        {
            CompletedContracts = Mathf.Max(0, completedContracts); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false;
            if (contractAccepted && savedContract != null) { AcceptContractInternal(savedContract); CargoLoaded = cargoLoaded; }
        }

        private void AcceptContractInternal(ContractOffer offer)
        {
            ContractId = offer.id; CargoName = offer.cargo; Pickup = offer.pickup; Destination = offer.destination; Reward = offer.reward; RewardXp = offer.xp;
            ContractDistanceKm = Mathf.Max(1f, offer.distanceKm); ContractWeightTons = Mathf.Max(0f, offer.weightTons); ContractDifficulty = Mathf.Clamp(offer.difficulty, 1, 5); ContractAccepted = true;
        }

        public void LoadCargo(Vector3 worldPosition)
        {
            if (!ContractAccepted || CargoLoaded) return; CargoLoaded = true; pickupWorldPosition = worldPosition; hasPickupPosition = true;
            var activeTruck = FleetManager.Instance?.ActiveTruck; pickupFuelLitres = activeTruck != null ? activeTruck.fuel : -1f; SaveManager.Instance?.Save();
        }
        public void LoadCargo() => LoadCargo(Vector3.zero);

        public void CompleteDelivery(Vector3 worldPosition)
        {
            if (!ContractAccepted || !CargoLoaded) return;
            var fleet = FleetManager.Instance; var truck = fleet?.ActiveTruck; var playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck != null) { var binding = playerTruck.GetComponent<PlayerTruckFleetBinding>(); if (binding != null && binding.Truck != null) truck = binding.Truck; }
            float routeMeters = hasPickupPosition ? Vector3.Distance(pickupWorldPosition, worldPosition) : ContractDistanceKm * 1000f;
            float distanceKm = Mathf.Max(1f, routeMeters / 1000f); float fuelUsed = pickupFuelLitres >= 0f && truck != null ? Mathf.Max(0f, pickupFuelLitres - truck.fuel) : 0f;
            if (truck != null) fleet?.ApplyTripWear(truck.id, distanceKm, ContractWeightTons, false);
            FinanceManager.Instance?.RecordDelivery(Reward, fuelUsed * (fleet?.GetFuelPricePerLitre() ?? EconomyConfig.FuelPricePerLitre), 0f); CompanyManager.Instance?.AddRevenue(Reward); GameManager.Instance?.AddXp(RewardXp);
            CompletedContracts++; MissionManager.Instance?.NotifyDeliveryComplete(); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f;
            ContractMarket.Instance?.Refresh(); SaveManager.Instance?.Save();
        }
        public void CompleteDelivery() => CompleteDelivery(Vector3.zero);
    }
}
