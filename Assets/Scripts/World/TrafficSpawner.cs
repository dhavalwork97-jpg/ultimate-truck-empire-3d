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
            for (int i = 0; i < Mathf.Max(0, vehicleCount); i++)
            {
                var v = GameObject.CreatePrimitive(i % 4 == 0 ? PrimitiveType.Cube : PrimitiveType.Capsule);
                v.name = "Traffic Vehicle " + (i + 1);
                Object.Destroy(v.GetComponent<Collider>());
                v.transform.localScale = i % 4 == 0 ? new Vector3(1.6f, .9f, 3.2f) : new Vector3(1.1f, .8f, 2.2f);
                var ai = v.AddComponent<TrafficVehicle>();
                ai.Configure(Route, (i * 3) % Route.Length, cruiseSpeed + (i % 3) * 1.5f, i % 2 == 0 ? 1.8f : -1.8f);
            }
        }
    }
}
