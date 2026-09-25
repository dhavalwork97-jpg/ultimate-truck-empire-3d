using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Save;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.Freight
{
    public sealed class FreightMarketService : MonoBehaviour
    {
        public static FreightMarketService Instance { get; private set; }
        [SerializeField] private int offersPerRefresh = 6;
        [SerializeField] private int maxDistanceKm = 1200;
        private readonly List<FreightJob> offers = new List<FreightJob>();
        private FreightJob activeJob;

        public IReadOnlyList<FreightJob> Offers => offers;
        public FreightJob ActiveJob => activeJob;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Refresh(string currentCity = "Ahmedabad")
        {
            offers.Clear();
            string[] cities = FreightWorldMap.CityNames();
            string[] cargo = { "General Freight", "Steel", "Cement", "Food", "Fuel", "Machinery" };
            for (int i = 0; i < Mathf.Max(1, offersPerRefresh); i++)
            {
                // Keep the first generated offer aligned with the existing playable
                // Ahmedabad depot -> Vadodara warehouse route.
                string destination = i == 0 && string.Equals(currentCity, "Ahmedabad", StringComparison.OrdinalIgnoreCase)
                    ? "Vadodara"
                    : cities[(i * 3 + 2) % cities.Length];
                if (string.Equals(destination, currentCity, StringComparison.OrdinalIgnoreCase))
                    destination = cities[(i * 3 + 3) % cities.Length];
                float distance = Mathf.Clamp(FreightWorldMap.RouteDistanceKm(currentCity, destination), 100f, maxDistanceKm);
                LogisticsTrailerClass trailer = (LogisticsTrailerClass)(i % 5 + 1);
                float weight = 8f + i * 2.5f;
                int difficulty = Mathf.Clamp(1 + i / 2, 1, 5);
                float reward = Mathf.Round(18000f + distance * 95f + weight * 850f + difficulty * 2500f);
                offers.Add(new FreightJob
                {
                    id = "JOB-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + i,
                    cargoId = cargo[i % cargo.Length].Replace(" ", "_").ToUpperInvariant(),
                    originCity = currentCity,
                    destinationCity = destination,
                    distanceKm = distance,
                    weightTons = weight,
                    trailerClass = trailer,
                    reward = reward,
                    xp = 120 + difficulty * 70,
                    difficulty = difficulty,
                    deadlineHours = Mathf.Max(4f, distance / 45f * 1.6f)
                });
            }
        }

        public bool Accept(string jobId)
        {
            if (activeJob != null) return false;
            for (int i = 0; i < offers.Count; i++)
            {
                if (!string.Equals(offers[i].id, jobId, StringComparison.OrdinalIgnoreCase)) continue;
                activeJob = offers[i].Clone();
                activeJob.accepted = true;
                SaveManager.Instance?.Save();
                return true;
            }
            return false;
        }

        public bool MarkPickupComplete()
        {
            if (activeJob == null || !activeJob.accepted || activeJob.cargoLoaded) return false;
            activeJob.cargoLoaded = true;
            SaveManager.Instance?.Save();
            return true;
        }

        public bool TryPickupAt(string city)
        {
            return activeJob != null && activeJob.accepted &&
                   !activeJob.cargoLoaded &&
                   string.Equals(activeJob.originCity, city, StringComparison.OrdinalIgnoreCase) &&
                   MarkPickupComplete();
        }

        public bool TryPickupAt(string city, TrailerType physicalTrailerType)
        {
            if (activeJob == null || !activeJob.accepted || activeJob.cargoLoaded ||
                !string.Equals(activeJob.originCity, city, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!FreightRouteService.TryGetLogisticsTrailerClass(physicalTrailerType, out LogisticsTrailerClass actualClass) ||
                !FreightRouteService.TrailerCompatible(activeJob.trailerClass, actualClass))
                return false;

            return MarkPickupComplete();
        }

        public bool CompleteDelivery(out float payout)
        {
            payout = 0f;
            if (activeJob == null || !activeJob.accepted || !activeJob.cargoLoaded) return false;
            payout = activeJob.reward;
            activeJob = null;
            SaveManager.Instance?.Save();
            return true;
        }

        public bool TryDeliverAt(string city, out float payout)
        {
            payout = 0f;
            if (activeJob == null || !activeJob.accepted || !activeJob.cargoLoaded ||
                !string.Equals(activeJob.destinationCity, city, StringComparison.OrdinalIgnoreCase)) return false;
            return CompleteDelivery(out payout);
        }

        public FreightSaveState CaptureState() => new FreightSaveState { activeJob = activeJob?.Clone() };

        public void RestoreState(FreightSaveState state)
        {
            activeJob = state?.activeJob?.Clone();
        }

        public void ClearActiveJob() => activeJob = null;
    }
}
