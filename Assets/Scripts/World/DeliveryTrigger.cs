using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Freight;

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
            if (triggerType != TriggerType.Pickup) return;
            if (!IsPlayerTruck(other)) return;

            // Freight jobs use the existing warehouse/depot trigger. No duplicate
            // trigger infrastructure is created; an accepted freight job is loaded
            // when the player reaches its origin city.
            var freight = FreightMarketService.Instance;
            if (freight != null && freight.ActiveJob != null)
            {
                var trailer = other.transform.root.GetComponent<TrailerController>();
                if (trailer == null)
                {
                    Debug.LogWarning("Freight pickup requires a compatible trailer.");
                    return;
                }

                if (!FreightRouteService.TryGetLogisticsTrailerClass(trailer.Type, out LogisticsTrailerClass actualClass) ||
                    !FreightRouteService.TrailerCompatible(freight.ActiveJob.trailerClass, actualClass))
                {
                    Debug.LogWarning($"Freight pickup rejected: job requires {freight.ActiveJob.trailerClass}, trailer is {trailer.Type}.");
                    return;
                }

                if (freight.TryPickupAt(CityFromLocation(locationId)))
                {
                    trailer.Load(freight.ActiveJob != null ? freight.ActiveJob.weightTons : 0f);
                    return;
                }
            }

            var delivery = DeliveryManager.Instance;
            if (delivery == null) return;
            if (!string.IsNullOrWhiteSpace(locationId) && !string.Equals(locationId, delivery.Pickup, System.StringComparison.OrdinalIgnoreCase)) return;
            delivery.LoadCargo(transform.position);
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
                    var trailer = playerTruck.GetComponent<TrailerController>();
                    if (trailer == null || !trailer.CargoLoaded)
                    {
                        return;
                    }

                    if (!FreightRouteService.TryGetLogisticsTrailerClass(trailer.Type, out LogisticsTrailerClass actualClass) ||
                        !FreightRouteService.TrailerCompatible(freight.ActiveJob.trailerClass, actualClass))
                    {
                        return;
                    }

                    string jobId = freight.ActiveJob.id;
                    if (freight.TryDeliverAt(CityFromLocation(locationId), out float payout))
                    {
                        trailer.Unload();
                        TransactionLedger.Instance?.TryRecordIncome(payout, TransactionType.FreightRevenue, "Freight warehouse delivery", jobId);
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

        private static string CityFromLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return string.Empty;
            if (location.IndexOf("Ahmedabad", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Ahmedabad";
            if (location.IndexOf("Vadodara", System.StringComparison.OrdinalIgnoreCase) >= 0) return "Vadodara";
            return location.Trim();
        }
    }
}
