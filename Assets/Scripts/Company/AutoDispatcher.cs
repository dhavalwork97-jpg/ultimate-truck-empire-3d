using System;
using System.Linq;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Company
{
    public sealed class AutoDispatcher : MonoBehaviour
    {
        public static AutoDispatcher Instance { get; private set; }
        public bool Enabled { get; private set; } = true;
        public int LastDispatches { get; private set; }
        private float nextTick;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!Enabled || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 2f;
            DispatchAvailable();
        }

        public void SetEnabled(bool enabled) => Enabled = enabled;

        public int DispatchAvailable()
        {
            LastDispatches = 0;
            var fleet = FleetManager.Instance;
            var drivers = DriverManager.Instance;
            var market = ContractMarket.Instance;
            var delivery = AutomatedDeliveryManager.Instance;
            if (fleet == null || drivers == null || market == null || delivery == null) return 0;

            foreach (var truck in fleet.Trucks.Where(t => t.available).ToList())
            {
                var driver = drivers.Drivers.Where(d => d.available && d.employed).OrderByDescending(drivers.GetPerformance).FirstOrDefault();
                if (driver == null) continue;
                var offer = market.Offers
                    .Where(o => o.weightTons <= truck.capacityTons)
                    .OrderByDescending(o => Score(o, driver, truck))
                    .FirstOrDefault();
                if (offer == null) continue;
                if (delivery.StartDelivery(truck, driver, offer) != null)
                {
                    market.Remove(offer);
                    LastDispatches++;
                }
            }
            return LastDispatches;
        }

        private static float Score(ContractOffer offer, DriverData driver, FleetTruckData truck)
        {
            float fuel = offer.distanceKm * 0.38f * 105f;
            float wear = offer.distanceKm * 0.012f * 300f;
            float performance = DriverManager.Instance.GetPerformance(driver);
            float skillBonus = offer.reward * Mathf.Clamp((performance - 50f) / 1000f, -0.05f, 0.12f);
            float capacityPenalty = Mathf.Max(0f, offer.weightTons / Mathf.Max(1f, truck.capacityTons) - 0.75f) * offer.reward * 0.1f;
            return offer.reward + skillBonus - fuel - wear - capacityPenalty;
        }
    }
}
