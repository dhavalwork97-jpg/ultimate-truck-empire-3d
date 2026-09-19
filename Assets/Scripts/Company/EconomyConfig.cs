using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    /// <summary>
    /// Single source of truth for economy tuning. Keep gameplay costs and
    /// revenue formulas here so the player, AI and UI use the same numbers.
    /// </summary>
    public static class EconomyConfig
    {
        public const float FuelPricePerLitre = 95f;
        public const float MinimumRepairCostPerConditionPoint = 75f;
        public const float RepairMaintenanceMultiplier = 12f;

        public const float EngineUpgradeBaseCost = 85000f;
        public const float FuelTankUpgradeBaseCost = 60000f;
        public const float ReliabilityUpgradeBaseCost = 70000f;

        public const float AutomatedPayrollMinimumDays = 0.25f;

        public const float MarketBaseReward = 18000f;
        public const float MarketRewardPerKm = 95f;
        public const float MarketRewardPerTon = 850f;
        public const int MarketBaseXp = 80;
        public const int MarketXpPerDifficulty = 55;

        public const float PlayerBaseReward = 42000f;
        public const float PlayerRewardPerKm = 110f;
        public const float PlayerRewardPerTon = 900f;
        public const float PlayerRewardPerDifficulty = 2500f;
        public const int PlayerBaseXp = 120;
        public const int PlayerXpPerDifficulty = 70;

        public static float GetRepairCostPerConditionPoint(float maintenanceCostPerKm)
            => Mathf.Max(MinimumRepairCostPerConditionPoint, Mathf.Max(0f, maintenanceCostPerKm) * RepairMaintenanceMultiplier);

        public static float GetUpgradeCost(float baseCost, int currentLevel)
            => Mathf.Max(0f, baseCost) * (Mathf.Max(0, currentLevel) + 1);

        public static float GetAutomatedPayroll(float dailySalary, float etaHours)
        {
            float salary = Mathf.Max(0f, dailySalary);
            float days = Mathf.Max(AutomatedPayrollMinimumDays, Mathf.Max(0f, etaHours) / 24f);
            return salary * days;
        }
    }
}
