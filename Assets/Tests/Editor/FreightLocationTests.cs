using System.Collections.Generic;
using NUnit.Framework;
using UltimateTruckEmpire.FreightLocations;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class FreightLocationTests
    {
        [Test]
        public void CatalogHasReusableLocationsAcrossAllFreightCities()
        {
            Assert.That(FreightLocationRegistry.All.Count, Is.EqualTo(20));

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
    }
}
