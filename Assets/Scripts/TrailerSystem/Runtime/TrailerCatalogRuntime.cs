using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    /// <summary>
    /// Small runtime cache for the single Resources-backed trailer catalog.
    /// Keeps gameplay systems from repeatedly resolving the same asset.
    /// </summary>
    public static class TrailerCatalogRuntime
    {
        private const string ResourcePath = "TrailerSystem/Catalog/TrailerCatalog";
        private static TrailerCatalogAsset cachedCatalog;

        public static TrailerCatalogAsset Catalog
        {
            get
            {
                if (cachedCatalog == null)
                    cachedCatalog = Resources.Load<TrailerCatalogAsset>(ResourcePath);
                return cachedCatalog;
            }
        }

        public static void ClearCache()
        {
            cachedCatalog = null;
        }

        public static TrailerDefinition FindTrailer(string id) => Catalog?.FindTrailer(id);
        public static CargoDefinition FindCargo(string id) => Catalog?.FindCargo(id);
    }
}