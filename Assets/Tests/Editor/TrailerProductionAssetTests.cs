using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UltimateTruckEmpire.TrailerSystem;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Tests
{
    public sealed class TrailerProductionAssetTests
    {
        [TestCase("TRAILER_DRY_VAN_001")]
        [TestCase("TRAILER_FUEL_TANKER_001")]
        public void ProductionDefinitionExists(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(
                $"Assets/TrailerSystem/Data/Trailers/{id}.asset");
            Assert.IsNotNull(definition, $"Missing production definition: {id}");
            Assert.AreEqual(id, definition.id);
            Assert.IsNotNull(definition.prefab, $"{id} has no prefab reference");
        }

        [TestCase("TRAILER_DRY_VAN_001")]
        [TestCase("TRAILER_FUEL_TANKER_001")]
        public void ProductionPrefabExists(string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/TrailerSystem/Prefabs/Imported/{id}.prefab");
            Assert.IsNotNull(prefab, $"Missing production prefab: {id}");
        }

        [Test]
        public void Batch01ValidationReportExists()
        {
            var report = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/TrailerSystem/Production/Reports/Batch01Validation.json");
            Assert.IsNotNull(report);
            StringAssert.Contains("TRAILER_DRY_VAN_001", report.text);
            StringAssert.Contains("TRAILER_FUEL_TANKER_001", report.text);
        }
        [Test]
        public void ProductionCatalogContainsLaunchTrailers()
        {
            var catalog = Resources.Load<TrailerCatalogAsset>("TrailerSystem/Catalog/TrailerCatalog");
            Assert.IsNotNull(catalog, "Missing runtime trailer catalog.");
            Assert.IsNotNull(catalog.trailers);
            Assert.IsTrue(catalog.trailers.Exists(t => t != null && t.id == "TRAILER_DRY_VAN_001"));
            Assert.IsTrue(catalog.trailers.Exists(t => t != null && t.id == "TRAILER_FUEL_TANKER_001"));
        }

        [Test]
        public void TemporaryDryVanFallbackResolvesProductionTrailer()
        {
            var definition = TrailerFleetManager.FindTemporaryJobTrailerDefinition(TrailerType.Box);
            Assert.IsNotNull(definition, "No production trailer available for temporary dry-van freight.");
            Assert.AreEqual(TrailerCategory.DryVan, definition.category);
            Assert.IsNotNull(definition.prefab);
        }

        [Test]
        public void TemporaryTankerFallbackResolvesProductionTrailer()
        {
            var definition = TrailerFleetManager.FindTemporaryJobTrailerDefinition(TrailerType.Tanker);
            Assert.IsNotNull(definition, "No production trailer available for temporary tanker freight.");
            Assert.AreEqual(TrailerCategory.FuelTanker, definition.category);
            Assert.IsNotNull(definition.prefab);
        }

    }
}
