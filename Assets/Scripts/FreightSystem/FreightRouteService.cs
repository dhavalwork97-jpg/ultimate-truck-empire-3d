using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Truck;

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

        public static bool TryGetLogisticsTrailerClass(TrailerType physicalType, out LogisticsTrailerClass logisticsClass)
        {
            switch (physicalType)
            {
                case TrailerType.DryVan:
                case TrailerType.Container:
                    logisticsClass = LogisticsTrailerClass.DryVan;
                    return true;
                case TrailerType.Tanker:
                case TrailerType.CementTanker:
                    logisticsClass = LogisticsTrailerClass.Tanker;
                    return true;
                case TrailerType.Refrigerated:
                    logisticsClass = LogisticsTrailerClass.Refrigerated;
                    return true;
                case TrailerType.Flatbed:
                case TrailerType.HeavyFlatbed:
                    logisticsClass = LogisticsTrailerClass.Flatbed;
                    return true;
                case TrailerType.HeavyHaul:
                    logisticsClass = LogisticsTrailerClass.Oversized;
                    return true;
                default:
                    logisticsClass = LogisticsTrailerClass.None;
                    return false;
            }
        }
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
