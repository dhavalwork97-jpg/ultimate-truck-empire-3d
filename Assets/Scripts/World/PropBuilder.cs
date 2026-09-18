using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Street furniture, vegetation and industrial clutter.
    ///
    /// Most props are added into a shared <see cref="MeshBuilder"/> and emitted as
    /// one batched object per area, so a hundred bollards, pallets and fence posts
    /// cost one renderer rather than a hundred GameObjects. Only props that need
    /// their own behaviour - street lights and traffic lights - become individual
    /// objects.
    ///
    /// Nothing here gets a collider except guard rails and containers, which a
    /// truck can plausibly hit.
    /// </summary>
    public static class PropBuilder
    {
        private static int S(WorldSurface surface) => WorldPaletteAdapter.Slot(surface);

        // ==================================================================
        // Vegetation
        // ==================================================================
        public static void AddTree(MeshBuilder mb, Vector3 position, float scale, int seed, bool dry)
        {
            float trunkHeight = 2.2f * scale;
            mb.Add(ProcMesh.Cylinder(0.20f * scale, trunkHeight, 7, false),
                   position + new Vector3(0f, trunkHeight * 0.5f, 0f), S(WorldSurface.TreeTrunk));

            int crownSlot = S(dry ? WorldSurface.TreeCrownDry : WorldSurface.TreeCrown);
            int blobs = 2 + (Mathf.Abs(seed) % 3);
            for (int i = 0; i < blobs; i++)
            {
                float angle = (seed * 37 + i * 113) % 360 * Mathf.Deg2Rad;
                float radius = (0.35f + 0.22f * i) * scale;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, trunkHeight + 0.55f * scale + i * 0.35f * scale, Mathf.Sin(angle) * radius);
                // Low-poly crown: a squashed 8-segment sphere stand-in built from a
                // short wide cylinder plus a cap, which reads fine at road distance.
                mb.Add(ProcMesh.Cylinder((1.35f - i * 0.25f) * scale, (1.5f - i * 0.2f) * scale, 8, true),
                       position + offset, crownSlot);
            }
        }

        public static void AddBush(MeshBuilder mb, Vector3 position, float scale)
        {
            mb.Add(ProcMesh.Cylinder(0.85f * scale, 0.9f * scale, 7, true),
                   position + new Vector3(0f, 0.45f * scale, 0f), S(WorldSurface.TreeCrown));
        }

        /// <summary>Flat ground patch used to break up the uniform terrain colour.</summary>
        public static void AddGroundPatch(MeshBuilder mb, Vector3 position, float sizeX, float sizeZ, WorldSurface surface)
        {
            mb.Add(ProcMesh.Box(sizeX, 0.04f, sizeZ), position + new Vector3(0f, RoadBuilder.GroundY + 0.03f, 0f), S(surface));
        }

        // ==================================================================
        // Furniture
        // ==================================================================
        public static void AddBollard(MeshBuilder mb, Vector3 position)
        {
            mb.Add(ProcMesh.Cylinder(0.12f, 1.0f, 8, true), position + new Vector3(0f, 0.5f, 0f), S(WorldSurface.MetalPainted));
            mb.Add(ProcMesh.Cylinder(0.13f, 0.16f, 8, false), position + new Vector3(0f, 0.82f, 0f), S(WorldSurface.WarningStripe));
        }

        public static void AddFenceRun(MeshBuilder mb, Vector3 from, Vector3 to, float height)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;

            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 centre = (from + to) * 0.5f;

            // Mesh panel plus rails, then posts every three metres.
            mb.Add(ProcMesh.Box(0.05f, height * 0.9f, length), centre + new Vector3(0f, height * 0.55f, 0f), rotation, S(WorldSurface.Fence));
            mb.Add(ProcMesh.Box(0.09f, 0.09f, length), centre + new Vector3(0f, height, 0f), rotation, S(WorldSurface.Pole));

            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 3f));
            Mesh post = ProcMesh.Box(0.14f, height, 0.14f);
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)posts);
                mb.Add(post, p + new Vector3(0f, height * 0.5f, 0f), rotation, S(WorldSurface.Pole));
            }
        }

        public static void AddGuardRail(MeshBuilder mb, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;

            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 centre = (from + to) * 0.5f;

            mb.Add(ProcMesh.Box(0.10f, 0.34f, length), centre + new Vector3(0f, 0.72f, 0f), rotation, S(WorldSurface.Metal));
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 4f));
            Mesh post = ProcMesh.Box(0.12f, 0.75f, 0.12f);
            for (int i = 0; i <= posts; i++)
                mb.Add(post, Vector3.Lerp(from, to, i / (float)posts) + new Vector3(0f, 0.37f, 0f), rotation, S(WorldSurface.Pole));
        }

        public static void AddUtilityPole(MeshBuilder mb, Vector3 position)
        {
            mb.Add(ProcMesh.Cylinder(0.16f, 9f, 8, false), position + new Vector3(0f, 4.5f, 0f), S(WorldSurface.Timber));
            mb.Add(ProcMesh.Box(2.6f, 0.14f, 0.14f), position + new Vector3(0f, 8.3f, 0f), S(WorldSurface.Timber));
            mb.Add(ProcMesh.Box(1.9f, 0.12f, 0.12f), position + new Vector3(0f, 7.6f, 0f), S(WorldSurface.Timber));
        }

        public static void AddDumpster(MeshBuilder mb, Vector3 position, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            mb.Add(ProcMesh.Frustum(2.0f, 1.3f, 2.2f, 1.5f, 1.2f), position + new Vector3(0f, 0.6f, 0f), rotation, S(WorldSurface.MetalPainted));
            mb.Add(ProcMesh.Box(2.25f, 0.08f, 1.55f), position + new Vector3(0f, 1.24f, 0f), rotation, S(WorldSurface.Metal));
        }

        public static void AddPalletStack(MeshBuilder mb, Vector3 position, int layers, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Mesh pallet = ProcMesh.Box(1.2f, 0.14f, 1.0f);
            for (int i = 0; i < layers; i++)
                mb.Add(pallet, position + new Vector3(0f, 0.07f + i * 0.16f, 0f), rotation, S(WorldSurface.Timber));
        }

        public static void AddContainer(MeshBuilder mb, Vector3 position, float yaw, int colourIndex, bool tall)
        {
            WorldSurface skin = colourIndex % 3 == 0 ? WorldSurface.ContainerRed
                              : colourIndex % 3 == 1 ? WorldSurface.ContainerBlue
                              : WorldSurface.ContainerGreen;

            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            float height = tall ? 2.9f : 2.6f;
            mb.Add(ProcMesh.Box(6.1f, height, 2.44f), position + new Vector3(0f, height * 0.5f, 0f), rotation, S(skin));

            // Corrugation ribs and end doors.
            Mesh rib = ProcMesh.Box(0.10f, height * 0.88f, 0.08f);
            for (int i = 0; i < 12; i++)
            {
                float x = Mathf.Lerp(-2.7f, 2.7f, (i + 0.5f) / 12f);
                mb.Add(rib, position + rotation * new Vector3(x, height * 0.5f, 1.24f), rotation, S(skin));
                mb.Add(rib, position + rotation * new Vector3(x, height * 0.5f, -1.24f), rotation, S(skin));
            }
            mb.Add(ProcMesh.Box(0.12f, height * 0.9f, 2.3f), position + rotation * new Vector3(3.06f, height * 0.5f, 0f), rotation, S(WorldSurface.Metal));
        }

        // ==================================================================
        // Signage
        // ==================================================================
        /// <summary>Post-mounted sign board. Text is optional and used sparingly.</summary>
        public static GameObject CreateSign(Transform parent, Vector3 position, float yaw, float boardWidth,
                                            float boardHeight, float postHeight, WorldSurface boardSurface, string text)
        {
            MeshBuilder mb = new MeshBuilder();
            mb.Add(ProcMesh.Cylinder(0.09f, postHeight, 8, false), new Vector3(-boardWidth * 0.3f, postHeight * 0.5f, 0f), S(WorldSurface.Pole));
            mb.Add(ProcMesh.Cylinder(0.09f, postHeight, 8, false), new Vector3(boardWidth * 0.3f, postHeight * 0.5f, 0f), S(WorldSurface.Pole));
            mb.Add(ProcMesh.Box(boardWidth, boardHeight, 0.12f), new Vector3(0f, postHeight + boardHeight * 0.5f, 0f), S(boardSurface));
            mb.Add(ProcMesh.Box(boardWidth * 0.94f, boardHeight * 0.86f, 0.05f),
                   new Vector3(0f, postHeight + boardHeight * 0.5f, 0.09f), S(WorldSurface.SignWhite));

            GameObject go = mb.Emit("Sign", parent, WorldPaletteAdapter.Palette);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (!string.IsNullOrEmpty(text)) AddSignText(go.transform, text, boardWidth, boardHeight, postHeight, boardSurface);
            return go;
        }

        private static void AddSignText(Transform signRoot, string text, float boardWidth, float boardHeight, float postHeight, WorldSurface boardSurface)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;

            GameObject go = new GameObject("Sign Text");
            go.transform.SetParent(signRoot, false);
            go.transform.localPosition = new Vector3(0f, postHeight + boardHeight * 0.5f, 0.13f);
            go.transform.localRotation = Quaternion.identity;

            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 64;
            mesh.characterSize = boardHeight * 0.16f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = boardSurface == WorldSurface.SignWhite ? new Color(0.08f, 0.10f, 0.14f) : new Color(0.08f, 0.10f, 0.14f);

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = font.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // ==================================================================
        // Lit props
        // ==================================================================
        /// <summary>
        /// Street light. The pole and lamp head are geometry; the actual
        /// <see cref="Light"/> is registered with <see cref="StreetLightManager"/>
        /// and only switched on at night, near the player.
        /// </summary>
        public static GameObject CreateStreetLight(Transform parent, Vector3 position, float yaw, bool mirrored)
        {
            MeshBuilder mb = new MeshBuilder();
            const float poleHeight = 8.2f;
            float armDirection = mirrored ? -1f : 1f;

            mb.Add(ProcMesh.Cylinder(0.14f, poleHeight, 8, false), new Vector3(0f, poleHeight * 0.5f, 0f), S(WorldSurface.Pole));
            mb.Add(ProcMesh.Cylinder(0.26f, 0.5f, 8, true), new Vector3(0f, 0.25f, 0f), S(WorldSurface.Concrete));
            mb.Add(ProcMesh.Box(2.4f, 0.13f, 0.13f), new Vector3(1.2f * armDirection, poleHeight - 0.2f, 0f),
                   Quaternion.Euler(0f, 0f, -6f * armDirection), S(WorldSurface.Pole));

            GameObject go = mb.Emit("Street Light", parent, WorldPaletteAdapter.Palette);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Lamp head is a separate small renderer so its material can glow.
            MeshBuilder headBuilder = new MeshBuilder();
            headBuilder.Add(ProcMesh.Frustum(0.75f, 0.42f, 0.55f, 0.32f, 0.22f), Vector3.zero, S(WorldSurface.Metal));
            int[] slots;
            Mesh headMesh = headBuilder.Build("lampHead", out slots);

            GameObject head = new GameObject("Lamp Head");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(2.3f * armDirection, poleHeight - 0.42f, 0f);
            head.AddComponent<MeshFilter>().sharedMesh = headMesh;
            MeshRenderer headRenderer = head.AddComponent<MeshRenderer>();
            headRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            headRenderer.receiveShadows = false;

            GameObject lightGo = new GameObject("Lamp");
            lightGo.transform.SetParent(head.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 22f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.86f, 0.62f);
            light.shadows = LightShadows.None;
            light.enabled = false;

            StreetLight component = go.AddComponent<StreetLight>();
            component.Configure(light, headRenderer);
            return go;
        }

        /// <summary>Traffic signal. Emissive heads only - signals never get realtime lights.</summary>
        public static GameObject CreateTrafficLight(Transform parent, Vector3 position, float yaw)
        {
            MeshBuilder mb = new MeshBuilder();
            const float poleHeight = 6.4f;
            mb.Add(ProcMesh.Cylinder(0.13f, poleHeight, 8, false), new Vector3(0f, poleHeight * 0.5f, 0f), S(WorldSurface.Pole));
            mb.Add(ProcMesh.Box(3.4f, 0.14f, 0.14f), new Vector3(1.6f, poleHeight - 0.15f, 0f), S(WorldSurface.Pole));
            mb.Add(ProcMesh.Box(0.42f, 1.25f, 0.34f), new Vector3(3.0f, poleHeight - 0.85f, 0f), S(WorldSurface.ConcreteDark));

            GameObject go = mb.Emit("Traffic Light", parent, WorldPaletteAdapter.Palette);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            AddSignalLens(go.transform, new Vector3(3.0f, poleHeight - 0.45f, 0.20f), new Color(0.75f, 0.12f, 0.08f), 2.4f);
            AddSignalLens(go.transform, new Vector3(3.0f, poleHeight - 0.85f, 0.20f), new Color(0.70f, 0.55f, 0.08f), 0f);
            AddSignalLens(go.transform, new Vector3(3.0f, poleHeight - 1.25f, 0.20f), new Color(0.10f, 0.55f, 0.20f), 0f);
            return go;
        }

        private static void AddSignalLens(Transform parent, Vector3 localPosition, Color colour, float glow)
        {
            MeshBuilder mb = new MeshBuilder();
            mb.Add(ProcMesh.Cylinder(0.13f, 0.06f, 10, true), Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 0);
            int[] slots;
            Mesh mesh = mb.Build("lens", out slots);

            GameObject go = new GameObject("Signal Lens");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = TruckMaterialLibrary.MakeLens("signal", colour, glow);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    /// <summary>One managed street lamp. Holds no per-frame logic of its own.</summary>
    public sealed class StreetLight : MonoBehaviour
    {
        private Light lamp;
        private Renderer head;
        private bool state;

        public void Configure(Light light, Renderer headRenderer)
        {
            lamp = light;
            head = headRenderer;
            Apply(false, true);
            StreetLightManager.Register(this);
        }

        private void OnDestroy() => StreetLightManager.Unregister(this);

        public void Apply(bool on, bool force)
        {
            if (state == on && !force) return;
            state = on;
            if (lamp != null) lamp.enabled = on;
            if (head != null)
                head.sharedMaterial = on
                    ? TruckMaterialLibrary.MakeLens("lampHeadOn", new Color(1f, 0.88f, 0.66f), 3.0f)
                    : WorldPalette.Get(WorldSurface.Metal);
        }
    }

    /// <summary>
    /// Keeps the number of live street lamps under control: lamps are only on at
    /// night, and only the closest few to the player are enabled. Everything else
    /// stays disabled, so a hundred lamp posts never means a hundred realtime
    /// lights. Re-evaluated a few times a second, not per frame.
    /// </summary>
    public sealed class StreetLightManager : MonoBehaviour
    {
        private static readonly List<StreetLight> registry = new List<StreetLight>();
        private static StreetLightManager instance;

        [SerializeField] private int maxActiveLights = 10;
        [SerializeField] private float activationRadius = 70f;
        [SerializeField] private float evaluateInterval = 0.35f;

        private Transform focus;
        private float nextEvaluate;
        private bool nightMode;

        public static void Register(StreetLight light)
        {
            if (light != null && !registry.Contains(light)) registry.Add(light);
        }

        public static void Unregister(StreetLight light) => registry.Remove(light);

        public static StreetLightManager Ensure()
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<StreetLightManager>();
            if (instance == null) instance = new GameObject("Street Light Manager").AddComponent<StreetLightManager>();
            return instance;
        }

        /// <summary>Called by the atmosphere system when the light level changes.</summary>
        public static void SetNight(bool night)
        {
            StreetLightManager manager = Ensure();
            if (manager.nightMode == night) return;
            manager.nightMode = night;
            manager.nextEvaluate = 0f;
            if (!night)
                for (int i = 0; i < registry.Count; i++)
                    if (registry[i] != null) registry[i].Apply(false, false);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        private void Update()
        {
            if (!nightMode || Time.time < nextEvaluate) return;
            nextEvaluate = Time.time + evaluateInterval;

            if (focus == null)
            {
                Camera main = Camera.main;
                focus = main != null ? main.transform : null;
                if (focus == null) return;
            }

            Vector3 origin = focus.position;
            float radiusSqr = activationRadius * activationRadius;
            int enabled = 0;

            for (int i = 0; i < registry.Count; i++)
            {
                StreetLight light = registry[i];
                if (light == null) continue;

                Vector3 delta = light.transform.position - origin;
                delta.y = 0f;
                bool inRange = delta.sqrMagnitude < radiusSqr && enabled < maxActiveLights;
                if (inRange) enabled++;
                light.Apply(inRange, false);
            }
        }
    }
}
