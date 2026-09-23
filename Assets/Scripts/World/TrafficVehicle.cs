using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Lightweight lane-following traffic AI. Traffic has no colliders or rigidbodies;
    /// it uses deterministic spatial checks and shared junction reservations so the
    /// fleet remains cheap on mobile.
    /// </summary>
    public sealed class TrafficVehicle : MonoBehaviour
    {
        private static readonly List<TrafficVehicle> ActiveVehicles = new List<TrafficVehicle>(24);
        private static TruckController playerTruck;

        [SerializeField] private float cruiseSpeed = 10f;
        [SerializeField] private float acceleration = 4f;
        [SerializeField] private float braking = 9f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float lookAheadDistance = 18f;
        [SerializeField] private float minimumGap = 7f;
        [SerializeField] private float laneWidth = 3.5f;
        [SerializeField] private float laneChangeSpeed = 2.2f;
        [SerializeField] private float laneChangeCooldown = 4f;
        [SerializeField] private float junctionDetectionDistance = 30f;
        [SerializeField] private float junctionReservationDuration = 2.5f;
        [SerializeField] private float junctionExitPadding = 5f;

        private Vector3[] route;
        private int routeIndex;
        private bool routeForward = true;
        private bool reverseAtRouteEnds;
        private float laneOffset;
        private float targetLaneOffset;
        private float speed;
        private float targetSpeed;
        private float checkTimer;
        private float laneChangeTimer;
        private float stuckTimer;
        private float recoveryCooldown;
        private bool yielding;
        private bool playerAhead;
        private bool junctionBlocked;
        private RoadNetwork.Junction reservedJunction;
        private Vector3 lastPosition;

        public float CurrentSpeed => speed;

        public void Configure(
            Vector3[] points,
            int startIndex,
            float desiredSpeed,
            float offset,
            bool reverseAtEnds,
            bool forward)
        {
            route = points;
            routeIndex = Mathf.Clamp(startIndex, 0, points.Length - 1);
            cruiseSpeed = Mathf.Max(1f, desiredSpeed);
            targetSpeed = cruiseSpeed;
            speed = cruiseSpeed;
            laneOffset = Mathf.Clamp(offset, -laneWidth * 0.5f, laneWidth * 0.5f);
            targetLaneOffset = laneOffset;
            reverseAtRouteEnds = reverseAtEnds;
            routeForward = forward;
            laneChangeTimer = 0f;
            recoveryCooldown = 0f;
            stuckTimer = 0f;
            reservedJunction = null;

            transform.position = GetWaypointPosition(routeIndex, laneOffset);
            lastPosition = transform.position;
            FaceNextSegment();
        }

        private void OnEnable()
        {
            if (!ActiveVehicles.Contains(this))
                ActiveVehicles.Add(this);
        }

        private void OnDisable()
        {
            ReleaseJunctionReservation();
            ActiveVehicles.Remove(this);
        }

        private void Update()
        {
            if (route == null || route.Length < 2)
                return;

            laneChangeTimer = Mathf.Max(0f, laneChangeTimer - Time.deltaTime);
            recoveryCooldown = Mathf.Max(0f, recoveryCooldown - Time.deltaTime);

            Vector3 nextWaypoint = GetNextWaypointPosition();
            Vector3 toWaypoint = nextWaypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude < 16f)
            {
                AdvanceRouteIndex();
                nextWaypoint = GetNextWaypointPosition();
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
                EvaluateJunction(direction);
            }

            float desired = cruiseSpeed;
            if (yielding)
                desired = Mathf.Min(desired, Mathf.Max(1.5f, cruiseSpeed * 0.35f));
            if (playerAhead)
                desired = Mathf.Min(desired, Mathf.Max(1f, cruiseSpeed * 0.22f));
            if (junctionBlocked)
                desired = 0f;

            targetSpeed = desired;
            float rate = targetSpeed < speed ? braking : acceleration;
            speed = Mathf.MoveTowards(speed, targetSpeed, rate * Time.deltaTime);

            laneOffset = Mathf.MoveTowards(laneOffset, targetLaneOffset, laneChangeSpeed * Time.deltaTime);
            Vector3 movementDirection = GetWaypointPosition(routeIndex, laneOffset) - transform.position;
            movementDirection.y = 0f;
            if (movementDirection.sqrMagnitude > 0.04f)
                direction = movementDirection.normalized;

            transform.position += direction * speed * Time.deltaTime;
            EvaluateStuckRecovery();

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
            }
        }

        private Vector3 GetNextWaypointPosition()
        {
            int nextIndex = GetNextRouteIndex();
            return GetWaypointPosition(nextIndex, targetLaneOffset);
        }

        private int GetNextRouteIndex()
        {
            if (routeForward)
                return Mathf.Min(routeIndex + 1, route.Length - 1);
            return Mathf.Max(routeIndex - 1, 0);
        }

        private void AdvanceRouteIndex()
        {
            if (routeForward)
            {
                if (routeIndex >= route.Length - 1 && reverseAtRouteEnds)
                    routeForward = false;
                else if (routeIndex < route.Length - 1)
                    routeIndex++;
            }
            else
            {
                if (routeIndex <= 0 && reverseAtRouteEnds)
                    routeForward = true;
                else if (routeIndex > 0)
                    routeIndex--;
            }
        }

        private Vector3 GetWaypointPosition(int index, float offset)
        {
            Vector3 point = route[index];
            int nextIndex = routeForward
                ? Mathf.Min(index + 1, route.Length - 1)
                : Mathf.Max(index - 1, 0);
            Vector3 next = route[nextIndex];
            Vector3 forward = next - point;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
            {
                int previousIndex = routeForward
                    ? Mathf.Max(index - 1, 0)
                    : Mathf.Min(index + 1, route.Length - 1);
                forward = point - route[previousIndex];
                forward.y = 0f;
            }

            if (forward.sqrMagnitude < 0.01f)
                return point;

            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            return point + right * offset;
        }

        private void FaceNextSegment()
        {
            if (route == null || route.Length < 2)
                return;

            Vector3 current = GetWaypointPosition(routeIndex, laneOffset);
            Vector3 next = GetNextWaypointPosition();
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
            TrafficVehicle blocker = null;

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
                if (Vector3.Dot(direction, deltaDirection) < 0.72f)
                    continue;

                float lateral = Mathf.Abs(Vector3.Cross(direction, delta).y);
                if (lateral > laneWidth * 0.42f)
                    continue;

                float safeDistance = minimumGap + speed * 0.65f;
                if (distanceSqr < safeDistance * safeDistance)
                {
                    yielding = true;
                    blocker = other;
                    break;
                }
            }

            if (blocker != null && laneChangeTimer <= 0f && !junctionBlocked)
            {
                float alternateLane = Mathf.Abs(targetLaneOffset) < 0.25f
                    ? laneWidth * 0.5f
                    : -targetLaneOffset;

                if (IsLaneClear(alternateLane, direction, lookAhead * 0.85f))
                {
                    targetLaneOffset = alternateLane;
                    laneChangeTimer = laneChangeCooldown;
                    yielding = false;
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
                Mathf.Abs(Vector3.Cross(direction, playerDelta).y) < laneWidth * 0.42f)
            {
                playerAhead = true;
            }
        }

        private void EvaluateJunction(Vector3 direction)
        {
            junctionBlocked = false;

            RoadNetwork.Junction nearest = RoadNetwork.FindNearestJunction(
                transform.position,
                junctionDetectionDistance);

            if (nearest == null)
            {
                ReleaseJunctionReservation();
                return;
            }

            float distance = RoadNetwork.GetDistanceToJunctionEntry(nearest, transform.position, direction);
            if (distance > junctionDetectionDistance)
            {
                ReleaseJunctionReservation();
                return;
            }

            if (reservedJunction != null && reservedJunction != nearest)
                ReleaseJunctionReservation();

            bool insideJunction = RoadNetwork.IsInside(nearest, transform.position, 0f);
            if (!insideJunction && !RoadNetwork.AllowsEntry(nearest, transform.position, direction, Time.time))
            {
                junctionBlocked = true;
                yielding = true;
                return;
            }

            if (reservedJunction == nearest)
            {
                RoadNetwork.TryReserve(nearest, this, Time.time, junctionReservationDuration);
            }
            else if (RoadNetwork.IsReservedByOther(nearest, this, Time.time))
            {
                junctionBlocked = true;
                yielding = true;
                return;
            }
            else if (RoadNetwork.TryReserve(nearest, this, Time.time, junctionReservationDuration))
            {
                reservedJunction = nearest;
            }

            // Once clear of the junction pad, release the reservation for the next approach.
            if (reservedJunction == nearest &&
                !RoadNetwork.IsInside(nearest, transform.position, junctionExitPadding))
            {
                float fromCenter = Vector3.Distance(
                    Flat(transform.position),
                    Flat(nearest.Center));

                if (fromCenter > Mathf.Max(nearest.SizeX, nearest.SizeZ) * 0.5f + junctionExitPadding)
                    ReleaseJunctionReservation();
            }
        }

        private void ReleaseJunctionReservation()
        {
            if (reservedJunction == null)
                return;

            RoadNetwork.Release(reservedJunction, this);
            reservedJunction = null;
        }

        private bool IsLaneClear(float candidateOffset, Vector3 direction, float distance)
        {
            Vector3 position = transform.position;
            float distanceSqr = distance * distance;

            for (int i = 0; i < ActiveVehicles.Count; i++)
            {
                TrafficVehicle other = ActiveVehicles[i];
                if (other == null || other == this || other.route == null)
                    continue;

                Vector3 otherDelta = other.transform.position - position;
                otherDelta.y = 0f;
                if (otherDelta.sqrMagnitude > distanceSqr)
                    continue;

                Vector3 otherDirection = otherDelta.sqrMagnitude > 0.01f
                    ? otherDelta.normalized
                    : direction;

                if (Vector3.Dot(direction, otherDirection) < 0.35f)
                    continue;

                Vector3 forward = direction.normalized;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                float lateral = Mathf.Abs(Vector3.Dot(otherDelta, right));
                float laneDelta = Mathf.Abs(candidateOffset - other.laneOffset);

                if (laneDelta < laneWidth * 0.55f && lateral < laneWidth * 0.55f)
                    return false;
            }

            if (playerTruck != null)
            {
                Vector3 playerDelta = playerTruck.transform.position - position;
                playerDelta.y = 0f;
                if (playerDelta.sqrMagnitude <= distanceSqr)
                {
                    Vector3 forward = direction.normalized;
                    Vector3 right = new Vector3(forward.z, 0f, -forward.x);
                    float lateral = Mathf.Abs(Vector3.Dot(playerDelta, right));
                    if (lateral < laneWidth * 0.55f &&
                        Mathf.Abs(candidateOffset - laneOffset) < laneWidth * 0.55f)
                        return false;
                }
            }

            return true;
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

            AdvanceRouteIndex();
            laneOffset = targetLaneOffset;
            transform.position = GetWaypointPosition(routeIndex, laneOffset);
            FaceNextSegment();
            speed = Mathf.Max(2f, cruiseSpeed * 0.45f);
            targetSpeed = cruiseSpeed;
            yielding = false;
            playerAhead = false;
            junctionBlocked = false;
            stuckTimer = 0f;
            recoveryCooldown = 2f;
            ReleaseJunctionReservation();
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
