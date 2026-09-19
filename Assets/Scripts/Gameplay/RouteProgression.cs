using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    public enum RouteTier
    {
        Local = 1,
        Regional = 2,
        Interstate = 3,
        LongHaul = 4
    }

    [Serializable]
    public sealed class RouteDefinition
    {
        public string id;
        public string origin;
        public string destination;
        public RouteTier tier;
        public int requiredContracts;
        public float minDistanceKm;
        public float maxDistanceKm;
        public float minCargoTons;
        public float maxCargoTons;
        public int baseDifficulty;
    }

    /// <summary>
    /// Progression is driven by completed deliveries, not by a second save system.
    /// It controls which freight-market corridors can appear while leaving the
    /// existing driving, fleet and economy systems untouched.
    /// </summary>
    public static class RouteProgression
    {
        private static readonly RouteDefinition[] Routes =
        {
            new RouteDefinition { id = "GJ-AHM-GND", origin = "Ahmedabad", destination = "Gandhinagar", tier = RouteTier.Local, requiredContracts = 0, minDistanceKm = 70f, maxDistanceKm = 120f, minCargoTons = 4f, maxCargoTons = 14f, baseDifficulty = 1 },
            new RouteDefinition { id = "GJ-GND-AHM", origin = "Gandhinagar", destination = "Ahmedabad", tier = RouteTier.Local, requiredContracts = 0, minDistanceKm = 70f, maxDistanceKm = 120f, minCargoTons = 4f, maxCargoTons = 14f, baseDifficulty = 1 },
            new RouteDefinition { id = "GJ-AHM-VAD", origin = "Ahmedabad", destination = "Vadodara", tier = RouteTier.Local, requiredContracts = 0, minDistanceKm = 140f, maxDistanceKm = 220f, minCargoTons = 6f, maxCargoTons = 18f, baseDifficulty = 1 },
            new RouteDefinition { id = "GJ-VAD-AHM", origin = "Vadodara", destination = "Ahmedabad", tier = RouteTier.Local, requiredContracts = 0, minDistanceKm = 140f, maxDistanceKm = 220f, minCargoTons = 6f, maxCargoTons = 18f, baseDifficulty = 1 },

            new RouteDefinition { id = "GJ-AHM-SUR", origin = "Ahmedabad", destination = "Surat", tier = RouteTier.Regional, requiredContracts = 2, minDistanceKm = 260f, maxDistanceKm = 360f, minCargoTons = 8f, maxCargoTons = 22f, baseDifficulty = 2 },
            new RouteDefinition { id = "GJ-VAD-SUR", origin = "Vadodara", destination = "Surat", tier = RouteTier.Regional, requiredContracts = 2, minDistanceKm = 220f, maxDistanceKm = 320f, minCargoTons = 8f, maxCargoTons = 22f, baseDifficulty = 2 },
            new RouteDefinition { id = "GJ-AHM-RAJ", origin = "Ahmedabad", destination = "Rajkot", tier = RouteTier.Regional, requiredContracts = 2, minDistanceKm = 210f, maxDistanceKm = 330f, minCargoTons = 8f, maxCargoTons = 24f, baseDifficulty = 2 },

            new RouteDefinition { id = "GJ-SUR-RAJ", origin = "Surat", destination = "Rajkot", tier = RouteTier.Interstate, requiredContracts = 5, minDistanceKm = 350f, maxDistanceKm = 470f, minCargoTons = 10f, maxCargoTons = 28f, baseDifficulty = 3 },
            new RouteDefinition { id = "GJ-AHM-UDR", origin = "Ahmedabad", destination = "Udaipur", tier = RouteTier.Interstate, requiredContracts = 5, minDistanceKm = 300f, maxDistanceKm = 430f, minCargoTons = 10f, maxCargoTons = 28f, baseDifficulty = 3 },

            new RouteDefinition { id = "GJ-RAJ-UDR", origin = "Rajkot", destination = "Udaipur", tier = RouteTier.LongHaul, requiredContracts = 8, minDistanceKm = 500f, maxDistanceKm = 650f, minCargoTons = 14f, maxCargoTons = 42f, baseDifficulty = 4 },
            new RouteDefinition { id = "GJ-SUR-UDR", origin = "Surat", destination = "Udaipur", tier = RouteTier.LongHaul, requiredContracts = 8, minDistanceKm = 520f, maxDistanceKm = 680f, minCargoTons = 14f, maxCargoTons = 42f, baseDifficulty = 4 }
        };

        public static RouteTier CurrentTier(int completedContracts)
        {
            if (completedContracts >= 8) return RouteTier.LongHaul;
            if (completedContracts >= 5) return RouteTier.Interstate;
            if (completedContracts >= 2) return RouteTier.Regional;
            return RouteTier.Local;
        }

        public static IReadOnlyList<RouteDefinition> GetUnlockedRoutes(int completedContracts)
        {
            var result = new List<RouteDefinition>();
            int progress = Mathf.Max(0, completedContracts);
            foreach (var route in Routes)
                if (route.requiredContracts <= progress)
                    result.Add(route);
            return result;
        }

        public static bool IsUnlocked(string routeId, int completedContracts)
        {
            foreach (var route in Routes)
                if (string.Equals(route.id, routeId, StringComparison.OrdinalIgnoreCase))
                    return route.requiredContracts <= Mathf.Max(0, completedContracts);
            return false;
        }

        public static RouteDefinition Find(string routeId)
        {
            foreach (var route in Routes)
                if (string.Equals(route.id, routeId, StringComparison.OrdinalIgnoreCase))
                    return route;
            return null;
        }

        public static string GetTierLabel(RouteTier tier) => tier switch
        {
            RouteTier.Local => "LOCAL",
            RouteTier.Regional => "REGIONAL",
            RouteTier.Interstate => "INTERSTATE",
            RouteTier.LongHaul => "LONG HAUL",
            _ => "LOCAL"
        };
    }
}
