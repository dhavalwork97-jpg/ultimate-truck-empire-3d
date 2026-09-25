#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.EnvironmentAssets
{
    /// <summary>
    /// Editor-only import/preparation pipeline for the Batch 02 photogrammetry tiles.
    /// It owns environment presentation assets only; gameplay/world systems remain authoritative.
    /// </summary>
    public sealed class EnvironmentAssetBatch02ImportPipeline : AssetPostprocessor
    {
        private const string SourceRoot = "Assets/Environment/Batch02/Source/";
        private const string PrefabRoot = "Assets/Environment/Batch02/Resources/Batch02/Prefabs/";
        private const string MaterialRoot = "Assets/Environment/Batch02/Materials/";

        private static bool IsBatch02Model(string path)
        {
            return path.StartsWith(SourceRoot, StringComparison.OrdinalIgnoreCase) &&
                (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBatch02Texture(string path)
        {
            return path.StartsWith(SourceRoot, StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        void OnPreprocessModel()
        {
            if (!IsBatch02Model(assetPath)) return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
        }

        void OnPreprocessTexture()
        {
            if (!IsBatch02Texture(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.streamingMipmaps = true;
            importer.sRGBTexture = true;
        }

        [MenuItem("Ultimate Truck Empire/Environment/Batch 02/Prepare All Environment Tiles")]
        public static void PrepareAllEnvironmentTiles()
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(PrefabRoot + "008");
            EnsureFolder(PrefabRoot + "009");
            EnsureFolder(MaterialRoot);

            AssetDatabase.ImportAsset(SourceRoot, ImportAssetOptions.ForceUpdate);

            int prepared = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { SourceRoot }))
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsBatch02Model(modelPath)) continue;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model != null && PreparePrefab(modelPath, model))
                    prepared++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnvironmentBatch02] Prepared " + prepared + " environment prefab(s).");
        }

        [MenuItem("Ultimate Truck Empire/Environment/Batch 02/Validate Prepared Assets")]
        public static void ValidatePreparedAssets()
        {
            bool valid = Validate();
            if (!valid)
                throw new InvalidOperationException("[EnvironmentBatch02] Validation failed.");
            Debug.Log("[EnvironmentBatch02] Validation PASS.");
        }

        public static void PrepareAndValidateHeadless()
        {
            try
            {
                PrepareAllEnvironmentTiles();
                bool valid = Validate();
                string geometryReport;
                bool geometryValid = UltimateTruckEmpire.World.Batch02RoadWorldBuilder.ValidatePreparedGeometry(out geometryReport);
                Debug.Log(geometryReport);
                valid &= geometryValid;
                Debug.Log("[EnvironmentBatch02] Headless validation: " + (valid ? "PASS" : "FAIL"));
                EditorApplication.Exit(valid ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static bool PreparePrefab(string modelPath, GameObject model)
        {
            string capture = CaptureForPath(modelPath);
            string fileName = Path.GetFileNameWithoutExtension(modelPath);
            string prefabPath = PrefabRoot + capture + "/" + fileName + ".prefab";

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) return false;

            try
            {
                instance.name = fileName;
                MarkStatic(instance);
                ApplySharedMaterial(instance, capture);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                return prefab != null && ValidatePrefab(prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ApplySharedMaterial(GameObject root, string capture)
        {
            string materialPath = MaterialRoot + "EnvironmentBatch02_" + capture + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("[EnvironmentBatch02] Built-in Standard shader was not found.");

                material = new Material(shader) { name = "EnvironmentBatch02_" + capture };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            string texturePath = SourceRoot + capture + "/EnvAsset_" + capture + "_Albedo.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
                material.mainTexture = texture;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                int count = source != null && source.Length > 0 ? source.Length : 1;
                var materials = new Material[count];
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                renderer.sharedMaterials = materials;
            }

            EditorUtility.SetDirty(material);
        }

        private static bool Validate()
        {
            bool valid = true;
            int prefabCount = 0;

            foreach (string capture in new[] { "008", "009" })
            {
                string root = PrefabRoot + capture;
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
                int expected = capture == "008" ? 4 : 6;

                if (guids.Length != expected)
                {
                    Debug.LogError("[EnvironmentBatch02] " + capture + " expected " + expected +
                                   " prefab(s), found " + guids.Length + ".");
                    valid = false;
                }

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    valid &= ValidatePrefab(prefab);
                    prefabCount++;
                }
            }

            Debug.Log("[EnvironmentBatch02] Validated " + prefabCount + " prefab(s).");
            return valid;
        }

        private static bool ValidatePrefab(GameObject prefab)
        {
            if (prefab == null) return false;

            bool valid = true;
            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogError("[EnvironmentBatch02] Prefab has no renderers: " + prefab.name);
                valid = false;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                {
                    Debug.LogError("[EnvironmentBatch02] Renderer has no material: " + prefab.name);
                    valid = false;
                }
            }

            foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(behaviour is UltimateTruckEmpire.World.Batch02DriveableSurface))
                {
                    Debug.LogError("[EnvironmentBatch02] Environment prefab contains unexpected MonoBehaviour: " +
                                   behaviour.GetType().FullName + " on " + prefab.name);
                    valid = false;
                }
            }

            return valid;
        }

        private static void RemoveUnwantedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider, true);
        }

        private static void AddDriveableSurfaceColliders(GameObject root)
        {
            int surfaceCount = 0;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                var meshObject = meshFilter.gameObject;
                var meshCollider = meshObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = meshObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;

                if (meshObject.GetComponent<UltimateTruckEmpire.World.Batch02DriveableSurface>() == null)
                    meshObject.AddComponent<UltimateTruckEmpire.World.Batch02DriveableSurface>();

                surfaceCount++;
            }

            if (surfaceCount == 0)
                throw new InvalidOperationException("[EnvironmentBatch02] No mesh surfaces found for driveable collision generation: " + root.name);
        }

        private static void MarkStatic(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(
                    transform.gameObject,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic);
        }

        private static string CaptureForPath(string path)
        {
            string normalized = path.Replace('\\', '/');
            int marker = normalized.IndexOf("/Source/", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
                throw new InvalidOperationException("[EnvironmentBatch02] Invalid source path: " + path);

            string remainder = normalized.Substring(marker + "/Source/".Length);
            int slash = remainder.IndexOf('/');
            if (slash <= 0)
                throw new InvalidOperationException("[EnvironmentBatch02] Missing capture directory: " + path);

            return remainder.Substring(0, slash);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.TrimEnd('/').Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
