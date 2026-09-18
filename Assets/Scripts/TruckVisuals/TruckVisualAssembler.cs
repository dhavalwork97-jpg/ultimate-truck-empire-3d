using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// The only entry point gameplay code needs.
    ///
    ///     TruckVisualAssembler.Apply(truckGameObject, "atlas-hd");
    ///     TruckVisualAssembler.ApplyTrailer(trailerGameObject, null);
    ///
    /// It is additive and idempotent:
    ///  * it never adds, removes or edits physics components;
    ///  * it reads the existing WheelColliders and builds the bodywork around
    ///    them, rather than imposing its own wheelbase;
    ///  * placeholder renderers are disabled (not destroyed) and can be restored;
    ///  * running it twice replaces its own output instead of duplicating it.
    /// </summary>
    public static class TruckVisualAssembler
    {
        public static TruckVisuals Apply(GameObject truckRoot, string modelId)
        {
            return Apply(truckRoot, TruckVisualPresets.Resolve(modelId), true);
        }

        public static TruckVisuals Apply(GameObject truckRoot, string modelId, Color paint)
        {
            TruckVisualSpec spec = TruckVisualPresets.Resolve(modelId);
            spec.paint = paint;
            return Apply(truckRoot, spec, true);
        }

        public static TruckVisuals Apply(GameObject truckRoot, TruckVisualSpec spec, bool hidePlaceholderRenderers)
        {
            if (truckRoot == null)
            {
                Debug.LogWarning("[TruckVisualAssembler] Apply called with a null GameObject.");
                return null;
            }
            if (spec == null) spec = TruckVisualPresets.Resolve(null);

            TruckVisuals visuals = truckRoot.GetComponent<TruckVisuals>();
            if (visuals == null) visuals = truckRoot.AddComponent<TruckVisuals>();
            visuals.ClearGenerated();
            visuals.specId = spec.id;
            visuals.isTrailer = false;

            // ---- wheels -------------------------------------------------------
            WheelCollider[] ordered;
            WheelPlacement[] placements = DerivePlacements(truckRoot.transform, spec, out ordered);

            // ---- hide the old placeholder look --------------------------------
            if (hidePlaceholderRenderers) HidePlaceholders(truckRoot, visuals);

            // ---- build --------------------------------------------------------
            TruckBodyBuilder builder = new TruckBodyBuilder();
            TruckMaterialLibrary.Palette palette =
                TruckMaterialLibrary.Palette.Build(spec.paint, spec.accent, new Color(0.86f, 0.87f, 0.88f));

            TruckBuildResult result = builder.Build(truckRoot.transform, spec, palette, placements);

            TrackGenerated(visuals, truckRoot.transform, result);
            visuals.bodyRoot = result.bodyRoot;
            visuals.fifthWheelAnchor = result.fifthWheelAnchor;
            visuals.wheelVisuals = result.wheelVisuals;
            visuals.generatedVertexCount = result.vertexCount;

            // Cosmetic geometry must never carry colliders.
            for (int i = 0; i < visuals.generatedObjects.Count; i++)
                TruckVisualUtility.StripColliders(visuals.generatedObjects[i]);

            // ---- wheel sync ---------------------------------------------------
            if (ordered != null && ordered.Length > 0)
            {
                TruckWheelVisualSync sync = truckRoot.GetComponent<TruckWheelVisualSync>();
                if (sync == null) sync = truckRoot.AddComponent<TruckWheelVisualSync>();
                sync.Configure(ordered, result.wheelVisuals);
                visuals.wheelSync = sync;
            }

            // ---- lights -------------------------------------------------------
            TruckLightRig rig = truckRoot.GetComponent<TruckLightRig>();
            if (rig == null) rig = truckRoot.AddComponent<TruckLightRig>();
            WireLightRig(rig, result.lights);
            visuals.lightRig = rig;

            return visuals;
        }

        // ==================================================================
        // Trailer
        // ==================================================================
        public static TruckVisuals ApplyTrailer(GameObject trailerRoot, TrailerVisualSpec spec)
        {
            return ApplyTrailer(trailerRoot, spec, true);
        }

        public static TruckVisuals ApplyTrailer(GameObject trailerRoot, TrailerVisualSpec spec, bool hidePlaceholderRenderers)
        {
            if (trailerRoot == null)
            {
                Debug.LogWarning("[TruckVisualAssembler] ApplyTrailer called with a null GameObject.");
                return null;
            }
            if (spec == null) spec = new TrailerVisualSpec();

            TruckVisuals visuals = trailerRoot.GetComponent<TruckVisuals>();
            if (visuals == null) visuals = trailerRoot.AddComponent<TruckVisuals>();
            visuals.ClearGenerated();
            visuals.specId = spec.id;
            visuals.isTrailer = true;

            if (hidePlaceholderRenderers) HidePlaceholders(trailerRoot, visuals);

            TruckMaterialLibrary.Palette palette =
                TruckMaterialLibrary.Palette.Build(spec.skin, new Color(0.18f, 0.19f, 0.21f), spec.skin);

            TrailerVisualBuilder builder = new TrailerVisualBuilder();
            TruckBuildResult result = builder.Build(trailerRoot.transform, spec, palette);

            TrackGenerated(visuals, trailerRoot.transform, result);
            visuals.bodyRoot = result.bodyRoot;
            visuals.fifthWheelAnchor = result.fifthWheelAnchor;
            visuals.wheelVisuals = result.wheelVisuals;
            visuals.generatedVertexCount = result.vertexCount;

            for (int i = 0; i < visuals.generatedObjects.Count; i++)
                TruckVisualUtility.StripColliders(visuals.generatedObjects[i]);

            // Trailers usually have their own WheelColliders; wire them if the
            // count matches, otherwise leave the meshes parked at rest height.
            WheelCollider[] cols = CollectWheelColliders(trailerRoot.transform);
            if (cols.Length > 0 && result.wheelVisuals != null && cols.Length == result.wheelVisuals.Length)
            {
                WheelCollider[] ordered = OrderColliders(trailerRoot.transform, cols, false);
                TruckWheelVisualSync sync = trailerRoot.GetComponent<TruckWheelVisualSync>();
                if (sync == null) sync = trailerRoot.AddComponent<TruckWheelVisualSync>();
                sync.Configure(ordered, result.wheelVisuals);
                visuals.wheelSync = sync;
            }

            TruckLightRig rig = trailerRoot.GetComponent<TruckLightRig>();
            if (rig == null) rig = trailerRoot.AddComponent<TruckLightRig>();
            rig.allowRealtimeHeadlights = false;
            WireLightRig(rig, result.lights);
            visuals.lightRig = rig;

            return visuals;
        }

        /// <summary>
        /// Makes the tractor's light rig drive the trailer's lamps too, so brake
        /// lights and indicators stay consistent across the whole combination.
        /// </summary>
        public static void LinkTrailerLights(TruckVisuals truck, TruckVisuals trailer)
        {
            if (truck == null || trailer == null) return;
            if (truck.lightRig == null || trailer.lightRig == null) return;

            TruckLightSet set = new TruckLightSet();
            set.tailLights.AddRange(trailer.lightRig.tailLamps);
            set.brakeLights.AddRange(trailer.lightRig.brakeLamps);
            set.reverseLights.AddRange(trailer.lightRig.reverseLamps);
            set.indicatorsLeft.AddRange(trailer.lightRig.indicatorLeft);
            set.indicatorsRight.AddRange(trailer.lightRig.indicatorRight);
            set.markerLights.AddRange(trailer.lightRig.markerLamps);

            truck.lightRig.Absorb(set);
            trailer.lightRig.enabled = false;
        }

        /// <summary>
        /// Positions a trailer so its kingpin sits on the tractor's fifth wheel.
        /// Only use this for trailers that are NOT already constrained by a joint;
        /// teleporting a jointed rigidbody will make the physics explode.
        /// </summary>
        public static void AlignTrailerToFifthWheel(TruckVisuals truck, TruckVisuals trailer)
        {
            if (truck == null || trailer == null) return;
            if (truck.fifthWheelAnchor == null || trailer.fifthWheelAnchor == null) return;

            Transform t = trailer.transform;
            Vector3 offset = t.position - trailer.fifthWheelAnchor.position;
            t.rotation = truck.transform.rotation;
            t.position = truck.fifthWheelAnchor.position + offset;
        }

        // ==================================================================
        // Internals
        // ==================================================================
        private static void TrackGenerated(TruckVisuals visuals, Transform root, TruckBuildResult result)
        {
            // Everything the builders create lives under one of two containers.
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                string n = child.name;
                if (n == "TruckVisuals" || n == "TrailerVisuals" ||
                    n == "WheelVisuals" || n == "TrailerWheelVisuals")
                {
                    if (!visuals.generatedObjects.Contains(child.gameObject))
                        visuals.Track(child.gameObject);
                }
            }
        }

        private static void WireLightRig(TruckLightRig rig, TruckLightSet set)
        {
            rig.headlightLenses.Clear();
            rig.tailLamps.Clear();
            rig.brakeLamps.Clear();
            rig.reverseLamps.Clear();
            rig.indicatorLeft.Clear();
            rig.indicatorRight.Clear();
            rig.markerLamps.Clear();
            rig.headlightAnchors.Clear();

            rig.headlightLenses.AddRange(set.headlights);
            rig.tailLamps.AddRange(set.tailLights);
            rig.brakeLamps.AddRange(set.brakeLights);
            rig.reverseLamps.AddRange(set.reverseLights);
            rig.indicatorLeft.AddRange(set.indicatorsLeft);
            rig.indicatorRight.AddRange(set.indicatorsRight);
            rig.markerLamps.AddRange(set.markerLights);
            rig.headlightAnchors.AddRange(set.headlightAnchors);
            rig.cabInteriorAnchor = set.cabInteriorAnchor;
        }

        private static void HidePlaceholders(GameObject root, TruckVisuals visuals)
        {
            visuals.RestoreHiddenRenderers();

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (IsGenerated(r.transform)) continue;

                r.enabled = false;
                visuals.hiddenRenderers.Add(r);
            }
        }

        private static bool IsGenerated(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == "TruckVisuals" || n == "TrailerVisuals" ||
                    n == "WheelVisuals" || n == "TrailerWheelVisuals") return true;
                t = t.parent;
            }
            return false;
        }

        private static WheelCollider[] CollectWheelColliders(Transform root)
        {
            WheelCollider[] found = root.GetComponentsInChildren<WheelCollider>(true);
            return found ?? new WheelCollider[0];
        }

        /// <summary>Sorts wheels front to back, then left to right within an axle.</summary>
        private static WheelCollider[] OrderColliders(Transform root, WheelCollider[] cols, bool frontFirst)
        {
            List<WheelCollider> list = new List<WheelCollider>(cols);
            list.Sort(delegate (WheelCollider a, WheelCollider b)
            {
                Vector3 la = root.InverseTransformPoint(a.transform.position);
                Vector3 lb = root.InverseTransformPoint(b.transform.position);
                if (Mathf.Abs(la.z - lb.z) > 0.25f)
                    return frontFirst ? lb.z.CompareTo(la.z) : la.z.CompareTo(lb.z);
                return la.x.CompareTo(lb.x);
            });
            return list.ToArray();
        }

        /// <summary>
        /// Builds the visual wheel layout. Real WheelColliders win; the spec is
        /// only a fallback so the model still looks right in a scene that has not
        /// had its physics set up yet.
        /// </summary>
        private static WheelPlacement[] DerivePlacements(Transform root, TruckVisualSpec spec, out WheelCollider[] ordered)
        {
            WheelCollider[] cols = CollectWheelColliders(root);

            if (cols.Length == 0)
            {
                ordered = new WheelCollider[0];
                return SynthesisePlacements(spec);
            }

            ordered = OrderColliders(root, cols, true);
            List<WheelPlacement> placements = new List<WheelPlacement>(ordered.Length);

            float frontZ = float.NegativeInfinity;
            for (int i = 0; i < ordered.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(ordered[i].transform.TransformPoint(ordered[i].center));
                if (local.z > frontZ) frontZ = local.z;
            }

            int axleIndex = -1;
            float lastZ = float.PositiveInfinity;

            for (int i = 0; i < ordered.Length; i++)
            {
                WheelCollider wc = ordered[i];
                Vector3 local = root.InverseTransformPoint(wc.transform.TransformPoint(wc.center));

                if (Mathf.Abs(local.z - lastZ) > 0.25f)
                {
                    axleIndex++;
                    lastZ = local.z;
                }

                bool steering = Mathf.Abs(local.z - frontZ) < 0.25f;

                WheelPlacement w = new WheelPlacement();
                // Rest height: the collider anchor hangs above the wheel centre by
                // roughly half the suspension travel when the vehicle is settled.
                w.localPosition = new Vector3(local.x, local.y - wc.suspensionDistance * 0.5f, local.z);
                w.radius = wc.radius > 0.01f ? wc.radius : spec.wheelRadius;
                w.width = spec.wheelWidth;
                w.steering = steering;
                w.dual = !steering && spec.dualRearWheels;
                w.isLeft = local.x < 0f;
                w.axleIndex = Mathf.Max(0, axleIndex);
                placements.Add(w);
            }

            return placements.ToArray();
        }

        private static WheelPlacement[] SynthesisePlacements(TruckVisualSpec spec)
        {
            List<WheelPlacement> list = new List<WheelPlacement>();

            for (int side = -1; side <= 1; side += 2)
            {
                WheelPlacement w = new WheelPlacement();
                w.localPosition = new Vector3(spec.trackWidth * 0.5f * side, spec.wheelRadius, spec.frontAxleZ);
                w.radius = spec.wheelRadius;
                w.width = spec.wheelWidth;
                w.steering = true;
                w.dual = false;
                w.isLeft = side < 0;
                w.axleIndex = 0;
                list.Add(w);
            }

            int rearAxles = Mathf.Clamp(spec.rearAxleCount, 1, 4);
            for (int a = 0; a < rearAxles; a++)
            {
                float z = spec.rearAxleZ - a * spec.rearAxleSpacing;
                for (int side = -1; side <= 1; side += 2)
                {
                    WheelPlacement w = new WheelPlacement();
                    w.localPosition = new Vector3(spec.trackWidth * 0.5f * side, spec.wheelRadius, z);
                    w.radius = spec.wheelRadius;
                    w.width = spec.wheelWidth;
                    w.steering = false;
                    w.dual = spec.dualRearWheels;
                    w.isLeft = side < 0;
                    w.axleIndex = a + 1;
                    list.Add(w);
                }
            }

            return list.ToArray();
        }
    }
}
