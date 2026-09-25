using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Builds clean gameplay road geometry over the Batch 02 presentation tiles.
    /// The photogrammetry roads remain visual references; this component owns only
    /// the lightweight drive surface, collision and RoadNetwork corridor registration.
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

        private static readonly RoadPath[] Paths =
        {
            new RoadPath
            {
                id = "Batch02-EastWest-Highway",
                width = 11f,
                points = new[]
                {
                    new Vector3(-205f, 0.08f, 80f),
                    new Vector3(-135f, 0.08f, 80f),
                    new Vector3(-45f, 0.08f, 80f),
                    new Vector3(45f, 0.08f, 80f),
                    new Vector3(135f, 0.08f, 80f),
                    new Vector3(225f, 0.08f, 80f),
                    new Vector3(315f, 0.08f, 80f)
                }
            },
            new RoadPath
            {
                id = "Batch02-NorthSouth-Connector",
                width = 9f,
                points = new[]
                {
                    new Vector3(80f, 0.08f, 15f),
                    new Vector3(80f, 0.08f, 65f),
                    new Vector3(80f, 0.08f, 80f),
                    new Vector3(80f, 0.08f, 125f),
                    new Vector3(80f, 0.08f, 155f)
                }
            }
        };

        [SerializeField] private bool buildOnStart = true;

        private void Start()
        {
            if (buildOnStart)
                Build();
        }

        private bool built;

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

            int corridorCount = 0;

            foreach (RoadPath path in Paths)
            {
                if (path.points == null || path.points.Length < 2)
                    continue;

                BuildRoad(path, root);
                RegisterCorridors(path);
                corridorCount += Mathf.Max(0, path.points.Length - 1);
            }

            built = true;
            Debug.Log("[Batch02GameplayRoadOverlay] Built " + Paths.Length +
                      " invisible gameplay road overlays and registered " + corridorCount +
                      " RoadNetwork corridors. Batch 02 environment remains presentation-only.");
        }

        private GameObject BuildRoad(RoadPath path, Transform root)
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

            var mesh = new Mesh
            {
                name = path.id + " Mesh"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(path.id);
            go.transform.SetParent(root, false);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            return go;
        }

        private static void RegisterCorridors(RoadPath path)
        {
            for (int i = 0; i < path.points.Length - 1; i++)
            {
                Vector3 a = path.points[i];
                Vector3 b = path.points[i + 1];
                Vector3 delta = b - a;
                bool alongX = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);

                if (alongX)
                {
                    RoadNetwork.RegisterCorridor(
                        path.id + "-" + i,
                        true,
                        (a.z + b.z) * 0.5f,
                        Mathf.Min(a.x, b.x),
                        Mathf.Max(a.x, b.x),
                        path.width);
                }
                else
                {
                    RoadNetwork.RegisterCorridor(
                        path.id + "-" + i,
                        false,
                        (a.x + b.x) * 0.5f,
                        Mathf.Min(a.z, b.z),
                        Mathf.Max(a.z, b.z),
                        path.width);
                }
            }
        }

    }
}
