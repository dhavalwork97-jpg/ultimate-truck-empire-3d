using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Freight
{
    public static class FreightRouteService
    {
        public static bool IsValidRoute(string originCity, string destinationCity, float distanceKm)
            => !string.IsNullOrWhiteSpace(originCity) &&
               !string.IsNullOrWhiteSpace(destinationCity) &&
               !string.Equals(originCity.Trim(), destinationCity.Trim(), System.StringComparison.OrdinalIgnoreCase) &&
               distanceKm > 0f;

        public static float CalculateEtaHours(float distanceKm, float averageSpeedKph = 55f, float serviceFactor = 1.15f)
        {
            float speed = Mathf.Max(1f, averageSpeedKph);
            return Mathf.Max(0.1f, Mathf.Max(0f, distanceKm) / speed * Mathf.Max(1f, serviceFactor));
        }

        public static bool TrailerCompatible(LogisticsTrailerClass required, LogisticsTrailerClass actual)
            => required == LogisticsTrailerClass.None || required == actual;
    }

    public sealed class FreightCheckpointTracker
    {
        private readonly HashSet<string> visited = new HashSet<string>();
        public int Count => visited.Count;
        public bool Visit(string checkpointId)
            => !string.IsNullOrWhiteSpace(checkpointId) && visited.Add(checkpointId);
        public bool HasVisited(string checkpointId) => !string.IsNullOrWhiteSpace(checkpointId) && visited.Contains(checkpointId);
        public void Clear() => visited.Clear();
    }
}
