using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Lightweight runtime metadata for the procedural road network.
    /// Geometry remains owned by RoadBuilder; this registry is the shared semantic
    /// layer consumed by traffic and navigation.
    /// </summary>
    public static class RoadNetwork
    {
        public enum ApproachDirection
        {
            North,
            South,
            East,
            West
        }

        public sealed class Junction
        {
            public string Id { get; }
            public Vector3 Center { get; }
            public float SizeX { get; }
            public float SizeZ { get; }
            public bool Signalized { get; }

            internal TrafficReservation Reservation;
            internal Junction(string id, Vector3 center, float sizeX, float sizeZ, bool signalized)
            {
                Id = id;
                Center = center;
                SizeX = sizeX;
                SizeZ = sizeZ;
                Signalized = signalized;
            }
        }

        public readonly struct RoadCorridor
        {
            public readonly string Id;
            public readonly bool AlongX;
            public readonly float FixedCoordinate;
            public readonly float Min;
            public readonly float Max;
            public readonly float Width;

            public RoadCorridor(string id, bool alongX, float fixedCoordinate, float min, float max, float width)
            {
                Id = id;
                AlongX = alongX;
                FixedCoordinate = fixedCoordinate;
                Min = min;
                Max = max;
                Width = width;
            }
        }

        internal sealed class TrafficReservation
        {
            public TrafficVehicle Owner;
            public float ExpiresAt;
            public float RequestedAt;
        }

        private static readonly List<Junction> junctions = new List<Junction>(8);
        private static readonly List<RoadCorridor> corridors = new List<RoadCorridor>(24);

        public static IReadOnlyList<Junction> Junctions => junctions;
        public static int Version { get; private set; }
        public static IReadOnlyList<RoadCorridor> Corridors => corridors;

        public static void Reset()
        {
            junctions.Clear();
            corridors.Clear();
            Version++;
        }

        public static void RegisterCorridor(string id, bool alongX, float fixedCoordinate, float min, float max, float width)
        {
            corridors.Add(new RoadCorridor(id, alongX, fixedCoordinate, min, max, width));
        }

        public static Junction RegisterJunction(string id, Vector3 center, float sizeX, float sizeZ, bool signalized)
        {
            Junction existing = FindJunction(id);
            if (existing != null)
                return existing;

            Junction junction = new Junction(id, center, sizeX, sizeZ, signalized);
            junctions.Add(junction);
            return junction;
        }

        public static Junction FindJunction(string id)
        {
            for (int i = 0; i < junctions.Count; i++)
                if (junctions[i].Id == id)
                    return junctions[i];
            return null;
        }

        public static Junction FindNearestJunction(Vector3 position, float maxDistance)
        {
            Junction best = null;
            float bestSqr = maxDistance * maxDistance;

            for (int i = 0; i < junctions.Count; i++)
            {
                Vector3 delta = junctions[i].Center - position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    best = junctions[i];
                    bestSqr = sqr;
                }
            }

            return best;
        }

        public static bool IsInside(Junction junction, Vector3 position, float padding)
        {
            if (junction == null)
                return false;

            return Mathf.Abs(position.x - junction.Center.x) <= junction.SizeX * 0.5f + padding &&
                   Mathf.Abs(position.z - junction.Center.z) <= junction.SizeZ * 0.5f + padding;
        }

        public static ApproachDirection GetApproachDirection(Junction junction, Vector3 position)
        {
            Vector3 delta = position - junction.Center;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
                return delta.x >= 0f ? ApproachDirection.East : ApproachDirection.West;
            return delta.z >= 0f ? ApproachDirection.North : ApproachDirection.South;
        }

        /// <summary>
        /// First-arrival reservation. Only one vehicle may occupy a junction at a
        /// time; expired reservations are reclaimed automatically. This is a
        /// deterministic, collider-free priority gate for the 24-vehicle fleet.
        /// </summary>
        public static bool TryReserve(Junction junction, TrafficVehicle vehicle, float now, float duration)
        {
            if (junction == null || vehicle == null)
                return false;

            TrafficReservation reservation = junction.Reservation;
            if (reservation == null)
            {
                junction.Reservation = new TrafficReservation
                {
                    Owner = vehicle,
                    ExpiresAt = now + Mathf.Max(0.5f, duration),
                    RequestedAt = now
                };
                return true;
            }

            if (reservation.Owner == vehicle)
            {
                reservation.ExpiresAt = now + Mathf.Max(0.5f, duration);
                return true;
            }

            if (reservation.Owner == null || reservation.ExpiresAt <= now)
            {
                reservation.Owner = vehicle;
                reservation.ExpiresAt = now + Mathf.Max(0.5f, duration);
                reservation.RequestedAt = now;
                return true;
            }

            return false;
        }

        public static void Release(Junction junction, TrafficVehicle vehicle)
        {
            if (junction == null || junction.Reservation == null || junction.Reservation.Owner != vehicle)
                return;

            junction.Reservation.Owner = null;
            junction.Reservation.ExpiresAt = 0f;
        }

        public static bool IsReservedByOther(Junction junction, TrafficVehicle vehicle, float now)
        {
            if (junction == null || junction.Reservation == null)
                return false;

            if (junction.Reservation.Owner == null || junction.Reservation.ExpiresAt <= now)
            {
                junction.Reservation.Owner = null;
                return false;
            }

            return junction.Reservation.Owner != vehicle;
        }

        public static float GetDistanceToJunctionEntry(Junction junction, Vector3 position, Vector3 direction)
        {
            if (junction == null)
                return float.PositiveInfinity;

            Vector3 toCenter = junction.Center - position;
            toCenter.y = 0f;
            float forward = Vector3.Dot(toCenter, direction.normalized);
            float halfExtent = Mathf.Max(junction.SizeX, junction.SizeZ) * 0.5f;
            return Mathf.Max(0f, forward - halfExtent);
        }
    }
}
