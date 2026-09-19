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
        public string CargoId { get; private set; } = "machinery";
        public TrailerType Trailer { get; private set; } = TrailerType.Flatbed;
        public ContractModifierRules.Modifier ContractModifier { get; private set; } = ContractModifierRules.Modifier.Standard;
        public float ContractQualityBonus { get; private set; }
        public float ContractQualityPenalty { get; private set; }
        public float Reward { get; private set; } = 145000f; public int RewardXp { get; private set; } = 350;
        public float ContractDistanceKm { get; private set; } = 180f; public float ContractWeightTons { get; private set; } = 18f;
        public int ContractDifficulty { get; private set; } = 2; public int CompletedContracts { get; private set; }
        public string Pickup { get; private set; } = "Ahmedabad Logistics Depot"; public string Destination { get; private set; } = "Vadodara Factory Warehouse";
        public float LastDeliveryScore { get; private set; } public string LastDeliveryRating { get; private set; } = "N/A";
        public float LastDeliveryBonus { get; private set; }
        public float LastDockingScore { get; private set; }

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
            CargoName = string.IsNullOrWhiteSpace(offer.cargo) ? "General Freight" : offer.cargo;
            CargoId = string.IsNullOrWhiteSpace(offer.cargoId) ? "legacy-general" : offer.cargoId;
            Trailer = offer.trailerType;
            ContractModifier = offer.modifier;
            ContractQualityBonus = Mathf.Max(0f, offer.qualityBonus);
            ContractQualityPenalty = Mathf.Max(0f, offer.qualityPenalty);
            Pickup = offer.pickup; Destination = offer.destination; Reward = Mathf.Max(0f, offer.reward);
            RewardXp = Mathf.Max(1, offer.xp); ContractDistanceKm = Mathf.Max(1f, offer.distanceKm); ContractWeightTons = Mathf.Max(0f, offer.weightTons); ContractDifficulty = Mathf.Clamp(offer.difficulty, 1, 5);
            var activeTruck = FleetManager.Instance?.EnsureActiveTruck();
            if (activeTruck == null || TrailerFleetManager.Instance == null || !TrailerFleetManager.Instance.AssignForContract(activeTruck.id, ContractId, Trailer))
                return false;
            var trailerController = activeTruck == FleetManager.Instance?.ActiveTruck ? FindFirstObjectByType<TruckController>()?.GetComponent<TrailerController>() : null;
            trailerController?.ConfigureGameplay(Trailer, ContractWeightTons);
            ContractAccepted = true; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f; SaveManager.Instance?.Save(); return true;
        }

        public void Restore(bool contractAccepted, bool cargoLoaded, ContractOffer savedContract, int completedContracts)
        {
            CompletedContracts = Mathf.Max(0, completedContracts); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false;
            if (contractAccepted && savedContract != null) { AcceptContractInternal(savedContract); CargoLoaded = cargoLoaded; }
        }

        private void AcceptContractInternal(ContractOffer offer)
        {
            ContractId = offer.id; CargoName = string.IsNullOrWhiteSpace(offer.cargo) ? "General Freight" : offer.cargo;
            CargoId = string.IsNullOrWhiteSpace(offer.cargoId) ? "legacy-general" : offer.cargoId;
            Trailer = offer.trailerType; ContractModifier = offer.modifier;
            ContractQualityBonus = Mathf.Max(0f, offer.qualityBonus);
            ContractQualityPenalty = Mathf.Max(0f, offer.qualityPenalty);
            Pickup = offer.pickup; Destination = offer.destination; Reward = Mathf.Max(0f, offer.reward); RewardXp = Mathf.Max(1, offer.xp);
            ContractDistanceKm = Mathf.Max(1f, offer.distanceKm); ContractWeightTons = Mathf.Max(0f, offer.weightTons); ContractDifficulty = Mathf.Clamp(offer.difficulty, 1, 5);
            var activeTruck = FleetManager.Instance?.EnsureActiveTruck();
            if (activeTruck != null && TrailerFleetManager.Instance != null)
            {
                TrailerFleetManager.Instance.AssignForContract(activeTruck.id, ContractId, Trailer);
                FindFirstObjectByType<TruckController>()?.GetComponent<TrailerController>()?.ConfigureGameplay(Trailer, ContractWeightTons);
            }
            ContractAccepted = true;
        }

        public void LoadCargo(Vector3 worldPosition)
        {
            if (!ContractAccepted || CargoLoaded) return; CargoLoaded = true; pickupWorldPosition = worldPosition; hasPickupPosition = true;
            var activeTruck = FleetManager.Instance?.ActiveTruck; pickupFuelLitres = activeTruck != null ? activeTruck.fuel : -1f; SaveManager.Instance?.Save();
        }
        public void LoadCargo() => LoadCargo(Vector3.zero);

        public float EvaluateDocking(TruckController truck, Vector3 zonePosition)
        {
            if (truck == null || TrailerFleetManager.Instance == null) return 0f;
            var trailer = truck.GetComponent<TrailerController>();
            if (trailer == null || trailer.ContractTrailerType != Trailer) return 0f;

            Vector3 dockingPosition = trailer.DockingPoint != null ? trailer.DockingPoint.position : truck.transform.position;
            float distance = Vector3.Distance(new Vector3(dockingPosition.x, 0f, dockingPosition.z), new Vector3(zonePosition.x, 0f, zonePosition.z));
            float distanceScore = Mathf.InverseLerp(12f, 2.5f, distance) * 100f;
            float angle = Quaternion.Angle(truck.transform.rotation, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            angle = Mathf.Min(angle, 360f - angle);
            // The generated road is aligned on the X axis, so compare against both
            // forward directions; this also permits a reverse docking approach.
            float forwardAngle = Vector3.Angle(truck.transform.forward, Vector3.right);
            float reverseAngle = Vector3.Angle(-truck.transform.forward, Vector3.right);
            float headingError = Mathf.Min(forwardAngle, reverseAngle);
            float headingScore = Mathf.InverseLerp(35f, 5f, headingError) * 100f;
            float speedScore = Mathf.InverseLerp(7f, 0.5f, truck.SpeedKph) * 100f;
            return Mathf.Clamp(0.45f * distanceScore + 0.35f * headingScore + 0.20f * speedScore, 0f, 100f);
        }

        public bool TryCompleteDockedDelivery(TruckController truck, Vector3 zonePosition)
        {
            if (!ContractAccepted || !CargoLoaded || truck == null) return false;
            float dockingScore = EvaluateDocking(truck, zonePosition);
            if (dockingScore < 50f) return false;
            CompleteDelivery(zonePosition, dockingScore);
            return true;
        }

        public void CompleteDelivery(Vector3 worldPosition, float dockingScore)
        {
            if (!ContractAccepted || !CargoLoaded) return;
            LastDockingScore = Mathf.Clamp(dockingScore, 0f, 100f);
            var fleet = FleetManager.Instance; var truck = fleet?.ActiveTruck; var playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck != null) { var binding = playerTruck.GetComponent<PlayerTruckFleetBinding>(); if (binding != null && binding.Truck != null) truck = binding.Truck; }
            float routeMeters = hasPickupPosition ? Vector3.Distance(pickupWorldPosition, worldPosition) : ContractDistanceKm * 1000f;
            float distanceKm = Mathf.Max(1f, routeMeters / 1000f); float fuelUsed = pickupFuelLitres >= 0f && truck != null ? Mathf.Max(0f, pickupFuelLitres - truck.fuel) : 0f;
            if (truck != null) fleet?.ApplyTripWear(truck.id, distanceKm, ContractWeightTons, false);

            var evaluation = DeliveryEvaluation.EvaluatePlayer(truck, distanceKm, fuelUsed, LastDockingScore, ContractModifier, Reward, ContractQualityBonus, ContractQualityPenalty);
            LastDeliveryScore = evaluation.score; LastDeliveryRating = evaluation.rating; LastDeliveryBonus = evaluation.payoutAdjustment;
            float payment = Mathf.Max(0f, Reward + evaluation.payoutAdjustment);
            FinanceManager.Instance?.RecordDelivery(payment, fuelUsed * (fleet?.GetFuelPricePerLitre() ?? EconomyConfig.FuelPricePerLitre), 0f);
            CompanyManager.Instance?.AddRevenue(payment); GameManager.Instance?.AddXp(RewardXp + evaluation.bonusXp);
            CompletedContracts++; TrailerFleetManager.Instance?.Release(truck?.id ?? ""); ContractQualityBonus = 0f; ContractQualityPenalty = 0f; MissionManager.Instance?.NotifyDeliveryComplete(); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f;
            ContractMarket.Instance?.Refresh(); SaveManager.Instance?.Save();
        }
        public void CompleteDelivery() => CompleteDelivery(Vector3.zero);
    }
}
