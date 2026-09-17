using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    [Serializable]
    public sealed class ContractOffer
    {
        public string cargo;
        public string pickup;
        public string destination;
        public float weightTons;
        public float reward;
        public int xp;
        public float distanceKm;
        public int difficulty;
    }

    public sealed class ContractMarket : MonoBehaviour
    {
        public static ContractMarket Instance { get; private set; }
        public IReadOnlyList<ContractOffer> Offers => offers;
        private readonly List<ContractOffer> offers = new();

        private static readonly string[] Cargo = { "Electronics", "Refrigerated Food", "Steel Coils", "Furniture", "Machinery", "Agricultural Goods" };
        private static readonly string[] Cities = { "Ahmedabad", "Vadodara", "Surat", "Rajkot", "Gandhinagar", "Udaipur" };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Refresh();
        }

        public void Refresh()
        {
            offers.Clear();
            var rng = new System.Random(42);
            for (int i = 0; i < 6; i++)
            {
                var from = Cities[rng.Next(Cities.Length)];
                var to = Cities[rng.Next(Cities.Length)];
                if (to == from) to = Cities[(Array.IndexOf(Cities, from) + 1) % Cities.Length];
                var distance = 80f + rng.Next(40, 650);
                var weight = 4f + (float)rng.NextDouble() * 24f;
                var difficulty = Mathf.Clamp(Mathf.CeilToInt(weight / 8f), 1, 5);
                offers.Add(new ContractOffer { cargo = Cargo[rng.Next(Cargo.Length)], pickup = from, destination = to, weightTons = weight, distanceKm = distance, difficulty = difficulty, reward = 18000f + distance * 95f + weight * 850f, xp = 80 + difficulty * 55 });
            }
        }
    }
}
