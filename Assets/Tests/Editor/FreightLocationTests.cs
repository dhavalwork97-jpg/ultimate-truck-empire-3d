using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UltimateTruckEmpire.FreightLocations;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class FreightLocationTests
    {
        [Test]
        public void CatalogHasReusableLocationsAcrossAllFreightCities()
        {
            Assert.That(FreightLocationRegistry.All.Count, Is.EqualTo(30));

            var cities = new HashSet<string>();
            foreach (var location in FreightLocationRegistry.All)
            {
                Assert.That(location.id, Is.Not.Null.And.Not.Empty);
                Assert.That(location.city, Is.Not.Null.And.Not.Empty);
                Assert.That(location.prefabResourcePath, Does.StartWith("FreightLocations/Prefabs/"));
                Assert.That(location.loadingDockCount, Is.GreaterThan(0));
                cities.Add(location.city);
            }

            Assert.That(cities.Count, Is.EqualTo(FreightWorldMap.Cities.Count));
        }

        [Test]
        public void CatalogUsesOnlySupportedFreightWorldCities()
        {
            foreach (var location in FreightLocationRegistry.All)
                Assert.That(FreightWorldMap.Find(location.city), Is.Not.Null, location.id);
        }

        [Test]
        public void CatalogIdsAreUnique()
        {
            var ids = new HashSet<string>();
            foreach (var location in FreightLocationRegistry.All)
                Assert.That(ids.Add(location.id), Is.True, $"Duplicate location id: {location.id}");
        }

        [Test]
        public void CityLookupReturnsAtLeastFactoryOrWarehouse()
        {
            foreach (var city in FreightWorldMap.Cities)
            {
                var locations = FreightLocationRegistry.ForCity(city.name);
                Assert.That(locations.Count, Is.GreaterThan(0), city.name);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void ProductionPrefabsExistForEveryCatalogLocation()
        {
            var missing = new List<string>();
            foreach (var location in FreightLocationRegistry.All)
            {
                string prefabPath = "Assets/FreightLocations/Prefabs/" +
                    location.prefabResourcePath.Substring("FreightLocations/Prefabs/".Length) + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                    missing.Add(location.id + " -> " + prefabPath);
            }

            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }


#if UNITY_EDITOR
        [Test]
        public void PrefabContractsContainAnchorsAndThreeLodLevels()
        {
            string[] prefabNames =
            {
                "Warehouse_Base", "Warehouse_Large", "Warehouse_Port",
                "Factory_Base", "Factory_Textile", "Factory_Chemical", "Factory_Automotive"
            };
            string[] anchors =
            {
                "MeshRoot", "LoadingDock_A", "LoadingDock_B", "TrailerSpawn",
                "CargoSpawn", "DeliveryTrigger", "ParkingSlot_01", "ParkingSlot_02",
                "CompanySign", "EnvironmentCollision"
            };

            foreach (var prefabName in prefabNames)
            {
                string folder = prefabName.StartsWith("Warehouse") ? "Warehouse" : "Factory";
                string path = $"Assets/FreightLocations/Prefabs/{folder}/{prefabName}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Assert.That(root, Is.Not.Null, path);
                    foreach (var anchor in anchors)
                        Assert.That(FindChild(root.transform, anchor), Is.Not.Null, $"{prefabName} missing {anchor}");

                    var lod = root.GetComponent<LODGroup>();
                    Assert.That(lod, Is.Not.Null, $"{prefabName} missing LODGroup");
                    Assert.That(lod.lodCount, Is.EqualTo(3), $"{prefabName} must have 3 LOD levels");

                    var delivery = FindChild(root.transform, "DeliveryTrigger");
                    Assert.That(delivery.gameObject.isStatic, Is.False, $"{prefabName} DeliveryTrigger must be non-static");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        [Test]
        public void ResourcePrefabCatalogContainsEveryRegisteredLocation()
        {
            var catalog = Resources.Load<FreightLocationPrefabCatalog>("FreightLocations/FreightLocationPrefabCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (var location in FreightLocationRegistry.All)
                Assert.That(catalog.Find(location.id), Is.Not.Null, location.id);
        }

        [Test]
        public void ExampleCompanyDefinitionsExist()
        {
            string folder = "Assets/FreightLocations/Data/Companies";
            string[] assets = AssetDatabase.FindAssets("t:FreightLocationCompanyDefinition", new[] { folder });
            Assert.That(assets.Length, Is.GreaterThanOrEqualTo(6));
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
#endif
    }
}