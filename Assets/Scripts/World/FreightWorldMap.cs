using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    [Serializable]
    public sealed class FreightCityDefinition
    {
        public string name;
        public Vector3 worldPosition;
        public float routeKmFromAhmedabad;
        public bool hasPickup;
        public bool hasDelivery;

        public FreightCityDefinition(string name, Vector3 worldPosition, float routeKmFromAhmedabad,
            bool hasPickup = true, bool hasDelivery = true)
        {
            this.name = name;
            this.worldPosition = worldPosition;
            this.routeKmFromAhmedabad = routeKmFromAhmedabad;
            this.hasPickup = hasPickup;
            this.hasDelivery = hasDelivery;
        }
    }

    public static class FreightWorldMap
    {
        private static readonly FreightCityDefinition[] definitions =
        {
            new FreightCityDefinition("Ahmedabad", new Vector3(-55f, 0f, 0f), 0f),
            new FreightCityDefinition("Vadodara", new Vector3(55f, 0f, 0f), 120f),
            new FreightCityDefinition("Surat", new Vector3(95f, 0f, -88f), 265f),
            new FreightCityDefinition("Mumbai", new Vector3(190f, 0f, -130f), 525f),
            new FreightCityDefinition("Pune", new Vector3(250f, 0f, -190f), 660f),
            new FreightCityDefinition("Rajkot", new Vector3(-110f, 0f, -88f), 215f),
            new FreightCityDefinition("Kandla", new Vector3(-205f, 0f, -30f), 305f),
            new FreightCityDefinition("Jaipur", new Vector3(-205f, 0f, 150f), 650f),
            new FreightCityDefinition("Delhi", new Vector3(-60f, 0f, 250f), 950f),
            new FreightCityDefinition("Indore", new Vector3(80f, 0f, 150f), 405f)
        };

        public static IReadOnlyList<FreightCityDefinition> Cities => definitions;

        public static FreightCityDefinition Find(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return null;
            for (int i = 0; i < definitions.Length; i++)
                if (string.Equals(definitions[i].name, city, StringComparison.OrdinalIgnoreCase))
                    return definitions[i];
            return null;
        }

        public static float RouteDistanceKm(string origin, string destination)
        {
            if (string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase)) return 0f;

            // Ahmedabad is the launch hub, so preserve the authored route distances
            // for that hub and use map geometry for other city-to-city contracts.
            var a = Find(origin);
            var b = Find(destination);
            if (a == null || b == null) return 0f;
            if (string.Equals(origin, "Ahmedabad", StringComparison.OrdinalIgnoreCase))
                return b.routeKmFromAhmedabad;
            if (string.Equals(destination, "Ahmedabad", StringComparison.OrdinalIgnoreCase))
                return a.routeKmFromAhmedabad;

            float worldDistance = Vector2.Distance(
                new Vector2(a.worldPosition.x, a.worldPosition.z),
                new Vector2(b.worldPosition.x, b.worldPosition.z));
            return Mathf.Max(80f, Mathf.Round(worldDistance * 2.25f / 5f) * 5f);
        }

        public static string[] CityNames()
        {
            var result = new string[definitions.Length];
            for (int i = 0; i < definitions.Length; i++) result[i] = definitions[i].name;
            return result;
        }
    }
}
