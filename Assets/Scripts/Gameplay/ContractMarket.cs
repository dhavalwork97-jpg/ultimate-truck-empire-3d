using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Gameplay
{
    [Serializable]
    public sealed class ContractOffer
    {
        public string id; public string cargo; public string cargoId; public string pickup; public string destination;
        public float weightTons; public float reward; public int xp; public float distanceKm; public int difficulty;
        public string routeId; public RouteTier routeTier;
        public TrailerType trailerType = TrailerType.Curtainsider;
        public ContractModifierRules.Modifier modifier = ContractModifierRules.Modifier.Standard;
        public float qualityBonus;
        public float qualityPenalty;
        public string originIndustryId;
        public string destinationIndustryId;
        public string customerName;
        public string supplyChainRouteId;
    }

    public sealed class ContractMarket : MonoBehaviour
    {
        public static ContractMarket Instance { get; private set; }
        public IReadOnlyList<ContractOffer> Offers => offers;
        private readonly List<ContractOffer> offers = new();
        private int seed = 42;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Refresh();
        }

        public void Refresh()
        {
            offers.Clear();
            var rng = new System.Random(seed++);
            int completedContracts = DeliveryManager.Instance?.CompletedContracts ?? 0;
            var supplyRoutes = SupplyChainManager.Instance?.GetUnlockedRoutes(completedContracts);
            var routes = RouteProgression.GetUnlockedRoutes(completedContracts);
            if ((supplyRoutes == null || supplyRoutes.Count == 0) && routes.Count == 0) return;

            for (int i = 0; i < 8; i++)
            {
                if (supplyRoutes != null && supplyRoutes.Count > 0)
                {
                    var supplyRoute = supplyRoutes[rng.Next(supplyRoutes.Count)];
                    var origin = SupplyChainManager.Instance.GetIndustry(supplyRoute.originIndustryId);
                    var destination = SupplyChainManager.Instance.GetIndustry(supplyRoute.destinationIndustryId);
                    var cargo = CargoCatalog.Find(origin != null ? origin.cargoId : null) ?? CargoCatalog.PickFor(i + seed, supplyRoute.baseDifficulty);
                    float distance = Mathf.Lerp(supplyRoute.minDistanceKm, supplyRoute.maxDistanceKm, (float)rng.NextDouble());
                    float maxWeight = Mathf.Min(30f, Mathf.Min(cargo.maxWeightTons, SupplyChainManager.Instance.GetAvailableStock(supplyRoute)));
                    float minWeight = Mathf.Min(Mathf.Max(cargo.minWeightTons, 4f), maxWeight);
                    float weight = maxWeight > 0f ? Mathf.Lerp(minWeight, maxWeight, (float)rng.NextDouble()) : cargo.minWeightTons;
                    int difficulty = Mathf.Clamp(supplyRoute.baseDifficulty + Mathf.CeilToInt(weight / 12f) - 1, 1, 5);
                    var modifier = ContractModifierRules.GetModifier(cargo, difficulty, seed + i);
                    float baseReward = (EconomyConfig.MarketBaseReward + distance * EconomyConfig.MarketRewardPerKm + weight * EconomyConfig.MarketRewardPerTon)
                        * supplyRoute.baseRewardMultiplier * SupplyChainManager.Instance.GetRouteDemandMultiplier(supplyRoute);
                    float bonus = baseReward * ContractModifierRules.GetBonusMultiplier(modifier);
                    float penalty = baseReward * ContractModifierRules.GetPenaltyMultiplier(modifier);
                    offers.Add(new ContractOffer
                    {
                        id = "SC-" + seed + "-" + i, cargo = cargo.displayName, cargoId = cargo.id,
                        pickup = origin != null ? origin.name : supplyRoute.originIndustryId,
                        destination = destination != null ? destination.name : supplyRoute.destinationIndustryId,
                        weightTons = weight, distanceKm = distance, difficulty = difficulty,
                        routeId = supplyRoute.id, routeTier = supplyRoute.tier, trailerType = cargo.trailer,
                        modifier = modifier, qualityBonus = bonus * 0.10f, qualityPenalty = penalty,
                        reward = baseReward, xp = EconomyConfig.MarketBaseXp + difficulty * EconomyConfig.MarketXpPerDifficulty,
                        originIndustryId = supplyRoute.originIndustryId, destinationIndustryId = supplyRoute.destinationIndustryId,
                        customerName = (destination != null ? destination.city : "Regional") + " Logistics",
                        supplyChainRouteId = supplyRoute.id
                    });
                    continue;
                }

                var legacyRoute = routes[rng.Next(routes.Count)];
                float legacyDistance = Mathf.Lerp(legacyRoute.minDistanceKm, legacyRoute.maxDistanceKm, (float)rng.NextDouble());
                var legacyCargo = CargoCatalog.PickFor(i + seed, legacyRoute.baseDifficulty);
                float legacyWeight = Mathf.Lerp(Mathf.Max(legacyRoute.minCargoTons, legacyCargo.minWeightTons),
                    Mathf.Min(legacyRoute.maxCargoTons, legacyCargo.maxWeightTons), (float)rng.NextDouble());
                if (legacyWeight <= 0f) legacyWeight = Mathf.Lerp(legacyRoute.minCargoTons, legacyRoute.maxCargoTons, (float)rng.NextDouble());
                int legacyDifficulty = Mathf.Clamp(legacyRoute.baseDifficulty + Mathf.CeilToInt(legacyWeight / 12f) - 1, 1, 5);
                var legacyModifier = ContractModifierRules.GetModifier(legacyCargo, legacyDifficulty, seed + i);
                float legacyBaseReward = EconomyConfig.MarketBaseReward + legacyDistance * EconomyConfig.MarketRewardPerKm + legacyWeight * EconomyConfig.MarketRewardPerTon;
                float legacyBonus = legacyBaseReward * ContractModifierRules.GetBonusMultiplier(legacyModifier);
                float legacyPenalty = legacyBaseReward * ContractModifierRules.GetPenaltyMultiplier(legacyModifier);

                offers.Add(new ContractOffer
                {
                    id = "MKT-" + seed + "-" + i,
                    cargo = legacyCargo.displayName,
                    cargoId = legacyCargo.id,
                    pickup = legacyRoute.origin,
                    destination = legacyRoute.destination,
                    weightTons = legacyWeight,
                    distanceKm = legacyDistance,
                    difficulty = legacyDifficulty,
                    routeId = legacyRoute.id,
                    routeTier = legacyRoute.tier,
                    trailerType = legacyCargo.trailer,
                    modifier = legacyModifier,
                    qualityBonus = legacyBonus,
                    qualityPenalty = legacyPenalty,
                    reward = legacyBaseReward,
                    xp = EconomyConfig.MarketBaseXp + legacyDifficulty * EconomyConfig.MarketXpPerDifficulty,
                    originIndustryId = "", destinationIndustryId = "", customerName = "Open Market", supplyChainRouteId = ""
                });
            }
        }

        public ContractOffer CreateNextPlayerContract(int progression)
        {
            int level = Mathf.Max(1, progression + 1);
            var cargo = CargoCatalog.Get((level - 1) % CargoCatalog.Count);
            float weight = Mathf.Clamp(10f + level * 1.5f, Mathf.Max(8f, cargo.minWeightTons), Mathf.Min(28f, cargo.maxWeightTons));
            float distance = 140f + level * 35f;
            int difficulty = Mathf.Clamp(1 + (level - 1) / 2, 1, 5);
            var modifier = ContractModifierRules.GetModifier(cargo, difficulty, level);
            float baseReward = EconomyConfig.PlayerBaseReward + distance * EconomyConfig.PlayerRewardPerKm + weight * EconomyConfig.PlayerRewardPerTon + difficulty * EconomyConfig.PlayerRewardPerDifficulty;

            return new ContractOffer
            {
                id = "PLAYER-" + level.ToString("000"),
                cargo = cargo.displayName,
                cargoId = cargo.id,
                pickup = "Ahmedabad Logistics Depot",
                destination = "Vadodara Factory Warehouse",
                weightTons = weight,
                distanceKm = distance,
                difficulty = difficulty,
                routeId = "PLAYER-AHM-VAD",
                routeTier = RouteTier.Local,
                trailerType = cargo.trailer,
                modifier = modifier,
                qualityBonus = baseReward * ContractModifierRules.GetBonusMultiplier(modifier),
                qualityPenalty = baseReward * ContractModifierRules.GetPenaltyMultiplier(modifier),
                reward = baseReward,
                xp = EconomyConfig.PlayerBaseXp + difficulty * EconomyConfig.PlayerXpPerDifficulty
            };
        }

        public bool Remove(ContractOffer offer) => offer != null && offers.Remove(offer);
    }
}
