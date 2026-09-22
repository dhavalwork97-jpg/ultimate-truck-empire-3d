#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    public static class TrailerSystemAssetValidator
    {
        [MenuItem("Ultimate Truck Empire/Trailer System/Validate Catalog")]
        public static void ValidateCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:TrailerCatalogAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[TrailerSystem] No TrailerCatalogAsset found.");
                return;
            }

            int errors = 0;
            int warnings = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TrailerCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<TrailerCatalogAsset>(path);
                if (catalog == null) continue;

                ValidateList("trailer", catalog.trailers, ref errors, ref warnings);
                ValidateUniqueIds("trailer", catalog.trailers, ref errors);
                ValidateList("cargo", catalog.cargo, ref errors, ref warnings);
                ValidateUniqueIds("cargo", catalog.cargo, ref errors);
                ValidateList("skin", catalog.skins, ref errors, ref warnings);
                ValidateUniqueIds("skin", catalog.skins, ref errors);

                for (int t = 0; t < catalog.trailers.Count; t++)
                {
                    TrailerDefinition trailer = catalog.trailers[t];
                    if (trailer == null) continue;

                    if (trailer.prefab == null)
                    {
                        Debug.LogWarning("[TrailerSystem] Trailer '" + trailer.id + "' has no prefab.", trailer);
                        warnings++;
                    }

                    if (string.IsNullOrWhiteSpace(trailer.id))
                    {
                        Debug.LogError("[TrailerSystem] Trailer has no stable id.", trailer);
                        errors++;
                    }

                    if (trailer.defaultSkin == null)
                    {
                        Debug.LogWarning("[TrailerSystem] Trailer '" + trailer.id + "' has no default skin.", trailer);
                        warnings++;
                    }

                    if (trailer.compatibleCargo == null || trailer.compatibleCargo.Length == 0)
                    {
                        Debug.LogWarning("[TrailerSystem] Trailer '" + trailer.id + "' has no compatible cargo.", trailer);
                        warnings++;
                    }
                    else
                    {
                        for (int cc = 0; cc < trailer.compatibleCargo.Length; cc++)
                        {
                            var cargoRef = trailer.compatibleCargo[cc];
                            if (cargoRef == null) continue;
                            if (cargoRef.compatibleTrailers == null || System.Array.IndexOf(cargoRef.compatibleTrailers, trailer) < 0)
                            {
                                Debug.LogError("[TrailerSystem] Compatibility mismatch: trailer '" + trailer.id +
                                    "' references cargo '" + cargoRef.id + "' but cargo does not reference the trailer.", trailer);
                                errors++;
                            }
                        }
                    }

                    if (trailer.materialSlotBudget <= 0)
                    {
                        Debug.LogError("[TrailerSystem] Trailer '" + trailer.id + "' has an invalid material budget.", trailer);
                        errors++;
                    }

                    if (trailer.availableSkins != null)
                    {
                        for (int s = 0; s < trailer.availableSkins.Length; s++)
                        {
                            if (trailer.availableSkins[s] == null) continue;
                            if (!ContainsSkin(catalog, trailer.availableSkins[s]))
                            {
                                Debug.LogWarning("[TrailerSystem] Trailer '" + trailer.id + "' references skin '" +
                                    trailer.availableSkins[s].id + "' which is not in the catalog.", trailer);
                                warnings++;
                            }
                        }
                    }
                }

                for (int c = 0; c < catalog.cargo.Count; c++)
                {
                    CargoDefinition cargo = catalog.cargo[c];
                    if (cargo == null) continue;

                    if (string.IsNullOrWhiteSpace(cargo.id))
                    {
                        Debug.LogError("[TrailerSystem] Cargo has no stable id.", cargo);
                        errors++;
                    }

                    if (cargo.minWeightTons > cargo.maxWeightTons)
                    {
                        Debug.LogError("[TrailerSystem] Cargo '" + cargo.id + "' has min weight above max weight.", cargo);
                        errors++;
                    }

                    if (cargo.compatibleTrailers == null || cargo.compatibleTrailers.Length == 0)
                    {
                        Debug.LogWarning("[TrailerSystem] Cargo '" + cargo.id + "' has no compatible trailers.", cargo);
                        warnings++;
                    }
                    else
                    {
                        for (int ct = 0; ct < cargo.compatibleTrailers.Length; ct++)
                        {
                            var trailerRef = cargo.compatibleTrailers[ct];
                            if (trailerRef == null) continue;
                            if (trailerRef.compatibleCargo == null || System.Array.IndexOf(trailerRef.compatibleCargo, cargo) < 0)
                            {
                                Debug.LogError("[TrailerSystem] Compatibility mismatch: cargo '" + cargo.id +
                                    "' references trailer '" + trailerRef.id + "' but trailer does not reference the cargo.", cargo);
                                errors++;
                            }
                        }
                    }
                }
            }

            Debug.Log("[TrailerSystem] Validation complete. Errors: " + errors + ", warnings: " + warnings + ".");
        }

        private static void ValidateList<T>(string kind, System.Collections.Generic.List<T> list,
            ref int errors, ref int warnings) where T : Object
        {
            if (list == null)
            {
                Debug.LogError("[TrailerSystem] " + kind + " list is null.");
                errors++;
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                T asset = list[i];
                if (asset == null)
                {
                    Debug.LogWarning("[TrailerSystem] Catalog contains a null " + kind + " entry at index " + i + ".");
                    warnings++;
                }
            }
        }

        private static void ValidateUniqueIds<T>(string kind, System.Collections.Generic.List<T> list, ref int errors)
            where T : ScriptableObject
        {
            var ids = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var asset = list[i];
                if (asset == null) continue;

                string id = null;
                var trailer = asset as TrailerDefinition;
                var cargo = asset as CargoDefinition;
                var skin = asset as TrailerSkinDefinition;
                if (trailer != null) id = trailer.id;
                else if (cargo != null) id = cargo.id;
                else if (skin != null) id = skin.id;

                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!ids.Add(id))
                {
                    Debug.LogError("[TrailerSystem] Duplicate " + kind + " id '" + id + "'.", asset);
                    errors++;
                }
            }
        }

        private static bool ContainsSkin(TrailerCatalogAsset catalog, TrailerSkinDefinition skin)
        {
            for (int i = 0; i < catalog.skins.Count; i++)
                if (catalog.skins[i] == skin) return true;
            return false;
        }
    }
}
#endif
