using UltimateTruckEmpire.Truck;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay.RestArea
{
    public sealed class RestAreaZone : MonoBehaviour
    {
        private static readonly List<RestAreaZone> All = new();

        [SerializeField] private string areaId = "rest-ahmedabad";
        [SerializeField] private string displayName = "Gujarat Highway Rest Area";
        [SerializeField] private bool open = true;
        [SerializeField] private int bayCount = 4;
        [SerializeField] private float defaultRestHours = 8f;
        [SerializeField] private float minRestHours = 1f;
        [SerializeField] private float maxRestHours = 12f;
        [SerializeField] private float costPerHour = 35f;
        [SerializeField] private float maxParkingSpeedKph = 5f;
        [SerializeField] private Transform[] parkingBays;

        private readonly HashSet<int> occupiedBays = new();

        public string AreaId => areaId;
        public string DisplayName => displayName;
        public bool IsOpen => open;
        public bool CanUse => open && AvailableBays > 0;
        public int AvailableBays => Mathf.Max(0, EffectiveBayCount - occupiedBays.Count);
        public float DefaultRestHours => defaultRestHours;
        public float MinRestHours => minRestHours;
        public float MaxRestHours => maxRestHours;
        public float MaxParkingSpeedKph => maxParkingSpeedKph;
        private int EffectiveBayCount => parkingBays != null && parkingBays.Length > 0 ? parkingBays.Length : Mathf.Max(1, bayCount);

        private void Awake() { if (!All.Contains(this)) All.Add(this); }
        private void OnDestroy() { All.Remove(this); }

        public float GetCost(float hours) => Mathf.Max(0f, hours) * Mathf.Max(0f, costPerHour);

        public bool TryReserveBay(out int index)
        {
            index = -1;
            if (!CanUse) return false;
            for (int i = 0; i < EffectiveBayCount; i++)
                if (!occupiedBays.Contains(i)) { occupiedBays.Add(i); index = i; return true; }
            return false;
        }

        public bool TryReserveSpecificBay(int index)
        {
            if (!CanUse || index < 0 || index >= EffectiveBayCount || occupiedBays.Contains(index)) return false;
            occupiedBays.Add(index);
            return true;
        }

        public void ReleaseBay(int index) { if (index >= 0) occupiedBays.Remove(index); }

        public Transform GetBay(int index)
        {
            if (parkingBays == null || index < 0 || index >= parkingBays.Length) return transform;
            return parkingBays[index];
        }

        public void SetOpen(bool value) { open = value; }

        public void Configure(string id, string name, Transform[] bays, float restHours = 8f, float ratePerHour = 35f)
        {
            areaId = string.IsNullOrWhiteSpace(id) ? areaId : id;
            displayName = string.IsNullOrWhiteSpace(name) ? displayName : name;
            parkingBays = bays;
            bayCount = bays != null && bays.Length > 0 ? bays.Length : bayCount;
            defaultRestHours = Mathf.Clamp(restHours, minRestHours, maxRestHours);
            costPerHour = Mathf.Max(0f, ratePerHour);
        }

        public static RestAreaZone Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return All.Find(a => a != null && a.areaId == id);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<TruckController>() == null) return;
            RestAreaManager.Instance?.SetNearbyArea(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<TruckController>() == null) return;
            RestAreaManager.Instance?.ClearNearbyArea(this);
        }
    }
}
