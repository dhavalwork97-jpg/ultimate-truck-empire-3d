using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UltimateTruckEmpire.TrailerSystem;

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

    }
}
