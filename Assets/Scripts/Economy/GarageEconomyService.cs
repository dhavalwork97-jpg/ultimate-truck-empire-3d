using UnityEngine;

namespace UltimateTruckEmpire.Economy
{
    public enum GarageUpgradeType { Engine, Gearbox, Suspension, FuelTank, Tires }

    public static class GarageEconomyService
    {
        public static float CalculateRepairCost(float conditionPercent, float vehicleValue, float repairMultiplier = 0.004f)
        {
            float missing = Mathf.Clamp(100f - conditionPercent, 0f, 100f);
            return Mathf.Round(Mathf.Max(0f, vehicleValue) * missing * Mathf.Max(0f, repairMultiplier));
        }

        public static float CalculateUpgradeCost(float baseCost, int currentLevel, float growth = 1.35f)
        {
            int level = Mathf.Max(0, currentLevel);
            return Mathf.Round(Mathf.Max(0f, baseCost) * Mathf.Pow(Mathf.Max(1f, growth), level));
        }

        public static float GetUpgradeMultiplier(GarageUpgradeType type, int level)
        {
            float step = Mathf.Clamp(Mathf.Max(0, level), 0, 10);
            switch (type)
            {
                case GarageUpgradeType.Engine: return 1f + step * 0.05f;
                case GarageUpgradeType.Gearbox: return 1f + step * 0.035f;
                case GarageUpgradeType.Suspension: return 1f + step * 0.04f;
                case GarageUpgradeType.FuelTank: return 1f + step * 0.08f;
                case GarageUpgradeType.Tires: return 1f + step * 0.03f;
                default: return 1f;
            }
        }
    }
}
