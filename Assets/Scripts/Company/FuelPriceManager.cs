using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FuelPriceRegionState
    {
        public string region;
        public float pricePerLitre;
    }

    public sealed class FuelPriceManager : MonoBehaviour
    {
        public static FuelPriceManager Instance { get; private set; }

        public const float BasePricePerLitre = 95f;
        public const float MinPricePerLitre = 82f;
        public const float MaxPricePerLitre = 118f;

        private readonly Dictionary<string, float> prices = new();
        private long currentBucket = long.MinValue;

        public IReadOnlyDictionary<string, float> Prices => prices;

        private static readonly string[] Regions =
        {
            "Ahmedabad", "Vadodara", "Bharuch", "Surat", "Kandla"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePrices();
        }

        private void Update()
        {
            long bucket = GetCurrentBucket();
            if (bucket != currentBucket)
                RefreshPrices(bucket);
        }

        private void EnsurePrices()
        {
            foreach (string region in Regions)
                if (!prices.ContainsKey(region))
                    prices[region] = BasePricePerLitre;
            if (currentBucket == long.MinValue)
                RefreshPrices(GetCurrentBucket());
        }

        private void RefreshPrices(long bucket)
        {
            currentBucket = bucket;
            foreach (string region in Regions)
            {
                int hash = StableHash(region);
                double wave = Math.Sin((bucket + hash) * 0.73);
                double local = Math.Cos((bucket * 0.37) + hash * 0.11);
                float regionalBias = GetRegionalBias(region);
                float price = BasePricePerLitre + regionalBias + (float)(wave * 7.0 + local * 3.0);
                prices[region] = Mathf.Clamp(price, MinPricePerLitre, MaxPricePerLitre);
            }
        }

        private static long GetCurrentBucket()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (6L * 60L * 60L);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return Mathf.Abs(hash);
            }
        }

        private static float GetRegionalBias(string region)
        {
            switch (region)
            {
                case "Kandla": return -3f;
                case "Bharuch": return -1.5f;
                case "Vadodara": return 0f;
                case "Ahmedabad": return 1f;
                case "Surat": return 2.5f;
                default: return 0f;
            }
        }

        public float GetPricePerLitre(string region)
        {
            EnsurePrices();
            string key = ResolveRegion(region);
            if (!prices.TryGetValue(key, out float price))
            {
                price = BasePricePerLitre;
                prices[key] = price;
            }
            return price;
        }

        private static string ResolveRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region)) return "Ahmedabad";
            string value = region.Trim();
            foreach (string knownRegion in Regions)
                if (value.IndexOf(knownRegion, StringComparison.OrdinalIgnoreCase) >= 0)
                    return knownRegion;
            return value;
        }

        public string GetPriceLabel(string region)
            => "₹" + GetPricePerLitre(region).ToString("0.00") + " / L";

        public FuelPriceRegionState[] CaptureState()
        {
            EnsurePrices();
            var state = new FuelPriceRegionState[prices.Count];
            int i = 0;
            foreach (var pair in prices)
                state[i++] = new FuelPriceRegionState { region = pair.Key, pricePerLitre = pair.Value };
            return state;
        }

        public void RestoreState(FuelPriceRegionState[] saved)
        {
            prices.Clear();
            if (saved != null)
            {
                foreach (var entry in saved)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.region)) continue;
                    prices[entry.region] = Mathf.Clamp(entry.pricePerLitre, MinPricePerLitre, MaxPricePerLitre);
                }
            }
            EnsurePrices();
            currentBucket = GetCurrentBucket();
        }
    }
}
