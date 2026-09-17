using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficVehicle : MonoBehaviour
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float turnSpeed = 4f;
        private Vector3 target;
        private float laneOffset;
        private int routeIndex;
        private Vector3[] route;

        public void Configure(Vector3[] points, int startIndex, float cruiseSpeed, float offset)
        {
            route = points; routeIndex = Mathf.Clamp(startIndex, 0, points.Length - 1); speed = cruiseSpeed; laneOffset = offset;
            transform.position = route[routeIndex]; target = route[routeIndex];
        }

        private void Update()
        {
            if (route == null || route.Length < 2) return;
            target = route[routeIndex] + Vector3.right * laneOffset;
            Vector3 flat = target - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude < 9f) routeIndex = (routeIndex + 1) % route.Length;
            Vector3 direction = flat.sqrMagnitude > .01f ? flat.normalized : transform.forward;
            transform.position += direction * speed * Time.deltaTime;
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }
    }
}
