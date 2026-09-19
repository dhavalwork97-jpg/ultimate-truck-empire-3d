using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class AutomatedDelivery
    {
        public string id; public string truckId; public string driverId; public string cargo; public string origin; public string destination;
        public float reward; public float distanceKm; public float remainingKm; public float elapsedHours; public float etaHours; public bool active; public bool completed;
    }

    public sealed class AutomatedDeliveryManager : MonoBehaviour
    {
        public static AutomatedDeliveryManager Instance { get; private set; }
        public IReadOnlyList<AutomatedDelivery> ActiveDeliveries => deliveries;
        [SerializeField] private float simulationSpeed = 60f;
        private readonly List<AutomatedDelivery> deliveries = new();
        private int nextId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            simulationSpeed = Mathf.Clamp(simulationSpeed, 1f, 600f);
        }

        public float SimulationSpeed => simulationSpeed;
        public void SetSimulationSpeed(float value) => simulationSpeed = Mathf.Clamp(value, 1f, 600f);

        public AutomatedDelivery StartDelivery(FleetTruckData truck, DriverData driver, ContractOffer offer)
        {
            if (truck == null || driver == null || offer == null || FleetManager.Instance == null || DriverManager.Instance == null) return null;
            if (truck.capacityTons < offer.weightTons || !truck.available || !DriverManager.Instance.CanDispatch(driver)) return null;
            float speed = 48f + DriverManager.Instance.GetPerformance(driver) * .45f;
            var delivery = new AutomatedDelivery { id = "JOB-" + nextId++, truckId = truck.id, driverId = driver.id, cargo = offer.cargo, origin = offer.pickup, destination = offer.destination, reward = offer.reward, distanceKm = offer.distanceKm, remainingKm = offer.distanceKm, etaHours = Mathf.Max(.25f, offer.distanceKm / speed), active = true };
            if (!FleetManager.Instance.Assign(truck.id, driver.id, delivery.id)) return null;
            if (!DriverManager.Instance.CanDispatch(driver))
            {
                FleetManager.Instance.Release(truck.id);
                return null;
            }
            DriverManager.Instance.BeginDelivery(driver);
            deliveries.Add(delivery); return delivery;
        }

        private void Update()
        {
            if (deliveries.Count == 0) return;
            float hours = Time.deltaTime / 3600f * simulationSpeed;
            for (int i = deliveries.Count - 1; i >= 0; i--)
            {
                var job = deliveries[i]; if (!job.active) continue;
                job.elapsedHours += hours;
                float travel = Mathf.Max(1f, job.distanceKm / Mathf.Max(.1f, job.etaHours));
                job.remainingKm = Mathf.Max(0f, job.remainingKm - travel * hours);
                if (job.remainingKm <= .01f) { Complete(job); deliveries.RemoveAt(i); }
            }
        }

        private static void Complete(AutomatedDelivery job)
        {
            var driver = DriverManager.Instance?.Find(job.driverId); var truck = FleetManager.Instance?.Find(job.truckId);
            float performance = DriverManager.Instance?.GetEffectivePerformance(driver) ?? 50f;
            float bonus = job.reward * Mathf.Clamp((performance - 50f) / 1000f, 0f, .12f);
            float xp = Mathf.Max(20, Mathf.RoundToInt(job.distanceKm * .6f));
            float fuelBefore = truck?.fuel ?? 0f;

            if (truck != null)
                FleetManager.Instance?.ApplyTripWear(job.truckId, job.distanceKm, 0f);

            float fuelLitres = Mathf.Max(0f, fuelBefore - (truck?.fuel ?? fuelBefore));
            float payment = job.reward + bonus;
            CompanyManager.Instance?.AddRevenue(payment);
            float fuelCost = FleetManager.Instance == null ? 0f : fuelLitres * FleetManager.Instance.GetFuelPricePerLitre();
            FinanceManager.Instance?.RecordDelivery(payment, fuelCost, driver?.salary ?? 0f);

            FleetManager.Instance?.Release(job.truckId);
            DriverManager.Instance?.CompleteDelivery(driver, job.distanceKm, Mathf.RoundToInt(xp));
            MissionManager.Instance?.NotifyDeliveryComplete();
            ContractMarket.Instance?.Refresh();
            job.completed = true; job.active = false;
        }
    }
}
