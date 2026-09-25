using NUnit.Framework;
using UnityEngine;
using UltimateTruckEmpire.Economy;
using UltimateTruckEmpire.Freight;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class LogisticsEconomyTests
    {
        [Test] public void FuelCostUsesLitresAndPrice() => Assert.That(FuelEconomyService.CalculateCost(100f, 95f), Is.EqualTo(9500f));
        [Test] public void FuelCostClampsNegativeInputs() => Assert.That(FuelEconomyService.CalculateCost(-10f, -5f), Is.EqualTo(0f));
        [Test] public void FuelConsumptionScalesWithDistance() => Assert.That(FuelEconomyService.CalculateConsumptionLitres(200f, 20f), Is.EqualTo(40f).Within(0.001f));
        [Test] public void FuelConsumptionScalesWithLoad() => Assert.That(FuelEconomyService.CalculateConsumptionLitres(100f, 20f, 20f), Is.GreaterThan(20f));
        [Test] public void RefillNeverExceedsCapacity() => Assert.That(FuelEconomyService.CalculateRefillLitres(90f, 100f), Is.EqualTo(10f));
        [Test] public void RefillClampsInvalidCapacity() => Assert.That(FuelEconomyService.CalculateRefillLitres(10f, -1f), Is.EqualTo(0f));
        [Test] public void DryVanHasHigherTollThanSolo() => Assert.That(TollEconomyService.GetTrailerMultiplier(LogisticsTrailerClass.DryVan), Is.GreaterThan(1f));
        [Test] public void TankerCostsMoreThanDryVan() => Assert.That(TollEconomyService.GetTrailerMultiplier(LogisticsTrailerClass.Tanker), Is.GreaterThan(TollEconomyService.GetTrailerMultiplier(LogisticsTrailerClass.DryVan)));
        [Test] public void AxleMultiplierIncreasesWithAxles() => Assert.That(TollEconomyService.GetAxleMultiplier(6), Is.GreaterThan(TollEconomyService.GetAxleMultiplier(4)));
        [Test] public void TollFeeIsRoundedAndNonNegative() => Assert.That(TollEconomyService.CalculateFee(220f, LogisticsTrailerClass.Tanker, 6), Is.EqualTo(399f));
        [Test] public void TollRouteFactorHasFloor() => Assert.That(TollEconomyService.CalculateFee(100f, LogisticsTrailerClass.None, 2, 0f), Is.EqualTo(50f));
        [Test] public void RepairCostFallsToZeroAtFullCondition() => Assert.That(GarageEconomyService.CalculateRepairCost(100f, 100000f), Is.EqualTo(0f));
        [Test] public void RepairCostIncreasesWithDamage() => Assert.That(GarageEconomyService.CalculateRepairCost(50f, 100000f), Is.GreaterThan(GarageEconomyService.CalculateRepairCost(75f, 100000f)));
        [Test] public void UpgradeCostGrowsByLevel() => Assert.That(GarageEconomyService.CalculateUpgradeCost(1000f, 2), Is.GreaterThan(GarageEconomyService.CalculateUpgradeCost(1000f, 1)));
        [Test] public void EngineUpgradeMultiplierGrows() => Assert.That(GarageEconomyService.GetUpgradeMultiplier(GarageUpgradeType.Engine, 3), Is.GreaterThan(1f));
        [Test] public void FuelTankUpgradeMultiplierGrows() => Assert.That(GarageEconomyService.GetUpgradeMultiplier(GarageUpgradeType.FuelTank, 3), Is.GreaterThan(GarageEconomyService.GetUpgradeMultiplier(GarageUpgradeType.FuelTank, 1)));
        [Test] public void RouteRejectsSameCity() => Assert.IsFalse(FreightRouteService.IsValidRoute("Ahmedabad", "Ahmedabad", 100f));
        [Test] public void RouteAcceptsDistinctCities() => Assert.IsTrue(FreightRouteService.IsValidRoute("Ahmedabad", "Vadodara", 110f));
        [Test] public void RouteEtaUsesPositiveSpeed() => Assert.That(FreightRouteService.CalculateEtaHours(550f, 55f, 1f), Is.EqualTo(10f).Within(0.001f));
        [Test] public void TrailerCompatibilityMatchesRequiredClass() => Assert.IsTrue(FreightRouteService.TrailerCompatible(LogisticsTrailerClass.Tanker, LogisticsTrailerClass.Tanker));
        [Test] public void PhysicalTankerMapsToTankerFreightClass()
        {
            Assert.IsTrue(FreightRouteService.TryGetLogisticsTrailerClass(UltimateTruckEmpire.Truck.TrailerType.Tanker, out var logisticsClass));
            Assert.That(logisticsClass, Is.EqualTo(LogisticsTrailerClass.Tanker));
        }
        [Test] public void PhysicalHeavyHaulMapsToOversizedFreightClass()
        {
            Assert.IsTrue(FreightRouteService.TryGetLogisticsTrailerClass(UltimateTruckEmpire.Truck.TrailerType.HeavyHaul, out var logisticsClass));
            Assert.That(logisticsClass, Is.EqualTo(LogisticsTrailerClass.Oversized));
        }
        [Test] public void UnsupportedTrailerCannotMapToFreightClass()
        {
            Assert.IsFalse(FreightRouteService.TryGetLogisticsTrailerClass(UltimateTruckEmpire.Truck.TrailerType.GrainHopper, out _));
        }
        [Test] public void TrailerCompatibilityRejectsWrongClass() => Assert.IsFalse(FreightRouteService.TrailerCompatible(LogisticsTrailerClass.Tanker, LogisticsTrailerClass.DryVan));
        [Test] public void CheckpointTrackerDeduplicatesVisits()
        {
            var tracker = new FreightCheckpointTracker();
            Assert.IsTrue(tracker.Visit("A")); Assert.IsFalse(tracker.Visit("A")); Assert.That(tracker.Count, Is.EqualTo(1));
        }
        [Test] public void CheckpointTrackerReportsVisitedState()
        {
            var tracker = new FreightCheckpointTracker();
            tracker.Visit("DELIVERY"); Assert.IsTrue(tracker.HasVisited("DELIVERY")); Assert.IsFalse(tracker.HasVisited("PICKUP"));
        }
        [Test] public void MarketGeneratesConfiguredOffers()
        {
            var go = new GameObject("FreightMarketTest");
            try { var market = go.AddComponent<FreightMarketService>(); market.Refresh("Ahmedabad"); Assert.That(market.Offers.Count, Is.EqualTo(6)); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MarketFirstOfferTargetsExistingVadodaraWarehouse()
        {
            var go = new GameObject("FreightMarketTest");
            try { var market = go.AddComponent<FreightMarketService>(); market.Refresh("Ahmedabad"); Assert.That(market.Offers[0].destinationCity, Is.EqualTo("Vadodara")); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MarketPickupRejectsIncompatiblePhysicalTrailer()
        {
            var go = new GameObject("FreightMarketTest");
            try
            {
                var market = go.AddComponent<FreightMarketService>();
                market.Refresh("Ahmedabad");
                var job = market.Offers[0];
                Assert.IsTrue(market.Accept(job.id));
                Assert.That(job.trailerClass, Is.EqualTo(LogisticsTrailerClass.DryVan));
                Assert.IsFalse(market.TryPickupAt("Ahmedabad", UltimateTruckEmpire.Truck.TrailerType.Tanker));
                Assert.IsFalse(market.ActiveJob.cargoLoaded);
                Assert.IsTrue(market.TryPickupAt("Ahmedabad", UltimateTruckEmpire.Truck.TrailerType.DryVan));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void MarketPickupAndDeliveryRespectWarehouseCities()
        {
            var go = new GameObject("FreightMarketTest");
            try
            {
                var market = go.AddComponent<FreightMarketService>();
                market.Refresh("Ahmedabad");
                var job = market.Offers[0];
                Assert.IsTrue(market.Accept(job.id));
                Assert.IsTrue(market.TryPickupAt("Ahmedabad"));
                Assert.IsFalse(market.TryPickupAt("Vadodara"));
                Assert.IsTrue(market.TryDeliverAt("Vadodara", out float payout));
                Assert.That(payout, Is.GreaterThan(0f));
                Assert.IsNull(market.ActiveJob);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MarketOffersUseDistinctDestinationFromOrigin()
        {
            var go = new GameObject("FreightMarketTest");
            try { var market = go.AddComponent<FreightMarketService>(); market.Refresh("Ahmedabad"); foreach (var job in market.Offers) Assert.That(job.destinationCity, Is.Not.EqualTo(job.originCity)); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MarketAcceptsOnlyKnownOffer()
        {
            var go = new GameObject("FreightMarketTest");
            try { var market = go.AddComponent<FreightMarketService>(); market.Refresh(); Assert.IsFalse(market.Accept("MISSING")); Assert.IsNull(market.ActiveJob); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MarketLifecycleAcceptPickupDelivery()
        {
            var go = new GameObject("FreightMarketTest");
            try { var market = go.AddComponent<FreightMarketService>(); market.Refresh(); var job = market.Offers[0]; Assert.IsTrue(market.Accept(job.id)); Assert.IsTrue(market.MarkPickupComplete()); Assert.IsTrue(market.CompleteDelivery(out float payout)); Assert.That(payout, Is.GreaterThan(0f)); Assert.IsNull(market.ActiveJob); }
            finally { Object.DestroyImmediate(go); }
        }
    }
}