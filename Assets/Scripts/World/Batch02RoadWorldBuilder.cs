using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Runtime bridge between the Batch 02 environment prefabs and the existing
    /// RoadNetwork. Prefabs are loaded from Resources after the editor pipeline
    /// prepares them; no freight, GPS or truck system is replaced.
    /// Batch 02 is presentation/background geometry only and never owns gameplay
    /// collision or road corridors.
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

        private static readonly TilePlacement[] Placements =
        {
            // Capture 008: interchange/overpass/railway/village. The four tiles
            // are kept together as one reusable road/interchange presentation set.
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_00", position = new Vector3(-180f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_01", position = new Vector3(-90f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_10", position = new Vector3(-180f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/008/EnvAsset_008_Tile_11", position = new Vector3(-90f, 0f, 125f) },

            // Capture 009: country-road/farmland/highway presentation set.
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_00", position = new Vector3(35f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_01", position = new Vector3(125f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_10", position = new Vector3(35f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_11", position = new Vector3(125f, 0f, 125f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_20", position = new Vector3(215f, 0f, 35f) },
            new TilePlacement { resourcePath = "Batch02/Prefabs/009/EnvAsset_009_Tile_21", position = new Vector3(215f, 0f, 125f) }
        };

        private const string RuntimeRootName = "Batch 02 Environment";

        private void Start()
        {
            if (buildOnStart) Build();
        }

        public void Build()
        {
            Transform root = transform.Find(RuntimeRootName);
            if (root != null)
                DestroyImmediate(root.gameObject);

            root = new GameObject(RuntimeRootName).transform;
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

                // Keep the presentation layer passive. Prepared prefabs are
                // validated in CI, but this runtime guard protects development
                // builds from accidentally importing gameplay components/colliders.
                if (instance.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                {
                    Debug.LogError("[Batch02RoadWorldBuilder] Skipping gameplay validation for environment prefab containing MonoBehaviour: " + prefab.name);
                }

                Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
                if (colliders.Length != 0)
                {
                    Debug.LogError("[Batch02RoadWorldBuilder] Batch 02 environment prefab contains " +
                                    colliders.Length + " collider(s): " + prefab.name +
                                    ". Gameplay collision must remain owned by the existing world.");
                }

                loaded++;
            }

            Debug.Log("[Batch02RoadWorldBuilder] Loaded " + loaded + "/" + Placements.Length +
                      " prepared environment tiles as presentation-only geometry.");
        }

        /// <summary>
        /// Editor/CI validation for the generated Batch 02 presentation tiles.
        /// Batch 02 remains an environment/background layer; the authoritative
        /// gameplay road network owns drivable surfaces and collision.
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
                if (renderers.Length == 0)
                {
                    valid = false;
                    lines.Add("No renderers: " + placement.resourcePath);
                }

                Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
                if (colliders.Length != 0)
                {
                    valid = false;
                    lines.Add("Gameplay colliders are not permitted: " + placement.resourcePath +
                              " (" + colliders.Length + ")");
                }

                MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours.Length != 0)
                {
                    valid = false;
                    lines.Add("Gameplay MonoBehaviours are not permitted: " + placement.resourcePath +
                              " (" + behaviours.Length + ")");
                }

                loaded++;
            }

            lines.Add("Prepared tile validation: " + (valid ? "PASS" : "FAIL") +
                      " (" + loaded + "/" + Placements.Length + " tiles).");

            lines.Add("Gameplay road ownership: existing RoadNetwork remains authoritative; Batch 02 registers no corridors.");

            report = string.Join("\n", lines);
            return valid;
        }
    }
}
