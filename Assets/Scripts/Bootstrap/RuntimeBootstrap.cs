using UnityEngine;

namespace UltimateTruckEmpire.Bootstrap
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<WorldBootstrap>() != null) return;
            var go = new GameObject("WorldBootstrap");
            go.AddComponent<WorldBootstrap>();
        }
    }
}
