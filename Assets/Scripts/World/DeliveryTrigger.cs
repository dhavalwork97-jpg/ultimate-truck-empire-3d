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
            // Component check first: the "PlayerTruck" tag only exists if it has been
            // added in the Tag Manager, and CompareTag throws when it has not been.
            if (root.GetComponent<UltimateTruckEmpire.Truck.TruckController>() != null) return true;
            return root.CompareTag("PlayerTruck");
        }

        private void OnTriggerEnter(Collider other)
        {
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !IsPlayerTruck(other)) return;
            if (triggerType == TriggerType.Pickup) delivery.LoadCargo();
            else delivery.CompleteDelivery();
        }
    }
}
