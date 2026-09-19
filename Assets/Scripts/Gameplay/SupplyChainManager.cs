using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    public enum IndustryType
    {
        Farm,
        TextileMill,
        Factory,
        Warehouse,
        Port,
        Distribution
    }

    [Serializable]
    public sealed class IndustryNode
    {
        public string id;
        public string name;
        public string city;
        public IndustryType type;
        public string cargoId;
        public float stockTons;
        public float maxStockTons;
        public float demandTons;

        public IndustryNode(string id, string name, string city, IndustryType type, string cargoId, float stock, float maxStock, float demand)
        {
            this.id = id; this.name = name; this.city = city; this.type = type; this.cargoId = cargoId;
            stockTons = stock; maxStockTons = maxStock; demandTons = demand;
        }
    }

    [Serializable]
    public sealed class SupplyChainRoute
    {
        public string id;
        public string originIndustryId;
        public string destinationIndustryId;
        public float minDistanceKm;
        public float maxDistanceKm;
        public RouteTier tier;
        public int baseDifficulty;
        public float baseRewardMultiplier;

        public SupplyChainRoute(string id, string origin, string destination, float minDistance, float maxDistance,
            RouteTier tier, int difficulty, float rewardMultiplier = 1f)
        {
            this.id = id; originIndustryId = origin; destinationIndustryId = destination;
            minDistanceKm = minDistance; maxDistanceKm = maxDistance; this.tier = tier;
            baseDifficulty = difficulty; baseRewardMultiplier = rewardMultiplier;
        }
    }

    [Serializable]
    public sealed class SupplyChainSaveState
    {
        public string industryId;
        public float stockTons;
        public float demandTons;
    }

    /// <summary>
    /// Persistent industry network for the living logistics layer.
    /// Industries consume and produce stock, while freight contracts move cargo
    /// between connected nodes. The graph is deterministic and intentionally small
    /// enough for mobile play, but can grow as Gujarat expands.
    /// </summary>
    public sealed class SupplyChainManager : MonoBehaviour
    {
        public static SupplyChainManager Instance { get; private set; }
        public IReadOnlyList<IndustryNode> Industries => industries;
        public IReadOnlyList<SupplyChainRoute> Routes => routes;

        private readonly List<IndustryNode> industries = new();
        private readonly List<SupplyChainRoute> routes = new();
        private float simulationTimer;
        private const float SimulationIntervalSeconds = 30f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildNetwork();
        }

        private void Update()
        {
            simulationTimer += Time.deltaTime;
            if (simulationTimer < SimulationIntervalSeconds) return;
            simulationTimer = 0f;
            SimulateIndustryDemand();
        }

        private void BuildNetwork()
        {
            if (industries.Count > 0) return;

            industries.Add(new IndustryNode("IND-SUR-COTTON", "Surat Cotton Market", "Surat", IndustryType.Farm,
                "agricultural-goods", 180f, 260f, 35f));
            industries.Add(new IndustryNode("IND-SUR-TEXTILE", "Surat Textile Mill", "Surat", IndustryType.TextileMill,
                "furniture", 80f, 220f, 75f));
            industries.Add(new IndustryNode("IND-BHR-CHEM", "Bharuch Industrial Plant", "Bharuch", IndustryType.Factory,
                "chemicals", 120f, 300f, 90f));
            industries.Add(new IndustryNode("IND-AHM-WH", "Ahmedabad Freight Warehouse", "Ahmedabad", IndustryType.Warehouse,
                "electronics", 100f, 300f, 80f));
            industries.Add(new IndustryNode("IND-VAD-DIST", "Vadodara Distribution Hub", "Vadodara", IndustryType.Distribution,
                "machinery", 90f, 280f, 85f));
            industries.Add(new IndustryNode("IND-KANDLA-PORT", "Kandla Container Terminal", "Kandla", IndustryType.Port,
                "steel-coils", 220f, 420f, 120f));

            routes.Add(new SupplyChainRoute("SC-SUR-TEXTILE", "IND-SUR-COTTON", "IND-SUR-TEXTILE", 15f, 55f, RouteTier.Local, 1, 0.95f));
            routes.Add(new SupplyChainRoute("SC-SUR-AHM", "IND-SUR-TEXTILE", "IND-AHM-WH", 260f, 310f, RouteTier.Regional, 2, 1.08f));
            routes.Add(new SupplyChainRoute("SC-AHM-VAD", "IND-AHM-WH", "IND-VAD-DIST", 110f, 150f, RouteTier.Regional, 2, 1.05f));
            routes.Add(new SupplyChainRoute("SC-BHR-VAD", "IND-BHR-CHEM", "IND-VAD-DIST", 75f, 110f, RouteTier.Regional, 2, 1.04f));
            routes.Add(new SupplyChainRoute("SC-KANDLA-AHM", "IND-KANDLA-PORT", "IND-AHM-WH", 330f, 390f, RouteTier.Interstate, 3, 1.16f));
            routes.Add(new SupplyChainRoute("SC-KANDLA-VAD", "IND-KANDLA-PORT", "IND-VAD-DIST", 390f, 460f, RouteTier.Interstate, 3, 1.20f));
            routes.Add(new SupplyChainRoute("SC-AHM-SUR", "IND-AHM-WH", "IND-SUR-TEXTILE", 260f, 310f, RouteTier.Regional, 2, 1.10f));
        }

        public IndustryNode GetIndustry(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var industry in industries)
                if (string.Equals(industry.id, id, StringComparison.OrdinalIgnoreCase)) return industry;
            return null;
        }

        public SupplyChainRoute GetRoute(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var route in routes)
                if (string.Equals(route.id, id, StringComparison.OrdinalIgnoreCase)) return route;
            return null;
        }

        public List<SupplyChainRoute> GetUnlockedRoutes(int completedContracts)
        {
            var result = new List<SupplyChainRoute>();
            foreach (var route in routes)
            {
                bool unlocked = route.tier == RouteTier.Local
                    || (route.tier == RouteTier.Regional && completedContracts >= 2)
                    || (route.tier == RouteTier.Interstate && completedContracts >= 6)
                    || (route.tier == RouteTier.LongHaul && completedContracts >= 12);
                if (unlocked) result.Add(route);
            }
            return result;
        }

        public float GetRouteDemandMultiplier(SupplyChainRoute route)
        {
            var destination = route == null ? null : GetIndustry(route.destinationIndustryId);
            if (destination == null || destination.maxStockTons <= 0f) return 1f;
            float shortage = Mathf.Clamp01(destination.demandTons / Mathf.Max(1f, destination.maxStockTons));
            return Mathf.Lerp(0.92f, 1.25f, shortage);
        }

        public float GetAvailableStock(SupplyChainRoute route)
        {
            var origin = route == null ? null : GetIndustry(route.originIndustryId);
            return origin == null ? 0f : Mathf.Max(0f, origin.stockTons);
        }

        public void RecordShipment(string originId, string destinationId, string cargoId, float tons)
        {
            if (tons <= 0f) return;
            var origin = GetIndustry(originId);
            var destination = GetIndustry(destinationId);
            if (origin != null && string.Equals(origin.cargoId, cargoId, StringComparison.OrdinalIgnoreCase))
                origin.stockTons = Mathf.Max(0f, origin.stockTons - tons);
            if (destination != null)
            {
                destination.stockTons = Mathf.Min(destination.maxStockTons, destination.stockTons + tons);
                destination.demandTons = Mathf.Max(0f, destination.demandTons - tons);
            }
        }

        private void SimulateIndustryDemand()
        {
            foreach (var industry in industries)
            {
                float replenishment = industry.type == IndustryType.Farm || industry.type == IndustryType.Port ? 2.5f : 1.2f;
                industry.stockTons = Mathf.Min(industry.maxStockTons, industry.stockTons + replenishment);
                industry.demandTons = Mathf.Min(industry.maxStockTons, industry.demandTons + 1.5f);
            }
        }

        public SupplyChainSaveState[] CaptureState()
        {
            var result = new SupplyChainSaveState[industries.Count];
            for (int i = 0; i < industries.Count; i++)
                result[i] = new SupplyChainSaveState { industryId = industries[i].id, stockTons = industries[i].stockTons, demandTons = industries[i].demandTons };
            return result;
        }

        public void RestoreState(SupplyChainSaveState[] states)
        {
            BuildNetwork();
            if (states == null) return;
            foreach (var state in states)
            {
                var industry = GetIndustry(state.industryId);
                if (industry == null) continue;
                industry.stockTons = Mathf.Clamp(state.stockTons, 0f, industry.maxStockTons);
                industry.demandTons = Mathf.Clamp(state.demandTons, 0f, industry.maxStockTons);
            }
        }
    }
}
