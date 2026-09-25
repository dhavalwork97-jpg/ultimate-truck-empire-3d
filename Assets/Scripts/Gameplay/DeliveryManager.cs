using UnityEngine;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Save;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.TrailerSystem;

namespace UltimateTruckEmpire.Gameplay
{
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }
        public bool ContractAccepted { get; private set; } public bool CargoLoaded { get; private set; }
        public string ContractId { get; private set; } = "PLAYER-001"; public string CargoName { get; private set; } = "Industrial Machinery";
        public string CargoId { get; private set; } = "machinery";
        public string ActiveOriginIndustryId { get; private set; } = "";
        public string ActiveDestinationIndustryId { get; private set; } = "";
        public string ActiveCustomerName { get; private set; } = "";
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
        public float LastCargoDamagePercent { get; private set; }
        public string LastCargoDamageStatus { get; private set; } = "Intact";
        public bool UnloadingActive { get; private set; }
        public string UnloadingStatus { get; private set; } = "";
        public bool DockingActive { get; private set; }
        public string DockingStatus { get; private set; } = "";
        public string LastAcceptMessage { get; private set; } = "";

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
            ActiveOriginIndustryId = offer.originIndustryId ?? "";
            ActiveDestinationIndustryId = offer.destinationIndustryId ?? "";
            ActiveCustomerName = offer.customerName ?? "";
            ContractModifier = offer.modifier;
            ContractQualityBonus = Mathf.Max(0f, offer.qualityBonus);
            ContractQualityPenalty = Mathf.Max(0f, offer.qualityPenalty);
            Pickup = offer.pickup; Destination = offer.destination; Reward = Mathf.Max(0f, offer.reward);
            RewardXp = Mathf.Max(1, offer.xp); ContractDistanceKm = Mathf.Max(1f, offer.distanceKm); ContractWeightTons = Mathf.Max(0f, offer.weightTons); ContractDifficulty = Mathf.Clamp(offer.difficulty, 1, 5);
            var activeTruck = FleetManager.Instance?.EnsureActiveTruck();
            if (activeTruck == null || TrailerFleetManager.Instance == null || !TrailerFleetManager.Instance.AssignForContract(activeTruck.id, ContractId, Trailer))
            {
                LastAcceptMessage = "No free " + Trailer + " trailer available for this contract.";
                return false;
            }
            LastAcceptMessage = "";
            ResetDocking();
            var trailerController = activeTruck == FleetManager.Instance?.ActiveTruck ? FindFirstObjectByType<TruckController>()?.GetComponent<TrailerController>() : null;
            if (!ConfigureActiveTrailerFromContract(false))
                trailerController?.ConfigureGameplay(Trailer, ContractWeightTons);
            ContractAccepted = true; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f; SaveManager.Instance?.Save(); return true;
        }

        public void Restore(bool contractAccepted, bool cargoLoaded, ContractOffer savedContract, int completedContracts)
        {
            CompletedContracts = Mathf.Max(0, completedContracts); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false; ResetDocking();
            if (contractAccepted && savedContract != null)
            {
                AcceptContractInternal(savedContract);
                CargoLoaded = false;
                if (cargoLoaded)
                {
                    CargoLoaded = ConfigureActiveTrailerFromContract(true);
                    if (!CargoLoaded)
                        CargoLoaded = true; // Preserve saved gameplay state when visual cargo data is unavailable.
                }
            }
        }

        private void AcceptContractInternal(ContractOffer offer)
        {
            ContractId = offer.id; CargoName = string.IsNullOrWhiteSpace(offer.cargo) ? "General Freight" : offer.cargo;
            ActiveOriginIndustryId = offer.originIndustryId ?? "";
            ActiveDestinationIndustryId = offer.destinationIndustryId ?? "";
            ActiveCustomerName = offer.customerName ?? "";
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
                if (!ConfigureActiveTrailerFromContract(false))
                    FindFirstObjectByType<TruckController>()?.GetComponent<TrailerController>()?.ConfigureGameplay(Trailer, ContractWeightTons);
            }
            ContractAccepted = true;
        }

        private TrailerCatalogAsset GetTrailerCatalog() => TrailerCatalogRuntime.Catalog;

        private bool ConfigureActiveTrailerFromContract(bool loadCargo)
        {
            var fleet = TrailerFleetManager.Instance;
            var activeTruck = FleetManager.Instance?.ActiveTruck;
            var playerTruck = FindFirstObjectByType<TruckController>();
            if (fleet == null || activeTruck == null || playerTruck == null) return false;

            var ownedTrailer = fleet.FindAssignedToTruck(activeTruck.id);
            var catalog = GetTrailerCatalog();
            if (ownedTrailer == null || catalog == null || string.IsNullOrWhiteSpace(ownedTrailer.definitionId)) return false;

            var definition = catalog.FindTrailer(ownedTrailer.definitionId);
            if (definition == null) return false;

            TrailerSkinDefinition skin = null;
            if (!string.IsNullOrWhiteSpace(ownedTrailer.skinId) && definition.availableSkins != null)
            {
                for (int i = 0; i < definition.availableSkins.Length; i++)
                {
                    var candidate = definition.availableSkins[i];
                    if (candidate != null && string.Equals(candidate.id, ownedTrailer.skinId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        skin = candidate;
                        break;
                    }
                }
            }
            if (skin == null) skin = definition.defaultSkin;

            var controller = playerTruck.GetComponent<TrailerController>() ?? playerTruck.gameObject.AddComponent<TrailerController>();
            if (!loadCargo)
                return controller.ConfigureDefinition(definition, null, 0f, skin);

            var cargo = catalog.FindCargo(CargoId);
            if (cargo == null || !controller.ConfigureDefinition(definition, cargo, ContractWeightTons, skin))
                return false;

            return true;
        }

        public void LoadCargo(Vector3 worldPosition)
        {
            if (!ContractAccepted || CargoLoaded) return;

            // Prefer the authored trailer/cargo definitions when available. The legacy
            // gameplay path remains as a safe fallback for old saves and incomplete data.
            if (!ConfigureActiveTrailerFromContract(true))
            {
                var playerTruck = FindFirstObjectByType<TruckController>();
                playerTruck?.GetComponent<TrailerController>()?.ConfigureGameplay(Trailer, ContractWeightTons);
            }

            CargoLoaded = true;
            pickupWorldPosition = worldPosition; hasPickupPosition = true;
            var activeTruck = FleetManager.Instance?.ActiveTruck; pickupFuelLitres = activeTruck != null ? activeTruck.fuel : -1f; SaveManager.Instance?.Save();
        }
        public void LoadCargo() => LoadCargo(Vector3.zero);

        public const float DockingMaxDistanceM = 7f;
        public const float DockingMaxHeadingErrorDeg = 20f;
        public const float DockingMaxSpeedKph = 2f;
        public const float DockingHoldSeconds = 1f;
        private float dockingHoldStart = -1f;

        private bool MeasureDocking(TruckController truck, Vector3 zonePosition, Vector3 dockingAxis,
            out float distance, out float headingError, out float speedKph, out bool trailerOk)
        {
            distance = float.MaxValue; headingError = 180f; speedKph = 0f; trailerOk = false;
            if (truck == null) return false;
            var trailer = truck.GetComponent<TrailerController>();
            trailerOk = trailer != null && trailer.ContractTrailerType == Trailer;
            Vector3 reference = trailer != null && trailer.DockingPoint != null ? trailer.DockingPoint.position : truck.transform.position;
            distance = Vector3.Distance(new Vector3(reference.x, 0f, reference.z), new Vector3(zonePosition.x, 0f, zonePosition.z));
            Vector3 axis = new Vector3(dockingAxis.x, 0f, dockingAxis.z);
            if (axis.sqrMagnitude < 0.0001f) axis = Vector3.right;
            Vector3 heading = new Vector3(truck.transform.forward.x, 0f, truck.transform.forward.z);
            headingError = Mathf.Min(Vector3.Angle(heading, axis), Vector3.Angle(heading, -axis));
            speedKph = truck.SpeedKph;
            return true;
        }

        private static float ScoreDocking(float distance, float headingError, float speedKph)
        {
            float distanceScore = Mathf.InverseLerp(12f, 2.5f, distance) * 100f;
            float headingScore = Mathf.InverseLerp(35f, 5f, headingError) * 100f;
            float speedScore = Mathf.InverseLerp(7f, 0.5f, speedKph) * 100f;
            return Mathf.Clamp(0.45f * distanceScore + 0.35f * headingScore + 0.20f * speedScore, 0f, 100f);
        }

        public float EvaluateDocking(TruckController truck, Vector3 zonePosition, Vector3 dockingAxis)
        {
            if (!MeasureDocking(truck, zonePosition, dockingAxis, out float d, out float h, out float v, out bool ok) || !ok) return 0f;
            return ScoreDocking(d, h, v);
        }

        public void ResetDocking() { DockingActive = false; DockingStatus = ""; UnloadingActive = false; UnloadingStatus = ""; dockingHoldStart = -1f; }

        public bool TryCompleteDockedDelivery(TruckController truck, Vector3 zonePosition, Vector3 dockingAxis)
        {
            if (!ContractAccepted || !CargoLoaded || truck == null) { ResetDocking(); return false; }
            DockingActive = true;
            UnloadingActive = true;
            UnloadingStatus = "Docking cargo...";
            MeasureDocking(truck, zonePosition, dockingAxis, out float distance, out float headingError, out float speedKph, out bool trailerOk);

            string problem = null;
            if (!trailerOk) problem = "Wrong trailer - this job needs " + Trailer;
            else if (distance > DockingMaxDistanceM) problem = "Move the trailer closer to the bay";
            else if (headingError > DockingMaxHeadingErrorDeg) problem = "Straighten up - align with the yard";
            else if (speedKph > DockingMaxSpeedKph) problem = "Stop the truck";
            if (problem != null) { dockingHoldStart = -1f; UnloadingActive = false; UnloadingStatus = ""; DockingStatus = problem; return false; }

            if (dockingHoldStart < 0f) dockingHoldStart = Time.time;
            float held = Time.time - dockingHoldStart;
            if (held < DockingHoldSeconds)
            {
                DockingStatus = "Hold still... " + (DockingHoldSeconds - held).ToString("0.0") + "s";
                UnloadingStatus = "Positioning for unload...";
                return false;
            }

            float score = ScoreDocking(distance, headingError, speedKph);
            ResetDocking();
            CompleteDelivery(zonePosition, score);
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
            LastDeliveryScore = evaluation.score; LastDeliveryRating = evaluation.rating;
            var cargo = CargoCatalog.Find(CargoId);
            LastCargoDamagePercent = CargoDamageSystem.CalculateDamagePercent(cargo, evaluation.score, LastDockingScore);
            LastCargoDamageStatus = CargoDamageSystem.GetStatus(LastCargoDamagePercent);
            float cargoDamagePenalty = CargoDamageSystem.GetPayoutPenalty(Reward, LastCargoDamagePercent);
            LastDeliveryBonus = evaluation.payoutAdjustment - cargoDamagePenalty;
            float payment = Mathf.Max(0f, Reward + LastDeliveryBonus);
            FinanceManager.Instance?.RecordDelivery(payment, fuelUsed * (fleet?.GetFuelPricePerLitre() ?? EconomyConfig.FuelPricePerLitre), 0f);
            TransactionLedger.Instance?.RecordIncomeWithoutWallet(payment, TransactionType.FreightRevenue, "Freight delivery", ContractId);
            CompanyManager.Instance?.AddRevenue(payment); GameManager.Instance?.AddXp(RewardXp + evaluation.bonusXp);
            SupplyChainManager.Instance?.RecordShipment(
                ActiveOriginIndustryId, ActiveDestinationIndustryId, CargoId, ContractWeightTons);
            playerTruck?.GetComponent<TrailerController>()?.Unload();
            CompletedContracts++; TrailerFleetManager.Instance?.ReleaseContract(truck?.id ?? ""); ContractQualityBonus = 0f; ContractQualityPenalty = 0f; MissionManager.Instance?.NotifyDeliveryComplete(); ContractAccepted = false; CargoLoaded = false; hasPickupPosition = false; pickupFuelLitres = -1f;
            ContractMarket.Instance?.Refresh(); SaveManager.Instance?.Save();
        }
        // Legacy API retained for existing callers. New delivery completion uses the docking-aware overload so existing gameplay code remains source-compatible.
        public void CompleteDelivery(Vector3 worldPosition) => CompleteDelivery(worldPosition, 100f);
        public void CompleteDelivery() => CompleteDelivery(Vector3.zero, 100f);
    }
}
