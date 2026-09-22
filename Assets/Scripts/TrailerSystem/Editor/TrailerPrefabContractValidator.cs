#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// <summary>
    /// Production-time validation for trailer prefabs referenced by TrailerDefinition assets.
    /// Does not alter prefab geometry; it verifies the runtime contract expected by the trailer system.
    /// </summary>
    public static class TrailerPrefabContractValidator
    {
        private const string CargoSocketName = "CargoSocket";
        private const string KingpinName = "Kingpin";
        private const string SocketsName = "Sockets";

        [MenuItem("Ultimate Truck Empire/Trailer System/Validate Trailer Prefabs")]
        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:TrailerCatalogAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[TrailerSystem] No TrailerCatalogAsset found.");
                return;
            }

            int errors = 0;
            int warnings = 0;
            int checkedPrefabs = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TrailerCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<TrailerCatalogAsset>(path);
                if (catalog == null || catalog.trailers == null) continue;

                for (int t = 0; t < catalog.trailers.Count; t++)
                {
                    TrailerDefinition definition = catalog.trailers[t];
                    if (definition == null || definition.prefab == null) continue;

                    checkedPrefabs++;
                    ValidatePrefab(definition, ref errors, ref warnings);
                }
            }

            Debug.Log("[TrailerSystem] Trailer prefab validation complete. Prefabs: " +
                checkedPrefabs + ", errors: " + errors + ", warnings: " + warnings + ".");
        }

        private static void ValidatePrefab(TrailerDefinition definition, ref int errors, ref int warnings)
        {
            string path = AssetDatabase.GetAssetPath(definition.prefab);
            if (string.IsNullOrEmpty(path))
            {
                Error(definition, "Prefab path could not be resolved.", ref errors);
                return;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    Error(definition, "Prefab could not be opened: " + path, ref errors);
                    return;
                }

                if (root.GetComponent<LoadedTrailer>() == null)
                    Error(definition, "Missing LoadedTrailer component.", ref errors);

                if (root.GetComponent<TrailerCargoModule>() == null)
                    Error(definition, "Missing TrailerCargoModule component.", ref errors);

                if (root.GetComponent<TrailerSkinApplier>() == null)
                    Error(definition, "Missing TrailerSkinApplier component.", ref errors);

                LODGroup lodGroup = root.GetComponentInChildren<LODGroup>(true);
                if (lodGroup == null)
                {
                    Error(definition, "Missing LODGroup.", ref errors);
                }
                else
                {
                    ValidateLods(definition, lodGroup, ref errors, ref warnings);
                }

                Transform cargoSocket = FindChild(root.transform, CargoSocketName);
                Transform kingpin = FindChild(root.transform, KingpinName);
                Transform sockets = FindChild(root.transform, SocketsName);

                if (cargoSocket == null)
                    Error(definition, "Missing required CargoSocket transform.", ref errors);
                if (kingpin == null)
                    Error(definition, "Missing required Kingpin transform.", ref errors);
                if (sockets == null)
                    Warn(definition, "No Sockets parent found; socket hierarchy convention is recommended.", ref warnings);

                if (definition.wheelSockets == null || definition.wheelSockets.Length < 4)
                    Warn(definition, "Definition has fewer than 4 wheel sockets assigned.", ref warnings);

                ValidateRenderBudget(definition, root, lodGroup, ref errors, ref warnings);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateLods(TrailerDefinition definition, LODGroup lodGroup,
            ref int errors, ref int warnings)
        {
            LOD[] lods = lodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
            {
                Error(definition, "LODGroup contains no LOD levels.", ref errors);
                return;
            }

            if (lods.Length != definition.lodCount)
                Warn(definition, "Definition requests " + definition.lodCount +
                    " LOD levels but prefab has " + lods.Length + ".", ref warnings);

            for (int i = 0; i < lods.Length; i++)
            {
                if (lods[i].renderers == null || lods[i].renderers.Length == 0)
                    Warn(definition, "LOD" + i + " contains no renderers.", ref warnings);
            }
        }

        private static void ValidateRenderBudget(TrailerDefinition definition, GameObject root, LODGroup lodGroup,
            ref int errors, ref int warnings)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            var materials = new HashSet<Material>();
            long triangles = 0;
            int maxTexture = 0;
            var lod0Renderers = new HashSet<Renderer>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods != null && lods.Length > 0 && lods[0].renderers != null)
                    for (int i = 0; i < lods[0].renderers.Length; i++)
                        if (lods[0].renderers[i] != null) lod0Renderers.Add(lods[0].renderers[i]);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] sharedMaterials = renderer.sharedMaterials;

                for (int m = 0; m < sharedMaterials.Length; m++)
                {
                    Material material = sharedMaterials[m];
                    if (material == null) continue;
                    materials.Add(material);

                    Texture texture = GetMainTexture(material);
                    if (texture is Texture2D texture2D)
                        maxTexture = Mathf.Max(maxTexture, Mathf.Max(texture2D.width, texture2D.height));
                }

                if (!lod0Renderers.Contains(renderer)) continue;

                MeshFilter meshFilter = renderer as MeshRenderer != null
                    ? renderer.GetComponent<MeshFilter>()
                    : null;
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    triangles += meshFilter.sharedMesh.triangles.Length / 3;

                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null && skinned.sharedMesh != null)
                    triangles += skinned.sharedMesh.triangles.Length / 3;
            }

            if (triangles > definition.targetTriangles)
                Error(definition, "LOD0/baseline mesh triangle budget exceeded: " +
                    triangles + " > " + definition.targetTriangles + ".", ref errors);

            if (materials.Count > definition.materialSlotBudget)
                Error(definition, "Material budget exceeded: " + materials.Count +
                    " unique materials > " + definition.materialSlotBudget + ".", ref errors);

            if (maxTexture > definition.maxTextureResolution)
                Error(definition, "Texture resolution budget exceeded: " + maxTexture +
                    " > " + definition.maxTextureResolution + ".", ref errors);

            if (renderers.Length == 0)
                Error(definition, "Prefab contains no renderers.", ref errors);
        }

        private static Texture GetMainTexture(Material material)
        {
            if (material.HasProperty("_BaseMap"))
                return material.GetTexture("_BaseMap");
            if (material.HasProperty("_MainTex"))
                return material.GetTexture("_MainTex");
            return null;
        }

        private static Transform FindChild(Transform root, string targetName)
        {
            if (root == null) return null;
            if (root.name == targetName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), targetName);
                if (result != null) return result;
            }

            return null;
        }

        private static void Error(TrailerDefinition definition, string message, ref int errors)
        {
            errors++;
            Debug.LogError("[TrailerSystem] " + definition.id + ": " + message, definition);
        }

        private static void Warn(TrailerDefinition definition, string message, ref int warnings)
        {
            warnings++;
            Debug.LogWarning("[TrailerSystem] " + definition.id + ": " + message, definition);
        }
    }
}
#endif
