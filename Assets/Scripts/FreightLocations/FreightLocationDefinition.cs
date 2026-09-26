using System;
using UnityEngine;

namespace UltimateTruckEmpire.FreightLocations
{
    public enum FreightLocationKind
    {
        Warehouse,
        Factory
    }

    [Serializable]
    public sealed class FreightLocationDefinition
    {
        public string id;
        public string city;
        public string displayName;
        public FreightLocationKind kind;
        public string prefabResourcePath;
        public string[] cargoIds;
        public int loadingDockCount;

        public FreightLocationDefinition(
            string id,
            string city,
            string displayName,
            FreightLocationKind kind,
            string prefabResourcePath,
            int loadingDockCount,
            params string[] cargoIds)
        {
            this.id = id;
            this.city = city;
            this.displayName = displayName;
            this.kind = kind;
            this.prefabResourcePath = prefabResourcePath;
            this.loadingDockCount = Mathf.Max(1, loadingDockCount);
            this.cargoIds = cargoIds ?? Array.Empty<string>();
        }
    }
}
