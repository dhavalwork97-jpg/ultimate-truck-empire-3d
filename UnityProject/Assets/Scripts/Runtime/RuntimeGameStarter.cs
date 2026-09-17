using UnityEngine;

namespace UltimateTruckEmpire
{
    public static class RuntimeGameStarter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindFirstObjectByType<PrototypeWorldBuilder>() != null)
                return;

            var root = new GameObject("UltimateTruckEmpireRuntime");
            root.AddComponent<TycoonStateService>();
            root.AddComponent<GameBootstrap>();
            root.AddComponent<PrototypeWorldBuilder>();
            root.AddComponent<ContractSystem>();
            root.AddComponent<MobileInput>();
            root.AddComponent<MobileHUD>();
        }
    }
}
