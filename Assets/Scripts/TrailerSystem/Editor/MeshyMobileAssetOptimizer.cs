#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// Meshy production intake utility.
    /// It applies safe mobile import settings and produces a budget report.
    /// It intentionally does not fake LOD geometry: authored LOD meshes remain required.
    public static class MeshyMobileAssetOptimizer
    {
        private const int MaxTextureSize = 1024;
        private const int TargetMaterialSlots = 3;

        [MenuItem("Ultimate Truck Empire/Trailer Assets/Optimize Meshy Mobile Imports")]
        public static void Optimize()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/TrailerSystem/Production/Meshy" });
            int models = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
                importer.importBlendShapes = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importVisibility = false;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
                models++;
            }

            string[] textures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/TrailerSystem/Production/Meshy" });
            int textureCount = 0;
            foreach (string guid in textures)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.maxTextureSize = MaxTextureSize;
                importer.mipmapEnabled = true;
                importer.streamingMipmaps = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                textureCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MeshyMobileAssetOptimizer] Optimized import settings for {models} FBX model(s) and {textureCount} texture(s).");
            Debug.Log($"[MeshyMobileAssetOptimizer] Texture cap: {MaxTextureSize}; material-slot target: {TargetMaterialSlots}; authored 3-LOD geometry remains mandatory.");
        }

        [MenuItem("Ultimate Truck Empire/Trailer Assets/Validate Meshy Mobile Budgets")]
        public static void ValidateBudgets()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/TrailerSystem/Production/Meshy" });
            int checkedPrefabs = 0;
            int warnings = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                checkedPrefabs++;

                MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                int triangles = 0;
                HashSet<Material> materials = new HashSet<Material>();
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh != null)
                        triangles += (int)filter.sharedMesh.GetIndexCount(0) / 3;

                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer != null)
                        foreach (Material material in renderer.sharedMaterials)
                            if (material != null) materials.Add(material);
                }

                if (triangles > 15000)
                {
                    warnings++;
                    Debug.LogWarning($"[MeshyMobileAssetOptimizer] {path}: {triangles:N0} triangles exceeds LOD0 target of 15,000.");
                }

                if (materials.Count > TargetMaterialSlots)
                {
                    warnings++;
                    Debug.LogWarning($"[MeshyMobileAssetOptimizer] {path}: {materials.Count} material slots exceeds target of {TargetMaterialSlots}.");
                }

                LODGroup lod = prefab.GetComponentInChildren<LODGroup>(true);
                if (lod == null || lod.lodCount != 3)
                {
                    warnings++;
                    Debug.LogWarning($"[MeshyMobileAssetOptimizer] {path}: requires an authored 3-level LODGroup.");
                }
            }

            Debug.Log($"[MeshyMobileAssetOptimizer] Checked {checkedPrefabs} Meshy prefab(s); {warnings} production-budget warning(s).");
        }
    }
}
#endif
