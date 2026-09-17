using UnityEngine;

namespace UltimateTruckEmpire
{
    [RequireComponent(typeof(Collider))]
    public sealed class DeliveryZone : MonoBehaviour
    {
        [SerializeField] private ContractSystem contracts;
        [SerializeField] private TycoonStateService tycoon;
        [SerializeField] private string zoneName = "North Depot";

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (contracts == null) contracts = FindFirstObjectByType<ContractSystem>();
            if (tycoon == null) tycoon = FindFirstObjectByType<TycoonStateService>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<TruckController>()) return;
            if (contracts == null || !contracts.HasActiveContract) return;
            if (!string.Equals(contracts.ActiveContract.destination, zoneName, System.StringComparison.OrdinalIgnoreCase)) return;

            int payout = contracts.ActiveContract.payout;
            contracts.Complete();
            if (tycoon != null) tycoon.AddCash(payout);
        }
    }
}
