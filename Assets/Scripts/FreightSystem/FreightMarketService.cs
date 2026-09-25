using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Economy;

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

        public void Refresh(string currentCity = "Ahmedabad")
        {
            offers.Clear();
            string[] cities = { "Ahmedabad", "Vadodara", "Surat", "Mumbai", "Pune", "Jaipur", "Delhi", "Indore", "Rajkot", "Kandla" };
            string[] cargo = { "General Freight", "Steel", "Cement", "Food", "Fuel", "Machinery" };
            for (int i = 0; i < Mathf.Max(1, offersPerRefresh); i++)
            {
                string destination = cities[(i * 3 + 2) % cities.Length];
                if (string.Equals(destination, currentCity, StringComparison.OrdinalIgnoreCase))
                    destination = cities[(i * 3 + 3) % cities.Length];
                float distance = Mathf.Clamp(120f + i * 145f, 100f, maxDistanceKm);
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
                return true;
            }
            return false;
        }

        public bool MarkPickupComplete()
        {
            if (activeJob == null || !activeJob.accepted || activeJob.cargoLoaded) return false;
            activeJob.cargoLoaded = true;
            return true;
        }

        public bool CompleteDelivery(out float payout)
        {
            payout = 0f;
            if (activeJob == null || !activeJob.accepted || !activeJob.cargoLoaded) return false;
            payout = activeJob.reward;
            activeJob = null;
            return true;
        }

        public void ClearActiveJob() => activeJob = null;
    }
}
