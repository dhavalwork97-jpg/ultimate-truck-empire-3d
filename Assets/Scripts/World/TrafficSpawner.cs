using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficSpawner : MonoBehaviour
    {
        [SerializeField, Range(0, 24)] private int vehicleCount = 14;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float speedVariation = 2.5f;
        [SerializeField] private float laneOffset = 1.75f;

        // These routes are derived from the actual generated road corridors.
        // They intentionally terminate and reverse instead of inventing a road
        // loop that does not exist in the procedural world.
        private static readonly Vector3[] EastWestRoute =
        {
            new Vector3(-160f, .55f, 0f),
            new Vector3(-90f, .55f, 0f),
            new Vector3(0f, .55f, 0f),
            new Vector3(80f, .55f, 0f),
            new Vector3(160f, .55f, 0f)
        };

        private static readonly Vector3[] NorthSouthRoute =
        {
            new Vector3(-90f, .55f, -35f),
            new Vector3(-90f, .55f, 0f),
            new Vector3(-90f, .55f, 70f),
            new Vector3(-90f, .55f, 105f)
        };

        private void Start()
        {
            var fleet = new GameObject("Traffic").transform;
            int count = Mathf.Clamp(vehicleCount, 0, 24);

            for (int i = 0; i < count; i++)
            {
                var vehicle = TrafficVisualFactory.Create(i, fleet);
                vehicle.name = "Traffic Vehicle " + (i + 1);

                bool northSouth = i % 3 == 0;
                Vector3[] route = northSouth ? NorthSouthRoute : EastWestRoute;
                int routeStart = (i * 2) % route.Length;
                float speed = Mathf.Max(4f, cruiseSpeed + ((i % 5) - 2) * speedVariation * 0.5f);

                // Alternating directions produce both approaches at each junction.
                bool forward = i % 2 == 0;
                float directionLane = northSouth
                    ? (forward ? laneOffset : -laneOffset)
                    : (forward ? laneOffset : -laneOffset);

                var ai = vehicle.AddComponent<TrafficVehicle>();
                ai.Configure(route, routeStart, speed, directionLane, true, forward);
            }
        }
    }
}
