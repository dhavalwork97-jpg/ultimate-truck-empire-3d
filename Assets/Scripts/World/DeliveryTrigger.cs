using UnityEngine;
using UltimateTruckEmpire.Gameplay;

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
            if (root.GetComponent<UltimateTruckEmpire.Truck.TruckController>() != null) return true;
            try { return root.CompareTag("PlayerTruck"); }
            catch (UnityException) { return false; }
        }

        private void OnTriggerEnter(Collider other)
        {
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !IsPlayerTruck(other)) return;

            if (triggerType == TriggerType.Pickup)
                delivery.LoadCargo(transform.position);
            else
                delivery.CompleteDelivery(transform.position);
        }
    }
}