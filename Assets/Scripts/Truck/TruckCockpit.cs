using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Procedural cab interior. Built once, welded into a small number of meshes,
    /// and hidden entirely unless the cockpit view is active - so exterior views
    /// pay nothing for it.
    ///
    /// Right-hand drive, matching the Gujarat setting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TruckCockpit : MonoBehaviour
    {
        [Header("Animated parts")]
        public Transform steeringWheel;
        public Transform speedNeedle;
        public Transform fuelNeedle;
        public Transform eyeAnchor;

        [Header("Tuning")]
        public float wheelLockDegrees = 420f;
        public float needleSmooth = 6f;
        public float maxDialKph = 140f;

        private TruckController controller;
        private readonly List<Renderer> interiorRenderers = new List<Renderer>();
        private float wheelAngle, speedAngle, fuelAngle;
        private bool visible = true;

        public void Register(Renderer r)
        {
            if (r != null && !interiorRenderers.Contains(r)) interiorRenderers.Add(r);
        }

        private void Awake()
        {
            controller = GetComponentInParent<TruckController>();
        }

        public void SetInteriorVisible(bool value)
        {
            if (visible == value && interiorRenderers.Count > 0) return;
            visible = value;
            for (int i = 0; i < interiorRenderers.Count; i++)
                if (interiorRenderers[i] != null) interiorRenderers[i].enabled = value;
        }

        private void LateUpdate()
        {
            // Nothing is visible, so nothing needs animating.
            if (!visible || controller == null) return;

            float t = 1f - Mathf.Exp(-needleSmooth * Time.deltaTime);

            if (steeringWheel != null)
            {
                float wanted = -controller.SteerInput * wheelLockDegrees * 0.5f;
                wheelAngle = Mathf.Lerp(wheelAngle, wanted, t);
                steeringWheel.localRotation = Quaternion.Euler(0f, 0f, wheelAngle);
            }

            if (speedNeedle != null)
            {
                float fraction = Mathf.Clamp01(controller.SpeedKph / Mathf.Max(1f, maxDialKph));
                speedAngle = Mathf.Lerp(speedAngle, Mathf.Lerp(120f, -120f, fraction), t);
                speedNeedle.localRotation = Quaternion.Euler(0f, 0f, speedAngle);
            }

            if (fuelNeedle != null)
            {
                float fraction = Mathf.Clamp01(controller.Fuel / 100f);
                fuelAngle = Mathf.Lerp(fuelAngle, Mathf.Lerp(-55f, 55f, fraction), t);
                fuelNeedle.localRotation = Quaternion.Euler(0f, 0f, fuelAngle);
            }
        }
    }

    /// <summary>
    /// Builds the cab interior geometry. Kept separate from the component so the
    /// construction code never runs at play time after the first frame.
    /// </summary>
    public static class TruckCockpitBuilder
    {
        // Interior layout constants (truck local space, +Z forward, +X right).
        // These are tied to the cab the visual pass builds for the player truck:
        // cab floor line 1.10, roof 3.28, front face 3.12, width 2.50.
        private const float DriverX = 0.62f;          // right-hand drive
        private const float FloorY = 1.18f;
        private const float RoofY = 3.16f;
        private const float DashY = 2.00f;
        private const float DashZ = 2.68f;
        private const float ScreenZ = 2.95f;
        private const float HalfWidth = 1.14f;
        private const float SeatZ = 1.62f;
        private const float EyeY = 2.45f;
        private const float EyeZ = 1.82f;

        public static TruckCockpit Build(Transform truckRoot)
        {
            if (truckRoot == null) return null;

            Transform existing = truckRoot.Find("Cockpit");
            if (existing != null) Object.Destroy(existing.gameObject);

            GameObject root = new GameObject("Cockpit");
            root.transform.SetParent(truckRoot, false);
            TruckCockpit cockpit = root.AddComponent<TruckCockpit>();

            TruckMaterialLibrary.Palette palette = TruckMaterialLibrary.Palette.Build(
                new Color(0.10f, 0.11f, 0.13f),    // trim
                new Color(0.16f, 0.17f, 0.20f),    // accent trim
                new Color(0.30f, 0.32f, 0.36f));

            MeshBuilder shell = new MeshBuilder();
            BuildShell(shell);
            BuildDashboard(shell);
            BuildSeats(shell);
            BuildConsole(shell);

            GameObject shellGo = shell.Emit("CockpitShell", root.transform, palette);
            StripAndRegister(shellGo, cockpit);

            BuildInstruments(root.transform, palette, cockpit);
            BuildSteeringWheel(root.transform, palette, cockpit);

            // Driver eye point: above the dash top, behind the windscreen.
            GameObject eye = new GameObject("CockpitEyeAnchor");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(DriverX, EyeY, EyeZ);
            eye.transform.localRotation = Quaternion.identity;
            cockpit.eyeAnchor = eye.transform;

            return cockpit;
        }

        // ------------------------------------------------------------------
        private static void BuildShell(MeshBuilder mb)
        {
            int trim = TruckMaterialLibrary.Paint;
            int dark = TruckMaterialLibrary.InteriorDark;

            // Floor, rear bulkhead, headliner.
            mb.Add(ProcMesh.Box(HalfWidth * 2f, 0.06f, 3.4f), new Vector3(0f, FloorY, 1.20f), dark);
            mb.Add(ProcMesh.Box(HalfWidth * 2f, RoofY - FloorY, 0.08f), new Vector3(0f, (RoofY + FloorY) * 0.5f, -0.52f), trim);
            mb.Add(ProcMesh.Box(HalfWidth * 2f, 0.07f, 3.4f), new Vector3(0f, RoofY, 1.20f), trim);

            // Door cards with an armrest and a window sill.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = HalfWidth * side;
                mb.Add(ProcMesh.Box(0.07f, 1.45f, 2.8f), new Vector3(x, FloorY + 0.75f, 1.35f), trim);
                mb.Add(ProcMesh.Box(0.16f, 0.09f, 0.95f), new Vector3(x - 0.07f * side, FloorY + 1.22f, 1.45f), dark);
                mb.Add(ProcMesh.Box(0.05f, 0.10f, 0.26f), new Vector3(x - 0.10f * side, FloorY + 1.00f, 1.95f), TruckMaterialLibrary.Chrome);

                // A-pillar, raked to meet the windscreen header.
                mb.Add(ProcMesh.Box(0.10f, 1.05f, 0.12f),
                       new Vector3(x - 0.04f * side, 2.74f, ScreenZ - 0.18f),
                       Quaternion.Euler(-16f, 0f, 0f), trim);
            }

            // Windscreen header and lower cowl frame the road correctly.
            mb.Add(ProcMesh.Box(HalfWidth * 2f, 0.14f, 0.20f), new Vector3(0f, RoofY - 0.10f, ScreenZ - 0.10f), trim);
            mb.Add(ProcMesh.Box(HalfWidth * 2f, 0.12f, 0.24f), new Vector3(0f, DashY + 0.26f, ScreenZ - 0.04f), dark);

            // Sun visors.
            for (int side = -1; side <= 1; side += 2)
                mb.Add(ProcMesh.Box(0.92f, 0.04f, 0.26f),
                       new Vector3(0.58f * side, RoofY - 0.19f, ScreenZ - 0.24f),
                       Quaternion.Euler(18f, 0f, 0f), dark);
        }

        private static void BuildDashboard(MeshBuilder mb)
        {
            int trim = TruckMaterialLibrary.Paint;
            int dark = TruckMaterialLibrary.InteriorDark;

            // Main dash face, raked back towards the driver.
            mb.Add(ProcMesh.Box(HalfWidth * 2f - 0.08f, 0.46f, 0.50f),
                   new Vector3(0f, DashY, DashZ), Quaternion.Euler(-10f, 0f, 0f), trim);

            // Dash top pad, narrowing towards the screen.
            mb.Add(ProcMesh.Frustum(HalfWidth * 2f - 0.10f, 0.52f, HalfWidth * 2f - 0.26f, 0.30f, 0.09f),
                   new Vector3(0f, DashY + 0.25f, DashZ + 0.08f), dark);

            // Instrument binnacle hood over the dials.
            mb.Add(ProcMesh.Frustum(0.80f, 0.40f, 0.72f, 0.20f, 0.14f),
                   new Vector3(DriverX, DashY + 0.32f, DashZ - 0.12f),
                   Quaternion.Euler(-12f, 0f, 0f), dark);

            // Centre vents and radio stack.
            for (int i = -1; i <= 1; i += 2)
                mb.Add(ProcMesh.Box(0.22f, 0.10f, 0.05f), new Vector3(i * 0.20f, DashY + 0.12f, DashZ + 0.24f), dark);
            mb.Add(ProcMesh.Box(0.34f, 0.18f, 0.05f), new Vector3(0f, DashY - 0.06f, DashZ + 0.24f), dark);

            // Outboard vents.
            for (int side = -1; side <= 1; side += 2)
                mb.Add(ProcMesh.Box(0.18f, 0.10f, 0.05f), new Vector3(0.92f * side, DashY + 0.10f, DashZ + 0.22f), dark);

            // Glovebox seam on the passenger side.
            mb.Add(ProcMesh.Box(0.62f, 0.02f, 0.04f), new Vector3(-DriverX, DashY - 0.14f, DashZ + 0.25f), dark);
        }

        private static void BuildSeats(MeshBuilder mb)
        {
            int dark = TruckMaterialLibrary.InteriorDark;
            int accent = TruckMaterialLibrary.PaintAccent;

            for (int side = -1; side <= 1; side += 2)
            {
                float x = DriverX * side;
                mb.Add(ProcMesh.Box(0.20f, 0.50f, 0.24f), new Vector3(x, FloorY + 0.25f, SeatZ), dark);       // pedestal
                mb.Add(ProcMesh.Box(0.58f, 0.13f, 0.56f), new Vector3(x, FloorY + 0.50f, SeatZ), accent);      // squab
                mb.Add(ProcMesh.Box(0.56f, 0.70f, 0.14f),
                       new Vector3(x, FloorY + 0.88f, SeatZ - 0.28f), Quaternion.Euler(-8f, 0f, 0f), accent);  // backrest
                mb.Add(ProcMesh.Box(0.30f, 0.20f, 0.12f), new Vector3(x, FloorY + 1.28f, SeatZ - 0.33f), dark); // headrest
                mb.Add(ProcMesh.Box(0.10f, 0.26f, 0.12f), new Vector3(x - 0.34f * side, FloorY + 0.66f, SeatZ - 0.06f), dark); // armrest
            }
        }

        private static void BuildConsole(MeshBuilder mb)
        {
            int dark = TruckMaterialLibrary.InteriorDark;

            // Engine tunnel / centre console between the seats.
            mb.Add(ProcMesh.Frustum(0.62f, 1.60f, 0.50f, 1.40f, 0.42f), new Vector3(0f, FloorY + 0.22f, 1.90f), dark);

            // Gear lever.
            mb.Add(ProcMesh.Cylinder(0.022f, 0.34f, 8, false),
                   new Vector3(0.16f, FloorY + 0.62f, 2.02f), Quaternion.Euler(-14f, 0f, 0f), TruckMaterialLibrary.Chassis);
            mb.Add(ProcMesh.Cylinder(0.055f, 0.07f, 10, true),
                   new Vector3(0.16f, FloorY + 0.80f, 2.06f), dark);

            // Steering column shroud.
            mb.Add(ProcMesh.Cylinder(0.055f, 0.36f, 10, true),
                   new Vector3(DriverX, FloorY + 0.62f, 2.18f), Quaternion.Euler(58f, 0f, 0f), dark);

            // Pedals.
            mb.Add(ProcMesh.Box(0.10f, 0.16f, 0.04f),
                   new Vector3(DriverX - 0.10f, FloorY + 0.14f, 2.52f), Quaternion.Euler(-24f, 0f, 0f), TruckMaterialLibrary.Chassis);
            mb.Add(ProcMesh.Box(0.10f, 0.16f, 0.04f),
                   new Vector3(DriverX + 0.12f, FloorY + 0.14f, 2.52f), Quaternion.Euler(-24f, 0f, 0f), TruckMaterialLibrary.Chassis);
        }

        // ------------------------------------------------------------------
        private static void BuildInstruments(Transform root, TruckMaterialLibrary.Palette palette, TruckCockpit cockpit)
        {
            // Two backlit dials. The glow is a cached emissive material, not a light.
            Material face = TruckMaterialLibrary.Make("dialFace", new Color(0.04f, 0.05f, 0.06f), 0f, 0.4f,
                                                     new Color(0.05f, 0.16f, 0.22f));
            Material needleMat = TruckMaterialLibrary.Make("dialNeedle", new Color(0.85f, 0.18f, 0.12f), 0f, 0.5f,
                                                          new Color(0.75f, 0.10f, 0.06f));

            cockpit.speedNeedle = BuildDial(root, palette, face, needleMat, cockpit,
                                            new Vector3(DriverX - 0.17f, DashY + 0.22f, DashZ - 0.22f), 0.125f);
            cockpit.fuelNeedle = BuildDial(root, palette, face, needleMat, cockpit,
                                           new Vector3(DriverX + 0.19f, DashY + 0.22f, DashZ - 0.22f), 0.095f);

            // Soft dash illumination strip under the binnacle.
            MeshBuilder glow = new MeshBuilder();
            glow.Add(ProcMesh.Box(0.70f, 0.012f, 0.10f), Vector3.zero, TruckMaterialLibrary.LensAmber);
            int[] slots;
            Mesh mesh = glow.Build("dashGlow", out slots);

            GameObject strip = new GameObject("DashIllumination");
            strip.transform.SetParent(root, false);
            strip.transform.localPosition = new Vector3(DriverX, DashY + 0.14f, DashZ - 0.16f);
            strip.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer stripRenderer = strip.AddComponent<MeshRenderer>();
            stripRenderer.sharedMaterial = TruckMaterialLibrary.MakeLens("dashStrip", new Color(0.55f, 0.35f, 0.10f), 1.4f);
            stripRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stripRenderer.receiveShadows = false;
            cockpit.Register(stripRenderer);
        }

        private static Transform BuildDial(Transform root, TruckMaterialLibrary.Palette palette,
                                           Material faceMaterial, Material needleMaterial,
                                           TruckCockpit cockpit, Vector3 position, float radius)
        {
            GameObject dial = new GameObject("Dial");
            dial.transform.SetParent(root, false);
            dial.transform.localPosition = position;
            dial.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            // Bezel + face as one small mesh.
            MeshBuilder mb = new MeshBuilder();
            mb.Add(ProcMesh.Cylinder(radius * 1.12f, 0.02f, 14, true), new Vector3(0f, 0f, -0.012f),
                   Quaternion.Euler(90f, 0f, 0f), TruckMaterialLibrary.Chrome);
            mb.Add(ProcMesh.Cylinder(radius, 0.012f, 14, true), Vector3.zero,
                   Quaternion.Euler(90f, 0f, 0f), TruckMaterialLibrary.InteriorDark);

            // Tick marks around the face.
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(120f, -120f, i / 8f) * Mathf.Deg2Rad;
                mb.Add(ProcMesh.Box(0.012f, 0.026f, 0.008f),
                       new Vector3(Mathf.Sin(a) * radius * 0.78f, Mathf.Cos(a) * radius * 0.78f, 0.010f),
                       TruckMaterialLibrary.Chrome);
            }

            int[] slots;
            Mesh mesh = mb.Build("dial", out slots);
            dial.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer dialRenderer = dial.AddComponent<MeshRenderer>();
            Material[] mats = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                mats[i] = slots[i] == TruckMaterialLibrary.InteriorDark ? faceMaterial : palette.Get(slots[i]);
            dialRenderer.sharedMaterials = mats;
            dialRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dialRenderer.receiveShadows = false;
            cockpit.Register(dialRenderer);

            // Needle pivots about the dial centre.
            GameObject needle = new GameObject("Needle");
            needle.transform.SetParent(dial.transform, false);
            needle.transform.localPosition = new Vector3(0f, 0f, 0.014f);

            MeshBuilder nb = new MeshBuilder();
            nb.Add(ProcMesh.Box(0.010f, radius * 0.82f, 0.006f), new Vector3(0f, radius * 0.34f, 0f),
                   TruckMaterialLibrary.LensRed);
            int[] nslots;
            needle.AddComponent<MeshFilter>().sharedMesh = nb.Build("needle", out nslots);
            MeshRenderer needleRenderer = needle.AddComponent<MeshRenderer>();
            needleRenderer.sharedMaterial = needleMaterial;
            needleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            needleRenderer.receiveShadows = false;
            cockpit.Register(needleRenderer);

            return needle.transform;
        }

        private static void BuildSteeringWheel(Transform root, TruckMaterialLibrary.Palette palette, TruckCockpit cockpit)
        {
            GameObject wheel = new GameObject("SteeringWheel");
            wheel.transform.SetParent(root, false);
            wheel.transform.localPosition = new Vector3(DriverX, FloorY + 0.86f, 2.30f);
            wheel.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);

            MeshBuilder mb = new MeshBuilder();
            const float rimRadius = 0.235f;
            const int segments = 18;

            // Rim approximated by short chords - far cheaper than a real torus and
            // indistinguishable at cockpit distance.
            Mesh chord = ProcMesh.Box(0.030f, 0.030f, (2f * Mathf.PI * rimRadius) / segments * 1.06f);
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * rimRadius, Mathf.Sin(a) * rimRadius, 0f);
                mb.Add(chord, pos, Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg + 90f), TruckMaterialLibrary.InteriorDark);
            }

            // Three spokes and a hub.
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f - 90f) * Mathf.Deg2Rad;
                Vector3 mid = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rimRadius * 0.5f;
                mb.Add(ProcMesh.Box(0.045f, rimRadius * 0.95f, 0.022f), mid,
                       Quaternion.Euler(0f, 0f, -(a * Mathf.Rad2Deg - 90f)), TruckMaterialLibrary.InteriorDark);
            }
            mb.Add(ProcMesh.Cylinder(0.072f, 0.05f, 12, true), new Vector3(0f, 0f, -0.01f),
                   Quaternion.Euler(90f, 0f, 0f), TruckMaterialLibrary.PaintAccent);

            int[] slots;
            wheel.AddComponent<MeshFilter>().sharedMesh = mb.Build("steeringWheel", out slots);
            MeshRenderer renderer = wheel.AddComponent<MeshRenderer>();
            Material[] mats = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++) mats[i] = palette.Get(slots[i]);
            renderer.sharedMaterials = mats;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            cockpit.Register(renderer);
            cockpit.steeringWheel = wheel.transform;
        }

        private static void StripAndRegister(GameObject go, TruckCockpit cockpit)
        {
            TruckVisualUtility.StripColliders(go);
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cockpit.Register(renderers[i]);
            }
        }
    }
}
