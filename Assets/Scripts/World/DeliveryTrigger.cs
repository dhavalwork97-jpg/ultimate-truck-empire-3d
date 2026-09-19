using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.World
{
    public sealed class DeliveryTrigger : MonoBehaviour
    {
        public enum TriggerType { Pickup, Destination }
        [SerializeField] private TriggerType triggerType;
        [SerializeField] private Transform dockTransform;

        public TriggerType Type => triggerType;
        public void Configure(TriggerType type) => triggerType = type;

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
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !IsPlayerTruck(other)) return;
            delivery.LoadCargo(transform.position);
        }

        private void FixedUpdate()
        {
            if (triggerType != TriggerType.Destination) return;
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !delivery.ContractAccepted || !delivery.CargoLoaded) return;
            if (zone == null) zone = GetComponent<Collider>();
            if (playerTruck == null) playerTruck = FindFirstObjectByType<TruckController>();
            if (playerTruck == null || zone == null) return;

            var trailer = playerTruck.GetComponent<TrailerController>();
            Vector3 reference = trailer != null && trailer.DockingPoint != null ? trailer.DockingPoint.position : playerTruck.transform.position;
            Bounds bounds = zone.bounds;
            reference.y = bounds.center.y;
            if (!bounds.Contains(reference)) { delivery.ResetDocking(); return; }

            Vector3 dockingAxis = dockTransform != null ? dockTransform.forward : transform.forward;
            delivery.TryCompleteDockedDelivery(playerTruck, transform.position, dockingAxis);
        }
    }
}
