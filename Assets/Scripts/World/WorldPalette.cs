using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    public enum WorldSurface
    {
        Ground, GrassDry, GrassLush, Dirt, Gravel,
        Asphalt, AsphaltWorn, Concrete, ConcreteDark, Curb,
        MarkingWhite, MarkingYellow, WarningStripe,
        FacadeSand, FacadeClay, FacadeGrey, FacadeCream, FacadeBlue, FacadeRust,
        Glass, GlassDark, Metal, MetalPainted, MetalRoof, Rust,
        SignGreen, SignBlue, SignWhite, SignYellow,
        TreeTrunk, TreeCrown, TreeCrownDry, Pole, Fence,
        ContainerRed, ContainerBlue, ContainerGreen, Tyre, Timber
    }

    /// <summary>
    /// Every material used by the procedural world, created once and shared.
    /// The whole environment renders from roughly forty Material instances, no
    /// matter how many objects are placed, which is what keeps the SRP batcher
    /// happy and the draw calls sane.
    ///
    /// Materials come from TruckMaterialLibrary, so they resolve the correct
    /// shader for Built-in, URP or HDRP at runtime instead of going magenta.
    /// </summary>
    public static class WorldPalette
    {
        private static bool wetRoads;

        public static Material Get(WorldSurface surface)
        {
            switch (surface)
            {
                // --- terrain -------------------------------------------------
                case WorldSurface.GrassDry: return M("grassDry", 0.34f, 0.36f, 0.22f, 0f, 0.08f);
                case WorldSurface.GrassLush: return M("grassLush", 0.20f, 0.33f, 0.16f, 0f, 0.10f);
                case WorldSurface.Dirt: return M("dirt", 0.35f, 0.29f, 0.21f, 0f, 0.06f);
                case WorldSurface.Gravel: return M("gravel", 0.40f, 0.39f, 0.37f, 0f, 0.14f);

                // --- road ----------------------------------------------------
                case WorldSurface.Asphalt:
                    return wetRoads
                        ? M("asphaltWet", 0.075f, 0.080f, 0.092f, 0.10f, 0.62f)
                        : M("asphalt", 0.118f, 0.122f, 0.132f, 0f, 0.26f);
                case WorldSurface.AsphaltWorn:
                    return wetRoads
                        ? M("asphaltWornWet", 0.10f, 0.105f, 0.115f, 0.10f, 0.55f)
                        : M("asphaltWorn", 0.155f, 0.157f, 0.163f, 0f, 0.20f);
                case WorldSurface.Concrete: return M("concrete", 0.58f, 0.575f, 0.55f, 0f, 0.20f);
                case WorldSurface.ConcreteDark: return M("concreteDark", 0.40f, 0.40f, 0.39f, 0f, 0.22f);
                case WorldSurface.Curb: return M("curb", 0.70f, 0.69f, 0.65f, 0f, 0.18f);

                // --- markings ------------------------------------------------
                case WorldSurface.MarkingWhite: return M("markWhite", 0.86f, 0.86f, 0.82f, 0f, 0.16f);
                case WorldSurface.MarkingYellow: return M("markYellow", 0.80f, 0.66f, 0.18f, 0f, 0.16f);
                case WorldSurface.WarningStripe: return M("warnStripe", 0.85f, 0.62f, 0.06f, 0f, 0.28f);

                // --- facades -------------------------------------------------
                case WorldSurface.FacadeSand: return M("facadeSand", 0.72f, 0.66f, 0.54f, 0f, 0.16f);
                case WorldSurface.FacadeClay: return M("facadeClay", 0.60f, 0.42f, 0.33f, 0f, 0.16f);
                case WorldSurface.FacadeGrey: return M("facadeGrey", 0.52f, 0.53f, 0.54f, 0f, 0.20f);
                case WorldSurface.FacadeCream: return M("facadeCream", 0.78f, 0.75f, 0.67f, 0f, 0.18f);
                case WorldSurface.FacadeBlue: return M("facadeBlue", 0.38f, 0.45f, 0.53f, 0f, 0.22f);
                case WorldSurface.FacadeRust: return M("facadeRust", 0.46f, 0.33f, 0.26f, 0.1f, 0.18f);

                // --- industrial ----------------------------------------------
                case WorldSurface.Glass: return M("glassBand", 0.20f, 0.28f, 0.34f, 0.15f, 0.88f);
                case WorldSurface.GlassDark: return M("glassDark", 0.10f, 0.13f, 0.17f, 0.15f, 0.90f);
                case WorldSurface.Metal: return M("metal", 0.58f, 0.60f, 0.63f, 0.85f, 0.55f);
                case WorldSurface.MetalPainted: return M("metalPainted", 0.30f, 0.42f, 0.50f, 0.4f, 0.42f);
                case WorldSurface.MetalRoof: return M("metalRoof", 0.46f, 0.48f, 0.50f, 0.6f, 0.35f);
                case WorldSurface.Rust: return M("rust", 0.42f, 0.25f, 0.15f, 0.25f, 0.20f);

                // --- signage -------------------------------------------------
                case WorldSurface.SignGreen: return M("signGreen", 0.08f, 0.32f, 0.20f, 0f, 0.35f);
                case WorldSurface.SignBlue: return M("signBlue", 0.10f, 0.25f, 0.48f, 0f, 0.35f);
                case WorldSurface.SignWhite: return M("signWhite", 0.88f, 0.88f, 0.86f, 0f, 0.30f);
                case WorldSurface.SignYellow: return M("signYellow", 0.86f, 0.70f, 0.10f, 0f, 0.32f);

                // --- vegetation and furniture --------------------------------
                case WorldSurface.TreeTrunk: return M("treeTrunk", 0.27f, 0.20f, 0.14f, 0f, 0.10f);
                case WorldSurface.TreeCrown: return M("treeCrown", 0.17f, 0.31f, 0.15f, 0f, 0.09f);
                case WorldSurface.TreeCrownDry: return M("treeCrownDry", 0.29f, 0.33f, 0.17f, 0f, 0.09f);
                case WorldSurface.Pole: return M("pole", 0.33f, 0.35f, 0.37f, 0.6f, 0.38f);
                case WorldSurface.Fence: return M("fence", 0.45f, 0.46f, 0.47f, 0.7f, 0.30f);

                case WorldSurface.ContainerRed: return M("containerRed", 0.48f, 0.17f, 0.14f, 0.3f, 0.25f);
                case WorldSurface.ContainerBlue: return M("containerBlue", 0.14f, 0.28f, 0.45f, 0.3f, 0.25f);
                case WorldSurface.ContainerGreen: return M("containerGreen", 0.15f, 0.34f, 0.26f, 0.3f, 0.25f);
                case WorldSurface.Tyre: return M("tyre", 0.06f, 0.06f, 0.065f, 0f, 0.16f);
                case WorldSurface.Timber: return M("timber", 0.55f, 0.44f, 0.28f, 0f, 0.14f);

                default: return M("ground", 0.30f, 0.31f, 0.23f, 0f, 0.10f);
            }
        }

        private static Material M(string label, float r, float g, float b, float metallic, float smoothness)
        {
            return TruckMaterialLibrary.Make(label, new Color(r, g, b), metallic, smoothness);
        }

        public static void Paint(GameObject target, WorldSurface surface)
        {
            if (target == null) return;
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = Get(surface);
        }

        /// <summary>
        /// Rain and storms darken and gloss the road surface. This mutates the two
        /// shared asphalt materials rather than swapping materials per object, so
        /// the whole road network changes in one call with no allocation.
        /// </summary>
        public static void SetWetRoads(bool wet)
        {
            wetRoads = wet;
        }

        public static bool WetRoads => wetRoads;

        /// <summary>Deterministic facade choice so the skyline varies but never flickers.</summary>
        public static WorldSurface FacadeFor(int seed)
        {
            switch (Mathf.Abs(seed) % 6)
            {
                case 0: return WorldSurface.FacadeSand;
                case 1: return WorldSurface.FacadeGrey;
                case 2: return WorldSurface.FacadeCream;
                case 3: return WorldSurface.FacadeClay;
                case 4: return WorldSurface.FacadeBlue;
                default: return WorldSurface.FacadeRust;
            }
        }

        /// <summary>Back-compatible helper kept for existing callers.</summary>
        public static WorldSurface BuildingShade(int x, int z) => FacadeFor(x * 31 + z * 17);
    }
}
