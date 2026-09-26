using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.FreightLocations
{
    [Serializable]
    public sealed class FreightLocationPrefabEntry
    {
        public string id;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "Ultimate Truck Empire/Freight Location/Prefab Catalog", fileName = "FreightLocationPrefabCatalog")]
    public sealed class FreightLocationPrefabCatalog : ScriptableObject
    {
        [SerializeField] private List<FreightLocationPrefabEntry> entries = new List<FreightLocationPrefabEntry>();

        public GameObject Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].id, id, StringComparison.OrdinalIgnoreCase))
                    return entries[i].prefab;
            return null;
        }
    }
}