using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.World
{
    public sealed class DeliveryTrigger : MonoBehaviour
    {
        public enum TriggerType { Pickup, Destination }
        [SerializeField] private TriggerType triggerType;

        private void OnTriggerEnter(Collider other)
        {
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !other.CompareTag("PlayerTruck")) return;

            if (triggerType == TriggerType.Pickup)
                delivery.LoadCargo();
            else
                delivery.CompleteDelivery();
        }
    }
}
