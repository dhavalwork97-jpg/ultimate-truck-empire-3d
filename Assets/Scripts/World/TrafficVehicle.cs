using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private static readonly List<TrafficVehicle> ActiveVehicles = new List<TrafficVehicle>();

        [SerializeField] private float cruiseSpeed = 10f;
        [SerializeField] private float acceleration = 4f;
        [SerializeField] private float braking = 7f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float lookAheadDistance = 14f;

        private Vector3[] route;
        private int routeIndex;
        private float laneOffset;
        private float speed;
        private float targetSpeed;
        private float checkTimer;
        private bool yielding;

        public void Configure(Vector3[] points, int startIndex, float desiredSpeed, float offset)
        {
            route = points;
            routeIndex = Mathf.Clamp(startIndex, 0, points.Length - 1);
            cruiseSpeed = Mathf.Max(1f, desiredSpeed);
            targetSpeed = cruiseSpeed;
            speed = cruiseSpeed;
            laneOffset = offset;

            transform.position = route[routeIndex] + Vector3.forward * laneOffset;
            if (route.Length > 1)
            {
                Vector3 next = route[(routeIndex + 1) % route.Length] - route[routeIndex];
                next.y = 0f;
                if (next.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(next.normalized, Vector3.up);
            }
        }

        private void OnEnable()
        {
            if (!ActiveVehicles.Contains(this))
                ActiveVehicles.Add(this);
        }

        private void OnDisable()
        {
            ActiveVehicles.Remove(this);
        }

        private void Update()
        {
            if (route == null || route.Length < 2) return;

            Vector3 waypoint = route[routeIndex] + Vector3.forward * laneOffset;
            Vector3 toWaypoint = waypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude < 9f)
            {
                routeIndex = (routeIndex + 1) % route.Length;
                waypoint = route[routeIndex] + Vector3.forward * laneOffset;
                toWaypoint = waypoint - transform.position;
                toWaypoint.y = 0f;
            }

            Vector3 direction = toWaypoint.sqrMagnitude > 0.01f
                ? toWaypoint.normalized
                : transform.forward;

            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.2f;
                yielding = ShouldYield(direction);
            }

            targetSpeed = yielding ? cruiseSpeed * 0.28f : cruiseSpeed;
            float rate = targetSpeed < speed ? braking : acceleration;
            speed = Mathf.MoveTowards(speed, targetSpeed, rate * Time.deltaTime);

            transform.position += direction * speed * Time.deltaTime;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion desired = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeed * Time.deltaTime);
            }
        }

        private bool ShouldYield(Vector3 direction)
        {
            Vector3 position = transform.position;
            float closestAhead = lookAheadDistance * lookAheadDistance;

            for (int i = 0; i < ActiveVehicles.Count; i++)
            {
                TrafficVehicle other = ActiveVehicles[i];
                if (other == null || other == this || other.route == null) continue;

                Vector3 delta = other.transform.position - position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;
                if (distanceSqr < 0.25f || distanceSqr > closestAhead) continue;

                float forwardDot = Vector3.Dot(direction, delta.normalized);
                if (forwardDot < 0.65f) continue;

                // Only slow for a vehicle travelling in roughly the same direction.
                float headingDot = Vector3.Dot(direction, other.transform.forward);
                if (headingDot > 0.65f)
                    return true;
            }

            return false;
        }
    }
}
