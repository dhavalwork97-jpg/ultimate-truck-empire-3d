using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Builds clean gameplay road collision over the Batch 02 presentation tiles.
    /// The photogrammetry roads remain visual references; gameplay road ownership
    /// stays with the existing WorldVisualBuilder/RoadNetwork graph.
    /// </summary>
    public sealed class Batch02GameplayRoadOverlay : MonoBehaviour
    {
        [Serializable]
        private sealed class RoadPath
        {
            public string id;
            public float width = 10f;
            public Vector3[] points;
        }

        // These paths deliberately follow the authoritative procedural road graph:
        // the north highway at Z=70 and the existing link road at X=-90.
        // Batch 02 supplies presentation geometry; this component only supplies
        // clean collision where that geometry needs a dependable drive surface.
        private static readonly RoadPath[] Paths =
        {
            new RoadPath
            {
                id = "Batch02-North-Highway-Overlay",
                width = 14f,
                points = new[]
                {
                    new Vector3(-205f, 0.08f, 70f),
                    new Vector3(-135f, 0.08f, 70f),
                    new Vector3(-45f, 0.08f, 70f),
                    new Vector3(45f, 0.08f, 70f),
                    new Vector3(135f, 0.08f, 70f),
                    new Vector3(220f, 0.08f, 70f)
                }
            },
            new RoadPath
            {
                id = "Batch02-Link-Road-Overlay",
                width = 14f,
                points = new[]
                {
                    new Vector3(-90f, 0.08f, -88f),
                    new Vector3(-90f, 0.08f, -30f),
                    new Vector3(-90f, 0.08f, 35f),
                    new Vector3(-90f, 0.08f, 70f),
                    new Vector3(-90f, 0.08f, 105f),
                    new Vector3(-90f, 0.08f, 135f)
                }
            }
        };

        [SerializeField] private bool buildOnStart = true;
        private bool built;

        private void Start()
        {
            if (buildOnStart)
                Build();
        }

        public void Build()
        {
            if (built)
                return;

            Transform root = transform.Find("Batch 02 Gameplay Roads");
            if (root != null)
            {
                Debug.LogWarning("[Batch02GameplayRoadOverlay] Gameplay road overlay already exists; leaving it intact.");
                built = true;
                return;
            }

            root = new GameObject("Batch 02 Gameplay Roads").transform;
            root.SetParent(transform, false);

            int segmentCount = 0;
            int registeredCount = 0;

            foreach (RoadPath path in Paths)
            {
                if (path.points == null || path.points.Length < 2)
                    continue;

                BuildRoad(path, root);
                segmentCount += Mathf.Max(0, path.points.Length - 1);
                registeredCount += RegisterMissingCorridors(path);
            }

            built = true;
            Debug.Log("[Batch02GameplayRoadOverlay] Built " + Paths.Length +
                      " collision-only road overlays across " + segmentCount +
                      " segments; registered " + registeredCount +
                      " previously-missing RoadNetwork corridors.");
        }

        private static GameObject BuildRoad(RoadPath path, Transform root)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int i = 0; i < path.points.Length - 1; i++)
            {
                Vector3 a = path.points[i];
                Vector3 b = path.points[i + 1];
                Vector3 direction = b - a;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f)
                    continue;

                direction.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, direction) * (path.width * 0.5f);

                int start = vertices.Count;
                vertices.Add(a - side);
                vertices.Add(a + side);
                vertices.Add(b + side);
                vertices.Add(b - side);

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            var mesh = new Mesh { name = path.id + " Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(path.id);
            go.transform.SetParent(root, false);

            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            return go;
        }

        private static int RegisterMissingCorridors(RoadPath path)
        {
            int registered = 0;

            for (int i = 0; i < path.points.Length - 1; i++)
            {
                Vector3 a = path.points[i];
                Vector3 b = path.points[i + 1];
                Vector3 delta = b - a;
                bool alongX = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
                float fixedCoordinate = alongX ? (a.z + b.z) * 0.5f : (a.x + b.x) * 0.5f;
                float min = alongX ? Mathf.Min(a.x, b.x) : Mathf.Min(a.z, b.z);
                float max = alongX ? Mathf.Max(a.x, b.x) : Mathf.Max(a.z, b.z);

                if (HasEquivalentCorridor(alongX, fixedCoordinate, min, max, path.width))
                    continue;

                RoadNetwork.RegisterCorridor(
                    path.id + "-" + i,
                    alongX,
                    fixedCoordinate,
                    min,
                    max,
                    path.width);
                registered++;
            }

            return registered;
        }

        private static bool HasEquivalentCorridor(
            bool alongX,
            float fixedCoordinate,
            float min,
            float max,
            float width)
        {
            const float tolerance = 0.05f;

            for (int i = 0; i < RoadNetwork.Corridors.Count; i++)
            {
                RoadNetwork.RoadCorridor corridor = RoadNetwork.Corridors[i];
                if (corridor.AlongX != alongX)
                    continue;
                if (Mathf.Abs(corridor.FixedCoordinate - fixedCoordinate) > tolerance)
                    continue;
                if (corridor.Min > min + tolerance || corridor.Max < max - tolerance)
                    continue;
                if (Mathf.Abs(corridor.Width - width) > tolerance)
                    continue;
                return true;
            }

            return false;
        }
    }
}
