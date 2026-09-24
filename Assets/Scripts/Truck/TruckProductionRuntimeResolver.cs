using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// Resolves generated production metadata at runtime and exposes the
    /// corresponding prefab without creating a second truck ownership system.
    public static class TruckProductionRuntimeResolver
    {
        private const string ProfileResourcesRoot = "TruckSystem/Data/Production/";

        public static bool TryGetPrefab(string productionProfileId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrWhiteSpace(productionProfileId)) return false;

            var profile = Resources.Load<TruckProductionProfile>(ProfileResourcesRoot + productionProfileId);
            if (profile == null || profile.prefab == null) return false;

            prefab = profile.prefab;
            return true;
        }
    }
}
