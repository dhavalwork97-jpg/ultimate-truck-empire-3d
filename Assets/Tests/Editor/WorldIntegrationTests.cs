using NUnit.Framework;
using UnityEngine;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class WorldIntegrationTests
    {
        [SetUp]
        public void SetUp()
        {
            UltimateTruckEmpire.World.RoadNetwork.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            UltimateTruckEmpire.World.RoadNetwork.Reset();

            var overlays = Object.FindObjectsByType<UltimateTruckEmpire.World.Batch02GameplayRoadOverlay>(
                FindObjectsSortMode.None);
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] != null)
                    Object.DestroyImmediate(overlays[i].gameObject);
            }
        }

        [Test]
        public void Batch02OverlayBuildsCollisionOnlyRoads()
        {
            var go = new GameObject("Batch02 Overlay Test");
            try
            {
                var overlay = go.AddComponent<UltimateTruckEmpire.World.Batch02GameplayRoadOverlay>();
                overlay.Build();

                var root = go.transform.Find("Batch 02 Gameplay Roads");
                Assert.That(root, Is.Not.Null);

                var colliders = root.GetComponentsInChildren<MeshCollider>();
                Assert.That(colliders.Length, Is.EqualTo(2));

                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers.Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Batch02OverlayReusesAuthoritativeRoadCorridors()
        {
            UltimateTruckEmpire.World.WorldVisualBuilder.Build();
            int before = UltimateTruckEmpire.World.RoadNetwork.Corridors.Count;

            var go = new GameObject("Batch02 Overlay Test");
            try
            {
                var overlay = go.AddComponent<UltimateTruckEmpire.World.Batch02GameplayRoadOverlay>();
                overlay.Build();

                Assert.That(
                    UltimateTruckEmpire.World.RoadNetwork.Corridors.Count,
                    Is.EqualTo(before),
                    "Batch 02 must not duplicate corridors already owned by the authoritative world road graph.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Batch02OverlayDoesNotOverlapFreightCityTriggerFootprints()
        {
            var overlayGo = new GameObject("Batch02 Overlay Test");
            var freightRoot = new GameObject("Freight Zones Test");

            try
            {
                var overlay = overlayGo.AddComponent<UltimateTruckEmpire.World.Batch02GameplayRoadOverlay>();
                overlay.Build();
                UltimateTruckEmpire.World.FreightWorldBuilder.BuildZones(freightRoot.transform);

                var roadColliders = overlayGo.GetComponentsInChildren<MeshCollider>();
                var deliveryTriggers = freightRoot.GetComponentsInChildren<UltimateTruckEmpire.Gameplay.DeliveryTrigger>();

                Assert.That(roadColliders.Length, Is.GreaterThan(0));
                Assert.That(deliveryTriggers.Length, Is.EqualTo(UltimateTruckEmpire.World.FreightWorldMap.Cities.Count * 2));

                for (int i = 0; i < deliveryTriggers.Length; i++)
                {
                    var trigger = deliveryTriggers[i].GetComponent<BoxCollider>();
                    Assert.That(trigger, Is.Not.Null);

                    for (int j = 0; j < roadColliders.Length; j++)
                    {
                        bool overlaps = Physics.ComputePenetration(
                            roadColliders[j],
                            roadColliders[j].transform.position,
                            roadColliders[j].transform.rotation,
                            trigger,
                            trigger.transform.position,
                            trigger.transform.rotation,
                            out _,
                            out _);

                        Assert.IsFalse(
                            overlaps,
                            trigger.transform.parent.name + " overlaps " + roadColliders[j].gameObject.name);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(overlayGo);
                Object.DestroyImmediate(freightRoot);
            }
        }
    }
}
