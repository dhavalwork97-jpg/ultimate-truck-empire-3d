using UnityEngine;

namespace UltimateTruckEmpire.Economy
{
    public static class FuelEconomyService
    {
        public static float CalculateCost(float litres, float pricePerLitre)
            => Mathf.Max(0f, litres) * Mathf.Max(0f, pricePerLitre);

        public static float CalculateConsumptionLitres(float distanceKm, float litresPer100Km, float loadTons = 0f, float loadFactorPerTon = 0.015f)
        {
            float distance = Mathf.Max(0f, distanceKm);
            float baseRate = Mathf.Max(0f, litresPer100Km);
            float loadMultiplier = 1f + Mathf.Max(0f, loadTons) * Mathf.Max(0f, loadFactorPerTon);
            return distance * baseRate * loadMultiplier / 100f;
        }

        public static float CalculateRefillLitres(float currentLitres, float capacityLitres)
            => Mathf.Clamp(Mathf.Max(0f, capacityLitres) - Mathf.Max(0f, currentLitres), 0f, Mathf.Max(0f, capacityLitres));

        public static bool TryPurchase(float litres, float pricePerLitre, ref float currentLitres, float capacityLitres, float availableMoney, out float cost)
        {
            cost = CalculateCost(litres, pricePerLitre);
            float refill = Mathf.Clamp(litres, 0f, CalculateRefillLitres(currentLitres, capacityLitres));
            cost = CalculateCost(refill, pricePerLitre);
            if (refill <= 0f || cost > Mathf.Max(0f, availableMoney)) return false;
            currentLitres += refill;
            return true;
        }
    }
}
