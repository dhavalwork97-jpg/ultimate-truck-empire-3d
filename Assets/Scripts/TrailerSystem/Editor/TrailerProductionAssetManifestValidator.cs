#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    public static class TrailerProductionAssetManifestValidator
    {
        private const string ManifestPath = "Assets/TrailerSystem/Production/TrailerAssetManifest.json";

        private static readonly (string id, string category)[] Required =
        {
            ("dry-van", "DryVan"),
            ("refrigerated", "Refrigerated"),
            ("flatbed", "Flatbed"),
            ("heavy-flatbed", "HeavyFlatbed"),
            ("lowboy-rgn", "Lowboy"),
            ("container-chassis", "ContainerChassis"),
            ("grain-hopper", "GrainHopper"),
            ("cement-tanker", "CementTanker")
        };

        [MenuItem("Ultimate Truck Empire/Trailer System/Validate Production Asset Manifest")]
        public static void Validate()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[Trailer Production] Missing manifest: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            var manifest = JsonUtility.FromJson<Manifest>(json);
            var errors = 0;
            var warnings = 0;

            if (manifest == null || manifest.trailers == null)
            {
                Debug.LogError("[Trailer Production] Manifest could not be parsed.");
                return;
            }

            if (manifest.schemaVersion != 1)
            {
                Debug.LogError($"[Trailer Production] Unsupported schema version: {manifest.schemaVersion}");
                errors++;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var required in Required)
            {
                var entry = Find(manifest.trailers, required.id);
                if (entry == null)
                {
                    Debug.LogError($"[Trailer Production] Missing required trailer: {required.id}");
                    errors++;
                    continue;
                }

                if (!seen.Add(entry.id))
                {
                    Debug.LogError($"[Trailer Production] Duplicate trailer id: {entry.id}");
                    errors++;
                }

                if (!string.Equals(entry.category, required.category, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[Trailer Production] {entry.id}: expected category {required.category}, got {entry.category}.");
                    errors++;
                }

                if (!string.Equals(entry.status, "pending", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(entry.status, "verified", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[Trailer Production] {entry.id}: status must be pending or verified.");
                    errors++;
                }

                if (string.Equals(entry.status, "verified", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsAllowedLicense(entry.licenseType))
                    {
                        Debug.LogError($"[Trailer Production] {entry.id}: verified asset has no accepted commercial license type.");
                        errors++;
                    }

                    if (string.IsNullOrWhiteSpace(entry.sourceUrl))
                    {
                        Debug.LogError($"[Trailer Production] {entry.id}: verified asset requires a source URL or documented private source.");
                        errors++;
                    }

                    if (string.IsNullOrWhiteSpace(entry.creator))
                    {
                        Debug.LogError($"[Trailer Production] {entry.id}: verified asset requires creator/rights-holder attribution.");
                        errors++;
                    }

                    if (string.IsNullOrWhiteSpace(entry.licenseTextPath) || !File.Exists(entry.licenseTextPath))
                    {
                        Debug.LogError($"[Trailer Production] {entry.id}: verified asset requires a checked-in license/permission file.");
                        errors++;
                    }
                }
                else
                {
                    warnings++;
                    Debug.LogWarning($"[Trailer Production] {entry.id}: pending rights verification; not cleared for commercial release.");
                }
            }

            if (manifest.trailers.Length != Required.Length)
            {
                Debug.LogError($"[Trailer Production] Expected exactly {Required.Length} launch entries, found {manifest.trailers.Length}.");
                errors++;
            }

            if (errors == 0)
                Debug.Log($"[Trailer Production] Manifest validation passed with {warnings} pending rights entries. Pending entries are not release-cleared.");
            else
                Debug.LogError($"[Trailer Production] Manifest validation failed with {errors} error(s) and {warnings} warning(s).");
        }

        private static ManifestEntry Find(ManifestEntry[] entries, string id)
        {
            foreach (var entry in entries)
            {
                if (entry != null && string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        private static bool IsAllowedLicense(string licenseType)
        {
            return string.Equals(licenseType, "CC0", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(licenseType, "CommercialLicense", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(licenseType, "CustomPermission", StringComparison.OrdinalIgnoreCase);
        }

        [Serializable]
        private sealed class Manifest
        {
            public int schemaVersion;
            public ManifestEntry[] trailers;
        }

        [Serializable]
        private sealed class ManifestEntry
        {
            public string id;
            public string category;
            public string status;
            public string assetPath;
            public string licenseType;
            public string sourceUrl;
            public string creator;
            public string licenseTextPath;
        }
    }
}
#endif
