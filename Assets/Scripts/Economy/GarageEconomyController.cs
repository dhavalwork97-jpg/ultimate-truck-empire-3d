using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Economy
{
    public sealed class GarageEconomyController : MonoBehaviour
    {
        public bool TryRepairActiveTruck()
        {
            var truck = FleetManager.Instance?.EnsureActiveTruck();
            return truck != null && FleetManager.Instance.Repair(truck.id);
        }

        public bool TryUpgradeActiveTruck(GarageUpgradeType upgrade)
        {
            var fleet = FleetManager.Instance;
            var truck = fleet?.EnsureActiveTruck();
            if (fleet == null || truck == null) return false;

            switch (upgrade)
            {
                case GarageUpgradeType.Engine: return fleet.UpgradeEngine(truck.id);
                case GarageUpgradeType.FuelTank: return fleet.UpgradeFuelTank(truck.id);
                case GarageUpgradeType.Tires:
                case GarageUpgradeType.Gearbox:
                case GarageUpgradeType.Suspension:
                    return false;
                default: return false;
            }
        }

        public bool TrySelectStoredTruck(string truckId)
            => FleetManager.Instance != null && FleetManager.Instance.SetActiveTruck(truckId);

        public float RepairCostForActiveTruck()
        {
            var truck = FleetManager.Instance?.EnsureActiveTruck();
            return truck == null ? 0f : FleetManager.Instance.GetRepairCostPerConditionPoint(truck) * Mathf.Max(0f, 100f - truck.condition);
        }
    }
}
