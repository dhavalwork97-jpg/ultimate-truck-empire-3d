#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.Truck.Editor
{
    public sealed class TruckAssetImportPipeline : AssetPostprocessor
    {
        private const string ImportRoot = "Assets/TruckSystem/Imports/Trucks/";
        private const string PrefabRoot = "Assets/TruckSystem/Prefabs/Imported";
        private const string ProfileRoot = "Assets/TruckSystem/Data/Production";
        private const string RuntimeProfileRoot = "Assets/Resources/TruckSystem/Data/Production";

        private static bool IsTruckModel(string path)
        {
            return path.StartsWith(ImportRoot, StringComparison.OrdinalIgnoreCase) &&
                (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));
        }

        void OnPreprocessModel()
        {
            if (!IsTruckModel(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ImportRoot, StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool data = file.Contains("_metallic") || file.Contains("_roughness") || file.Contains("_normal");
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.streamingMipmaps = true;
            importer.sRGBTexture = !data;
            if (file.Contains("_normal")) importer.textureType = TextureImporterType.NormalMap;
        }

        [MenuItem("Ultimate Truck Empire/Truck System/Prepare All Imported Trucks")]
        public static void PrepareAllImported()
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(ProfileRoot);
            EnsureFolder(RuntimeProfileRoot);
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ImportRoot.TrimEnd('/') });
            int prepared = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsTruckModel(path)) continue;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null && Prepare(path, model)) prepared++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TruckImport] Prepared " + prepared + " imported truck model(s).");
        }

        public static void PrepareAllImportedHeadless()
        {
            try
            {
                PrepareAllImported();
                bool valid = ValidatePrepared();
                Debug.Log("[TruckImport] Headless production validation: " + (valid ? "PASS" : "FAIL"));
                EditorApplication.Exit(valid ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static bool Prepare(string modelPath, GameObject model)
        {
            string id = ProductionIdForSource(modelPath);
            string prefabPath = PrefabRoot + "/" + id + ".prefab";
            var instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) return false;

            try
            {
                instance.name = id;

                var body = instance.GetComponent<Rigidbody>();
                if (body == null)
                    body = Undo.AddComponent<Rigidbody>(instance);
                if (body == null)
                    body = instance.AddComponent<Rigidbody>();
                if (body == null)
                    throw new InvalidOperationException("[TruckImport] Failed to add Rigidbody to " + id);

                body.mass = 8000f;
                body.centerOfMass = new Vector3(0f, -0.65f, 0.15f);

                AddComponent<TruckController>(instance);
                AddComponent<TruckPhysics>(instance);
                AddComponent<TruckInput>(instance);
                AddComponent<PlayerTruckFleetBinding>(instance);
                AddComponent<TrailerController>(instance);
                AddComponent<TruckWheelRig>(instance);
                var runtimeMarker = AddComponent<TruckProductionRuntimeInstance>(instance);
                runtimeMarker.Initialize(id);

                ConfigureWheelColliders(instance);
                EnsureCouplingSocket(instance);

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (prefab == null) return false;

                EnsureProductionProfile(id, prefab);
                CopyRuntimeProfile(id);
                return ValidateTruckPrefab(prefab, id);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ConfigureWheelColliders(GameObject root)
        {
            var bounds = GetRendererBounds(root);
            float halfTrack = Mathf.Max(0.75f, bounds.size.x * 0.34f);
            float frontZ = bounds.center.z + bounds.size.z * 0.28f;
            float rearZ = bounds.center.z - bounds.size.z * 0.22f;
            float y = bounds.min.y + Mathf.Max(0.25f, bounds.size.y * 0.12f);

            var fl = CreateWheel(root.transform, "Wheel_FL", new Vector3(-halfTrack, y, frontZ));
            var fr = CreateWheel(root.transform, "Wheel_FR", new Vector3(halfTrack, y, frontZ));
            var rl = CreateWheel(root.transform, "Wheel_RL", new Vector3(-halfTrack, y, rearZ));
            var rr = CreateWheel(root.transform, "Wheel_RR", new Vector3(halfTrack, y, rearZ));

            var controller = root.GetComponent<TruckController>();
            if (controller == null || fl == null || fr == null || rl == null || rr == null)
                throw new InvalidOperationException("[TruckImport] Failed to create the four production WheelColliders.");

            controller.ConfigureWheels(fl, fr, rl, rr);
        }

        private static WheelCollider CreateWheel(Transform root, string name, Vector3 position)
        {
            var existing = FindChild(root, name);
            GameObject go;

            if (existing != null && existing.GetComponent<WheelCollider>() != null)
            {
                go = existing.gameObject;
            }
            else
            {
                string physicsName = name + "_Physics";
                var physics = FindChild(root, physicsName);
                go = physics != null ? physics.gameObject : new GameObject(physicsName);
                go.transform.SetParent(root, false);
            }

            go.transform.localPosition = position;

            var wheel = go.GetComponent<WheelCollider>();
            if (wheel == null)
                wheel = Undo.AddComponent<WheelCollider>(go);
            if (wheel == null)
                wheel = go.AddComponent<WheelCollider>();
            if (wheel == null)
                throw new InvalidOperationException("[TruckImport] Failed to add WheelCollider to " + go.name);

            wheel.radius = 0.62f;
            wheel.suspensionDistance = 0.20f;
            wheel.mass = 80f;
            return wheel;
        }

        private static void EnsureCouplingSocket(GameObject root)
        {
            var socket = FindChild(root.transform, "TrailerCoupling") ?? CreateChild(root.transform, "TrailerCoupling");
            var bounds = GetRendererBounds(root);
            socket.localPosition = root.transform.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.62f, bounds.max.z - bounds.size.z * 0.06f));
        }

        private static void CopyRuntimeProfile(string id)
        {
            string source = ProfileRoot + "/" + id + ".asset";
            string destination = RuntimeProfileRoot + "/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TruckProductionProfile>(source) == null) return;
            if (AssetDatabase.LoadAssetAtPath<TruckProductionProfile>(destination) != null)
                AssetDatabase.DeleteAsset(destination);
            AssetDatabase.CopyAsset(source, destination);
        }

        private static void ApplyProductionCalibration(TruckProductionProfile profile, string id)
        {
            if (string.Equals(id, "meshy-ai-volvo-fh-globetrotter-0923130558-texture", StringComparison.OrdinalIgnoreCase))
            {
                profile.emptyMassTons = 8f;
                profile.fuelCapacityLitres = 750f;
                profile.enginePowerHp = 500f;
                profile.maxSpeedKph = 120f;
                profile.fuelEfficiency = 6.5f;
                profile.drivetrain = "6x4";
                return;
            }

            if (string.Equals(id, "meshy-ai-golden-hauler-0923132145-texture", StringComparison.OrdinalIgnoreCase))
            {
                profile.emptyMassTons = 8f;
                profile.fuelCapacityLitres = 900f;
                profile.enginePowerHp = 600f;
                profile.maxSpeedKph = 115f;
                profile.fuelEfficiency = 5.8f;
                profile.drivetrain = "6x4";
                return;
            }

            profile.emptyMassTons = 8f;
            profile.fuelCapacityLitres = 500f;
            profile.enginePowerHp = 320f;
            profile.maxSpeedKph = 110f;
            profile.fuelEfficiency = 7.2f;
            profile.drivetrain = "6x4";
        }

        private static void EnsureProductionProfile(string id, GameObject prefab)
        {
            string path = ProfileRoot + "/" + id + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<TruckProductionProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<TruckProductionProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.id = id;
            profile.displayName = ToDisplayName(id);
            profile.prefab = prefab;
            ApplyProductionCalibration(profile, id);
            profile.couplingSocket = FindChild(prefab.transform, "TrailerCoupling");
            profile.wheelSockets = new[]
            {
                FindWheelSocket(prefab.transform, "Wheel_FL"),
                FindWheelSocket(prefab.transform, "Wheel_FR"),
                FindWheelSocket(prefab.transform, "Wheel_RL"),
                FindWheelSocket(prefab.transform, "Wheel_RR")
            };
            profile.maxTextureResolution = 1024;
            profile.materialSlotBudget = 6;
            EditorUtility.SetDirty(profile);
        }

        private static bool ValidatePrepared()
        {
            string[] guids = AssetDatabase.FindAssets("t:TruckProductionProfile", new[] { ProfileRoot });
            if (guids.Length == 0)
            {
                Debug.LogError("[TruckImport] No truck production profiles found.");
                return false;
            }

            bool valid = true;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<TruckProductionProfile>(path);
                if (profile == null || profile.prefab == null || profile.wheelSockets == null ||
                    profile.wheelSockets.Length != 4 || profile.wheelSockets.Any(socket => socket == null) ||
                    profile.couplingSocket == null)
                {
                    Debug.LogError("[TruckImport] Production profile incomplete: " + path);
                    valid = false;
                    continue;
                }
                valid &= ValidateTruckPrefab(profile.prefab, profile.id);
            }
            return valid;
        }

        private static bool ValidateTruckPrefab(GameObject prefab, string id)
        {
            bool valid = prefab != null &&
                prefab.GetComponent<TruckController>() != null &&
                prefab.GetComponent<TruckPhysics>() != null &&
                prefab.GetComponent<TruckInput>() != null &&
                prefab.GetComponent<PlayerTruckFleetBinding>() != null &&
                prefab.GetComponent<TrailerController>() != null &&
                prefab.GetComponent<Rigidbody>() != null &&
                prefab.GetComponentsInChildren<WheelCollider>(true).Length >= 4 &&
                FindChild(prefab.transform, "TrailerCoupling") != null;

            int materialSlots = 0;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                materialSlots += renderer.sharedMaterials?.Length ?? 0;
            valid &= materialSlots <= 6;

            if (!valid) Debug.LogError("[TruckImport] Runtime/mobile contract failed for " + id + " (material slots: " + materialSlots + ")");
            return valid;
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, new Vector3(6f, 3f, 10f));

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static T AddComponent<T>(GameObject root) where T : Component
        {
            return root.GetComponent<T>() ?? root.AddComponent<T>();
        }

        private static string ProductionIdForSource(string path)
        {
            return SanitizeId(Path.GetFileNameWithoutExtension(path));
        }

        private static string SanitizeId(string value)
        {
            var chars = value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
            string result = new string(chars);
            while (result.Contains("--")) result = result.Replace("--", "-");
            return result.Trim('-');
        }

        private static string ToDisplayName(string id)
        {
            if (string.Equals(id, "meshy-ai-volvo-fh-globetrotter-0923130558-texture", StringComparison.OrdinalIgnoreCase))
                return "Nordic Titan 500";
            if (string.IsNullOrEmpty(id)) return "Imported Truck";
            return string.Join(" ", id.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private static Transform FindWheelSocket(Transform root, string name)
        {
            var existing = FindChild(root, name);
            if (existing != null && existing.GetComponent<WheelCollider>() != null) return existing;
            return FindChild(root, name + "_Physics");
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif