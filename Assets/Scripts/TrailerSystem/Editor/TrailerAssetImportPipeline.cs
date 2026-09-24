#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// <summary>
    /// Production-safe import bridge for externally authored trailer models.
    /// It never downloads or embeds third-party assets; it only prepares models already
    /// present in the Unity project and registers them with the existing trailer catalog.
    /// </summary>
    public sealed class TrailerAssetImportPipeline : AssetPostprocessor
    {
        private const string ImportRoot = "Assets/TrailerSystem/Imports/Trailers/";

        private static bool IsTrailerImportPath(string path)
        {
            return path.StartsWith(ImportRoot, StringComparison.OrdinalIgnoreCase) &&
                   (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));
        }

        void OnPreprocessModel()
        {
            if (!IsTrailerImportPath(assetPath))
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ImportRoot, StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool dataTexture = file.Contains("_metallic") || file.Contains("_roughness") || file.Contains("_normal");
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.sRGBTexture = !dataTexture;
            if (file.Contains("_normal"))
                importer.textureType = TextureImporterType.NormalMap;
        }

        [MenuItem("Ultimate Truck Empire/Trailer System/Prepare Selected Imported Trailer")]
        public static void PrepareSelected()
        {
            var selected = Selection.activeObject as GameObject;
            if (selected == null)
            {
                Debug.LogWarning("[TrailerImport] Select an imported model asset first.");
                return;
            }

            string modelPath = AssetDatabase.GetAssetPath(selected);
            if (!IsTrailerImportPath(modelPath))
            {
                Debug.LogWarning("[TrailerImport] Selected asset must live under " + ImportRoot + ".");
                return;
            }

            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError("[TrailerImport] Could not load imported model: " + modelPath);
                return;
            }

            string id = ProductionIdForSource(modelPath);
            string prefabPath = "Assets/TrailerSystem/Prefabs/Imported/" + id + ".prefab";
            EnsureFolder("Assets/TrailerSystem/Prefabs/Imported");

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[TrailerImport] Could not instantiate imported model: " + modelPath);
                return;
            }

            try
            {
                instance.name = id;
                AddRuntimeContract(instance);
                CreateAuthoredPlaceholderSockets(instance);
                CreateGeneratedPhysicsProxy(instance);
                AddProductionLodGroup(instance);
                AssignProductionMaterial(instance, modelPath, id);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[TrailerImport] Failed to create prefab: " + prefabPath);
                    return;
                }

                RegisterOrCreateDefinition(id, prefab);
                RegisterDefinitionInCatalog(id);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;

                Debug.Log("[TrailerImport] Prepared trailer '" + id +
                    "'. Replace generated sockets/collider with authored production data before shipping.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Headless CI entry point. Runs the same production preparation used by the Editor menu,
        /// validates the generated assets, and returns a non-zero Unity process exit code on failure.
        /// </summary>
        public static void PrepareAllImportedBatch()
        {
            try
            {
                PrepareAllImported();
                bool valid = ValidatePreparedProductionAssets();
                Debug.Log("[TrailerImport] Headless production preparation validation: " + (valid ? "PASS" : "FAIL"));
                EditorApplication.Exit(valid ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        [Serializable]
        private sealed class ProductionValidationReport
        {
            public string generatedUtc;
            public List<ProductionValidationEntry> trailers = new List<ProductionValidationEntry>();
        }

        [Serializable]
        private sealed class ProductionValidationEntry
        {
            public string productionId;
            public string sourceModel;
            public int rendererCount;
            public int materialSlotCount;
            public int triangleCount;
            public int vertexCount;
            public Vector3 boundsSize;
            public bool hasReducedLods;
            public bool runtimeContractValid;
            public bool colliderValid;
            public bool definitionValid;
            public int lod0TriangleCount;
            public int lod1TriangleCount;
            public int lod2TriangleCount;
            public bool lod1WithinBudget;
            public bool lod2WithinBudget;
        }

        private static bool ValidatePreparedProductionAssets()
        {
            bool valid = true;
            var report = new ProductionValidationReport { generatedUtc = DateTime.UtcNow.ToString("O") };
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ImportRoot.TrimEnd('/') });
            int checkedModels = 0;
            foreach (string guid in guids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsTrailerImportPath(modelPath)) continue;
                checkedModels++;
                string id = ProductionIdForSource(modelPath);
                string prefabPath = "Assets/TrailerSystem/Prefabs/Imported/" + id + ".prefab";
                string definitionPath = "Assets/TrailerSystem/Data/Trailers/" + id + ".asset";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(definitionPath);
                if (prefab == null || definition == null)
                {
                    Debug.LogError("[TrailerImport] Missing production asset(s) for " + id);
                    valid = false;
                    continue;
                }

                var reportEntry = BuildValidationEntry(id, modelPath, prefab);
                report.trailers.Add(reportEntry);
                Debug.Log("[TrailerImport] " + id + " metrics: renderers=" + reportEntry.rendererCount +
                    ", materials=" + reportEntry.materialSlotCount + ", triangles=" + reportEntry.triangleCount +
                    ", vertices=" + reportEntry.vertexCount + ", LOD0=" + reportEntry.lod0TriangleCount +
                    ", LOD1=" + reportEntry.lod1TriangleCount + ", LOD2=" + reportEntry.lod2TriangleCount +
                    ", reducedLods=" + reportEntry.hasReducedLods);
                if (!reportEntry.lod1WithinBudget || !reportEntry.lod2WithinBudget)
                {
                    Debug.LogError("[TrailerImport] Reduced LOD triangle budget exceeded for " + id);
                    valid = false;
                }
                if (prefab.GetComponent<LoadedTrailer>() == null ||
                    prefab.GetComponent<TrailerCargoModule>() == null ||
                    prefab.GetComponent<TrailerSkinApplier>() == null)
                {
                    Debug.LogError("[TrailerImport] Runtime contract missing on " + id);
                    valid = false;
                }
                if (prefab.GetComponentInChildren<LODGroup>(true) == null)
                {
                    Debug.LogError("[TrailerImport] LODGroup missing on " + id);
                    valid = false;
                }
                if (!prefab.GetComponentsInChildren<Collider>(true).Any(col => col != null && !col.isTrigger))
                {
                    Debug.LogError("[TrailerImport] No non-trigger collider found on " + id);
                    valid = false;
                }
                if (definition.prefab != prefab)
                {
                    Debug.LogError("[TrailerImport] Definition prefab reference mismatch for " + id);
                    valid = false;
                }
            }
            if (checkedModels == 0)
            {
                Debug.LogError("[TrailerImport] No imported trailer models found under " + ImportRoot);
                return false;
            }
            string reportDir = "Assets/TrailerSystem/Production/Reports";
            EnsureFolder(reportDir);
            string reportPath = reportDir + "/Batch01Validation.json";
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            return valid;
        }

        private static ProductionValidationEntry BuildValidationEntry(string id, string modelPath, GameObject prefab)
        {
            var entry = new ProductionValidationEntry
            {
                productionId = id,
                sourceModel = modelPath,
                runtimeContractValid = prefab.GetComponent<LoadedTrailer>() != null &&
                    prefab.GetComponent<TrailerCargoModule>() != null &&
                    prefab.GetComponent<TrailerSkinApplier>() != null,
                colliderValid = prefab.GetComponentsInChildren<Collider>(true).Any(col => col != null && !col.isTrigger),
                definitionValid = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(
                    "Assets/TrailerSystem/Data/Trailers/" + id + ".asset")?.prefab == prefab
            };

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            entry.rendererCount = renderers.Length;
            entry.materialSlotCount = renderers.Sum(r => r.sharedMaterials?.Length ?? 0);
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    entry.triangleCount += skinned.sharedMesh.triangles.Length / 3;
                    entry.vertexCount += skinned.sharedMesh.vertexCount;
                }
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        entry.triangleCount += meshFilter.sharedMesh.triangles.Length / 3;
                        entry.vertexCount += meshFilter.sharedMesh.vertexCount;
                    }
                }
            }

            var lodGroups = prefab.GetComponentsInChildren<LODGroup>(true);
            if (lodGroups.Length > 0)
            {
                var lods = lodGroups[0].GetLODs();
                entry.lod0TriangleCount = CountLodTriangles(lods, 0);
                entry.lod1TriangleCount = CountLodTriangles(lods, 1);
                entry.lod2TriangleCount = CountLodTriangles(lods, 2);
                entry.lod1WithinBudget = entry.lod1TriangleCount <= 15000;
                entry.lod2WithinBudget = entry.lod2TriangleCount <= 15000;
            }
            entry.hasReducedLods = lodGroups.Any(g =>
            {
                var lods = g.GetLODs();
                if (lods.Length < 2) return false;
                var first = new HashSet<Renderer>(lods[0].renderers);
                return lods.Skip(1).Any(l => l.renderers.Any(r => r != null && !first.Contains(r)));
            });

            Bounds bounds = new Bounds(prefab.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (var renderer in renderers)
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            entry.boundsSize = hasBounds ? bounds.size : Vector3.zero;
            return entry;
        }

        [MenuItem("Ultimate Truck Empire/Trailer System/Prepare All Imported Trailers")]
        public static void PrepareAllImported()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ImportRoot.TrimEnd('/') });
            int prepared = 0;
            int skipped = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsTrailerImportPath(modelPath))
                    continue;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    skipped++;
                    Debug.LogWarning("[TrailerImport] Could not load model: " + modelPath);
                    continue;
                }

                if (PrepareImportedModel(modelPath, model))
                    prepared++;
                else
                    skipped++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrailerImport] Batch preparation complete. Prepared: " + prepared + ", skipped: " + skipped + ".");
        }

        private static bool PrepareImportedModel(string modelPath, GameObject model)
        {
            string id = ProductionIdForSource(modelPath);
            if (string.IsNullOrEmpty(id))
                return false;

            string prefabPath = "Assets/TrailerSystem/Prefabs/Imported/" + id + ".prefab";
            EnsureFolder("Assets/TrailerSystem/Prefabs/Imported");

            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[TrailerImport] Could not instantiate imported model: " + modelPath);
                return false;
            }

            try
            {
                instance.name = id;
                AddRuntimeContract(instance);
                CreateAuthoredPlaceholderSockets(instance);
                CreateGeneratedPhysicsProxy(instance);
                AddProductionLodGroup(instance);
                AssignProductionMaterial(instance, modelPath, id);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[TrailerImport] Failed to create prefab: " + prefabPath);
                    return false;
                }

                RegisterOrCreateDefinition(id, prefab);
                RegisterDefinitionInCatalog(id);
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [MenuItem("Ultimate Truck Empire/Trailer System/Prepare Selected Imported Trailer", true)]
        private static bool ValidatePrepareSelected()
        {
            var selected = Selection.activeObject as GameObject;
            if (selected == null)
                return false;

            return IsTrailerImportPath(AssetDatabase.GetAssetPath(selected));
        }

        private static void AddRuntimeContract(GameObject root)
        {
            if (root.GetComponent<LoadedTrailer>() == null)
                root.AddComponent<LoadedTrailer>();
            if (root.GetComponent<TrailerCargoModule>() == null)
                root.AddComponent<TrailerCargoModule>();
            if (root.GetComponent<TrailerSkinApplier>() == null)
                root.AddComponent<TrailerSkinApplier>();
        }

        private static void CreateAuthoredPlaceholderSockets(GameObject root)
        {
            Transform sockets = FindDirectChild(root.transform, "Sockets") ??
                                CreateChild(root.transform, "Sockets");

            EnsureChild(sockets, "CargoSocket");
            EnsureChild(sockets, "Kingpin");
            EnsureChild(sockets, "Wheel_FL");
            EnsureChild(sockets, "Wheel_FR");
            EnsureChild(sockets, "Wheel_RL");
            EnsureChild(sockets, "Wheel_RR");

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 size = root.transform.InverseTransformVector(bounds.size);
            float halfX = Mathf.Abs(size.x) * 0.45f;
            float halfZ = Mathf.Abs(size.z) * 0.42f;
            float y = center.y - Mathf.Abs(size.y) * 0.38f;
            FindDirectChild(sockets, "CargoSocket").localPosition = center + Vector3.up * (Mathf.Abs(size.y) * 0.05f);
            FindDirectChild(sockets, "Kingpin").localPosition = center + new Vector3(0f, -Mathf.Abs(size.y) * 0.25f, halfZ);
            FindDirectChild(sockets, "Wheel_FL").localPosition = center + new Vector3(-halfX, y - center.y, -halfZ);
            FindDirectChild(sockets, "Wheel_FR").localPosition = center + new Vector3(halfX, y - center.y, -halfZ);
            FindDirectChild(sockets, "Wheel_RL").localPosition = center + new Vector3(-halfX, y - center.y, halfZ);
            FindDirectChild(sockets, "Wheel_RR").localPosition = center + new Vector3(halfX, y - center.y, halfZ);
        }

        private static void AssignProductionMaterial(GameObject root, string modelPath, string id)
        {
            string directory = Path.GetDirectoryName(modelPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(directory)) return;

            string source = Path.GetFileNameWithoutExtension(modelPath);
            string baseName = source;
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + baseName + ".png");
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + baseName + "_metallic.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + baseName + "_normal.png");
            Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + baseName + "_roughness.png");
            if (baseColor == null && metallic == null && normal == null && roughness == null)
            {
                Debug.LogWarning("[TrailerImport] No matching PBR textures found for " + modelPath);
                return;
            }

            string materialFolder = "Assets/TrailerSystem/Production/Materials";
            EnsureFolder(materialFolder);
            string materialPath = materialFolder + "/" + id + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) return;
                material = new Material(shader) { name = id + "_PBR" };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            if (baseColor != null && material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
            else if (baseColor != null && material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (metallic != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic != null ? 1f : 0.2f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.55f);
            if (roughness != null)
                Debug.Log("[TrailerImport] Roughness map retained at 1024 source resolution; automatic roughness-to-smoothness packing is intentionally deferred.");

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0) slots = new Material[1];
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
            EditorUtility.SetDirty(material);
        }

        private static void AddProductionLodGroup(GameObject root)
        {
            var sourceRenderers = root.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null)
                .ToArray();
            if (sourceRenderers.Length == 0) return;

            var lodHost = EnsureChild(root.transform, "LODGroup");
            var group = lodHost.GetComponent<LODGroup>();
            if (group == null) group = lodHost.gameObject.AddComponent<LODGroup>();

            // Build genuine reduced meshes from the imported geometry. This is intentionally
            // deterministic and editor-only: production LOD meshes are generated once and
            // stored as .asset meshes, while the original imported renderers remain LOD0.
            var lod1Renderers = CreateReducedLodRenderers(lodHost, sourceRenderers, 0.45f, "LOD1");
            var lod2Renderers = CreateReducedLodRenderers(lodHost, sourceRenderers, 0.18f, "LOD2");

            var lod0 = new LOD(0.60f, sourceRenderers.Cast<Renderer>().ToArray());
            var lod1 = new LOD(0.25f, lod1Renderers);
            var lod2 = new LOD(0.08f, lod2Renderers);
            group.SetLODs(new[] { lod0, lod1, lod2 });
            group.RecalculateBounds();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = false;
            EditorUtility.SetDirty(group);
        }

        private static Renderer[] CreateReducedLodRenderers(Transform lodHost, MeshRenderer[] sourceRenderers, float ratio, string lodName)
        {
            var container = EnsureChild(lodHost, lodName);
            var output = new List<Renderer>();
            string meshFolder = "Assets/TrailerSystem/Production/Meshes/" + lodHost.parent.name;
            EnsureFolder(meshFolder);

            foreach (var sourceRenderer in sourceRenderers)
            {
                var sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
                var sourceMesh = sourceFilter.sharedMesh;
                string meshPath = meshFolder + "/" + sourceMesh.name + "_" + lodName + ".asset";
                var reduced = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (reduced == null)
                {
                    reduced = ReduceMeshByVertexClustering(sourceMesh, ratio);
                    reduced.name = sourceMesh.name + "_" + lodName;
                    AssetDatabase.CreateAsset(reduced, meshPath);
                }

                var go = new GameObject(sourceRenderer.name + "_" + lodName);
                go.transform.SetParent(container, false);
                go.transform.localPosition = sourceRenderer.transform.localPosition;
                go.transform.localRotation = sourceRenderer.transform.localRotation;
                go.transform.localScale = sourceRenderer.transform.localScale;

                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = reduced;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                output.Add(renderer);
            }
            return output.ToArray();
        }

        private static Mesh ReduceMeshByVertexClustering(Mesh source, float targetRatio)
        {
            var srcVertices = source.vertices;
            var srcTriangles = source.triangles;
            if (srcVertices.Length < 64 || srcTriangles.Length < 96)
                return UnityEngine.Object.Instantiate(source);

            int targetVertices = Mathf.Clamp(Mathf.RoundToInt(srcVertices.Length * targetRatio), 32, srcVertices.Length - 1);
            Bounds bounds = source.bounds;
            Vector3 size = bounds.size;
            float maxSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (maxSize <= Mathf.Epsilon) return UnityEngine.Object.Instantiate(source);

            float low = maxSize / 100000f;
            float high = maxSize;
            int[] bestMap = null;
            int bestCount = srcVertices.Length;

            for (int iteration = 0; iteration < 20; iteration++)
            {
                float cell = (low + high) * 0.5f;
                var map = BuildClusterMap(srcVertices, bounds.min, cell, out int count);
                if (count > targetVertices)
                    low = cell;
                else
                {
                    high = cell;
                    bestMap = map;
                    bestCount = count;
                }
            }

            if (bestMap == null)
                bestMap = BuildClusterMap(srcVertices, bounds.min, high, out bestCount);

            var sums = new Vector3[bestCount];
            var counts = new int[bestCount];
            for (int i = 0; i < srcVertices.Length; i++)
            {
                int c = bestMap[i];
                sums[c] += srcVertices[i];
                counts[c]++;
            }

            var vertices = new Vector3[bestCount];
            for (int i = 0; i < bestCount; i++)
                vertices[i] = sums[i] / Mathf.Max(1, counts[i]);

            var triangles = new List<int>(srcTriangles.Length);
            for (int i = 0; i < srcTriangles.Length; i += 3)
            {
                int a = bestMap[srcTriangles[i]];
                int b = bestMap[srcTriangles[i + 1]];
                int c = bestMap[srcTriangles[i + 2]];
                if (a == b || b == c || a == c) continue;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }

            var result = new Mesh();
            result.indexFormat = vertices.Length > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            result.vertices = vertices;
            result.triangles = triangles.ToArray();
            result.RecalculateBounds();
            result.RecalculateNormals();
            return result;
        }

        private static int[] BuildClusterMap(Vector3[] vertices, Vector3 origin, float cell, out int clusterCount)
        {
            var clusters = new Dictionary<Vector3Int, int>();
            var map = new int[vertices.Length];
            clusterCount = 0;
            float inv = 1f / Mathf.Max(cell, 0.000001f);
            for (int i = 0; i < vertices.Length; i++)
            {
                var key = new Vector3Int(
                    Mathf.FloorToInt((vertices[i].x - origin.x) * inv),
                    Mathf.FloorToInt((vertices[i].y - origin.y) * inv),
                    Mathf.FloorToInt((vertices[i].z - origin.z) * inv));
                if (!clusters.TryGetValue(key, out int cluster))
                {
                    cluster = clusterCount++;
                    clusters.Add(key, cluster);
                }
                map[i] = cluster;
            }
            return map;
        }

        private static int CountLodTriangles(LOD[] lods, int index)
        {
            if (lods == null || index < 0 || index >= lods.Length) return 0;
            int total = 0;
            foreach (var renderer in lods[index].renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                    total += skinned.sharedMesh.triangles.Length / 3;
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        total += filter.sharedMesh.triangles.Length / 3;
                }
            }
            return total;
        }

        private static void CreateGeneratedPhysicsProxy(GameObject root)
        {
            if (root.GetComponentsInChildren<Collider>(true).Any(c => c != null && !c.isTrigger))
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[TrailerImport] No renderers found; physics proxy was not generated.");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var proxy = root.AddComponent<BoxCollider>();
            proxy.name = "GeneratedPhysicsProxy";
            proxy.center = root.transform.InverseTransformPoint(bounds.center);
            proxy.size = bounds.size;
            Debug.LogWarning("[TrailerImport] GeneratedPhysicsProxy created. It is an integration placeholder, not final collision geometry.");
        }

        private static void RegisterOrCreateDefinition(string id, GameObject prefab)
        {
            string path = "Assets/TrailerSystem/Data/Trailers/" + id + ".asset";
            EnsureFolder("Assets/TrailerSystem/Data/Trailers");

            var definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TrailerDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.id = id;
            definition.displayName = ToDisplayName(id);
            definition.category = InferCategory(id);
            definition.manufacturer = "UTE Trailers";
            definition.prefab = prefab;
            definition.payloadCapacityTons = definition.category == TrailerCategory.FuelTanker ? 30f : 30f;
            definition.emptyWeightTons = definition.category == TrailerCategory.FuelTanker ? 9f : 7f;
            definition.purchasePrice = definition.category == TrailerCategory.FuelTanker ? 180000f : 100000f;
            definition.axleCount = definition.category == TrailerCategory.DryVan ? 2 : 3;
            definition.hazmat = definition.category == TrailerCategory.FuelTanker;
            definition.trafficSpawnWeight = definition.category == TrailerCategory.FuelTanker ? 0.35f : 1f;
            definition.maintenanceCostMultiplier = definition.category == TrailerCategory.FuelTanker ? 1.25f : 1f;
            definition.targetTriangles = 15000;
            definition.materialSlotBudget = 3;
            definition.maxTextureResolution = 1024;
            definition.lodCount = 3;
            var root = prefab != null ? prefab.transform : null;
            definition.cargoSocket = FindChild(root, "CargoSocket");
            definition.kingpinSocket = FindChild(root, "Kingpin");
            var sockets = FindChild(root, "Sockets");
            string[] names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
            var wheels = new Transform[names.Length];
            for (int i = 0; i < names.Length; i++)
                wheels[i] = FindDirectChild(sockets, names[i]);
            definition.wheelSockets = wheels;
            EditorUtility.SetDirty(definition);
        }

        private static void RegisterDefinitionInCatalog(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(
                "Assets/TrailerSystem/Data/Trailers/" + id + ".asset");
            if (definition == null) return;

            string[] guids = AssetDatabase.FindAssets("t:TrailerCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<TrailerCatalogAsset>(path);
                if (catalog == null) continue;
                if (catalog.trailers == null)
                    catalog.trailers = new System.Collections.Generic.List<TrailerDefinition>();
                if (!catalog.trailers.Contains(definition))
                {
                    catalog.trailers.Add(definition);
                    EditorUtility.SetDirty(catalog);
                }
                return;
            }
            Debug.LogWarning("[TrailerImport] No TrailerCatalogAsset found; definition was created but not registered.");
        }

        private static string ProductionIdForSource(string modelPath)
        {
            string source = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
            if (source.Contains("covered_cargo_trailer_0921131731")) return "TRAILER_DRY_VAN_001";
            if (source.Contains("gas_combustion_tanker_0921132427")) return "TRAILER_FUEL_TANKER_001";
            return SanitizeId(source);
        }

        private static TrailerCategory InferCategory(string id)
        {
            string value = id.ToLowerInvariant();
            if (value.Contains("reefer") || value.Contains("refriger")) return TrailerCategory.Refrigerated;
            if (value.Contains("lowboy") || value.Contains("rgn")) return TrailerCategory.Lowboy;
            if (value.Contains("heavy") && value.Contains("flat")) return TrailerCategory.HeavyFlatbed;
            if (value.Contains("flat")) return TrailerCategory.Flatbed;
            if (value.Contains("container")) return TrailerCategory.ContainerChassis;
            if (value.Contains("grain") || value.Contains("hopper")) return TrailerCategory.GrainHopper;
            if (value.Contains("fuel-tanker") || value.Contains("fuel_tanker") || value.Contains("fuel")) return TrailerCategory.FuelTanker;
            if (value.Contains("cement") || value.Contains("tanker")) return TrailerCategory.CementTanker;
            if (value.Contains("dump")) return TrailerCategory.DumpTrailer;
            if (value.Contains("agri") || value.Contains("bulk")) return TrailerCategory.AgriculturalBulk;
            return TrailerCategory.DryVan;
        }

        private static string SanitizeId(string value)
        {
            var chars = value.ToLowerInvariant().Select(c =>
                char.IsLetterOrDigit(c) ? c : '-').ToArray();
            string result = new string(chars);
            while (result.Contains("--"))
                result = result.Replace("--", "-");
            return result.Trim('-');
        }

        private static string ToDisplayName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "Imported Trailer";

            return string.Join(" ", id.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var existing = FindDirectChild(parent, name);
            return existing ?? CreateChild(parent, name);
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (string.Equals(parent.GetChild(i).name, name, StringComparison.Ordinal))
                    return parent.GetChild(i);
            return null;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
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
