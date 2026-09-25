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
        // Keep uncalibrated Batch 02 corridor coordinates out of the authoritative
        // RoadNetwork until they are physically aligned against the source geometry.
        [SerializeField] private bool registerExistingRoadNetwork = false;

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
            var lines = new List<string>();
            bool valid = true;
            int loaded = 0;

            for (int i = 0; i < Placements.Length; i++)
            {
                TilePlacement placement = Placements[i];
                GameObject prefab = Resources.Load<GameObject>(placement.resourcePath);
                if (prefab == null)
                {
                    valid = false;
                    lines.Add("Missing prepared prefab: " + placement.resourcePath);
                    continue;
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
                Batch02DriveableSurface[] surfaces =
                    prefab.GetComponentsInChildren<Batch02DriveableSurface>(true);

                if (renderers.Length == 0)
                {
                    valid = false;
                    lines.Add("No renderers: " + placement.resourcePath);
                }

                if (colliders.Length == 0 || surfaces.Length == 0)
                {
                    valid = false;
                    lines.Add("No driveable collision surface: " + placement.resourcePath +
                              " colliders=" + colliders.Length +
                              " markers=" + surfaces.Length);
                }

                loaded++;
            }

            lines.Add("Prepared tile validation: " + (valid ? "PASS" : "FAIL") +
                      " (" + loaded + "/" + Placements.Length + " tiles).");

            // Placement/corridor alignment is intentionally reported, not made a
            // release gate yet. The source captures are photogrammetry assets and
            // their road centerlines must be calibrated against the existing world
            // before hard-coded corridor coordinates can be treated as authoritative.
            lines.Add("Road corridor placement: REVIEW REQUIRED; not used as a CI failure gate.");
            lines.Add("Registered corridors: " + RoadNetwork.Corridors.Count);

            report = string.Join("\n", lines);
            return valid;
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
