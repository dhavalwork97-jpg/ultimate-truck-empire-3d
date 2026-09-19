using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.World
{
    public sealed class DeliveryTrigger : MonoBehaviour
    {
        public enum TriggerType { Pickup, Destination }
        [SerializeField] private TriggerType triggerType;

        public TriggerType Type => triggerType;
        public void Configure(TriggerType type) => triggerType = type;

        private static bool IsPlayerTruck(Collider other)
        {
            if (other == null) return false;
            Transform root = other.transform.root;
            if (root.GetComponent<TruckController>() != null) return true;
            try { return root.CompareTag("PlayerTruck"); }
            catch (UnityException) { return false; }
        }

        private void TryProcess(Collider other)
        {
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !IsPlayerTruck(other)) return;

            var truck = other.transform.root.GetComponent<TruckController>();
            if (truck == null) return;

            if (triggerType == TriggerType.Pickup)
            {
                delivery.LoadCargo(transform.position);
                return;
            }

            // Destination zones stay active until the player is actually docked.
            // OnTriggerStay is used so entering the yard at speed does not silently
            // complete a delivery; Unity documents it as the physics-timed callback
            // for colliders that remain inside a trigger.
            delivery.TryCompleteDockedDelivery(truck, transform.position);
        }

        private void OnTriggerEnter(Collider other) => TryProcess(other);
        private void OnTriggerStay(Collider other) => TryProcess(other);
    }
}
