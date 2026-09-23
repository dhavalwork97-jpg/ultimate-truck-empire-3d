using UnityEngine;

namespace UltimateTruckEmpire.Navigation
{
    internal static class NavigationRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (NavigationManager.Instance == null)
            {
                var manager = new GameObject("GPS Navigation Manager");
                manager.AddComponent<NavigationManager>();
            }

            if (Object.FindFirstObjectByType<NavigationHUD>() == null)
            {
                var hud = new GameObject("GPS Navigation HUD");
                hud.AddComponent<NavigationHUD>();
            }
        }
    }
}