using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Lightweight lane-following traffic AI. Traffic has no colliders or rigidbodies;
    /// it uses deterministic spatial checks so the same fleet remains cheap on mobile.
    /// </summary>
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private static readonly List<TrafficVehicle> ActiveVehicles = new List<TrafficVehicle>(24);
        private static TruckController playerTruck;

        private Vector3 lastPosition;
        private float stuckTimer;
        private float recoveryCooldown;


        [SerializeField] private float cruiseSpeed = 10f;
        [SerializeField] private float acceleration = 4f;
        [SerializeField] private float braking = 9f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float lookAheadDistance = 18f;
        [SerializeField] private float minimumGap = 7f;
        [SerializeField] private float laneWidth = 3.6f;

        private Vector3[] route;
        private int routeIndex;
        private float laneOffset;
        private float speed;
        private float targetSpeed;
        private float checkTimer;
        private bool yielding;
        private bool playerAhead;

        public float CurrentSpeed => speed;

        public void Configure(Vector3[] points, int startIndex, float desiredSpeed, float offset)
        {
            route = points;
            routeIndex = Mathf.Clamp(startIndex, 0, points.Length - 1);
            cruiseSpeed = Mathf.Max(1f, desiredSpeed);
            targetSpeed = cruiseSpeed;
            speed = cruiseSpeed;
            laneOffset = Mathf.Clamp(offset, -laneWidth * 0.6f, laneWidth * 0.6f);

            transform.position = GetWaypointPosition(routeIndex);
            FaceNextSegment();
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
            if (route == null || route.Length < 2)
                return;

            Vector3 nextWaypoint = GetWaypointPosition(routeIndex);
            Vector3 toWaypoint = nextWaypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude < 16f)
            {
                routeIndex = (routeIndex + 1) % route.Length;
                nextWaypoint = GetWaypointPosition(routeIndex);
                toWaypoint = nextWaypoint - transform.position;
                toWaypoint.y = 0f;
            }

            Vector3 direction = toWaypoint.sqrMagnitude > 0.04f
                ? toWaypoint.normalized
                : transform.forward;

            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.12f;
                EvaluateTraffic(direction);
            }

            float desired = cruiseSpeed;

            // Smoothly follow the vehicle in front instead of hard stopping.
            if (yielding)
                desired = Mathf.Min(desired, Mathf.Max(1.5f, cruiseSpeed * 0.35f));

            // Keep AI traffic from driving through the player. This is deliberately
            // soft so the player can still merge through traffic rather than getting
            // physically blocked by a collider.
            if (playerAhead)
                desired = Mathf.Min(desired, Mathf.Max(1f, cruiseSpeed * 0.22f));

            targetSpeed = desired;
            float rate = targetSpeed < speed ? braking : acceleration;
            speed = Mathf.MoveTowards(speed, targetSpeed, rate * Time.deltaTime);

            transform.position += direction * speed * Time.deltaTime;
            EvaluateStuckRecovery();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    turnSpeed * Time.deltaTime);
            }
        }

        private Vector3 GetWaypointPosition(int index)
        {
            Vector3 point = route[index];
            Vector3 next = route[(index + 1) % route.Length];
            Vector3 forward = next - point;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
                return point;

            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            return point + right * laneOffset;
        }

        private void FaceNextSegment()
        {
            if (route == null || route.Length < 2)
                return;

            Vector3 current = GetWaypointPosition(routeIndex);
            Vector3 next = GetWaypointPosition((routeIndex + 1) % route.Length);
            Vector3 direction = next - current;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void EvaluateTraffic(Vector3 direction)
        {
            yielding = false;
            playerAhead = false;

            Vector3 position = transform.position;
            float lookAhead = Mathf.Max(lookAheadDistance, speed * 1.4f + minimumGap);
            float lookAheadSqr = lookAhead * lookAhead;

            for (int i = 0; i < ActiveVehicles.Count; i++)
            {
                TrafficVehicle other = ActiveVehicles[i];
                if (other == null || other == this || other.route == null)
                    continue;

                Vector3 delta = other.transform.position - position;
                delta.y = 0f;
                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr < 0.25f || distanceSqr > lookAheadSqr)
                    continue;

                Vector3 deltaDirection = delta.normalized;
                float forwardDot = Vector3.Dot(direction, deltaDirection);
                if (forwardDot < 0.72f)
                    continue;

                float lateral = Mathf.Abs(Vector3.Cross(direction, delta).y);
                if (lateral > laneWidth * 0.75f)
                    continue;

                float safeDistance = minimumGap + speed * 0.65f;
                if (distanceSqr < safeDistance * safeDistance)
                {
                    yielding = true;
                    break;
                }
            }

            if (playerTruck == null)
                playerTruck = FindFirstObjectByType<TruckController>();

            TruckController player = playerTruck;
            if (player == null)
                return;

            Vector3 playerDelta = player.transform.position - position;
            playerDelta.y = 0f;
            float playerDistance = playerDelta.magnitude;

            if (playerDistance <= lookAhead &&
                playerDistance > 0.5f &&
                Vector3.Dot(direction, playerDelta.normalized) > 0.75f &&
                Mathf.Abs(Vector3.Cross(direction, playerDelta).y) < laneWidth)
            {
                playerAhead = true;
            }
        }
    }
}

        private void EvaluateStuckRecovery()
        {
            Vector3 movement = transform.position - lastPosition;
            movement.y = 0f;
            float movedSqr = movement.sqrMagnitude;
            lastPosition = transform.position;

            if (speed > 2f && movedSqr < 0.0025f)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = Mathf.Max(0f, stuckTimer - Time.deltaTime * 0.5f);

            if (stuckTimer < 3.5f || recoveryCooldown > 0f)
                return;

            // Presentation traffic has no physics body, so a deterministic recovery
            // is safer and cheaper than adding colliders or rigidbody simulation.
            routeIndex = (routeIndex + 1) % route.Length;
            transform.position = GetWaypointPosition(routeIndex);
            FaceNextSegment();
            speed = Mathf.Max(2f, cruiseSpeed * 0.45f);
            targetSpeed = cruiseSpeed;
            yielding = false;
            playerAhead = false;
            stuckTimer = 0f;
            recoveryCooldown = 2f;
            recoveryDistance += 1f;
        }
