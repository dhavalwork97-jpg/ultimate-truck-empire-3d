#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.FreightLocations.Editor
{
    /// <summary>
    /// Builds the seven production Freight Location prefabs from the two imported
    /// FBX source models already committed under Assets/FreightLocations/Source.
    ///
    /// The generator deliberately keeps the imported mesh/material assets intact:
    /// the generated prefab hierarchy references the imported FBX sub-assets rather
    /// than copying mesh or texture data.
    ///
    /// Unity's LODGroup API controls which Renderer set is visible at each screen
    /// size. Because the source pack does not contain authored lower-detail meshes,
    /// this first-pass generator uses the same imported renderers for all three LOD
    /// levels. This satisfies the production contract without tripling mesh memory.
    /// Replace the LOD1/LOD2 renderer sets with authored/decimated meshes later when
    /// real distance-optimized geometry is available.
    /// </summary>
    public static class FreightLocationPrefabBuilder
    {
        private const string SourceRoot = "Assets/FreightLocations/Source";
        private const string WarehouseSource =
            "Assets/FreightLocations/Source/Warehouse/warehouseupload2.fbx";
        private const string FactorySource =
            "Assets/FreightLocations/Source/Factory/basic_factory_modeling_.fbx";

        private const string PrefabRoot = "Assets/FreightLocations/Prefabs";
        private const string WarehousePrefabRoot = PrefabRoot + "/Warehouse";
        private const string FactoryPrefabRoot = PrefabRoot + "/Factory";
        private const string CatalogPath =
            "Assets/Resources/FreightLocations/FreightLocationPrefabCatalog.asset";

        private static readonly PrefabSpec[] Specs =
        {
            new PrefabSpec("Warehouse_Base", false, 1.00f),
            new PrefabSpec("Warehouse_Large", false, 1.12f),
            new PrefabSpec("Warehouse_Port", false, 1.20f),
            new PrefabSpec("Factory_Base", true, 1.00f),
            new PrefabSpec("Factory_Textile", true, 1.02f),
            new PrefabSpec("Factory_Chemical", true, 1.08f),
            new PrefabSpec("Factory_Automotive", true, 1.10f)
        };

        private static readonly string[] RequiredAnchors =
        {
            "MeshRoot",
            "LoadingDock_A",
            "LoadingDock_B",
            "TrailerSpawn",
            "CargoSpawn",
            "DeliveryTrigger",
            "ParkingSlot_01",
            "ParkingSlot_02",
            "CompanySign",
            "EnvironmentCollision"
        };

        private readonly struct PrefabSpec
        {
            public readonly string Name;
            public readonly bool IsFactory;
            public readonly float ModelScale;

            public PrefabSpec(string name, bool isFactory, float modelScale)
            {
                Name = name;
                IsFactory = isFactory;
                ModelScale = modelScale;
            }
        }

        [MenuItem("Ultimate Truck Empire/Freight Locations/Build Production Prefabs")]
        public static void BuildProductionPrefabs()
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(WarehousePrefabRoot);
            EnsureFolder(FactoryPrefabRoot);

            AssetDatabase.ImportAsset(WarehouseSource, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(FactorySource, ImportAssetOptions.ForceUpdate);

            var missingSources = new List<string>();
            if (LoadModel(WarehouseSource) == null) missingSources.Add(WarehouseSource);
            if (LoadModel(FactorySource) == null) missingSources.Add(FactorySource);

            if (missingSources.Count > 0)
            {
                Debug.LogError(
                    "[FreightLocationPrefabBuilder] Missing source FBX files:\n" +
                    string.Join("\n", missingSources));
                return;
            }

            int created = 0;
            foreach (var spec in Specs)
            {
                string sourcePath = spec.IsFactory ? FactorySource : WarehouseSource;
                string folder = spec.IsFactory ? FactoryPrefabRoot : WarehousePrefabRoot;
                string prefabPath = folder + "/" + spec.Name + ".prefab";

                if (BuildOne(spec, sourcePath, prefabPath))
                    created++;
            }

            UpdateRuntimeCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FreightLocationPrefabBuilder] Finished. Generated " + created +
                "/" + Specs.Length +
                " production prefabs and refreshed the runtime prefab catalog.");
        }

        [MenuItem("Ultimate Truck Empire/Freight Locations/Validate Production Prefabs")]
        public static void ValidateProductionPrefabs()
        {
            AssetDatabase.Refresh();

            int errors = 0;
            foreach (var spec in Specs)
            {
                string folder = spec.IsFactory ? FactoryPrefabRoot : WarehousePrefabRoot;
                string path = folder + "/" + spec.Name + ".prefab";
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (root == null)
                {
                    Debug.LogError("[FreightLocationPrefabBuilder] Missing prefab: " + path);
                    errors++;
                    continue;
                }

                if (root.GetComponent<LODGroup>() == null)
                {
                    Debug.LogError(spec.Name + " is missing LODGroup.");
                    errors++;
                }
                else if (root.GetComponent<LODGroup>().lodCount != 3)
                {
                    Debug.LogError(
                        spec.Name + " has " +
                        root.GetComponent<LODGroup>().lodCount +
                        " LOD levels; expected 3.");
                    errors++;
                }

                foreach (var anchor in RequiredAnchors)
                {
                    if (FindChild(root.transform, anchor) == null)
                    {
                        Debug.LogError(spec.Name + " is missing " + anchor + ".");
                        errors++;
                    }
                }

                var delivery = FindChild(root.transform, "DeliveryTrigger");
                if (delivery != null && delivery.gameObject.isStatic)
                {
                    Debug.LogError(spec.Name + " DeliveryTrigger must be non-static.");
                    errors++;
                }
            }

            Debug.Log(
                errors == 0
                    ? "[FreightLocationPrefabBuilder] Validation passed for all seven production prefabs."
                    : "[FreightLocationPrefabBuilder] Validation found " + errors + " issue(s).");
        }

        private static bool BuildOne(PrefabSpec spec, string sourcePath, string prefabPath)
        {
            var source = LoadModel(sourcePath);
            if (source == null)
                return false;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            var root = new GameObject(spec.Name);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            try
            {
                var meshRoot = new GameObject("MeshRoot");
                meshRoot.transform.SetParent(root.transform, false);

                // Instantiate the FBX model so its imported renderers/materials are
                // retained. Unpack the model prefab connection before saving a new
                // production prefab.
                var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                    model = UnityEngine.Object.Instantiate(source);

                model.name = source.name;
                model.transform.SetParent(meshRoot.transform, false);
                model.transform.localScale = Vector3.one * spec.ModelScale;

                if (PrefabUtility.IsAnyPrefabInstanceRoot(model))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        model,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                var renderers = meshRoot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError(
                        "[FreightLocationPrefabBuilder] Source model contains no Renderer components: " +
                        sourcePath);
                    return false;
                }

                Bounds bounds = CalculateBounds(root.transform, renderers);
                CreateContractAnchors(root.transform, bounds);
                CreateEnvironmentCollision(root.transform, bounds);

                var lodGroup = root.AddComponent<LODGroup>();
                lodGroup.SetLODs(new[]
                {
                    new LOD(0.60f, renderers),
                    new LOD(0.25f, renderers),
                    new LOD(0.05f, renderers)
                });
                lodGroup.RecalculateBounds();

                bool success;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                if (!success)
                {
                    Debug.LogError(
                        "[FreightLocationPrefabBuilder] Failed to save " + prefabPath);
                    return false;
                }

                Debug.Log(
                    "[FreightLocationPrefabBuilder] Built " + prefabPath +
                    " from " + sourcePath +
                    " (" + renderers.Length + " renderers).");

                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateContractAnchors(Transform root, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            // Keep anchors outside the visual mesh where a truck can actually use them.
            float groundY = bounds.min.y;
            float frontZ = bounds.min.z;
            float rearZ = bounds.max.z;
            float sideX = Mathf.Max(size.x * 0.35f, 2.5f);
            float truckClearance = Mathf.Max(size.z * 0.12f, 3.0f);

            NewAnchor(root, "LoadingDock_A",
                new Vector3(center.x - sideX, groundY, frontZ - truckClearance));
            NewAnchor(root, "LoadingDock_B",
                new Vector3(center.x + sideX, groundY, frontZ - truckClearance));

            NewAnchor(root, "TrailerSpawn",
                new Vector3(center.x, groundY, frontZ - truckClearance * 2.0f));

            NewAnchor(root, "CargoSpawn",
                new Vector3(center.x, groundY + Mathf.Min(1.0f, size.y * 0.25f), center.z));

            NewAnchor(root, "DeliveryTrigger",
                new Vector3(center.x, groundY + 0.5f, frontZ - truckClearance * 1.5f));

            NewAnchor(root, "ParkingSlot_01",
                new Vector3(center.x - sideX, groundY, rearZ + truckClearance));
            NewAnchor(root, "ParkingSlot_02",
                new Vector3(center.x + sideX, groundY, rearZ + truckClearance));

            NewAnchor(root, "CompanySign",
                new Vector3(center.x, bounds.max.y, rearZ));

            // DeliveryTrigger is an anchor/marker only. The authoritative runtime
            // DeliveryTrigger component is added by the existing world builder.
            var delivery = FindChild(root, "DeliveryTrigger");
            delivery.gameObject.isStatic = false;
        }

        private static void CreateEnvironmentCollision(Transform root, Bounds bounds)
        {
            var go = NewAnchor(root, "EnvironmentCollision", bounds.center);
            var collider = go.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(
                Mathf.Max(bounds.size.x, 1f),
                Mathf.Max(bounds.size.y, 1f),
                Mathf.Max(bounds.size.z, 1f));

            collider.isTrigger = false;
            go.gameObject.isStatic = true;
        }

        private static Transform NewAnchor(Transform parent, string name, Vector3 worldPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static Bounds CalculateBounds(Transform root, Renderer[] renderers)
        {
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            bool initialized = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!initialized)
                bounds = new Bounds(root.position, Vector3.one);

            return bounds;
        }

        private static GameObject LoadModel(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void UpdateRuntimeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FreightLocationPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning(
                    "[FreightLocationPrefabBuilder] Runtime catalog not found at " +
                    CatalogPath +
                    ". Prefabs were still generated.");
                return;
            }

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");

            if (entries == null || !entries.isArray)
            {
                Debug.LogError(
                    "[FreightLocationPrefabBuilder] Could not access serialized catalog entries.");
                return;
            }

            entries.ClearArray();

            foreach (var location in FreightLocationRegistry.All)
            {
                string prefabPath = "Assets/" + location.prefabResourcePath + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null)
                {
                    Debug.LogError(
                        "[FreightLocationPrefabBuilder] Catalog target missing for " +
                        location.id + ": " + prefabPath);
                    continue;
                }

                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = location.id;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

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
