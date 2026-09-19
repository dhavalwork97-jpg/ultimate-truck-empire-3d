using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Save;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class AutomatedDelivery
    {
        public string id; public string truckId; public string driverId; public string cargo; public string cargoId; public string origin; public string destination;
        public TrailerType trailerType = TrailerType.Curtainsider;
        public ContractModifierRules.Modifier modifier = ContractModifierRules.Modifier.Standard;
        public float qualityBonus; public float qualityPenalty;
        public float reward; public float distanceKm; public float cargoWeightTons; public float remainingKm; public float elapsedHours; public float etaHours; public bool active; public bool completed;
    }

    public sealed class AutomatedDeliveryManager : MonoBehaviour
    {
        public static AutomatedDeliveryManager Instance { get; private set; }
        public IReadOnlyList<AutomatedDelivery> ActiveDeliveries => deliveries;
        [SerializeField] private float simulationSpeed = 60f;
        [SerializeField] private float saveIntervalSeconds = 10f;
        private float nextSaveTime;
        private readonly List<AutomatedDelivery> deliveries = new();
        private int nextId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            simulationSpeed = Mathf.Clamp(simulationSpeed, 1f, 600f);
            saveIntervalSeconds = Mathf.Max(2f, saveIntervalSeconds);
        }

        public float SimulationSpeed => simulationSpeed;

        public AutomatedDelivery[] CaptureState()
        {
            return deliveries.Count == 0 ? Array.Empty<AutomatedDelivery>() : deliveries.ToArray();
        }

        public void RestoreState(AutomatedDelivery[] savedDeliveries)
        {
            deliveries.Clear();
            nextId = 1;
            if (savedDeliveries == null) return;

            foreach (var saved in savedDeliveries)
            {
                if (saved == null || !saved.active || saved.completed) continue;
                var truck = FleetManager.Instance?.Find(saved.truckId);
                var driver = DriverManager.Instance?.Find(saved.driverId);
                if (truck == null || driver == null)
                {
                    Debug.LogWarning($"[AutomatedDeliveryManager] Skipping saved job {saved.id}: truck or driver is missing.");
                    continue;
                }

                // Save/Load already restored the fleet/driver assignment fields.
                // Re-register the job without running StartDelivery, which would
                // create a new ID and mutate driver availability.
                truck.assignedContractId = saved.id;
                truck.assignedDriverId = saved.driverId;
                truck.available = false;
                driver.assignedTruckId = saved.truckId;
                driver.assignedContractId = saved.id;
                driver.available = false;
                driver.resting = false;

                deliveries.Add(saved);
                UpdateNextId(saved.id);
            }

            nextSaveTime = Time.time + saveIntervalSeconds;
        }

        private void UpdateNextId(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId)) return;
            const string prefix = "JOB-";
            if (!jobId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;
            if (int.TryParse(jobId.Substring(prefix.Length), out var number))
                nextId = Mathf.Max(nextId, number + 1);
        }

        private void RequestSave()
        {
            SaveManager.Instance?.Save();
        }
        public void SetSimulationSpeed(float value) => simulationSpeed = Mathf.Clamp(value, 1f, 600f);

        public AutomatedDelivery StartDelivery(FleetTruckData truck, DriverData driver, ContractOffer offer)
        {
            if (truck == null || driver == null || offer == null || FleetManager.Instance == null || DriverManager.Instance == null) return null;
            if (truck.capacityTons < offer.weightTons || !truck.available || !DriverManager.Instance.CanDispatch(driver)) return null;
            float speed = 48f + DriverManager.Instance.GetPerformance(driver) * .45f;
            var delivery = new AutomatedDelivery { id = "JOB-" + nextId++, truckId = truck.id, driverId = driver.id, cargo = offer.cargo, cargoId = offer.cargoId, origin = offer.pickup, destination = offer.destination, trailerType = offer.trailerType, modifier = offer.modifier, qualityBonus = offer.qualityBonus, qualityPenalty = offer.qualityPenalty, reward = offer.reward, distanceKm = offer.distanceKm, cargoWeightTons = offer.weightTons, remainingKm = offer.distanceKm, etaHours = Mathf.Max(.25f, offer.distanceKm / speed), active = true };
            if (!FleetManager.Instance.Assign(truck.id, driver.id, delivery.id)) return null;
            if (!DriverManager.Instance.CanDispatch(driver))
            {
                FleetManager.Instance.Release(truck.id);
                return null;
            }
            DriverManager.Instance.BeginDelivery(driver);
            deliveries.Add(delivery);
            RequestSave();
            nextSaveTime = Time.time + saveIntervalSeconds;
            return delivery;
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
                if (job.remainingKm <= .01f)
                {
                    Complete(job);
                    deliveries.RemoveAt(i);
                    RequestSave();
                }
            }

            if (Time.time >= nextSaveTime)
            {
                RequestSave();
                nextSaveTime = Time.time + saveIntervalSeconds;
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
                FleetManager.Instance?.ApplyTripWear(job.truckId, job.distanceKm, job.cargoWeightTons);

            float fuelLitres = Mathf.Max(0f, fuelBefore - (truck?.fuel ?? fuelBefore));
            var evaluation = DeliveryEvaluation.EvaluateAutomated(truck, performance, job.distanceKm, fuelLitres, job.modifier, job.reward, job.qualityBonus, job.qualityPenalty);
            float payment = Mathf.Max(0f, job.reward + bonus + evaluation.payoutAdjustment);
            CompanyManager.Instance?.AddRevenue(payment);
            float fuelCost = FleetManager.Instance == null ? 0f : fuelLitres * FleetManager.Instance.GetFuelPricePerLitre();
            float payrollCost = EconomyConfig.GetAutomatedPayroll(driver?.salary ?? 0f, job.etaHours);
            FinanceManager.Instance?.RecordDelivery(payment, fuelCost, payrollCost);

            FleetManager.Instance?.Release(job.truckId);
            DriverManager.Instance?.CompleteDelivery(driver, job.distanceKm, Mathf.RoundToInt(xp));
            MissionManager.Instance?.NotifyDeliveryComplete();
            ContractMarket.Instance?.Refresh();
            job.completed = true; job.active = false;
        }
    }
}
