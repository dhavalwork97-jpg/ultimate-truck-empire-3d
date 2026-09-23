using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public static class RoadNetwork
    {
        public enum ApproachDirection { North, South, East, West }
        public enum SignalState { Red, Yellow, Green }

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
                Id = id; Center = center; SizeX = sizeX; SizeZ = sizeZ; Signalized = signalized;
            }
        }

        public readonly struct RoadCorridor
        {
            public readonly string Id; public readonly bool AlongX; public readonly float FixedCoordinate;
            public readonly float Min; public readonly float Max; public readonly float Width;
            public RoadCorridor(string id, bool alongX, float fixedCoordinate, float min, float max, float width)
            { Id=id; AlongX=alongX; FixedCoordinate=fixedCoordinate; Min=min; Max=max; Width=width; }
        }

        internal sealed class TrafficReservation
        {
            public TrafficVehicle Owner; public float ExpiresAt; public float RequestedAt;
        }

        private static readonly List<Junction> junctions = new List<Junction>(8);
        private static readonly List<RoadCorridor> corridors = new List<RoadCorridor>(24);

        private const float GreenDuration = 10f;
        private const float YellowDuration = 2f;
        private const float AllRedDuration = 1f;
        private const float CycleDuration = (GreenDuration + YellowDuration + AllRedDuration) * 2f;

        public static IReadOnlyList<Junction> Junctions => junctions;
        public static IReadOnlyList<RoadCorridor> Corridors => corridors;
        public static int Version { get; private set; }

        public static void Reset() { junctions.Clear(); corridors.Clear(); Version++; }

        public static void RegisterCorridor(string id, bool alongX, float fixedCoordinate, float min, float max, float width)
            => corridors.Add(new RoadCorridor(id, alongX, fixedCoordinate, min, max, width));

        public static Junction RegisterJunction(string id, Vector3 center, float sizeX, float sizeZ, bool signalized)
        {
            Junction existing = FindJunction(id);
            if (existing != null) return existing;
            Junction junction = new Junction(id, center, sizeX, sizeZ, signalized);
            junctions.Add(junction);
            return junction;
        }

        public static Junction FindJunction(string id)
        {
            for (int i=0;i<junctions.Count;i++) if (junctions[i].Id == id) return junctions[i];
            return null;
        }

        public static Junction FindNearestJunction(Vector3 position, float maxDistance)
        {
            Junction best=null; float bestSqr=maxDistance*maxDistance;
            for (int i=0;i<junctions.Count;i++)
            {
                Vector3 d=junctions[i].Center-position; d.y=0f; float sqr=d.sqrMagnitude;
                if (sqr<bestSqr){best=junctions[i];bestSqr=sqr;}
            }
            return best;
        }

        public static bool IsInside(Junction junction, Vector3 position, float padding)
        {
            if (junction==null) return false;
            return Mathf.Abs(position.x-junction.Center.x)<=junction.SizeX*0.5f+padding &&
                   Mathf.Abs(position.z-junction.Center.z)<=junction.SizeZ*0.5f+padding;
        }

        public static ApproachDirection GetApproachDirection(Junction junction, Vector3 position)
        {
            Vector3 d=position-junction.Center;
            if (Mathf.Abs(d.x)>Mathf.Abs(d.z)) return d.x>=0f?ApproachDirection.East:ApproachDirection.West;
            return d.z>=0f?ApproachDirection.North:ApproachDirection.South;
        }

        public static SignalState GetSignalState(Junction junction, ApproachDirection approach, float now)
        {
            if (junction==null || !junction.Signalized) return SignalState.Green;
            float offset = StableOffset(junction.Id);
            float t = Mathf.Repeat(now + offset, CycleDuration);
            bool eastWest = approach==ApproachDirection.East || approach==ApproachDirection.West;
            float groupStart = eastWest ? 0f : GreenDuration + YellowDuration + AllRedDuration;
            float local = t - groupStart;
            if (local < 0f) local += CycleDuration;
            if (local < GreenDuration) return SignalState.Green;
            if (local < GreenDuration + YellowDuration) return SignalState.Yellow;
            return SignalState.Red;
        }

        public static bool AllowsEntry(Junction junction, Vector3 position, Vector3 direction, float now)
        {
            if (junction==null || IsInside(junction, position, 0f)) return true;
            ApproachDirection approach=GetApproachDirection(junction, position);
            SignalState state=GetSignalState(junction, approach, now);
            if (state==SignalState.Green) return true;
            if (state==SignalState.Yellow)
            {
                float distance=GetDistanceToJunctionEntry(junction, position, direction);
                return distance <= 3f;
            }
            return false;
        }

        private static float StableOffset(string id)
        {
            unchecked
            {
                int hash=17;
                for(int i=0;i<id.Length;i++) hash=hash*31+id[i];
                return Mathf.Abs(hash%11);
            }
        }

        public static bool TryReserve(Junction junction, TrafficVehicle vehicle, float now, float duration)
        {
            if (junction==null || vehicle==null) return false;
            TrafficReservation reservation=junction.Reservation;
            if (reservation==null)
            {
                junction.Reservation=new TrafficReservation{Owner=vehicle,ExpiresAt=now+Mathf.Max(0.5f,duration),RequestedAt=now};
                return true;
            }
            if (reservation.Owner==vehicle){reservation.ExpiresAt=now+Mathf.Max(0.5f,duration);return true;}
            if (reservation.Owner==null || reservation.ExpiresAt<=now)
            {
                reservation.Owner=vehicle; reservation.ExpiresAt=now+Mathf.Max(0.5f,duration); reservation.RequestedAt=now; return true;
            }
            return false;
        }

        public static void Release(Junction junction, TrafficVehicle vehicle)
        {
            if (junction==null || junction.Reservation==null || junction.Reservation.Owner!=vehicle) return;
            junction.Reservation.Owner=null; junction.Reservation.ExpiresAt=0f;
        }

        public static bool IsReservedByOther(Junction junction, TrafficVehicle vehicle, float now)
        {
            if (junction==null || junction.Reservation==null) return false;
            if (junction.Reservation.Owner==null || junction.Reservation.ExpiresAt<=now){junction.Reservation.Owner=null;return false;}
            return junction.Reservation.Owner!=vehicle;
        }

        public static float GetDistanceToJunctionEntry(Junction junction, Vector3 position, Vector3 direction)
        {
            if (junction==null) return float.PositiveInfinity;
            Vector3 toCenter=junction.Center-position; toCenter.y=0f;
            float forward=Vector3.Dot(toCenter,direction.normalized);
            float halfExtent=Mathf.Max(junction.SizeX,junction.SizeZ)*0.5f;
            return Mathf.Max(0f,forward-halfExtent);
        }
    }
}