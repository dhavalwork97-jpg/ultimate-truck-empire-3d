using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Freight;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.World
{
    public sealed class DeliveryTrigger : MonoBehaviour
    {
        public enum TriggerType { Pickup, Destination }
        [SerializeField] private TriggerType triggerType;
        [SerializeField] private Transform dockTransform;
        [SerializeField] private string locationId;

        public TriggerType Type => triggerType;
        public string LocationId => locationId;
        public void Configure(TriggerType type) => Configure(type, gameObject.name);
        public void Configure(TriggerType type, string location) { triggerType = type; locationId = location ?? ""; }

        private Collider zone;
        private TruckController playerTruck;

        private void Awake() { zone = GetComponent<Collider>(); }

        private static bool IsPlayerTruck(Collider other)
        {
            if (other == null) return false;
            Transform root = other.transform.root;
            if (root.GetComponent<TruckController>() != null) return true;
            try { return root.CompareTag("PlayerTruck"); }
            catch (UnityException) { return false; }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType != TriggerType.Pickup || !IsPlayerTruck(other)) return;
            ProcessFreightPickup(other.transform.root.GetComponent<TruckController>());
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerType != TriggerType.Pickup || !IsPlayerTruck(other)) return;
            ProcessFreightPickup(other.transform.root.GetComponent<TruckController>());
        }

        private void ProcessFreightPickup(TruckController playerTruck)
        {
            var freight = FreightMarketService.Instance;
            if (freight == null || freight.ActiveJob == null || playerTruck == null) return;

            var job = freight.ActiveJob;
            if (!job.accepted || job.cargoLoaded) return;
            if (!string.Equals(job.originCity, CityFromLocation(locationId), System.StringComparison.OrdinalIgnoreCase)) return;

            var fleet = TrailerFleetManager.Instance;
            var activeFleetTruck = FleetManager.Instance?.ActiveTruck;
            if (fleet == null || activeFleetTruck == null) return;

            var trailer = playerTruck.GetComponent<TrailerController>();
            var attachment = playerTruck.GetComponent<TrailerPhysicsAttachment>();
            bool physicallyAttached = attachment != null && attachment.IsAttached && trailer != null;
            bool hasOwnedTrailer = fleet.GetAssigned(activeFleetTruck.id) != null;

            if (!physicallyAttached)
            {
                // If the player owns a trailer but it is not attached, do not silently
                // replace it. They must attach the correct owned trailer first.
                if (hasOwnedTrailer)
                {
                    Debug.Log("Freight pickup waiting for the player's assigned trailer to be attached.");
                    return;
                }

                // No owned trailer: provide the job trailer physically in the warehouse
                // yard. It remains parked until the player backs into the kingpin zone.
                Vector3 spawnPosition = transform.position + transform.right * 8f + transform.forward * 1f;
                spawnPosition.y = transform.position.y;
                if (!fleet.TrySpawnTemporaryJobTrailerAtWarehouse(
                        job.id,
                        playerTruck,
                        trailerTypeFromJob(job.trailerClass),
                        spawnPosition,
                        transform.rotation))
                {
                    Debug.LogWarning($"Freight pickup could not provide a temporary {job.trailerClass} trailer at {locationId}.");
                }
                return;
            }

            if (!FreightRouteService.TryGetLogisticsTrailerClass(trailer.Type, out LogisticsTrailerClass actualClass) ||
                !FreightRouteService.TrailerCompatible(job.trailerClass, actualClass))
            {
                Debug.LogWarning($"Freight pickup rejected: job requires {job.trailerClass}, trailer is {trailer.Type}.");
                return;
            }

            if (freight.TryPickupAt(CityFromLocation(locationId), trailer.Type))
            {
                trailer.Load(job.weightTons);
                return;
            }
        }

        private void FixedUpdate()
        {
            if (triggerType != TriggerType.Destination) return;
            var freight = FreightMarketService.Instance;
            if (freight != null && playerTruck == null) playerTruck = FindFirstObjectByType<TruckController>();

            if (freight != null && playerTruck != null && zone != null)
            {
                var trailer = playerTruck.GetComponent<TrailerController>();
                Vector3 reference = trailer != null && trailer.DockingPoint != null ? trailer.DockingPoint.position : playerTruck.transform.position;
                Bounds bounds = zone.bounds;
                reference.y = bounds.center.y;
                if (bounds.Contains(reference))
                {
                    var cargoTrailer = playerTruck.GetComponent<TrailerController>();
                    if (cargoTrailer == null || !cargoTrailer.CargoLoaded)
                    {
                        return;
                    }

                    if (!FreightRouteService.TryGetLogisticsTrailerClass(cargoTrailer.Type, out LogisticsTrailerClass actualClass) ||
                        !FreightRouteService.TrailerCompatible(freight.ActiveJob.trailerClass, actualClass))
                    {
                        return;
                    }

                    string jobId = freight.ActiveJob.id;
                    bool temporaryTrailer = cargoTrailer.IsTemporaryJobTrailer;
                    if (freight.TryDeliverAt(CityFromLocation(locationId), out float payout))
                    {
                        cargoTrailer.Unload();
                        TransactionLedger.Instance?.TryRecordIncome(payout, TransactionType.FreightRevenue, "Freight warehouse delivery", jobId);
                        if (temporaryTrailer)
                            TrailerFleetManager.Instance?.RemoveTemporaryJobTrailer(playerTruck);
                        return;
                    }
                }
            }

            var delivery = DeliveryManager.Instance;
            if (delivery == null || !delivery.ContractAccepted || !delivery.CargoLoaded) return;
            if (!string.IsNullOrWhiteSpace(locationId) && !string.Equals(locationId, delivery.Destination, System.StringComparison.OrdinalIgnoreCase)) return;
            if (zone == null) zone = GetComponent<Collider>();
            if (playerTruck == null) playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck == null || zone == null) return;

            var deliveryTrailer = playerTruck.GetComponent<TrailerController>();
            Vector3 deliveryReference = deliveryTrailer != null && deliveryTrailer.DockingPoint != null ? deliveryTrailer.DockingPoint.position : playerTruck.transform.position;
            Bounds deliveryBounds = zone.bounds;
            deliveryReference.y = deliveryBounds.center.y;
            if (!deliveryBounds.Contains(deliveryReference)) { delivery.ResetDocking(); return; }

            Vector3 dockingAxis = dockTransform != null ? dockTransform.forward : transform.forward;
            delivery.TryCompleteDockedDelivery(playerTruck, transform.position, dockingAxis);
        }

        private static UltimateTruckEmpire.Gameplay.TrailerType trailerTypeFromJob(LogisticsTrailerClass trailerClass)
        {
            switch (trailerClass)
            {
                case LogisticsTrailerClass.Tanker: return UltimateTruckEmpire.Gameplay.TrailerType.Tanker;
                case LogisticsTrailerClass.Refrigerated: return UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated;
                case LogisticsTrailerClass.Flatbed: return UltimateTruckEmpire.Gameplay.TrailerType.Flatbed;
                case LogisticsTrailerClass.Oversized: return UltimateTruckEmpire.Gameplay.TrailerType.Lowboy;
                case LogisticsTrailerClass.DryVan:
                default: return UltimateTruckEmpire.Gameplay.TrailerType.Box;
            }
        }

        private static string CityFromLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return string.Empty;
            if (location.IndexOf("Ahmedabad", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Ahmedabad";
            if (location.IndexOf("Vadodara", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Vadodara";
            return location.Trim();
        }
    }
}
