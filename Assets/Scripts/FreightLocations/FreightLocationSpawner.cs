using UnityEngine;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.FreightLocations
{
    /// <summary>
    /// Lightweight bridge from a location definition to a production prefab.
    /// It owns only visual placement and anchor discovery; freight jobs remain
    /// owned by the existing FreightMarketService/DeliveryTrigger systems.
    /// </summary>
    public sealed class FreightLocationSpawner : MonoBehaviour
    {
        private const string CatalogResourcePath = "FreightLocations/FreightLocationPrefabCatalog";

        [SerializeField] private string locationId;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool addDeliveryTrigger = false;

        public string LocationId => locationId;
        public FreightLocationDefinition Definition => FreightLocationRegistry.Find(locationId);

        public void Configure(string id)
        {
            locationId = id;
        }

        public bool TryLoadVisual()
        {
            var definition = Definition;
            if (definition == null)
            {
                Debug.LogWarning($"Unknown freight location id '{locationId}'.", this);
                return false;
            }

            if (visualRoot != null && visualRoot.childCount > 0)
                return true;

            GameObject prefab = ResolvePrefab(definition);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Freight location '{definition.id}' has no production prefab reference. " +
                    "Check FreightLocationPrefabCatalog and the location registry.",
                    this);
                return false;
            }

            Transform root = visualRoot != null ? visualRoot : transform;
            GameObject instance = Instantiate(prefab, root);
            instance.name = definition.displayName;

            if (addDeliveryTrigger)
            {
                var trigger = instance.GetComponentInChildren<DeliveryTrigger>();
                if (trigger == null)
                {
                    var zone = new GameObject("DeliveryTrigger");
                    zone.transform.SetParent(instance.transform, false);
                    var collider = zone.AddComponent<BoxCollider>();
                    collider.isTrigger = true;
                    collider.size = new Vector3(12f, 4f, 12f);
                    zone.AddComponent<DeliveryTrigger>().Configure(
                        DeliveryTrigger.TriggerType.Destination,
                        definition.city);
                }
            }

            return true;
        }

        private static GameObject ResolvePrefab(FreightLocationDefinition definition)
        {
            var catalog = Resources.Load<FreightLocationPrefabCatalog>(CatalogResourcePath);
            return catalog != null ? catalog.Find(definition.id) : null;
        }
    }
}