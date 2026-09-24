using System;
using System.Collections.Generic;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class TruckDefinition
    {
        public string id;
        public string displayName;
        public string productionProfileId;
        public float purchasePrice;
        public float capacityTons;
        public float fuelCapacity;
        public float enginePower;
        public float maxSpeedKph;
        public float fuelEfficiency;
        public float reliability;
        public float maintenanceCostPerKm;
    }

    public static class TruckCatalog
    {
        private static readonly TruckDefinition[] Definitions =
        {
            new TruckDefinition
            {
                id = "ute-starter",
                displayName = "UTE Hauler 300",
                purchasePrice = 275000f,
                capacityTons = 18f,
                fuelCapacity = 350f,
                enginePower = 300f,
                maxSpeedKph = 90f,
                fuelEfficiency = 3.2f,
                reliability = 72f,
                maintenanceCostPerKm = 5.5f
            },
            new TruckDefinition
            {
                id = "ute-heavy-500",
                displayName = "UTE Heavy 500",
                purchasePrice = 575000f,
                capacityTons = 30f,
                fuelCapacity = 500f,
                enginePower = 500f,
                maxSpeedKph = 95f,
                fuelEfficiency = 2.8f,
                reliability = 78f,
                maintenanceCostPerKm = 7.5f
            },
            new TruckDefinition
            {
                id = "ute-longhaul-600",
                displayName = "UTE Long Haul 600",
                purchasePrice = 925000f,
                capacityTons = 42f,
                fuelCapacity = 650f,
                enginePower = 600f,
                maxSpeedKph = 100f,
                fuelEfficiency = 2.6f,
                reliability = 84f,
                maintenanceCostPerKm = 9f
            },
            new TruckDefinition
            {
                id = "nordic-titan-500",
                displayName = "Nordic Titan 500",
                productionProfileId = "meshy-ai-volvo-fh-globetrotter-0923130558-texture",
                purchasePrice = 450000f,
                capacityTons = 40f,
                fuelCapacity = 750f,
                enginePower = 500f,
                maxSpeedKph = 120f,
                fuelEfficiency = 6.5f,
                reliability = 82f,
                maintenanceCostPerKm = 7.5f
            },
            new TruckDefinition
            {
                id = "golden-hauler",
                displayName = "Golden Hauler",
                productionProfileId = "meshy-ai-golden-hauler-0923132145-texture",
                purchasePrice = 625000f,
                capacityTons = 48f,
                fuelCapacity = 900f,
                enginePower = 600f,
                maxSpeedKph = 115f,
                fuelEfficiency = 5.8f,
                reliability = 80f,
                maintenanceCostPerKm = 8.5f
            }
        };

        public static IReadOnlyList<TruckDefinition> All => Definitions;

        public static TruckDefinition Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var definition in Definitions)
                if (string.Equals(definition.id, id, StringComparison.OrdinalIgnoreCase))
                    return definition;
            return null;
        }

        public static TruckDefinition FindByName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return null;
            foreach (var definition in Definitions)
                if (string.Equals(definition.displayName, displayName, StringComparison.OrdinalIgnoreCase))
                    return definition;
            return null;
        }
    }
}
