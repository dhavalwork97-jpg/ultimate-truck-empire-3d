using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Gameplay
{
    [Serializable]
    public sealed class ContractOffer
    {
        public string id; public string cargo; public string pickup; public string destination;
        public float weightTons; public float reward; public int xp; public float distanceKm; public int difficulty;
    }

    public sealed class ContractMarket : MonoBehaviour
    {
        public static ContractMarket Instance { get; private set; }
        public IReadOnlyList<ContractOffer> Offers => offers;
        private readonly List<ContractOffer> offers = new();
        private int seed = 42;
        private static readonly string[] Cargo = { "Electronics", "Refrigerated Food", "Steel Coils", "Furniture", "Machinery", "Agricultural Goods" };
        private static readonly string[] Cities = { "Ahmedabad", "Vadodara", "Surat", "Rajkot", "Gandhinagar", "Udaipur" };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Refresh();
        }

        public void Refresh()
        {
            offers.Clear();
            var rng = new System.Random(seed++);
            for (int i = 0; i < 8; i++)
            {
                var from = Cities[rng.Next(Cities.Length)]; var to = Cities[rng.Next(Cities.Length)];
                if (to == from) to = Cities[(Array.IndexOf(Cities, from) + 1) % Cities.Length];
                var distance = 80f + rng.Next(40, 650); var weight = 4f + (float)rng.NextDouble() * 24f;
                var difficulty = Mathf.Clamp(Mathf.CeilToInt(weight / 8f), 1, 5);
                offers.Add(new ContractOffer { id = "MKT-" + seed + "-" + i, cargo = Cargo[rng.Next(Cargo.Length)], pickup = from, destination = to, weightTons = weight, distanceKm = distance, difficulty = difficulty, reward = EconomyConfig.MarketBaseReward + distance * EconomyConfig.MarketRewardPerKm + weight * EconomyConfig.MarketRewardPerTon, xp = EconomyConfig.MarketBaseXp + difficulty * EconomyConfig.MarketXpPerDifficulty });
            }
        }

        public ContractOffer CreateNextPlayerContract(int progression)
        {
            int level = Mathf.Max(1, progression + 1);
            float weight = Mathf.Clamp(10f + level * 1.5f, 8f, 28f);
            float distance = 140f + level * 35f;
            int difficulty = Mathf.Clamp(1 + (level - 1) / 2, 1, 5);
            return new ContractOffer {
                id = "PLAYER-" + level.ToString("000"), cargo = Cargo[(level - 1) % Cargo.Length],
                pickup = "Ahmedabad Logistics Depot", destination = "Vadodara Factory Warehouse",
                weightTons = weight, distanceKm = distance, difficulty = difficulty,
                reward = EconomyConfig.PlayerBaseReward + distance * EconomyConfig.PlayerRewardPerKm + weight * EconomyConfig.PlayerRewardPerTon + difficulty * EconomyConfig.PlayerRewardPerDifficulty,
                xp = EconomyConfig.PlayerBaseXp + difficulty * EconomyConfig.PlayerXpPerDifficulty
            };
        }

        public bool Remove(ContractOffer offer) => offer != null && offers.Remove(offer);
    }
}
