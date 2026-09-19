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
            ContractModifierRules.Modifier modifier)
        {
            float score = 100f;
            if (truck != null)
            {
                score -= Mathf.Clamp((100f - truck.ConditionPercent) * 0.55f, 0f, 30f);
                float expectedFuel = distanceKm / Mathf.Max(0.1f, truck.fuelEfficiency);
                if (fuelUsed > expectedFuel * 1.15f)
                    score -= Mathf.Clamp((fuelUsed / Mathf.Max(1f, expectedFuel) - 1.15f) * 20f, 0f, 12f);
            }

            score = ApplyModifierQuality(score, modifier);
            return Build(score, modifier);
        }

        public static DeliveryEvaluationResult EvaluateAutomated(FleetTruckData truck, float driverPerformance,
            float distanceKm, float fuelUsed, ContractModifierRules.Modifier modifier)
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
            return Build(score, modifier);
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

        private static DeliveryEvaluationResult Build(float score, ContractModifierRules.Modifier modifier)
        {
            string rating = score >= 95f ? "EXCELLENT" :
                score >= 85f ? "GREAT" :
                score >= 70f ? "GOOD" :
                score >= 50f ? "FAIR" : "POOR";

            float adjustment = 0f;
            float bonus = ContractModifierRules.GetBonusMultiplier(modifier);
            float penalty = ContractModifierRules.GetPenaltyMultiplier(modifier);
            if (score >= 85f)
                adjustment = Mathf.Max(0f, bonus) * 1f;
            else if (score < 60f)
                adjustment = -Mathf.Max(0f, penalty) * 1f;

            int bonusXp = score >= 95f ? 35 : score >= 85f ? 20 : score < 60f ? 0 : 5;
            return new DeliveryEvaluationResult(score, rating, adjustment, bonusXp);
        }

        public static float ResolvePayout(float baseReward, DeliveryEvaluationResult result, float qualityBonus, float qualityPenalty)
        {
            if (result.score >= 85f)
                return Mathf.Max(0f, baseReward + qualityBonus);
            if (result.score < 60f)
                return Mathf.Max(0f, baseReward - qualityPenalty);
            return Mathf.Max(0f, baseReward);
        }
    }
}
