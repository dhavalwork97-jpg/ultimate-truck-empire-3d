using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficSpawner : MonoBehaviour
    {
        [SerializeField] private int vehicleCount = 14;
        [SerializeField] private float cruiseSpeed = 12f;
        private static readonly Vector3[] Route =
        {
            new Vector3(-165, .55f, 3), new Vector3(-90, .55f, 3), new Vector3(0, .55f, 3), new Vector3(90, .55f, 3), new Vector3(165, .55f, 3),
            new Vector3(165, .55f, -3), new Vector3(90, .55f, -3), new Vector3(0, .55f, -3), new Vector3(-90, .55f, -3), new Vector3(-165, .55f, -3)
        };

        private void Start()
        {
            // The AI is untouched; only the bodywork changed. Vehicles are built
            // from cached procedural meshes and carry no colliders.
            var fleet = new GameObject("Traffic").transform;
            for (int i = 0; i < Mathf.Max(0, vehicleCount); i++)
            {
                var v = TrafficVisualFactory.Create(i, fleet);
                v.name = "Traffic Vehicle " + (i + 1);
                var ai = v.AddComponent<TrafficVehicle>();
                ai.Configure(Route, (i * 3) % Route.Length, cruiseSpeed + (i % 3) * 1.5f, i % 2 == 0 ? 1.8f : -1.8f);
            }
        }
    }
}
