using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.World
{
    public sealed class DeliveryTrigger : MonoBehaviour
    {
        public enum TriggerType { Pickup, Destination }
        [SerializeField] private TriggerType triggerType;

        public void Configure(TriggerType type) => triggerType = type;

        private void OnTriggerEnter(Collider other)
        {
            var delivery = DeliveryManager.Instance;
            if (delivery == null || !other.transform.root.CompareTag("PlayerTruck")) return;
            if (triggerType == TriggerType.Pickup) delivery.LoadCargo();
            else delivery.CompleteDelivery();
        }
    }
}
