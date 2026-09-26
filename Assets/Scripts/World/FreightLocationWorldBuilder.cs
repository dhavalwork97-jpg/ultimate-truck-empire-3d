using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.FreightLocations;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Places the reusable Freight Location Pack 01 prefabs beside each authored
    /// freight city. Existing FreightWorldBuilder zones remain authoritative for
    /// job pickup/delivery logic; these instances are visual location sites.
    /// </summary>
    public static class FreightLocationWorldBuilder
    {
        private static readonly Vector3[] Offsets =
        {
            new Vector3(-28f, 0f, 24f),
            new Vector3(28f, 0f, 24f),
            new Vector3(0f, 0f, -30f)
        };

        private const string RootName = "Freight Locations Pack 01";
        private const string CatalogResourcePath = "FreightLocations/FreightLocationPrefabCatalog";

        public static void BuildLocations(Transform parent = null)
        {
            if (GameObject.Find(RootName) != null) return;

            var root = new GameObject(RootName);
            if (parent != null) root.transform.SetParent(parent, false);

            var catalog = Resources.Load<FreightLocationPrefabCatalog>(CatalogResourcePath);
            if (catalog == null)
            {
                Debug.LogWarning("Freight Location Pack 01 catalog is missing from Resources.");
                return;
            }

            var cityCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var location in FreightLocationRegistry.All)
            {
                var city = FreightWorldMap.Find(location.city);
                if (city == null) continue;

                int index = cityCounters.TryGetValue(location.city, out var count) ? count : 0;
                cityCounters[location.city] = index + 1;

                var prefab = catalog.Find(location.id);
                if (prefab == null)
                {
                    Debug.LogWarning($"Freight location '{location.id}' has no catalog prefab.");
                    continue;
                }

                var instance = UnityEngine.Object.Instantiate(prefab, root.transform);
                instance.name = location.id + " - " + location.displayName;
                instance.transform.position = city.worldPosition + Offsets[index % Offsets.Length];
                instance.transform.rotation = Quaternion.Euler(0f, (index % 2 == 0) ? 0f : 180f, 0f);
                EnsureRuntimeDeliveryTrigger(instance, location.city);
            }
        }

        private static void EnsureRuntimeDeliveryTrigger(GameObject instance, string city)
        {
            var anchor = FindChild(instance.transform, "DeliveryTrigger");
            if (anchor == null) return;

            var zone = new GameObject("FreightLocationDeliveryZone");
            zone.transform.SetParent(instance.transform, false);
            zone.transform.position = anchor.position;
            zone.transform.rotation = anchor.rotation;

            var collider = zone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(14f, 4f, 14f);
            zone.AddComponent<DeliveryTrigger>().Configure(DeliveryTrigger.TriggerType.Destination, city);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
