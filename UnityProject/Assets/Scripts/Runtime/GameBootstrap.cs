using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TruckController truck;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (truck == null)
                truck = FindFirstObjectByType<TruckController>();
        }
    }
}
