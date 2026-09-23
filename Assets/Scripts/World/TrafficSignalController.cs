using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Lightweight runtime signal controller. Signal timing is derived from shared
    /// RoadNetwork metadata, so no per-junction Update loop or scene light dependency is required.
    /// </summary>
    public sealed class TrafficSignalController : MonoBehaviour
    {
        [SerializeField] private float refreshInterval = 0.25f;
        private float timer;

        public static TrafficSignalController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Mathf.Max(0.1f, refreshInterval);
        }

        public static RoadNetwork.SignalState GetState(RoadNetwork.Junction junction, Vector3 position)
        {
            return junction == null
                ? RoadNetwork.SignalState.Green
                : RoadNetwork.GetSignalState(junction, RoadNetwork.GetApproachDirection(junction, position), Time.time);
        }
    }
}