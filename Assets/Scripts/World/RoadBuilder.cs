using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Builds the road surface out of procedural geometry.
    ///
    /// Each segment is welded into ONE mesh with a submesh per material, so a
    /// road with shoulders, curbs, centre line, lane dashes, edge lines and
    /// arrows still costs a single renderer and a single box collider.
    ///
    /// Height layering is deliberate and fixed, because coplanar surfaces are
    /// what produce z-fighting: ground -0.06, gravel shoulder -0.015, asphalt
    /// 0.000, paint 0.012, arrows 0.014, curb top 0.14.
    /// </summary>
    public static class RoadBuilder
    {
        public const float GroundY = -0.06f;
        public const float AsphaltTopY = 0f;
        public const float ShoulderTopY = -0.015f;
        public const float PaintY = 0.012f;
        public const float ArrowY = 0.014f;
        private const float SlabDepth = 0.30f;
        private const float PaintThickness = 0.02f;

        public const float LaneWidth = 3.5f;
        public const float DefaultWidth = 14f;

        private static readonly List<GameObject> segments = new List<GameObject>();
        public static IReadOnlyList<GameObject> Segments => segments;

        public static void Reset() => segments.Clear();

        public struct RoadOptions
        {
            public bool centreLine;
            public bool laneDashes;
            public bool edgeLines;
            public bool shoulders;
            public bool curbs;
            public float dashLength;
            public float dashGap;

            public static RoadOptions Highway()
            {
                return new RoadOptions
                {
                    centreLine = true, laneDashes = true, edgeLines = true,
                    shoulders = true, curbs = false, dashLength = 3f, dashGap = 6f
                };
            }

            public static RoadOptions Street()
            {
                return new RoadOptions
                {
                    centreLine = true, laneDashes = false, edgeLines = true,
                    shoulders = false, curbs = true, dashLength = 2f, dashGap = 4f
                };
            }

            public static RoadOptions Access()
            {
                return new RoadOptions
                {
                    centreLine = false, laneDashes = false, edgeLines = true,
                    shoulders = true, curbs = false, dashLength = 2f, dashGap = 4f
                };
            }
        }

        // ------------------------------------------------------------------
        /// <summary>Road running along X, centred on <paramref name="z"/>.</summary>
        public static GameObject BuildEastWest(string roadName, float xMin, float xMax, float z, float width, RoadOptions options)
        {
            return Build(roadName, new Vector3((xMin + xMax) * 0.5f, 0f, z), Mathf.Abs(xMax - xMin), width, true, options);
        }

        /// <summary>Road running along Z, centred on <paramref name="x"/>.</summary>
        public static GameObject BuildNorthSouth(string roadName, float zMin, float zMax, float x, float width, RoadOptions options)
        {
            return Build(roadName, new Vector3(x, 0f, (zMin + zMax) * 0.5f), Mathf.Abs(zMax - zMin), width, false, options);
        }

        private static GameObject Build(string roadName, Vector3 centre, float length, float width, bool alongX, RoadOptions options)
        {
            MeshBuilder mb = new MeshBuilder();

            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            // Asphalt slab. Local space is built along X and rotated for Z roads,
            // which keeps one code path for both orientations.
            mb.Add(ProcMesh.Box(length, SlabDepth, width), new Vector3(0f, AsphaltTopY - SlabDepth * 0.5f, 0f),
                   Slot(WorldSurface.Asphalt));

            if (options.shoulders)
            {
                const float shoulderWidth = 2.6f;
                Mesh shoulder = ProcMesh.Box(length, 0.22f, shoulderWidth);
                for (int side = -1; side <= 1; side += 2)
                    mb.Add(shoulder, new Vector3(0f, ShoulderTopY - 0.11f, side * (halfW + shoulderWidth * 0.5f - 0.05f)),
                           Slot(WorldSurface.Gravel));
            }

            if (options.curbs)
            {
                Mesh curb = ProcMesh.Box(length, 0.20f, 0.30f);
                for (int side = -1; side <= 1; side += 2)
                    mb.Add(curb, new Vector3(0f, 0.04f, side * (halfW + 0.15f)), Slot(WorldSurface.Curb));
            }

            AddMarkings(mb, length, width, options);

            GameObject go = mb.Emit(roadName, null, WorldPaletteAdapter.Palette);
            go.transform.position = centre;
            if (!alongX) go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // One box collider for the drivable surface. Nothing decorative gets one.
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, AsphaltTopY - SlabDepth * 0.5f, 0f);
            box.size = new Vector3(length, SlabDepth, width);

            segments.Add(go);
            return go;
        }

        private static void AddMarkings(MeshBuilder mb, float length, float width, RoadOptions options)
        {
            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            if (options.edgeLines)
            {
                Mesh edge = ProcMesh.Box(length - 0.4f, PaintThickness, 0.16f);
                for (int side = -1; side <= 1; side += 2)
                    mb.Add(edge, new Vector3(0f, PaintY, side * (halfW - 0.45f)), Slot(WorldSurface.MarkingWhite));
            }

            if (options.centreLine)
            {
                // Double centre line, as used on Indian national highways.
                Mesh centre = ProcMesh.Box(length - 0.4f, PaintThickness, 0.14f);
                mb.Add(centre, new Vector3(0f, PaintY, 0.13f), Slot(WorldSurface.MarkingYellow));
                mb.Add(centre, new Vector3(0f, PaintY, -0.13f), Slot(WorldSurface.MarkingYellow));
            }

            if (options.laneDashes)
            {
                float step = Mathf.Max(1f, options.dashLength + options.dashGap);
                int count = Mathf.Max(0, Mathf.FloorToInt((length - 4f) / step));
                Mesh dash = ProcMesh.Box(options.dashLength, PaintThickness, 0.14f);

                // One dashed line per side of the carriageway on a four lane road.
                float laneOffset = halfW * 0.5f;
                for (int i = 0; i < count; i++)
                {
                    float x = -halfL + 2f + (i + 0.5f) * step;
                    mb.Add(dash, new Vector3(x, PaintY, laneOffset), Slot(WorldSurface.MarkingWhite));
                    mb.Add(dash, new Vector3(x, PaintY, -laneOffset), Slot(WorldSurface.MarkingWhite));
                }
            }
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Intersection pad. Crossing roads are built as segments that stop short
        /// of the junction and the pad fills the gap, so no two asphalt surfaces
        /// are ever coplanar.
        /// </summary>
        public static GameObject BuildIntersection(string padName, Vector3 centre, float sizeX, float sizeZ)
        {
            MeshBuilder mb = new MeshBuilder();
            mb.Add(ProcMesh.Box(sizeX, SlabDepth, sizeZ), new Vector3(0f, AsphaltTopY - SlabDepth * 0.5f, 0f),
                   Slot(WorldSurface.AsphaltWorn));

            // Stop bars and zebra crossings on all four approaches.
            float halfX = sizeX * 0.5f;
            float halfZ = sizeZ * 0.5f;

            Mesh stopBarX = ProcMesh.Box(0.45f, PaintThickness, sizeZ * 0.46f);
            mb.Add(stopBarX, new Vector3(-halfX + 0.9f, PaintY, -sizeZ * 0.24f), Slot(WorldSurface.MarkingWhite));
            mb.Add(stopBarX, new Vector3(halfX - 0.9f, PaintY, sizeZ * 0.24f), Slot(WorldSurface.MarkingWhite));

            Mesh stopBarZ = ProcMesh.Box(sizeX * 0.46f, PaintThickness, 0.45f);
            mb.Add(stopBarZ, new Vector3(sizeX * 0.24f, PaintY, -halfZ + 0.9f), Slot(WorldSurface.MarkingWhite));
            mb.Add(stopBarZ, new Vector3(-sizeX * 0.24f, PaintY, halfZ - 0.9f), Slot(WorldSurface.MarkingWhite));

            Mesh zebraX = ProcMesh.Box(0.55f, PaintThickness, 0.9f);
            for (int i = 0; i < 6; i++)
            {
                float t = (i + 0.5f) / 6f;
                float z = Mathf.Lerp(-halfZ + 1.2f, halfZ - 1.2f, t);
                mb.Add(zebraX, new Vector3(-halfX + 2.2f, PaintY, z), Slot(WorldSurface.MarkingWhite));
                mb.Add(zebraX, new Vector3(halfX - 2.2f, PaintY, z), Slot(WorldSurface.MarkingWhite));
            }

            GameObject go = mb.Emit(padName, null, WorldPaletteAdapter.Palette);
            go.transform.position = centre;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, AsphaltTopY - SlabDepth * 0.5f, 0f);
            box.size = new Vector3(sizeX, SlabDepth, sizeZ);

            segments.Add(go);
            return go;
        }

        // ------------------------------------------------------------------
        /// <summary>Directional arrow painted on the carriageway.</summary>
        public static void AddArrow(MeshBuilder mb, Vector3 localPosition, float headingDegrees)
        {
            Quaternion rotation = Quaternion.Euler(0f, headingDegrees, 0f);
            mb.Add(ProcMesh.Box(0.30f, PaintThickness, 2.2f), localPosition + new Vector3(0f, ArrowY, 0f),
                   rotation, Slot(WorldSurface.MarkingWhite));

            // Head built from two angled bars.
            Vector3 tip = localPosition + rotation * new Vector3(0f, 0f, 1.25f);
            mb.Add(ProcMesh.Box(0.28f, PaintThickness, 0.95f), tip + new Vector3(0f, ArrowY, 0f),
                   rotation * Quaternion.Euler(0f, 34f, 0f), Slot(WorldSurface.MarkingWhite));
            mb.Add(ProcMesh.Box(0.28f, PaintThickness, 0.95f), tip + new Vector3(0f, ArrowY, 0f),
                   rotation * Quaternion.Euler(0f, -34f, 0f), Slot(WorldSurface.MarkingWhite));
        }

        /// <summary>Hatched warning area, used at depot entrances and dock aprons.</summary>
        public static void AddHatching(MeshBuilder mb, Vector3 centre, float sizeX, float sizeZ, int stripes)
        {
            Mesh stripe = ProcMesh.Box(0.35f, PaintThickness, sizeZ * 1.25f);
            for (int i = 0; i < stripes; i++)
            {
                float t = (i + 0.5f) / stripes;
                float x = Mathf.Lerp(-sizeX * 0.5f, sizeX * 0.5f, t);
                mb.Add(stripe, centre + new Vector3(x, ArrowY, 0f), Quaternion.Euler(0f, 30f, 0f),
                       Slot(WorldSurface.WarningStripe));
            }
        }

        /// <summary>Painted parking bays.</summary>
        public static void AddParkingBays(MeshBuilder mb, Vector3 centre, int bays, float bayWidth, float bayLength, bool alongX)
        {
            Mesh line = alongX
                ? ProcMesh.Box(0.14f, PaintThickness, bayLength)
                : ProcMesh.Box(bayLength, PaintThickness, 0.14f);

            for (int i = 0; i <= bays; i++)
            {
                float offset = (i - bays * 0.5f) * bayWidth;
                Vector3 position = alongX
                    ? centre + new Vector3(offset, PaintY, 0f)
                    : centre + new Vector3(0f, PaintY, offset);
                mb.Add(line, position, Slot(WorldSurface.MarkingWhite));
            }
        }

        internal static int Slot(WorldSurface surface) => WorldPaletteAdapter.Slot(surface);
    }
}
