using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    public static class CargoDamageSystem
    {
        public static float CalculateDamagePercent(ContractCargoDefinition cargo, float deliveryScore, float dockingScore)
        {
            if (cargo == null) return 0f;
            float damage = Mathf.Clamp01((100f - deliveryScore) / 100f) * 12f;
            damage += Mathf.Clamp01((70f - dockingScore) / 70f) * 8f;
            if (cargo.fragile) damage *= 1.75f;
            if (cargo.highValue) damage *= 1.20f;
            if (cargo.temperatureSensitive) damage *= 1.35f;
            return Mathf.Clamp(damage, 0f, 35f);
        }
        public static float GetPayoutPenalty(float reward, float damagePercent) => Mathf.Max(0f, reward) * Mathf.Clamp01(damagePercent / 35f) * 0.30f;
        public static string GetStatus(float damagePercent)
        {
            if (damagePercent <= 1f) return "Intact";
            if (damagePercent <= 8f) return "Minor damage";
            if (damagePercent <= 18f) return "Damaged";
            return "Heavy damage";
        }
    }
}
