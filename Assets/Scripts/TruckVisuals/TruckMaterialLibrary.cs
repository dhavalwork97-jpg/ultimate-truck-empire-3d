using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Creates and caches every material used by the truck / trailer visuals.
    ///
    /// Works under Built-in, URP and HDRP: the correct shader is discovered at
    /// runtime and both the Standard (_Color/_Glossiness) and SRP
    /// (_BaseColor/_Smoothness) property names are written, so materials never
    /// come out magenta after a pipeline change.
    ///
    /// Materials are cached by their full description, so two trucks sharing a
    /// paint colour also share one Material instance (fewer SRP batch breaks).
    /// </summary>
    public static class TruckMaterialLibrary
    {
        // ---- material slots -------------------------------------------------
        public const int Paint = 0;
        public const int PaintAccent = 1;
        public const int Chassis = 2;
        public const int Rubber = 3;
        public const int Glass = 4;
        public const int Chrome = 5;
        public const int PlasticBlack = 6;
        public const int Grille = 7;
        public const int LensClear = 8;
        public const int LensRed = 9;
        public const int LensAmber = 10;
        public const int TrailerSkin = 11;
        public const int Aluminium = 12;
        public const int InteriorDark = 13;
        public const int SlotCount = 14;

        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
        private static Shader _litShader;
        private static bool _srpPropertyNames;

        private static Shader LitShader
        {
            get
            {
                if (_litShader != null) return _litShader;

                _litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_litShader != null) { _srpPropertyNames = true; return _litShader; }

                _litShader = Shader.Find("HDRP/Lit");
                if (_litShader != null) { _srpPropertyNames = true; return _litShader; }

                _litShader = Shader.Find("Standard");
                if (_litShader != null) { _srpPropertyNames = false; return _litShader; }

                // Last resort so nothing renders magenta in a stripped build.
                _litShader = Shader.Find("Diffuse");
                _srpPropertyNames = false;
                return _litShader;
            }
        }

        /// <summary>True when the active pipeline is URP or HDRP.</summary>
        public static bool UsingScriptableRenderPipeline
        {
            get { Shader s = LitShader; return s != null && _srpPropertyNames; }
        }

        // ---------------------------------------------------------------
        // Core factory
        // ---------------------------------------------------------------
        public static Material Make(string label, Color color, float metallic, float smoothness)
        {
            return Make(label, color, metallic, smoothness, Color.clear);
        }

        public static Material Make(string label, Color color, float metallic, float smoothness, Color emission)
        {
            string key = string.Format("{0}|{1:F3},{2:F3},{3:F3},{4:F3}|{5:F2}|{6:F2}|{7:F3},{8:F3},{9:F3}",
                label, color.r, color.g, color.b, color.a, metallic, smoothness,
                emission.r, emission.g, emission.b);

            Material cached;
            if (_cache.TryGetValue(key, out cached) && cached != null) return cached;

            Material m = new Material(LitShader);
            m.name = "TE_" + label;

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            m.color = color;

            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);

            bool emissive = emission.r > 0.001f || emission.g > 0.001f || emission.b > 0.001f;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            }

            if (color.a < 0.999f) SetTransparent(m);

            _cache[key] = m;
            return m;
        }

        private static void SetTransparent(Material m)
        {
            // Surface type / blend setup differs per pipeline; set what exists.
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // URP transparent
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);         // Standard transparent
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f); // SrcAlpha
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);// OneMinusSrcAlpha
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_ALPHABLEND_ON");
        }

        /// <summary>Convenience for light lenses: base colour plus matching glow.</summary>
        public static Material MakeLens(string label, Color color, float glow)
        {
            return Make(label, color, 0f, 0.85f, color * glow);
        }

        public static void ClearCache()
        {
            _cache.Clear();
            _litShader = null;
        }

        // ---------------------------------------------------------------
        // Palette: the per-truck slot table handed to MeshBuilder
        // ---------------------------------------------------------------
        public class Palette : IMaterialSlots
        {
            private readonly Material[] _slots = new Material[SlotCount];

            public Material Get(int slot)
            {
                if (slot < 0 || slot >= SlotCount) slot = Paint;
                return _slots[slot];
            }

            public void Set(int slot, Material m)
            {
                if (slot < 0 || slot >= SlotCount) return;
                _slots[slot] = m;
            }

            public static Palette Build(Color paintColor, Color accentColor, Color trailerColor)
            {
                Palette p = new Palette();
                p.Set(Paint, Make("paint", paintColor, 0.25f, 0.78f));
                p.Set(PaintAccent, Make("paintAccent", accentColor, 0.2f, 0.7f));
                p.Set(Chassis, Make("chassis", new Color(0.13f, 0.135f, 0.15f), 0.6f, 0.32f));
                p.Set(Rubber, Make("rubber", new Color(0.055f, 0.055f, 0.06f), 0f, 0.18f));
                p.Set(Glass, Make("glass", new Color(0.09f, 0.13f, 0.16f, 0.78f), 0f, 0.95f));
                p.Set(Chrome, Make("chrome", new Color(0.82f, 0.84f, 0.87f), 1f, 0.9f));
                p.Set(PlasticBlack, Make("plastic", new Color(0.09f, 0.09f, 0.1f), 0f, 0.35f));
                p.Set(Grille, Make("grille", new Color(0.2f, 0.21f, 0.23f), 0.75f, 0.55f));
                p.Set(LensClear, MakeLens("lensClear", new Color(0.93f, 0.94f, 0.85f), 0f));
                p.Set(LensRed, MakeLens("lensRed", new Color(0.55f, 0.04f, 0.04f), 0f));
                p.Set(LensAmber, MakeLens("lensAmber", new Color(0.7f, 0.33f, 0.03f), 0f));
                p.Set(TrailerSkin, Make("trailerSkin", trailerColor, 0.35f, 0.6f));
                p.Set(Aluminium, Make("aluminium", new Color(0.64f, 0.66f, 0.69f), 0.85f, 0.65f));
                p.Set(InteriorDark, Make("interior", new Color(0.07f, 0.07f, 0.08f), 0f, 0.2f));
                return p;
            }
        }
    }
}
