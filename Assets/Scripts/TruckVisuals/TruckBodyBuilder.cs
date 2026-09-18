using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>Where one visible wheel should sit and how it should look.</summary>
    public struct WheelPlacement
    {
        public Vector3 localPosition;
        public float radius;
        public float width;
        public bool steering;
        public bool dual;
        public bool isLeft;
        public int axleIndex;
    }

    /// <summary>Renderers grouped by lamp function so the light rig can drive them.</summary>
    public class TruckLightSet
    {
        public readonly List<Renderer> headlights = new List<Renderer>();
        public readonly List<Renderer> tailLights = new List<Renderer>();
        public readonly List<Renderer> brakeLights = new List<Renderer>();
        public readonly List<Renderer> reverseLights = new List<Renderer>();
        public readonly List<Renderer> indicatorsLeft = new List<Renderer>();
        public readonly List<Renderer> indicatorsRight = new List<Renderer>();
        public readonly List<Renderer> markerLights = new List<Renderer>();
        public readonly List<Transform> headlightAnchors = new List<Transform>();
        public Transform cabInteriorAnchor;
    }

    public class TruckBuildResult
    {
        public GameObject bodyRoot;
        public Transform[] wheelVisuals;
        public WheelPlacement[] placements;
        public Transform fifthWheelAnchor;
        public TruckLightSet lights = new TruckLightSet();
        public int vertexCount;
    }

    /// <summary>
    /// Builds a complete heavy-truck silhouette out of procedural geometry.
    ///
    /// Everything that never moves is welded into ONE mesh with a submesh per
    /// material, so a finished truck is roughly:
    ///   1 body renderer + 1 renderer per wheel + a few small lamp renderers.
    ///
    /// The design is entirely fictional. No real manufacturer's trade dress,
    /// badge or model geometry is reproduced.
    /// </summary>
    public class TruckBodyBuilder
    {
        // Maps a profile built in XY (X = forward, Y = up) onto the world so that
        // the extrusion depth runs across the vehicle (X).
        private static readonly Quaternion ProfileToSide = Quaternion.Euler(0f, -90f, 0f);
        private static readonly Quaternion AxisToX = Quaternion.Euler(0f, 0f, 90f);
        private static readonly Quaternion AxisToZ = Quaternion.Euler(90f, 0f, 0f);

        private TruckVisualSpec _s;
        private TruckMaterialLibrary.Palette _p;
        private MeshBuilder _mb;
        private TruckBuildResult _r;
        private Transform _root;

        // derived layout
        private float _cabBaseY, _cabTotalLength, _cabFrontZ, _cabCenterZ;
        private float _bumperCenterY, _frameBottomY;

        public static void ClearMeshCache()
        {
            TruckVisualUtility.ClearCaches();
        }

        public TruckBuildResult Build(Transform parent, TruckVisualSpec spec,
                                      TruckMaterialLibrary.Palette palette,
                                      WheelPlacement[] wheels)
        {
            _s = spec;
            _p = palette;
            _mb = new MeshBuilder();
            _r = new TruckBuildResult();
            _r.placements = wheels;

            GameObject container = new GameObject("TruckVisuals");
            container.transform.SetParent(parent, false);
            _root = container.transform;

            // ---- derived layout ------------------------------------------------
            _frameBottomY = _s.frameTopY - _s.frameHeight;
            _bumperCenterY = Mathf.Max(_frameBottomY - 0.10f, _s.bumperHeight * 0.5f + 0.16f);
            _cabTotalLength = _s.cabLength + (_s.sleeper ? _s.sleeperLength : 0f);
            _cabFrontZ = _s.layout == CabLayout.Conventional
                ? _s.frameFrontZ - _s.bumperDepth - _s.hoodLength
                : _s.frameFrontZ - _s.bumperDepth * 0.6f;
            _cabBaseY = _s.frameTopY + 0.05f;
            _cabCenterZ = _cabFrontZ - _cabTotalLength * 0.5f;

            BuildChassis();
            BuildCab();
            BuildGlass();
            if (_s.layout == CabLayout.Conventional) BuildHood();
            BuildFrontEnd();
            BuildFuelTanksAndSteps();
            BuildExhaust();
            BuildFenders();
            if (_s.fifthWheel) BuildFifthWheel();
            BuildRearEnd();

            GameObject body = _mb.Emit("Body", _root, _p);
            DisableShadowsIfTiny(body, false);
            _r.bodyRoot = body;
            _r.vertexCount = _mb.VertexCount;

            if (_s.mirrors) BuildMirrors();
            BuildLamps();
            BuildWheels(wheels);

            return _r;
        }

        // ==================================================================
        // Chassis
        // ==================================================================
        private void BuildChassis()
        {
            float railLength = _s.frameFrontZ - _s.frameRearZ;
            float railCenterZ = (_s.frameFrontZ + _s.frameRearZ) * 0.5f;
            float railY = _s.frameTopY - _s.frameHeight * 0.5f;
            float halfSpacing = _s.frameRailSpacing * 0.5f;

            Mesh rail = ProcMesh.Extrude(
                ProcMesh.ChannelProfile(_s.frameHeight, 0.09f, 0.035f), railLength);

            // Right rail: channel opens towards +X. Left rail is the same mesh
            // turned 180 degrees about Y, which mirrors it without flipping winding.
            _mb.Add(rail, new Vector3(halfSpacing, railY, railCenterZ),
                    Quaternion.identity, TruckMaterialLibrary.Chassis);
            _mb.Add(rail, new Vector3(-halfSpacing, railY, railCenterZ),
                    Quaternion.Euler(0f, 180f, 0f), TruckMaterialLibrary.Chassis);

            // Crossmembers
            Mesh cross = ProcMesh.Box(_s.frameRailSpacing, _s.frameHeight * 0.55f, 0.10f);
            int crossCount = 5;
            for (int i = 0; i < crossCount; i++)
            {
                float t = (i + 0.5f) / crossCount;
                float z = Mathf.Lerp(_s.frameRearZ + 0.25f, _s.frameFrontZ - 0.25f, t);
                _mb.Add(cross, new Vector3(0f, railY, z), TruckMaterialLibrary.Chassis);
            }

            // Axle beams between the wheels of each axle line.
            AddAxleBeams();
        }

        private void AddAxleBeams()
        {
            if (_r.placements == null) return;

            Dictionary<int, float> axleZ = new Dictionary<int, float>();
            Dictionary<int, float> axleY = new Dictionary<int, float>();
            Dictionary<int, float> axleSpan = new Dictionary<int, float>();

            for (int i = 0; i < _r.placements.Length; i++)
            {
                WheelPlacement w = _r.placements[i];
                axleZ[w.axleIndex] = w.localPosition.z;
                axleY[w.axleIndex] = w.localPosition.y;
                float span = Mathf.Abs(w.localPosition.x) * 2f;
                if (!axleSpan.ContainsKey(w.axleIndex) || span > axleSpan[w.axleIndex])
                    axleSpan[w.axleIndex] = span;
            }

            foreach (KeyValuePair<int, float> kv in axleZ)
            {
                float span = axleSpan[kv.Key];
                Mesh beam = ProcMesh.Box(span * 0.92f, 0.16f, 0.16f);
                _mb.Add(beam, new Vector3(0f, axleY[kv.Key], kv.Value), TruckMaterialLibrary.Chassis);

                // Differential bulge on driven axles (anything behind the cab).
                if (kv.Value < _cabCenterZ)
                {
                    Mesh diff = ProcMesh.Cylinder(0.24f, 0.30f, 10, true);
                    _mb.Add(diff, new Vector3(0f, axleY[kv.Key], kv.Value),
                            AxisToX, TruckMaterialLibrary.Chassis);
                }
            }
        }

        // ==================================================================
        // Cab
        // ==================================================================
        private void BuildCab()
        {
            Vector2[] profile = ProcMesh.CabSideProfile(
                _cabTotalLength, _s.cabHeight, _s.windscreenRake, _s.roofChamfer);

            Mesh cab = ProcMesh.Extrude(profile, _s.cabWidth);
            _mb.Add(cab, new Vector3(0f, _cabBaseY, _cabCenterZ), ProfileToSide, TruckMaterialLibrary.Paint);

            // Lower sill / skirt band in the accent colour.
            Mesh sill = ProcMesh.Box(_s.cabWidth * 1.005f, 0.16f, _cabTotalLength * 0.98f);
            _mb.Add(sill, new Vector3(0f, _cabBaseY + 0.08f, _cabCenterZ), TruckMaterialLibrary.PaintAccent);

            // Roof fairing: a tapered wedge that sits on the back half of the roof.
            if (_s.roofFairing)
            {
                float fairingLen = _cabTotalLength * 0.45f;
                Mesh fairing = ProcMesh.Frustum(_s.cabWidth * 0.94f, fairingLen,
                                                _s.cabWidth * 0.72f, fairingLen * 0.55f, 0.34f);
                _mb.Add(fairing,
                    new Vector3(0f, _cabBaseY + _s.cabHeight + 0.15f, _cabCenterZ - _cabTotalLength * 0.22f),
                    TruckMaterialLibrary.Paint);
            }

            // Sleeper panel seam so the cab does not read as one blank slab.
            if (_s.sleeper)
            {
                float seamZ = _cabFrontZ - _s.cabLength;
                Mesh seam = ProcMesh.Box(_s.cabWidth * 1.01f, _s.cabHeight * 0.92f, 0.035f);
                _mb.Add(seam, new Vector3(0f, _cabBaseY + _s.cabHeight * 0.48f, seamZ),
                        TruckMaterialLibrary.PlasticBlack);
            }

            BuildDoors();
        }

        private void BuildDoors()
        {
            float doorZ = _cabFrontZ - _s.cabLength * 0.52f;
            float doorLen = _s.cabLength * 0.78f;
            float doorH = _s.cabHeight * 0.74f;
            float x = _s.cabWidth * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                float sx = x * side;

                // Door outline: four thin seam strips, cheap but very readable.
                Mesh vert = ProcMesh.Box(0.02f, doorH, 0.022f);
                Mesh horiz = ProcMesh.Box(0.02f, 0.022f, doorLen);
                float baseY = _cabBaseY + 0.22f;
                float midY = baseY + doorH * 0.5f;

                _mb.Add(vert, new Vector3(sx, midY, doorZ + doorLen * 0.5f), TruckMaterialLibrary.PlasticBlack);
                _mb.Add(vert, new Vector3(sx, midY, doorZ - doorLen * 0.5f), TruckMaterialLibrary.PlasticBlack);
                _mb.Add(horiz, new Vector3(sx, baseY, doorZ), TruckMaterialLibrary.PlasticBlack);
                _mb.Add(horiz, new Vector3(sx, baseY + doorH, doorZ), TruckMaterialLibrary.PlasticBlack);

                // Handle
                Mesh handle = ProcMesh.Box(0.05f, 0.05f, 0.22f);
                _mb.Add(handle, new Vector3(sx + 0.02f * side, baseY + doorH * 0.55f, doorZ - doorLen * 0.18f),
                        TruckMaterialLibrary.Chrome);
            }
        }

        private void BuildGlass()
        {
            // --- windscreen, aligned exactly to the cab profile ---------------
            Vector2 wsBottom, wsTop;
            ProcMesh.CabWindscreenSegment(_cabTotalLength, _s.cabHeight, _s.windscreenRake,
                                          out wsBottom, out wsTop);

            float dz = wsTop.x - wsBottom.x;          // negative: leans back
            float dy = wsTop.y - wsBottom.y;
            float segLen = Mathf.Sqrt(dz * dz + dy * dy);
            float angleDeg = Mathf.Atan2(-dz, dy) * Mathf.Rad2Deg;

            Vector3 mid = new Vector3(
                0f,
                _cabBaseY + (wsBottom.y + wsTop.y) * 0.5f,
                _cabCenterZ + (wsBottom.x + wsTop.x) * 0.5f);

            Mesh glass = ProcMesh.Box(_s.cabWidth * 0.90f, segLen * 0.94f, 0.05f);
            _mb.Add(glass, mid + new Vector3(0f, 0f, 0.03f),
                    Quaternion.Euler(-angleDeg, 0f, 0f), TruckMaterialLibrary.Glass);

            // Black surround so the glass does not float on the paint.
            Mesh surround = ProcMesh.Box(_s.cabWidth * 0.95f, segLen, 0.03f);
            _mb.Add(surround, mid + new Vector3(0f, 0f, 0.012f),
                    Quaternion.Euler(-angleDeg, 0f, 0f), TruckMaterialLibrary.PlasticBlack);

            // --- side windows --------------------------------------------------
            float winZ = _cabFrontZ - _s.cabLength * 0.48f;
            float winLen = _s.cabLength * 0.60f;
            float winH = _s.cabHeight * 0.34f;
            float winY = _cabBaseY + _s.cabHeight * 0.66f;

            Mesh sideGlass = ProcMesh.Box(0.05f, winH, winLen);
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = _s.cabWidth * 0.5f * side;
                _mb.Add(sideGlass, new Vector3(sx, winY, winZ), TruckMaterialLibrary.Glass);

                // Small quarter light ahead of the door glass.
                Mesh quarter = ProcMesh.Box(0.045f, winH * 0.72f, winLen * 0.22f);
                _mb.Add(quarter, new Vector3(sx, winY - winH * 0.1f, winZ + winLen * 0.68f),
                        TruckMaterialLibrary.Glass);

                // Sleeper porthole.
                if (_s.sleeper)
                {
                    Mesh port = ProcMesh.Cylinder(winH * 0.30f, 0.05f, 12, true);
                    _mb.Add(port, new Vector3(sx, winY, _cabFrontZ - _s.cabLength - _s.sleeperLength * 0.45f),
                            AxisToX, TruckMaterialLibrary.Glass);
                }
            }

            // Sun visor above the screen.
            if (_s.sunVisor)
            {
                Mesh visor = ProcMesh.Frustum(_s.cabWidth * 0.98f, 0.30f, _s.cabWidth * 0.92f, 0.10f, 0.07f);
                _mb.Add(visor,
                    new Vector3(0f, _cabBaseY + _s.cabHeight + 0.02f, _cabCenterZ + wsTop.x + 0.10f),
                    Quaternion.Euler(-8f, 0f, 0f), TruckMaterialLibrary.PaintAccent);
            }
        }

        // ==================================================================
        // Conventional bonnet
        // ==================================================================
        private void BuildHood()
        {
            float hoodFrontZ = _s.frameFrontZ - _s.bumperDepth;
            float hoodCenterZ = (hoodFrontZ + _cabFrontZ) * 0.5f;
            float hoodW = _s.cabWidth * 0.86f;

            // Tapered towards the nose - reads as a real bonnet, not a crate.
            Mesh hood = ProcMesh.Frustum(hoodW, _s.hoodLength, hoodW * 0.90f, _s.hoodLength, _s.hoodHeight);
            _mb.Add(hood, new Vector3(0f, _cabBaseY + _s.hoodHeight * 0.5f, hoodCenterZ),
                    TruckMaterialLibrary.Paint);

            // Slight drop towards the nose.
            Mesh nose = ProcMesh.Frustum(hoodW * 0.90f, _s.hoodLength * 0.32f, hoodW * 0.84f, _s.hoodLength * 0.32f, 0.16f);
            _mb.Add(nose, new Vector3(0f, _cabBaseY + _s.hoodHeight - 0.02f, hoodFrontZ - _s.hoodLength * 0.16f),
                    Quaternion.Euler(4f, 0f, 0f), TruckMaterialLibrary.Paint);

            // Bonnet shut line + centre crease.
            Mesh crease = ProcMesh.Box(0.03f, 0.03f, _s.hoodLength * 0.95f);
            _mb.Add(crease, new Vector3(0f, _cabBaseY + _s.hoodHeight + 0.01f, hoodCenterZ),
                    TruckMaterialLibrary.PaintAccent);
        }

        // ==================================================================
        // Grille, bumper, front fascia
        // ==================================================================
        private void BuildFrontEnd()
        {
            float faceZ = _s.layout == CabLayout.Conventional
                ? _s.frameFrontZ - _s.bumperDepth
                : _cabFrontZ;

            float faceTopY = _s.layout == CabLayout.Conventional
                ? _cabBaseY + _s.hoodHeight
                : _cabBaseY + _s.cabHeight * 0.42f;

            float grilleW = _s.cabWidth * 0.66f;
            float grilleH = _s.layout == CabLayout.Conventional
                ? _s.hoodHeight * 0.62f
                : _s.cabHeight * 0.26f;
            float grilleY = faceTopY - grilleH * 0.75f;

            // Recessed dark backing panel.
            Mesh back = ProcMesh.Box(grilleW, grilleH, 0.05f);
            _mb.Add(back, new Vector3(0f, grilleY, faceZ + 0.015f), TruckMaterialLibrary.Grille);

            // Surround.
            Mesh surround = ProcMesh.Box(grilleW + 0.13f, grilleH + 0.11f, 0.04f);
            _mb.Add(surround, new Vector3(0f, grilleY, faceZ + 0.005f),
                    _s.chromeBumper ? TruckMaterialLibrary.Chrome : TruckMaterialLibrary.PaintAccent);

            BuildGrilleBars(grilleW, grilleH, grilleY, faceZ);

            // Lower fascia between the grille and the bumper (cab-over only, where
            // there is a visible gap under the cab).
            if (_s.layout == CabLayout.CabOverEngine)
            {
                float fasciaTop = _cabBaseY;
                float fasciaBottom = _bumperCenterY + _s.bumperHeight * 0.5f;
                float h = fasciaTop - fasciaBottom;
                if (h > 0.05f)
                {
                    Mesh fascia = ProcMesh.Box(_s.cabWidth * 0.94f, h, 0.10f);
                    _mb.Add(fascia, new Vector3(0f, fasciaBottom + h * 0.5f, faceZ - 0.02f),
                            TruckMaterialLibrary.PaintAccent);
                }
            }

            BuildBumper();
        }

        private void BuildGrilleBars(float w, float h, float y, float faceZ)
        {
            int slot = _s.chromeBumper ? TruckMaterialLibrary.Chrome : TruckMaterialLibrary.Aluminium;
            int count = Mathf.Clamp(_s.grilleBars, 2, 10);

            switch (_s.grille)
            {
                case GrilleStyle.VerticalBars:
                    {
                        Mesh bar = ProcMesh.Box(w / (count * 2.4f), h * 0.88f, 0.05f);
                        for (int i = 0; i < count; i++)
                        {
                            float t = (i + 0.5f) / count;
                            float x = Mathf.Lerp(-w * 0.45f, w * 0.45f, t);
                            _mb.Add(bar, new Vector3(x, y, faceZ + 0.045f), slot);
                        }
                        break;
                    }
                case GrilleStyle.MeshInsert:
                    {
                        Mesh hBar = ProcMesh.Box(w * 0.92f, h / (count * 3.2f), 0.04f);
                        Mesh vBar = ProcMesh.Box(w / (count * 4f), h * 0.88f, 0.035f);
                        for (int i = 0; i < count; i++)
                        {
                            float t = (i + 0.5f) / count;
                            _mb.Add(hBar, new Vector3(0f, Mathf.Lerp(y - h * 0.4f, y + h * 0.4f, t), faceZ + 0.04f), slot);
                            _mb.Add(vBar, new Vector3(Mathf.Lerp(-w * 0.42f, w * 0.42f, t), y, faceZ + 0.038f), slot);
                        }
                        break;
                    }
                case GrilleStyle.SplitChrome:
                    {
                        Mesh centre = ProcMesh.Box(w * 0.10f, h * 1.02f, 0.06f);
                        _mb.Add(centre, new Vector3(0f, y, faceZ + 0.05f), TruckMaterialLibrary.Chrome);
                        Mesh bar = ProcMesh.Box(w * 0.40f, h / (count * 2.6f), 0.05f);
                        for (int i = 0; i < count; i++)
                        {
                            float t = (i + 0.5f) / count;
                            float by = Mathf.Lerp(y - h * 0.40f, y + h * 0.40f, t);
                            _mb.Add(bar, new Vector3(-w * 0.26f, by, faceZ + 0.045f), TruckMaterialLibrary.Chrome);
                            _mb.Add(bar, new Vector3(w * 0.26f, by, faceZ + 0.045f), TruckMaterialLibrary.Chrome);
                        }
                        break;
                    }
                default:
                    {
                        Mesh bar = ProcMesh.Box(w * 0.94f, h / (count * 2.6f), 0.055f);
                        for (int i = 0; i < count; i++)
                        {
                            float t = (i + 0.5f) / count;
                            float by = Mathf.Lerp(y - h * 0.40f, y + h * 0.40f, t);
                            _mb.Add(bar, new Vector3(0f, by, faceZ + 0.045f), slot);
                        }
                        break;
                    }
            }
        }

        private void BuildBumper()
        {
            int slot = _s.chromeBumper ? TruckMaterialLibrary.Chrome : TruckMaterialLibrary.PaintAccent;
            float z = _s.frameFrontZ - _s.bumperDepth * 0.5f;
            float w = _s.cabWidth * 1.02f;

            Mesh main = ProcMesh.Box(w, _s.bumperHeight, _s.bumperDepth);
            _mb.Add(main, new Vector3(0f, _bumperCenterY, z), slot);

            // Wrapped ends, slightly pulled back.
            Mesh end = ProcMesh.Frustum(0.16f, _s.bumperDepth, 0.16f, _s.bumperDepth * 0.6f, _s.bumperHeight);
            _mb.Add(end, new Vector3(w * 0.5f - 0.02f, _bumperCenterY, z - 0.03f), slot);
            _mb.Add(end, new Vector3(-w * 0.5f + 0.02f, _bumperCenterY, z - 0.03f), slot);

            // Air intake slot and a valance under the bumper.
            Mesh intake = ProcMesh.Box(w * 0.45f, _s.bumperHeight * 0.26f, 0.05f);
            _mb.Add(intake, new Vector3(0f, _bumperCenterY - _s.bumperHeight * 0.18f, z + _s.bumperDepth * 0.5f),
                    TruckMaterialLibrary.Grille);

            Mesh valance = ProcMesh.Box(w * 0.88f, 0.14f, _s.bumperDepth * 0.7f);
            _mb.Add(valance, new Vector3(0f, _bumperCenterY - _s.bumperHeight * 0.5f - 0.05f, z),
                    TruckMaterialLibrary.PlasticBlack);

            // Two tow hooks.
            Mesh hook = ProcMesh.Box(0.07f, 0.12f, 0.10f);
            _mb.Add(hook, new Vector3(w * 0.22f, _bumperCenterY - _s.bumperHeight * 0.42f, z + _s.bumperDepth * 0.45f),
                    TruckMaterialLibrary.Chassis);
            _mb.Add(hook, new Vector3(-w * 0.22f, _bumperCenterY - _s.bumperHeight * 0.42f, z + _s.bumperDepth * 0.45f),
                    TruckMaterialLibrary.Chassis);
        }

        // ==================================================================
        // Tanks, steps, exhaust
        // ==================================================================
        private void BuildFuelTanksAndSteps()
        {
            float tankX = _s.frameRailSpacing * 0.5f + _s.fuelTankRadius + 0.06f;
            float tankY = _frameBottomY - _s.fuelTankRadius * 0.35f;
            float tankZ = Mathf.Lerp(_s.frameRearZ, _s.frameFrontZ, 0.42f);

            Mesh tank = ProcMesh.Cylinder(_s.fuelTankRadius, _s.fuelTankLength, 14, true);
            Mesh strap = ProcMesh.Box(_s.fuelTankRadius * 2.15f, _s.fuelTankRadius * 2.15f, 0.05f);

            int sides = _s.twinFuelTanks ? 2 : 1;
            for (int i = 0; i < sides; i++)
            {
                float sx = (i == 0) ? tankX : -tankX;
                _mb.Add(tank, new Vector3(sx, tankY, tankZ), AxisToZ, TruckMaterialLibrary.Aluminium);
                _mb.Add(strap, new Vector3(sx, tankY, tankZ + _s.fuelTankLength * 0.3f), TruckMaterialLibrary.Chassis);
                _mb.Add(strap, new Vector3(sx, tankY, tankZ - _s.fuelTankLength * 0.3f), TruckMaterialLibrary.Chassis);

                if (_s.sideSteps)
                {
                    Mesh step = ProcMesh.Box(0.44f, 0.05f, 0.34f);
                    float stepZ = tankZ + _s.fuelTankLength * 0.5f + 0.30f;
                    for (int st = 0; st < 2; st++)
                    {
                        float y = _frameBottomY - 0.18f - st * 0.30f;
                        _mb.Add(step, new Vector3(sx, y, stepZ), TruckMaterialLibrary.Chassis);
                    }
                    Mesh stepBack = ProcMesh.Box(0.06f, 0.70f, 0.30f);
                    _mb.Add(stepBack, new Vector3(sx * 0.82f, _frameBottomY - 0.32f, stepZ),
                            TruckMaterialLibrary.PlasticBlack);
                }
            }

            // Battery / air tank box on the opposite side when there is only one fuel tank.
            if (!_s.twinFuelTanks)
            {
                Mesh boxUnit = ProcMesh.Box(0.42f, 0.42f, 0.70f);
                _mb.Add(boxUnit, new Vector3(-tankX, tankY, tankZ), TruckMaterialLibrary.Chassis);
            }

            if (_s.sideSkirts)
            {
                float skirtLen = (_s.frameFrontZ - _s.frameRearZ) * 0.34f;
                Mesh skirt = ProcMesh.Box(0.05f, 0.46f, skirtLen);
                float skirtZ = Mathf.Lerp(_s.frameRearZ, _s.frameFrontZ, 0.55f);
                _mb.Add(skirt, new Vector3(_s.cabWidth * 0.46f, _frameBottomY - 0.16f, skirtZ), TruckMaterialLibrary.Paint);
                _mb.Add(skirt, new Vector3(-_s.cabWidth * 0.46f, _frameBottomY - 0.16f, skirtZ), TruckMaterialLibrary.Paint);
            }
        }

        private void BuildExhaust()
        {
            if (_s.stackExhaust)
            {
                float x = _s.cabWidth * 0.5f + 0.10f;
                float z = _cabFrontZ - _cabTotalLength + 0.20f;
                float h = _s.cabHeight * 1.05f;
                Mesh stack = ProcMesh.Cylinder(0.075f, h, 12, false);
                Mesh heatShield = ProcMesh.Cylinder(0.095f, h * 0.45f, 12, false);

                for (int side = -1; side <= 1; side += 2)
                {
                    _mb.Add(stack, new Vector3(x * side, _cabBaseY + h * 0.5f, z), TruckMaterialLibrary.Chrome);
                    _mb.Add(heatShield, new Vector3(x * side, _cabBaseY + h * 0.3f, z), TruckMaterialLibrary.Chrome);
                }
            }
            else
            {
                float x = _s.frameRailSpacing * 0.5f + 0.16f;
                float z = Mathf.Lerp(_s.frameRearZ, _s.frameFrontZ, 0.30f);
                Mesh muffler = ProcMesh.Cylinder(0.14f, 0.92f, 12, true);
                _mb.Add(muffler, new Vector3(-x, _frameBottomY - 0.06f, z), AxisToZ, TruckMaterialLibrary.Chassis);

                Mesh pipe = ProcMesh.Cylinder(0.055f, 0.55f, 8, false);
                _mb.Add(pipe, new Vector3(-x, _frameBottomY - 0.06f, z - 0.72f), AxisToZ, TruckMaterialLibrary.Chrome);
            }
        }

        // ==================================================================
        // Mudguards
        // ==================================================================
        private void BuildFenders()
        {
            if (_r.placements == null) return;

            for (int i = 0; i < _r.placements.Length; i++)
            {
                WheelPlacement w = _r.placements[i];
                float outerR = w.radius * 1.30f;
                float innerR = w.radius * 1.10f;
                float width = (w.dual ? w.width * 2.25f : w.width * 1.25f);

                Vector2[] arch = ProcMesh.RingSector(innerR, outerR, 5f, 175f, 9);
                Mesh fender = ProcMesh.Extrude(arch, width);

                float xOffset = w.dual ? Mathf.Sign(w.localPosition.x) * w.width * 0.5f : 0f;
                int slot = w.steering ? TruckMaterialLibrary.Paint : TruckMaterialLibrary.PlasticBlack;

                _mb.Add(fender,
                        new Vector3(w.localPosition.x + xOffset, w.localPosition.y, w.localPosition.z),
                        ProfileToSide, slot);

                // Mud flap behind each wheel group.
                if (!w.steering)
                {
                    Mesh flap = ProcMesh.Box(width * 0.92f, w.radius * 0.72f, 0.02f);
                    _mb.Add(flap,
                        new Vector3(w.localPosition.x + xOffset,
                                    w.localPosition.y + w.radius * 0.30f,
                                    w.localPosition.z - outerR),
                        TruckMaterialLibrary.PlasticBlack);
                }
            }
        }

        // ==================================================================
        // Fifth wheel / trailer coupling
        // ==================================================================
        private void BuildFifthWheel()
        {
            float y = _s.frameTopY + 0.12f;
            float z = _s.fifthWheelZ;

            Mesh plate = ProcMesh.Frustum(1.15f, 1.05f, 0.95f, 0.85f, 0.09f);
            _mb.Add(plate, new Vector3(0f, y, z), TruckMaterialLibrary.Chassis);

            // Approach ramps at the rear of the plate.
            Mesh ramp = ProcMesh.Box(0.34f, 0.07f, 0.42f);
            _mb.Add(ramp, new Vector3(0.36f, y - 0.03f, z - 0.62f), Quaternion.Euler(16f, 0f, 0f), TruckMaterialLibrary.Chassis);
            _mb.Add(ramp, new Vector3(-0.36f, y - 0.03f, z - 0.62f), Quaternion.Euler(16f, 0f, 0f), TruckMaterialLibrary.Chassis);

            // Mounting pedestals.
            Mesh pedestal = ProcMesh.Box(0.12f, 0.16f, 0.60f);
            _mb.Add(pedestal, new Vector3(_s.frameRailSpacing * 0.5f, _s.frameTopY + 0.04f, z), TruckMaterialLibrary.Chassis);
            _mb.Add(pedestal, new Vector3(-_s.frameRailSpacing * 0.5f, _s.frameTopY + 0.04f, z), TruckMaterialLibrary.Chassis);

            // Catch plate slot.
            Mesh slot = ProcMesh.Box(0.16f, 0.10f, 0.55f);
            _mb.Add(slot, new Vector3(0f, y + 0.01f, z - 0.30f), TruckMaterialLibrary.PlasticBlack);

            GameObject anchor = new GameObject("FifthWheelAnchor");
            anchor.transform.SetParent(_root, false);
            anchor.transform.localPosition = new Vector3(0f, y + 0.05f, z);
            _r.fifthWheelAnchor = anchor.transform;
        }

        private void BuildRearEnd()
        {
            float z = _s.frameRearZ;

            Mesh endPlate = ProcMesh.Box(_s.frameRailSpacing + 0.20f, _s.frameHeight * 0.9f, 0.08f);
            _mb.Add(endPlate, new Vector3(0f, _s.frameTopY - _s.frameHeight * 0.5f, z + 0.04f),
                    TruckMaterialLibrary.Chassis);

            // Light bar the rear lamps mount to.
            Mesh bar = ProcMesh.Box(_s.cabWidth * 0.86f, 0.13f, 0.09f);
            _mb.Add(bar, new Vector3(0f, _frameBottomY - 0.06f, z + 0.02f), TruckMaterialLibrary.Chassis);
        }

        // ==================================================================
        // Mirrors (separate object - thin geometry, no shadows)
        // ==================================================================
        private void BuildMirrors()
        {
            MeshBuilder mm = new MeshBuilder();
            float x = _s.cabWidth * 0.5f + 0.13f;
            float z = _cabFrontZ - _s.cabLength * 0.10f;
            float topY = _cabBaseY + _s.cabHeight * 0.92f;
            float armH = _s.cabHeight * 0.48f;

            Mesh arm = ProcMesh.Cylinder(0.022f, armH, 8, false);
            Mesh brace = ProcMesh.Cylinder(0.018f, 0.24f, 6, false);
            Mesh head = ProcMesh.Box(0.055f, armH * 0.72f, 0.19f);
            Mesh face = ProcMesh.Box(0.012f, armH * 0.64f, 0.165f);
            Mesh spot = ProcMesh.Box(0.05f, 0.17f, 0.17f);

            for (int side = -1; side <= 1; side += 2)
            {
                float sx = x * side;
                _mb.Add(brace, new Vector3(sx - 0.06f * side, topY, z), AxisToX, TruckMaterialLibrary.PlasticBlack);
                mm.Add(arm, new Vector3(sx, topY - armH * 0.5f, z), TruckMaterialLibrary.PlasticBlack);
                mm.Add(head, new Vector3(sx + 0.05f * side, topY - armH * 0.45f, z), TruckMaterialLibrary.PlasticBlack);
                mm.Add(face, new Vector3(sx + 0.082f * side, topY - armH * 0.45f, z), TruckMaterialLibrary.Chrome);
                mm.Add(spot, new Vector3(sx + 0.03f * side, topY - armH * 0.95f, z + 0.10f), TruckMaterialLibrary.PlasticBlack);
            }

            GameObject mirrors = mm.Emit("Mirrors", _root, _p);
            DisableShadowsIfTiny(mirrors, true);
        }

        // ==================================================================
        // Lamps
        // ==================================================================
        private void BuildLamps()
        {
            float faceZ = (_s.layout == CabLayout.Conventional ? _s.frameFrontZ - _s.bumperDepth : _cabFrontZ) + 0.04f;
            float headY = _bumperCenterY + _s.bumperHeight * 0.5f + 0.14f;
            float headX = _s.cabWidth * 0.40f;

            for (int side = -1; side <= 1; side += 2)
            {
                float sx = headX * side;

                Renderer head = CreateLamp("Headlight", new Vector3(sx, headY, faceZ),
                                           new Vector3(0.34f, 0.17f, 0.06f), TruckMaterialLibrary.LensClear);
                _r.lights.headlights.Add(head);
                _r.lights.headlightAnchors.Add(head.transform);

                Renderer ind = CreateLamp("IndicatorFront", new Vector3(sx + 0.20f * side, headY - 0.02f, faceZ),
                                          new Vector3(0.12f, 0.13f, 0.06f), TruckMaterialLibrary.LensAmber);
                if (side < 0) _r.lights.indicatorsLeft.Add(ind); else _r.lights.indicatorsRight.Add(ind);

                // Side repeater on the cab flank.
                Renderer rep = CreateLamp("IndicatorSide",
                                          new Vector3(_s.cabWidth * 0.5f * side, _cabBaseY + 0.30f, _cabFrontZ - _s.cabLength * 0.86f),
                                          new Vector3(0.05f, 0.08f, 0.14f), TruckMaterialLibrary.LensAmber);
                if (side < 0) _r.lights.indicatorsLeft.Add(rep); else _r.lights.indicatorsRight.Add(rep);
            }

            // Roof marker lamps.
            float markerY = _cabBaseY + _s.cabHeight + 0.02f;
            float markerZ = _cabCenterZ + _cabTotalLength * 0.5f - _s.windscreenRake - 0.06f;
            for (int i = 0; i < 5; i++)
            {
                float t = (i + 0.5f) / 5f;
                float mx = Mathf.Lerp(-_s.cabWidth * 0.34f, _s.cabWidth * 0.34f, t);
                _r.lights.markerLights.Add(CreateLamp("Marker", new Vector3(mx, markerY, markerZ),
                                                      new Vector3(0.09f, 0.045f, 0.09f), TruckMaterialLibrary.LensAmber));
            }

            // Rear cluster on the chassis end.
            float rearZ = _s.frameRearZ - 0.02f;
            float rearY = _frameBottomY - 0.06f;
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = _s.cabWidth * 0.36f * side;

                Renderer tail = CreateLamp("TailLight", new Vector3(sx, rearY, rearZ),
                                           new Vector3(0.20f, 0.10f, 0.05f), TruckMaterialLibrary.LensRed);
                _r.lights.tailLights.Add(tail);
                _r.lights.brakeLights.Add(tail);

                Renderer rInd = CreateLamp("IndicatorRear", new Vector3(sx + 0.16f * side, rearY, rearZ),
                                           new Vector3(0.11f, 0.10f, 0.05f), TruckMaterialLibrary.LensAmber);
                if (side < 0) _r.lights.indicatorsLeft.Add(rInd); else _r.lights.indicatorsRight.Add(rInd);

                Renderer rev = CreateLamp("ReverseLight", new Vector3(sx - 0.16f * side, rearY, rearZ),
                                          new Vector3(0.11f, 0.10f, 0.05f), TruckMaterialLibrary.LensClear);
                _r.lights.reverseLights.Add(rev);
            }

            GameObject interior = new GameObject("CabInteriorAnchor");
            interior.transform.SetParent(_root, false);
            interior.transform.localPosition = new Vector3(0f, _cabBaseY + _s.cabHeight * 0.72f,
                                                          _cabFrontZ - _s.cabLength * 0.55f);
            _r.lights.cabInteriorAnchor = interior.transform;
        }

        private Renderer CreateLamp(string lampName, Vector3 pos, Vector3 size, int slot)
        {
            GameObject go = new GameObject(lampName);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = pos;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProcMesh.Box(size);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _p.Get(slot);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        // ==================================================================
        // Wheels
        // ==================================================================
        private void BuildWheels(WheelPlacement[] wheels)
        {
            if (wheels == null || wheels.Length == 0)
            {
                _r.wheelVisuals = new Transform[0];
                return;
            }

            GameObject group = new GameObject("WheelVisuals");
            group.transform.SetParent(_root.parent != null ? _root.parent : _root, false);

            Transform[] result = new Transform[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelPlacement w = wheels[i];
                string objectName = "Wheel_" + i + (w.isLeft ? "_L" : "_R");
                GameObject go = TruckVisualUtility.CreateWheelObject(w, group.transform, _p, objectName);
                result[i] = go.transform;
            }

            _r.wheelVisuals = result;
        }

        // ==================================================================
        private static void DisableShadowsIfTiny(GameObject go, bool disable)
        {
            if (go == null) return;
            Renderer r = go.GetComponent<MeshRenderer>();
            if (r == null) return;
            r.shadowCastingMode = disable
                ? UnityEngine.Rendering.ShadowCastingMode.Off
                : UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }
}
