#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
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

            string id = SanitizeId(Path.GetFileNameWithoutExtension(modelPath));
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

                if (PrepareModel(modelPath, model))
                    prepared++;
                else
                    skipped++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrailerImport] Batch preparation complete. Prepared: " + prepared + ", skipped: " + skipped + ".");
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
            definition.payloadCapacityTons = 30f;
            definition.emptyWeightTons = 7f;
            definition.purchasePrice = 100000f;
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

        private static TrailerCategory InferCategory(string id)
        {
            string value = id.ToLowerInvariant();
            if (value.Contains("reefer") || value.Contains("refriger")) return TrailerCategory.Refrigerated;
            if (value.Contains("lowboy") || value.Contains("rgn")) return TrailerCategory.Lowboy;
            if (value.Contains("heavy") && value.Contains("flat")) return TrailerCategory.HeavyFlatbed;
            if (value.Contains("flat")) return TrailerCategory.Flatbed;
            if (value.Contains("container")) return TrailerCategory.ContainerChassis;
            if (value.Contains("grain") || value.Contains("hopper")) return TrailerCategory.GrainHopper;
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
#endif        [MenuItem("Ultimate Truck Empire/Trailer System/Prepare Selected Imported Trailer")]
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

            PrepareModel(modelPath, model);
        }

        private static bool PrepareModel(string modelPath, GameObject model)
        {
            string id = SanitizeId(Path.GetFileNameWithoutExtension(modelPath));
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[TrailerImport] Skipping model with empty sanitized id: " + modelPath);
                return false;
            }

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

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[TrailerImport] Failed to create prefab: " + prefabPath);
                    return false;
                }

                RegisterOrCreateDefinition(id, prefab);
                RegisterDefinitionInCatalog(id);
                Debug.Log("[TrailerImport] Prepared trailer '" + id +
                    "'. Replace generated sockets/collider with authored production data before shipping.");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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

            string id = SanitizeId(Path.GetFileNameWithoutExtension(modelPath));
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

                if (PrepareModel(modelPath, model))
                    prepared++;
                else
                    skipped++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrailerImport] Batch preparation complete. Prepared: " + prepared + ", skipped: " + skipped + ".");
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
            definition.payloadCapacityTons = 30f;
            definition.emptyWeightTons = 7f;
            definition.purchasePrice = 100000f;
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

        private static TrailerCategory InferCategory(string id)
        {
            string value = id.ToLowerInvariant();
            if (value.Contains("reefer") || value.Contains("refriger")) return TrailerCategory.Refrigerated;
            if (value.Contains("lowboy") || value.Contains("rgn")) return TrailerCategory.Lowboy;
            if (value.Contains("heavy") && value.Contains("flat")) return TrailerCategory.HeavyFlatbed;
            if (value.Contains("flat")) return TrailerCategory.Flatbed;
            if (value.Contains("container")) return TrailerCategory.ContainerChassis;
            if (value.Contains("grain") || value.Contains("hopper")) return TrailerCategory.GrainHopper;
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
