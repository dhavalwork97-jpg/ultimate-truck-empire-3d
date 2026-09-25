using UnityEngine;

namespace UltimateTruckEmpire.Economy
{
    public enum LogisticsTrailerClass { None, DryVan, Tanker, Refrigerated, Flatbed, Oversized }

    public static class TollEconomyService
    {
        public static float GetTrailerMultiplier(LogisticsTrailerClass trailerClass)
        {
            switch (trailerClass)
            {
                case LogisticsTrailerClass.Tanker: return 1.25f;
                case LogisticsTrailerClass.Refrigerated: return 1.20f;
                case LogisticsTrailerClass.Flatbed: return 1.15f;
                case LogisticsTrailerClass.Oversized: return 1.45f;
                case LogisticsTrailerClass.DryVan: return 1.10f;
                default: return 1f;
            }
        }

        public static float GetAxleMultiplier(int axleCount)
        {
            if (axleCount >= 6) return 1.45f;
            if (axleCount >= 5) return 1.30f;
            if (axleCount >= 4) return 1.15f;
            return 1f;
        }

        public static float CalculateFee(float baseRate, LogisticsTrailerClass trailerClass, int axleCount, float routeFactor = 1f)
        {
            float fee = Mathf.Max(0f, baseRate) *
                        GetTrailerMultiplier(trailerClass) *
                        GetAxleMultiplier(axleCount) *
                        Mathf.Max(0.5f, routeFactor);
            return Mathf.Round(Mathf.Max(0f, fee));
        }
    }
}
