using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class GarageService : MonoBehaviour
    {
        private FleetSystem fleet;

        private void Awake()
        {
            fleet = FindFirstObjectByType<FleetSystem>();
        }

        public bool BuyTruck(string truckId)
        {
            return fleet != null && fleet.Purchase(truckId);
        }

        public void UpgradeAcceleration()
        {
            fleet?.UpgradeAcceleration();
        }

        public string GetSelectedTruckName()
        {
            return fleet != null && fleet.Selected != null ? fleet.Selected.displayName : "Hauler 01";
        }
    }
}
