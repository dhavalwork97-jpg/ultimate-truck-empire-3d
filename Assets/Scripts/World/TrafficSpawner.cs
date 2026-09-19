using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficSpawner : MonoBehaviour
    {
        [SerializeField, Range(0, 24)] private int vehicleCount = 14;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float speedVariation = 2.5f;
        [SerializeField] private float laneOffset = 1.8f;

        private static readonly Vector3[] Route =
        {
            new Vector3(-165, .55f, 3), new Vector3(-90, .55f, 3), new Vector3(0, .55f, 3), new Vector3(90, .55f, 3), new Vector3(165, .55f, 3),
            new Vector3(165, .55f, -3), new Vector3(90, .55f, -3), new Vector3(0, .55f, -3), new Vector3(-90, .55f, -3), new Vector3(-165, .55f, -3)
        };

        private void Start()
        {
            // Traffic is presentation-only and collider-free. Keep the fleet capped
            // so world activity remains predictable on mobile hardware.
            var fleet = new GameObject("Traffic").transform;
            int count = Mathf.Clamp(vehicleCount, 0, 24);

            for (int i = 0; i < count; i++)
            {
                var vehicle = TrafficVisualFactory.Create(i, fleet);
                vehicle.name = "Traffic Vehicle " + (i + 1);

                int routeStart = (i * 3) % Route.Length;
                float speed = Mathf.Max(4f, cruiseSpeed + ((i % 5) - 2) * speedVariation * 0.5f);
                float directionLane = i % 2 == 0 ? laneOffset : -laneOffset;

                var ai = vehicle.AddComponent<TrafficVehicle>();
                ai.Configure(Route, routeStart, speed, directionLane);
            }
        }
    }
}
