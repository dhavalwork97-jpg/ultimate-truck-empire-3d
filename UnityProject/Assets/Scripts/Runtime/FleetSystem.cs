using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire
{
    [Serializable]
    public sealed class TruckSpec
    {
        public string id;
        public string displayName;
        public int price;
        public float topSpeed;
        public float acceleration;
        public float handling;

        public TruckSpec(string id, string displayName, int price, float topSpeed, float acceleration, float handling)
        {
            this.id = id;
            this.displayName = displayName;
            this.price = price;
            this.topSpeed = topSpeed;
            this.acceleration = acceleration;
            this.handling = handling;
        }
    }

    public sealed class FleetSystem : MonoBehaviour
    {
        public IReadOnlyList<TruckSpec> Trucks => trucks;
        public TruckSpec Selected { get; private set; }

        private readonly List<TruckSpec> trucks = new();
        private TycoonStateService tycoon;
        private TruckController truck;

        private void Awake()
        {
            tycoon = FindFirstObjectByType<TycoonStateService>();
            truck = FindFirstObjectByType<TruckController>();
            BuildCatalog();
            Select(tycoon != null ? tycoon.State.selectedTruckId : "starter_truck");
        }

        public void Select(string id)
        {
            var spec = trucks.Find(x => x.id == id);
            if (spec == null) return;
            Selected = spec;

            if (tycoon != null)
                tycoon.State.selectedTruckId = spec.id;

            ApplyToTruck();
        }

        public bool Purchase(string id)
        {
            var spec = trucks.Find(x => x.id == id);
            if (spec == null || tycoon == null || tycoon.State.cash < spec.price) return false;

            tycoon.State.cash -= spec.price;
            tycoon.State.selectedTruckId = spec.id;
            tycoon.Save();
            Select(spec.id);
            return true;
        }

        public void UpgradeAcceleration()
        {
            if (Selected == null || tycoon == null || tycoon.State.cash < 750) return;
            tycoon.State.cash -= 750;
            Selected.acceleration += 1.5f;
            tycoon.Save();
            ApplyToTruck();
        }

        private void ApplyToTruck()
        {
            if (truck == null || Selected == null) return;
            truck.Configure(Selected.topSpeed, Selected.acceleration, Selected.handling);
        }

        private void BuildCatalog()
        {
            trucks.Clear();
            trucks.Add(new TruckSpec("starter_truck", "Hauler 01", 0, 28f, 9f, 55f));
            trucks.Add(new TruckSpec("longhaul_x", "Longhaul X", 18500, 34f, 11f, 60f));
            trucks.Add(new TruckSpec("atlas_haul", "Atlas Haul", 42000, 40f, 13f, 65f));
            trucks.Add(new TruckSpec("titan_900", "Titan 900", 85000, 46f, 15f, 70f));
        }
    }
}
