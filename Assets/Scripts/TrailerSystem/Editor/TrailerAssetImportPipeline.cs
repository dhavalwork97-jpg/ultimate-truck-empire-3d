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
                AddProductionCalibration(instance, id);
                if (instance.GetComponent<TrailerRuntimeCalibrator>() == null)
                    instance.AddComponent<TrailerRuntimeCalibrator>();
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
            public bool calibrationValid;
            public bool calibrationAuthored;
            public float calibrationMassTons;
            public Vector3 calibrationColliderSize;
            public bool kingpinSocketValid;
            public bool wheelSocketsValid;
            public bool axleGeometryValid;
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
                if (!reportEntry.calibrationValid)
                {
                    Debug.LogError("[TrailerImport] Production calibration component missing/invalid on " + id);
                    valid = false;
                }
                if (!reportEntry.kingpinSocketValid || !reportEntry.wheelSocketsValid || !reportEntry.axleGeometryValid)
                {
                    Debug.LogError("[TrailerImport] Kingpin/wheel/axle calibration geometry invalid on " + id);
                    valid = false;
                }
                if (prefab.GetComponent<TrailerRuntimeCalibrator>() == null)
                {
                    Debug.LogError("[TrailerImport] Runtime calibrator missing on " + id);
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

        private static bool HasSocket(GameObject prefab, string socketName)
        {
            if (prefab == null || string.IsNullOrWhiteSpace(socketName)) return false;
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child != prefab.transform &&
                    string.Equals(child.name, socketName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasValidAxleGeometry(GameObject prefab)
        {
            if (prefab == null) return false;

            // Validate the production sockets under the dedicated Sockets parent. Imported
            // Meshy hierarchies may contain unrelated child transforms with the same names;
            // selecting by name across the whole hierarchy can therefore compare positions
            // from different coordinate spaces and produce a false failure.
            Transform sockets = FindChild(prefab.transform, "Sockets");
            if (sockets == null) return false;

            Transform fl = FindDirectChild(sockets, "Wheel_FL");
            Transform fr = FindDirectChild(sockets, "Wheel_FR");
            Transform rl = FindDirectChild(sockets, "Wheel_RL");
            Transform rr = FindDirectChild(sockets, "Wheel_RR");
            if (fl == null || fr == null || rl == null || rr == null) return false;

            float frontTrack = Vector3.Distance(fl.localPosition, fr.localPosition);
            float rearTrack = Vector3.Distance(rl.localPosition, rr.localPosition);
            Vector3 frontCenter = (fl.localPosition + fr.localPosition) * 0.5f;
            Vector3 rearCenter = (rl.localPosition + rr.localPosition) * 0.5f;
            float axleSeparation = Vector3.Distance(frontCenter, rearCenter);

            bool valid = frontTrack > 0.01f && rearTrack > 0.01f && axleSeparation > 0.01f;
            if (!valid)
            {
                Debug.LogError("[TrailerImport] Axle geometry values: frontTrack=" + frontTrack +
                    ", rearTrack=" + rearTrack + ", axleSeparation=" + axleSeparation);
            }
            return valid;
        }

        private static ProductionValidationEntry BuildValidationEntry(string id, string modelPath, GameObject prefab)
        {
            var calibration = prefab.GetComponent<TrailerProductionCalibration>();
            var entry = new ProductionValidationEntry
            {
                productionId = id,
                sourceModel = modelPath,
                runtimeContractValid = prefab.GetComponent<LoadedTrailer>() != null &&
                    prefab.GetComponent<TrailerCargoModule>() != null &&
                    prefab.GetComponent<TrailerSkinApplier>() != null,
                colliderValid = prefab.GetComponentsInChildren<Collider>(true).Any(col => col != null && !col.isTrigger),
                definitionValid = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(
                    "Assets/TrailerSystem/Data/Trailers/" + id + ".asset")?.prefab == prefab,
                calibrationValid = calibration != null && calibration.IsValid(),
                calibrationAuthored = calibration != null && calibration.authored,
                calibrationMassTons = calibration != null ? calibration.massTons : 0f,
                calibrationColliderSize = calibration != null ? calibration.colliderSize : Vector3.zero,
                kingpinSocketValid = HasSocket(prefab, "Kingpin"),
                wheelSocketsValid = HasSocket(prefab, "Wheel_FL") &&
                    HasSocket(prefab, "Wheel_FR") &&
                    HasSocket(prefab, "Wheel_RL") &&
                    HasSocket(prefab, "Wheel_RR"),
                axleGeometryValid = HasValidAxleGeometry(prefab)
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
                AddProductionCalibration(instance, id);
                if (instance.GetComponent<TrailerRuntimeCalibrator>() == null)
                    instance.AddComponent<TrailerRuntimeCalibrator>();
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

        private static void AddProductionCalibration(GameObject root, string id)
        {
            var calibration = root.GetComponent<TrailerProductionCalibration>();
            if (calibration == null)
                calibration = root.AddComponent<TrailerProductionCalibration>();

            calibration.modelScale = 1f;
            calibration.localScaleMultiplier = Vector3.one;
            calibration.massTons = id.Contains("FUEL_TANKER", StringComparison.OrdinalIgnoreCase) ? 9f : 7f;
            calibration.suspensionStiffness = 1f;
            calibration.suspensionDamping = 1f;
            calibration.brakingMultiplier = 1f;
            calibration.authored = false;
            calibration.source = "Meshy Batch 01 / generated baseline";
            calibration.calibrationNotes = "Generated baseline only. Unity-side dimensional, kingpin, axle, COM and collider calibration required before shipping.";

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = root.transform.InverseTransformVector(bounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            calibration.centerOfMassLocalPosition = localCenter + Vector3.down * localSize.y * 0.10f;
            calibration.colliderCenterLocalPosition = localCenter;
            calibration.colliderSize = localSize;

            Transform sockets = FindDirectChild(root.transform, "Sockets");
            calibration.kingpinLocalPosition = sockets != null && FindDirectChild(sockets, "Kingpin") != null
                ? FindDirectChild(sockets, "Kingpin").localPosition
                : localCenter;
            calibration.kingpinHeight = Mathf.Max(0f, calibration.kingpinLocalPosition.y);

            if (sockets != null)
            {
                calibration.wheelFL = FindDirectChild(sockets, "Wheel_FL")?.localPosition ?? localCenter;
                calibration.wheelFR = FindDirectChild(sockets, "Wheel_FR")?.localPosition ?? localCenter;
                calibration.wheelRL = FindDirectChild(sockets, "Wheel_RL")?.localPosition ?? localCenter;
                calibration.wheelRR = FindDirectChild(sockets, "Wheel_RR")?.localPosition ?? localCenter;
            }
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