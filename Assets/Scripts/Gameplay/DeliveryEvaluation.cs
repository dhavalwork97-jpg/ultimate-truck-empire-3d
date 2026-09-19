using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Gameplay
{
    public readonly struct DeliveryEvaluationResult
    {
        public readonly float score;
        public readonly string rating;
        public readonly float payoutAdjustment;
        public readonly int bonusXp;

        public DeliveryEvaluationResult(float score, string rating, float payoutAdjustment, int bonusXp)
        {
            this.score = score;
            this.rating = rating;
            this.payoutAdjustment = payoutAdjustment;
            this.bonusXp = bonusXp;
        }
    }

    public static class DeliveryEvaluation
    {
        public static DeliveryEvaluationResult EvaluatePlayer(FleetTruckData truck, float distanceKm, float fuelUsed,
            ContractModifierRules.Modifier modifier, float baseReward, float qualityBonus, float qualityPenalty)
        {
            return EvaluatePlayer(truck, distanceKm, fuelUsed, 100f, modifier, baseReward, qualityBonus, qualityPenalty);
        }

        public static DeliveryEvaluationResult EvaluatePlayer(FleetTruckData truck, float distanceKm, float fuelUsed,
            float dockingScore, ContractModifierRules.Modifier modifier, float baseReward, float qualityBonus, float qualityPenalty)
        {
            float score = Mathf.Lerp(55f, 100f, Mathf.Clamp01(dockingScore / 100f));
            if (truck != null)
            {
                score -= Mathf.Clamp((100f - truck.ConditionPercent) * 0.35f, 0f, 20f);
                float expectedFuel = distanceKm / Mathf.Max(0.1f, truck.fuelEfficiency);
                if (fuelUsed > expectedFuel * 1.15f)
                    score -= Mathf.Clamp((fuelUsed / Mathf.Max(1f, expectedFuel) - 1.15f) * 20f, 0f, 12f);
            }

            score = ApplyModifierQuality(score, modifier);
            return Build(score, modifier, baseReward, qualityBonus, qualityPenalty);
        }

        public static DeliveryEvaluationResult EvaluateAutomated(FleetTruckData truck, float driverPerformance,
            float distanceKm, float fuelUsed, ContractModifierRules.Modifier modifier, float baseReward,
            float qualityBonus, float qualityPenalty)
        {
            float score = Mathf.Clamp(driverPerformance, 0f, 100f);
            if (truck != null)
            {
                score = (score * 0.65f) + (truck.ConditionPercent * 0.35f);
                float expectedFuel = distanceKm / Mathf.Max(0.1f, truck.fuelEfficiency);
                if (fuelUsed > expectedFuel * 1.2f)
                    score -= Mathf.Clamp((fuelUsed / Mathf.Max(1f, expectedFuel) - 1.2f) * 15f, 0f, 10f);
            }

            score = ApplyModifierQuality(score, modifier);
            return Build(score, modifier, baseReward, qualityBonus, qualityPenalty);
        }

        private static float ApplyModifierQuality(float score, ContractModifierRules.Modifier modifier)
        {
            switch (modifier)
            {
                case ContractModifierRules.Modifier.Fragile:
                    if (score < 82f) score -= 8f;
                    break;
                case ContractModifierRules.Modifier.Express:
                    if (score < 78f) score -= 10f;
                    break;
                case ContractModifierRules.Modifier.HighValue:
                    if (score < 80f) score -= 9f;
                    break;
                case ContractModifierRules.Modifier.TemperatureControlled:
                    if (score < 80f) score -= 11f;
                    break;
            }
            return Mathf.Clamp(score, 0f, 100f);
        }

        private static DeliveryEvaluationResult Build(float score, ContractModifierRules.Modifier modifier,
            float baseReward, float qualityBonus, float qualityPenalty)
        {
            string rating = score >= 95f ? "EXCELLENT" :
                score >= 85f ? "GREAT" :
                score >= 70f ? "GOOD" :
                score >= 50f ? "FAIR" : "POOR";

            float adjustment = score >= 85f
                ? Mathf.Max(0f, qualityBonus)
                : score < 60f ? -Mathf.Max(0f, qualityPenalty) : 0f;

            int bonusXp = score >= 95f ? 35 : score >= 85f ? 20 : score < 60f ? 0 : 5;
            return new DeliveryEvaluationResult(Mathf.Clamp(score, 0f, 100f), rating, adjustment, bonusXp);
        }
    }
}
