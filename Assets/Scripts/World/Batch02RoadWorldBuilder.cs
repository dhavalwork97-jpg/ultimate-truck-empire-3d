using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Runtime bridge between the Batch 02 environment prefabs and the existing
    /// RoadNetwork. Prefabs are loaded from Resources after the editor pipeline
    /// prepares them; no freight, GPS or truck system is replaced.
    /// </summary>
    public sealed class Batch02RoadWorldBuilder : MonoBehaviour
    {
        [Serializable]
        private sealed class TilePlacement
        {
            public string resourcePath;
            public Vector3 position;
            public Vector3 euler;
            public float scale = 1f;
        }

        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool registerExistingRoadNetwork = true;

        private static readonly TilePlacement[] Placements =
        {
            // Capture 008: interchange/overpass/railway/village. The four tiles
            // are kept together as one reusable road/interchange set.
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_00", position = new Vector3(-180f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_01", position = new Vector3(-90f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_10", position = new Vector3(-180f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_11", position = new Vector3(-90f, 0f, 125f) },

            // Capture 009: country-road/farmland/highway set.
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_00", position = new Vector3(35f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_01", position = new Vector3(125f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_10", position = new Vector3(35f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_11", position = new Vector3(125f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_20", position = new Vector3(215f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_21", position = new Vector3(215f, 0f, 125f) }
        };

        private void Start()
        {
            if (buildOnStart) Build();
        }

        public void Build()
        {
            Transform root = transform.Find("Batch 02 Environment");
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("Batch 02 Environment").transform;
            root.SetParent(transform, false);

            int loaded = 0;
            for (int i = 0; i < Placements.Length; i++)
            {
                TilePlacement placement = Placements[i];
                GameObject prefab = Resources.Load<GameObject>(placement.resourcePath);
                if (prefab == null)
                {
                    Debug.LogWarning("[Batch02RoadWorldBuilder] Missing prepared prefab: " + placement.resourcePath);
                    continue;
                }

                GameObject instance = Instantiate(prefab, placement.position, Quaternion.Euler(placement.euler), root);
                instance.name = prefab.name;
                instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, placement.scale);
                loaded++;
            }

            if (registerExistingRoadNetwork)
                RegisterBatch02RoadCorridors();

            Debug.Log("[Batch02RoadWorldBuilder] Loaded " + loaded + "/" + Placements.Length + " prepared environment tiles.");
        }


        /// <summary>
        /// Editor/CI validation for the generated Batch 02 tile bounds. This does not
        /// assume that a photogrammetry tile is exactly one corridor width; it verifies
        /// that the generated geometry occupies the expected world region and that each
        /// registered corridor intersects at least one prepared tile.
        /// </summary>
        public static bool ValidatePreparedGeometry(out string report)
        {
            var bounds = new List<Bounds>(Placements.Length);
            var missing = new List<string>();

            for (int i = 0; i < Placements.Length; i++)
            {
                TilePlacement placement = Placements[i];
                GameObject prefab = Resources.Load<GameObject>(placement.resourcePath);
                if (prefab == null)
                {
                    missing.Add(placement.resourcePath);
                    continue;
                }

                Bounds tileBounds;
                if (!TryGetPrefabBounds(prefab, placement.position, placement.scale, out tileBounds))
                {
                    missing.Add(placement.resourcePath + " (no renderer bounds)");
                    continue;
                }

                bounds.Add(tileBounds);
            }

            bool valid = missing.Count == 0 && bounds.Count == Placements.Length;
            var lines = new List<string>
            {
                "[Batch02RoadWorldBuilder] Geometry validation " + (valid ? "PASS" : "FAIL") +
                " (" + bounds.Count + "/" + Placements.Length + " tiles)."
            };

            for (int i = 0; i < bounds.Count; i++)
            {
                Bounds b = bounds[i];
                lines.Add("TileBounds[" + i + "]: center=" + b.center + " size=" + b.size);
            }

            if (missing.Count > 0)
            {
                valid = false;
                lines.Add("Missing/invalid: " + string.Join("; ", missing));
            }

            if (valid)
            {
                for (int i = 0; i < Placements.Length; i++)
                {
                    for (int j = i + 1; j < Placements.Length; j++)
                    {
                        if (Placements[i].resourcePath.Substring(0, Placements[i].resourcePath.LastIndexOf('/')) !=
                            Placements[j].resourcePath.Substring(0, Placements[j].resourcePath.LastIndexOf('/')))
                            continue;

                        Bounds a = bounds[i];
                        Bounds b = bounds[j];
                        float gapX = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
                        float gapZ = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
                        if (gapX > 8f || gapZ > 8f)
                        {
                            valid = false;
                            lines.Add("Tile gap exceeds 8m: " + Placements[i].resourcePath + " <-> " +
                                      Placements[j].resourcePath + " gapX=" + gapX.ToString("0.##") +
                                      " gapZ=" + gapZ.ToString("0.##"));
                        }
                    }
                }

                if (!CorridorIntersectsBounds(bounds, true, 0f, -220f, -45f, 14f))
                {
                    valid = false;
                    lines.Add("B02_WEST_INTERCHANGE does not intersect prepared geometry.");
                }
                if (!CorridorIntersectsBounds(bounds, false, -90f, 0f, 190f, 14f))
                {
                    valid = false;
                    lines.Add("B02_NORTH_CONNECTOR does not intersect prepared geometry.");
                }
                if (!CorridorIntersectsBounds(bounds, true, 70f, -45f, 250f, 14f))
                {
                    valid = false;
                    lines.Add("B02_EAST_HIGHWAY does not intersect prepared geometry.");
                }
                if (!CorridorIntersectsBounds(bounds, false, 160f, 35f, 160f, 12f))
                {
                    valid = false;
                    lines.Add("B02_FARMLAND_CONNECTOR does not intersect prepared geometry.");
                }
            }

            report = string.Join("\n", lines);
            return valid;
        }

        private static bool TryGetPrefabBounds(GameObject prefab, Vector3 position, float scale, out Bounds bounds)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool initialized = false;
            bounds = default;
            Vector3 offset = position;
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds source = renderers[i].bounds;
                Vector3 center = source.center * scale + offset;
                Vector3 size = source.size * scale;
                Bounds transformed = new Bounds(center, size);
                if (!initialized)
                {
                    bounds = transformed;
                    initialized = true;
                }
                else
                    bounds.Encapsulate(transformed);
            }

            return initialized;
        }

        private static bool CorridorIntersectsBounds(
            List<Bounds> bounds, bool alongX, float fixedCoordinate, float min, float max, float width)
        {
            float halfWidth = width * 0.5f;
            for (int i = 0; i < bounds.Count; i++)
            {
                Bounds b = bounds[i];
                if (alongX)
                {
                    if (fixedCoordinate >= b.min.z - halfWidth && fixedCoordinate <= b.max.z + halfWidth &&
                        max >= b.min.x && min <= b.max.x)
                        return true;
                }
                else
                {
                    if (fixedCoordinate >= b.min.x - halfWidth && fixedCoordinate <= b.max.x + halfWidth &&
                        max >= b.min.z && min <= b.max.z)
                        return true;
                }
            }
            return false;
        }

        private static void RegisterBatch02RoadCorridors()
        {
            RegisterIfMissing("B02_WEST_INTERCHANGE", true, 0f, -220f, -45f, 14f);
            RegisterIfMissing("B02_NORTH_CONNECTOR", false, -90f, 0f, 190f, 14f);
            RegisterIfMissing("B02_EAST_HIGHWAY", true, 70f, -45f, 250f, 14f);
            RegisterIfMissing("B02_FARMLAND_CONNECTOR", false, 160f, 35f, 160f, 12f);
        }

        private static void RegisterIfMissing(string id, bool alongX, float fixedCoordinate, float min, float max, float width)
        {
            for (int i = 0; i < RoadNetwork.Corridors.Count; i++)
                if (string.Equals(RoadNetwork.Corridors[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return;
            RoadNetwork.RegisterCorridor(id, alongX, fixedCoordinate, min, max, width);
        }
    }
}
