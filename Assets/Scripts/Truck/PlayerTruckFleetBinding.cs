using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Truck
{
    /// Connects the runtime player vehicle to the persistent fleet record.
    /// The vehicle remains a normal TruckController; fleet ownership/state lives in FleetManager.
    [RequireComponent(typeof(TruckController))]
    public sealed class PlayerTruckFleetBinding : MonoBehaviour
    {
        [SerializeField] private float syncInterval = 0.5f;

        private TruckController controller;
        private FleetManager fleet;
        private FleetTruckData truck;
        private float nextSync;

        public FleetTruckData Truck => truck;

        private void Start()
        {
            fleet = FleetManager.Instance;

            if (fleet == null)
            {
                Debug.LogError("[PlayerTruckFleetBinding] FleetManager is missing; player truck cannot be linked to the fleet.");
                enabled = false;
                return;
            }

            truck = fleet.EnsureActiveTruck();
            if (truck == null)
            {
                Debug.LogError("[PlayerTruckFleetBinding] No available fleet truck can be used by the player.");
                enabled = false;
                return;
            }

            if (TryResolveProductionPrefab(truck))
                return;

            controller = GetComponent<TruckController>();
            controller.ApplyFleetConfiguration(truck);
            nextSync = Time.time + syncInterval;
        }

        private bool TryResolveProductionPrefab(FleetTruckData activeTruck)
        {
            if (activeTruck == null || string.IsNullOrWhiteSpace(activeTruck.productionProfileId))
                return false;

            if (GetComponent<TruckProductionRuntimeInstance>() != null)
                return false;

            if (!TruckProductionRuntimeResolver.TryGetPrefab(activeTruck.productionProfileId, out var prefab))
                return false;

            if (prefab == null || prefab == gameObject)
                return false;

            var instance = Instantiate(prefab, transform.position, transform.rotation, transform.parent);
            instance.name = prefab.name;
            var marker = instance.GetComponent<TruckProductionRuntimeInstance>();
            if (marker == null) marker = instance.AddComponent<TruckProductionRuntimeInstance>();
            marker.Initialize(activeTruck.productionProfileId);

            var old = gameObject;
            old.SetActive(false);
            Destroy(old);
            return true;
        }

        private void Update()
        {
            if (truck == null || controller == null) return;
            if (Time.time < nextSync) return;

            SyncToFleet();
            nextSync = Time.time + syncInterval;
        }

        public bool RefreshFromFleet()
        {
            if (fleet == null || controller == null) return false;

            var active = fleet.EnsureActiveTruck();
            if (active == null) return false;

            truck = active;
            controller.ApplyFleetConfiguration(truck);
            return true;
        }

        public void SyncToFleet()
        {
            if (truck == null || controller == null) return;

            truck.fuel = Mathf.Clamp(controller.GetFuelLitres(), 0f, truck.fuelCapacity);
            truck.condition = Mathf.Clamp(controller.Condition, 0f, 100f);
        }

        private void OnDisable()
        {
            if (controller != null && truck != null)
                SyncToFleet();
        }
    }
}
