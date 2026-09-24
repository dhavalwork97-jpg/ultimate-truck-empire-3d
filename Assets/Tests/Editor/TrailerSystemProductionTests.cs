using NUnit.Framework;
using UnityEngine;
using UltimateTruckEmpire.TrailerSystem;

namespace UltimateTruckEmpire.Tests
{
    public sealed class TrailerSystemProductionTests
    {
        private TrailerDefinition dryVan;
        private TrailerDefinition tanker;
        private CargoDefinition coveredCargo;
        private CargoDefinition fuelCargo;
        private CargoDefinition heavyCargo;
        private CargoDefinition bulkCargo;

        [SetUp]
        public void SetUp()
        {
            dryVan = ScriptableObject.CreateInstance<TrailerDefinition>();
            dryVan.id = "TRAILER_DRY_VAN_001";
            dryVan.category = TrailerCategory.DryVan;
            dryVan.payloadCapacityTons = 30f;

            tanker = ScriptableObject.CreateInstance<TrailerDefinition>();
            tanker.id = "TRAILER_FUEL_TANKER_001";
            tanker.category = TrailerCategory.FuelTanker;
            tanker.payloadCapacityTons = 32f;

            coveredCargo = CreateCargo(CargoCategory.Furniture, 5f, 20f);
            coveredCargo.requiresCoveredTrailer = true;
            fuelCargo = CreateCargo(CargoCategory.Fuel, 10f, 25f);
            heavyCargo = CreateCargo(CargoCategory.Machinery, 10f, 50f);
            heavyCargo.requiresHeavyHaul = true;
            bulkCargo = CreateCargo(CargoCategory.Grain, 5f, 25f);
            bulkCargo.requiresBulkTrailer = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(dryVan);
            Object.DestroyImmediate(tanker);
            Object.DestroyImmediate(coveredCargo);
            Object.DestroyImmediate(fuelCargo);
            Object.DestroyImmediate(heavyCargo);
            Object.DestroyImmediate(bulkCargo);
        }

        [Test] public void ProductionTrailerIdsAreStable()
        {
            Assert.AreEqual("TRAILER_DRY_VAN_001", dryVan.id);
            Assert.AreEqual("TRAILER_FUEL_TANKER_001", tanker.id);
        }

        [Test] public void DryVanAcceptsCoveredCargo() =>
            Assert.IsTrue(TrailerCompatibility.CanLoad(dryVan, coveredCargo, 15f));

        [Test] public void FuelTankerAcceptsFuelCargo() =>
            Assert.IsTrue(TrailerCompatibility.CanLoad(tanker, fuelCargo, 20f));

        [Test] public void DryVanRejectsFuelCargo() =>
            Assert.IsFalse(TrailerCompatibility.CanLoad(dryVan, fuelCargo, 15f));

        [Test] public void FuelTankerRejectsCoveredFreight() =>
            Assert.IsFalse(TrailerCompatibility.CanLoad(tanker, coveredCargo, 15f));

        [Test] public void HeavyHaulRequiresDedicatedTrailerFamily() =>
            Assert.IsFalse(TrailerCompatibility.CanLoad(dryVan, heavyCargo, 15f));

        [Test] public void BulkCargoRequiresBulkTrailerFamily() =>
            Assert.IsFalse(TrailerCompatibility.CanLoad(dryVan, bulkCargo, 15f));

        [Test] public void PayloadLimitIsEnforced()
        {
            Assert.IsFalse(TrailerCompatibility.CanLoad(dryVan, coveredCargo, 31f));
            Assert.IsTrue(TrailerCompatibility.CanLoad(dryVan, coveredCargo, 30f));
        }

        [Test] public void AllowedWeightClampsToTrailerAndCargoLimits()
        {
            Assert.AreEqual(20f, TrailerCompatibility.GetAllowedWeight(dryVan, coveredCargo, 29f), 0.001f);
            Assert.AreEqual(5f, TrailerCompatibility.GetAllowedWeight(dryVan, coveredCargo, 2f), 0.001f);
        }

        [Test] public void NegativeWeightIsRejected() =>
            Assert.IsFalse(TrailerCompatibility.CanLoad(dryVan, coveredCargo, -1f));

        [Test] public void ExplicitCompatibilityOverridesInference()
        {
            dryVan.compatibleCargo = new[] { fuelCargo };
            fuelCargo.compatibleTrailers = new[] { dryVan };
            Assert.IsTrue(TrailerCompatibility.CanLoad(dryVan, fuelCargo, 15f));
            Assert.IsFalse(TrailerCompatibility.CanLoad(tanker, fuelCargo, 15f));
        }

        private static CargoDefinition CreateCargo(CargoCategory category, float minWeight, float maxWeight)
        {
            var cargo = ScriptableObject.CreateInstance<CargoDefinition>();
            cargo.category = category;
            cargo.minWeightTons = minWeight;
            cargo.maxWeightTons = maxWeight;
            return cargo;
        }
    }
}
