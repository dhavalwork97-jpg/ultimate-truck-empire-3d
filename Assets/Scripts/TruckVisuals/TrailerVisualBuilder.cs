using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Builds a dry-van semi trailer: box body, ribbed sides, rear doors with
    /// seams and handles, chassis, rear bogie, mudguards, landing gear and the
    /// kingpin plate that mates with the tractor's fifth wheel.
    ///
    /// Same budget rules as the truck: one combined body mesh plus one renderer
    /// per wheel and a handful of tiny lamp renderers.
    /// </summary>
    public class TrailerVisualBuilder
    {
        private static readonly Quaternion ProfileToSide = Quaternion.Euler(0f, -90f, 0f);
        private static readonly Quaternion AxisToX = Quaternion.Euler(0f, 0f, 90f);

        private TrailerVisualSpec _s;
        private TruckMaterialLibrary.Palette _p;
        private MeshBuilder _mb;
        private TruckBuildResult _r;
        private Transform _root;

        private float _boxBottomY, _boxTopY, _boxCenterZ, _boxFrontZ, _boxRearZ;

        public TruckBuildResult Build(Transform parent, TrailerVisualSpec spec, TruckMaterialLibrary.Palette palette)
        {
            _s = spec;
            _p = palette;
            _mb = new MeshBuilder();
            _r = new TruckBuildResult();

            GameObject container = new GameObject("TrailerVisuals");
            container.transform.SetParent(parent, false);
            _root = container.transform;

            _boxBottomY = _s.floorY;
            _boxTopY = _s.floorY + _s.boxHeight;
            _boxFrontZ = _s.kingpinZ + 0.55f;
            _boxRearZ = _boxFrontZ - _s.boxLength;
            _boxCenterZ = (_boxFrontZ + _boxRearZ) * 0.5f;

            _r.placements = BuildWheelPlacements();

            BuildChassis();
            BuildBox();
            BuildRearDoors();
            BuildLandingGear();
            BuildKingpin();
            BuildFenders();

            GameObject body = _mb.Emit("Body", _root, _p);
            Renderer br = body.GetComponent<MeshRenderer>();
            if (br != null) br.receiveShadows = true;
            _r.bodyRoot = body;
            _r.vertexCount = _mb.VertexCount;

            BuildLamps();
            BuildWheels();

            return _r;
        }

        private WheelPlacement[] BuildWheelPlacements()
        {
            List<WheelPlacement> list = new List<WheelPlacement>();
            int axles = Mathf.Clamp(_s.axleCount, 1, 4);

            for (int a = 0; a < axles; a++)
            {
                float z = _s.rearAxleZ + a * _s.axleSpacing;
                for (int side = -1; side <= 1; side += 2)
                {
                    WheelPlacement w = new WheelPlacement();
                    w.localPosition = new Vector3(_s.trackWidth * 0.5f * side, _s.wheelRadius, z);
                    w.radius = _s.wheelRadius;
                    w.width = _s.wheelWidth;
                    w.steering = false;
                    w.dual = _s.dualWheels;
                    w.isLeft = side < 0;
                    w.axleIndex = a;
                    list.Add(w);
                }
            }
            return list.ToArray();
        }

        // ------------------------------------------------------------------
        private void BuildChassis()
        {
            float railY = _boxBottomY - 0.13f;
            float railLength = _s.boxLength * 0.96f;
            float halfSpacing = 0.52f;

            Mesh rail = ProcMesh.Extrude(ProcMesh.ChannelProfile(0.24f, 0.08f, 0.032f), railLength);
            _mb.Add(rail, new Vector3(halfSpacing, railY, _boxCenterZ), Quaternion.identity, TruckMaterialLibrary.Chassis);
            _mb.Add(rail, new Vector3(-halfSpacing, railY, _boxCenterZ), Quaternion.Euler(0f, 180f, 0f), TruckMaterialLibrary.Chassis);

            // Crossmembers under the floor.
            Mesh cross = ProcMesh.Box(_s.boxWidth * 0.96f, 0.07f, 0.07f);
            int count = 9;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                float z = Mathf.Lerp(_boxRearZ + 0.3f, _boxFrontZ - 0.3f, t);
                _mb.Add(cross, new Vector3(0f, railY - 0.05f, z), TruckMaterialLibrary.Chassis);
            }

            // Bogie subframe + axle beams.
            if (_r.placements != null)
            {
                Dictionary<int, float> axleZ = new Dictionary<int, float>();
                for (int i = 0; i < _r.placements.Length; i++)
                    axleZ[_r.placements[i].axleIndex] = _r.placements[i].localPosition.z;

                Mesh beam = ProcMesh.Box(_s.trackWidth * 0.94f, 0.14f, 0.14f);
                foreach (KeyValuePair<int, float> kv in axleZ)
                    _mb.Add(beam, new Vector3(0f, _s.wheelRadius, kv.Value), TruckMaterialLibrary.Chassis);

                float bogieFront = _s.rearAxleZ + (Mathf.Max(_s.axleCount, 1) - 1) * _s.axleSpacing;
                float bogieCenter = (_s.rearAxleZ + bogieFront) * 0.5f;
                float bogieLen = Mathf.Max(0.8f, (bogieFront - _s.rearAxleZ) + 1.1f);
                Mesh slider = ProcMesh.Box(0.10f, 0.20f, bogieLen);
                _mb.Add(slider, new Vector3(0.62f, _boxBottomY - 0.30f, bogieCenter), TruckMaterialLibrary.Chassis);
                _mb.Add(slider, new Vector3(-0.62f, _boxBottomY - 0.30f, bogieCenter), TruckMaterialLibrary.Chassis);
            }

            // Rear underrun (ICC) bar.
            Mesh iccBar = ProcMesh.Box(_s.boxWidth * 0.88f, 0.11f, 0.09f);
            _mb.Add(iccBar, new Vector3(0f, 0.55f, _boxRearZ + 0.12f), TruckMaterialLibrary.Chassis);
            Mesh iccLeg = ProcMesh.Box(0.09f, 0.62f, 0.09f);
            _mb.Add(iccLeg, new Vector3(0.52f, 0.85f, _boxRearZ + 0.12f), TruckMaterialLibrary.Chassis);
            _mb.Add(iccLeg, new Vector3(-0.52f, 0.85f, _boxRearZ + 0.12f), TruckMaterialLibrary.Chassis);
        }

        private void BuildBox()
        {
            // Main body. Very slightly tapered at the roof so highlights read.
            Mesh box = ProcMesh.Frustum(_s.boxWidth, _s.boxLength, _s.boxWidth * 0.995f, _s.boxLength, _s.boxHeight);
            _mb.Add(box, new Vector3(0f, _boxBottomY + _s.boxHeight * 0.5f, _boxCenterZ), TruckMaterialLibrary.TrailerSkin);

            // Corner posts and roof / floor rails give the box its structure.
            Mesh post = ProcMesh.Box(0.08f, _s.boxHeight * 0.99f, 0.08f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    _mb.Add(post,
                        new Vector3(_s.boxWidth * 0.5f * sx, _boxBottomY + _s.boxHeight * 0.5f,
                                    _boxCenterZ + _s.boxLength * 0.5f * sz),
                        TruckMaterialLibrary.Aluminium);
                }
            }

            Mesh rail = ProcMesh.Box(_s.boxWidth * 1.01f, 0.09f, _s.boxLength * 0.995f);
            _mb.Add(rail, new Vector3(0f, _boxTopY - 0.04f, _boxCenterZ), TruckMaterialLibrary.Aluminium);
            _mb.Add(rail, new Vector3(0f, _boxBottomY + 0.05f, _boxCenterZ), TruckMaterialLibrary.Aluminium);

            // Side ribs: cheap boxes, but they are what makes a van trailer read
            // as a trailer rather than a white brick.
            if (_s.ribbedSides)
            {
                int ribs = Mathf.Clamp(_s.ribCount, 0, 24);
                Mesh rib = ProcMesh.Box(0.03f, _s.boxHeight * 0.88f, 0.05f);
                for (int i = 0; i < ribs; i++)
                {
                    float t = (i + 0.5f) / ribs;
                    float z = Mathf.Lerp(_boxRearZ + 0.4f, _boxFrontZ - 0.4f, t);
                    _mb.Add(rib, new Vector3(_s.boxWidth * 0.5f, _boxBottomY + _s.boxHeight * 0.5f, z),
                            TruckMaterialLibrary.Aluminium);
                    _mb.Add(rib, new Vector3(-_s.boxWidth * 0.5f, _boxBottomY + _s.boxHeight * 0.5f, z),
                            TruckMaterialLibrary.Aluminium);
                }
            }

            // Front wall detail: a nose fairing lip and a bulkhead plate.
            Mesh nose = ProcMesh.Frustum(_s.boxWidth * 0.98f, 0.18f, _s.boxWidth * 0.86f, 0.10f, 0.22f);
            _mb.Add(nose, new Vector3(0f, _boxTopY - 0.10f, _boxFrontZ + 0.06f), TruckMaterialLibrary.TrailerSkin);

            Mesh bulkhead = ProcMesh.Box(_s.boxWidth * 0.92f, _s.boxHeight * 0.30f, 0.05f);
            _mb.Add(bulkhead, new Vector3(0f, _boxBottomY + _s.boxHeight * 0.30f, _boxFrontZ + 0.03f),
                    TruckMaterialLibrary.Aluminium);

            // Side skirt panels under the floor line.
            Mesh skirt = ProcMesh.Box(0.04f, 0.42f, _s.boxLength * 0.52f);
            float skirtZ = Mathf.Lerp(_boxRearZ, _boxFrontZ, 0.60f);
            _mb.Add(skirt, new Vector3(_s.boxWidth * 0.47f, _boxBottomY - 0.22f, skirtZ), TruckMaterialLibrary.TrailerSkin);
            _mb.Add(skirt, new Vector3(-_s.boxWidth * 0.47f, _boxBottomY - 0.22f, skirtZ), TruckMaterialLibrary.TrailerSkin);
        }

        private void BuildRearDoors()
        {
            float z = _boxRearZ - 0.03f;
            float doorH = _s.boxHeight * 0.94f;
            float doorW = _s.boxWidth * 0.48f;
            float y = _boxBottomY + _s.boxHeight * 0.5f;

            Mesh door = ProcMesh.Box(doorW, doorH, 0.05f);
            _mb.Add(door, new Vector3(-doorW * 0.5f - 0.005f, y, z), TruckMaterialLibrary.TrailerSkin);
            _mb.Add(door, new Vector3(doorW * 0.5f + 0.005f, y, z), TruckMaterialLibrary.TrailerSkin);

            // Centre seam + perimeter seal.
            Mesh seam = ProcMesh.Box(0.035f, doorH, 0.055f);
            _mb.Add(seam, new Vector3(0f, y, z - 0.01f), TruckMaterialLibrary.PlasticBlack);

            Mesh sealV = ProcMesh.Box(0.03f, doorH, 0.05f);
            _mb.Add(sealV, new Vector3(-_s.boxWidth * 0.48f, y, z - 0.01f), TruckMaterialLibrary.PlasticBlack);
            _mb.Add(sealV, new Vector3(_s.boxWidth * 0.48f, y, z - 0.01f), TruckMaterialLibrary.PlasticBlack);

            Mesh sealH = ProcMesh.Box(_s.boxWidth * 0.96f, 0.03f, 0.05f);
            _mb.Add(sealH, new Vector3(0f, y + doorH * 0.5f, z - 0.01f), TruckMaterialLibrary.PlasticBlack);
            _mb.Add(sealH, new Vector3(0f, y - doorH * 0.5f, z - 0.01f), TruckMaterialLibrary.PlasticBlack);

            // Vertical locking rods, cams and handles - two per door.
            Mesh rod = ProcMesh.Cylinder(0.022f, doorH * 0.92f, 8, false);
            Mesh keeper = ProcMesh.Box(0.07f, 0.09f, 0.07f);
            Mesh handle = ProcMesh.Box(0.05f, 0.05f, 0.24f);
            Mesh hinge = ProcMesh.Cylinder(0.035f, 0.13f, 8, true);

            float[] rodX = new float[] { -doorW * 0.80f, -doorW * 0.22f, doorW * 0.22f, doorW * 0.80f };
            for (int i = 0; i < rodX.Length; i++)
            {
                _mb.Add(rod, new Vector3(rodX[i], y, z - 0.05f), TruckMaterialLibrary.Aluminium);
                _mb.Add(keeper, new Vector3(rodX[i], y + doorH * 0.46f, z - 0.05f), TruckMaterialLibrary.Chassis);
                _mb.Add(keeper, new Vector3(rodX[i], y - doorH * 0.46f, z - 0.05f), TruckMaterialLibrary.Chassis);
                _mb.Add(handle, new Vector3(rodX[i] + 0.06f * Mathf.Sign(rodX[i]), y + 0.05f, z - 0.10f),
                        Quaternion.Euler(0f, 0f, 12f), TruckMaterialLibrary.Chassis);
            }

            for (int h = 0; h < 4; h++)
            {
                float hy = Mathf.Lerp(y - doorH * 0.42f, y + doorH * 0.42f, h / 3f);
                _mb.Add(hinge, new Vector3(_s.boxWidth * 0.49f, hy, z), AxisToX, TruckMaterialLibrary.Chassis);
                _mb.Add(hinge, new Vector3(-_s.boxWidth * 0.49f, hy, z), AxisToX, TruckMaterialLibrary.Chassis);
            }

            // Rear bumper step plate.
            Mesh plate = ProcMesh.Box(_s.boxWidth * 0.55f, 0.04f, 0.26f);
            _mb.Add(plate, new Vector3(0f, _boxBottomY - 0.06f, z - 0.14f), TruckMaterialLibrary.Chassis);
        }

        private void BuildLandingGear()
        {
            if (!_s.landingGear) return;

            float z = _s.kingpinZ - 2.10f;
            float legTop = _boxBottomY - 0.10f;
            float legH = legTop - 0.28f;

            Mesh leg = ProcMesh.Box(0.15f, legH, 0.15f);
            Mesh inner = ProcMesh.Box(0.10f, legH * 0.45f, 0.10f);
            Mesh foot = ProcMesh.Box(0.30f, 0.06f, 0.34f);
            Mesh brace = ProcMesh.Box(1.30f, 0.06f, 0.06f);
            Mesh crank = ProcMesh.Cylinder(0.018f, 0.34f, 6, false);
            Mesh crankHandle = ProcMesh.Cylinder(0.016f, 0.16f, 6, false);

            for (int side = -1; side <= 1; side += 2)
            {
                float sx = 0.72f * side;
                _mb.Add(leg, new Vector3(sx, legTop - legH * 0.5f, z), TruckMaterialLibrary.Chassis);
                _mb.Add(inner, new Vector3(sx, 0.28f + legH * 0.10f, z), TruckMaterialLibrary.Aluminium);
                _mb.Add(foot, new Vector3(sx, 0.28f, z), TruckMaterialLibrary.Chassis);
            }

            _mb.Add(brace, new Vector3(0f, legTop - 0.18f, z), TruckMaterialLibrary.Chassis);
            _mb.Add(crank, new Vector3(0.72f + 0.24f, legTop - 0.30f, z), AxisToX, TruckMaterialLibrary.Chassis);
            _mb.Add(crankHandle, new Vector3(0.72f + 0.38f, legTop - 0.30f, z + 0.10f), TruckMaterialLibrary.Chassis);
        }

        private void BuildKingpin()
        {
            float plateY = _boxBottomY - 0.06f;

            Mesh plate = ProcMesh.Box(1.25f, 0.06f, 1.35f);
            _mb.Add(plate, new Vector3(0f, plateY, _s.kingpinZ - 0.25f), TruckMaterialLibrary.Chassis);

            Mesh pin = ProcMesh.Cylinder(0.055f, 0.13f, 10, true);
            _mb.Add(pin, new Vector3(0f, plateY - 0.08f, _s.kingpinZ), TruckMaterialLibrary.Chassis);

            Mesh collar = ProcMesh.Cylinder(0.10f, 0.05f, 10, true);
            _mb.Add(collar, new Vector3(0f, plateY - 0.14f, _s.kingpinZ), TruckMaterialLibrary.Chassis);

            // Air / electrical line sockets on the bulkhead.
            Mesh socket = ProcMesh.Cylinder(0.045f, 0.10f, 8, true);
            for (int i = -1; i <= 1; i++)
            {
                _mb.Add(socket, new Vector3(i * 0.16f, _boxBottomY + 0.22f, _boxFrontZ + 0.04f),
                        Quaternion.Euler(90f, 0f, 0f), TruckMaterialLibrary.Chrome);
            }

            GameObject anchor = new GameObject("KingpinAnchor");
            anchor.transform.SetParent(_root, false);
            anchor.transform.localPosition = new Vector3(0f, plateY - 0.10f, _s.kingpinZ);
            _r.fifthWheelAnchor = anchor.transform;
        }

        private void BuildFenders()
        {
            if (_r.placements == null) return;

            for (int i = 0; i < _r.placements.Length; i++)
            {
                WheelPlacement w = _r.placements[i];
                float outerR = w.radius * 1.28f;
                float innerR = w.radius * 1.10f;
                float width = w.dual ? w.width * 2.25f : w.width * 1.25f;
                float xOffset = w.dual ? Mathf.Sign(w.localPosition.x) * w.width * 0.5f : 0f;

                Vector2[] arch = ProcMesh.RingSector(innerR, outerR, 8f, 172f, 8);
                Mesh fender = ProcMesh.Extrude(arch, width);
                _mb.Add(fender,
                    new Vector3(w.localPosition.x + xOffset, w.localPosition.y, w.localPosition.z),
                    ProfileToSide, TruckMaterialLibrary.PlasticBlack);

                // One mud flap behind the rearmost axle only.
                if (w.axleIndex == 0)
                {
                    Mesh flap = ProcMesh.Box(width * 0.95f, w.radius * 0.80f, 0.02f);
                    _mb.Add(flap,
                        new Vector3(w.localPosition.x + xOffset, w.localPosition.y + w.radius * 0.28f,
                                    w.localPosition.z - outerR),
                        TruckMaterialLibrary.PlasticBlack);
                }
            }
        }

        // ------------------------------------------------------------------
        private void BuildLamps()
        {
            float z = _boxRearZ - 0.08f;
            float y = 0.72f;

            for (int side = -1; side <= 1; side += 2)
            {
                float sx = _s.boxWidth * 0.40f * side;

                Renderer tail = CreateLamp("TrailerTail", new Vector3(sx, y, z),
                                           new Vector3(0.20f, 0.11f, 0.05f), TruckMaterialLibrary.LensRed);
                _r.lights.tailLights.Add(tail);
                _r.lights.brakeLights.Add(tail);

                Renderer ind = CreateLamp("TrailerIndicator", new Vector3(sx - 0.20f * side, y, z),
                                          new Vector3(0.12f, 0.11f, 0.05f), TruckMaterialLibrary.LensAmber);
                if (side < 0) _r.lights.indicatorsLeft.Add(ind); else _r.lights.indicatorsRight.Add(ind);

                Renderer rev = CreateLamp("TrailerReverse", new Vector3(sx + 0.18f * side, y - 0.14f, z),
                                          new Vector3(0.11f, 0.09f, 0.05f), TruckMaterialLibrary.LensClear);
                _r.lights.reverseLights.Add(rev);

                // Side markers along the skirt.
                for (int m = 0; m < 3; m++)
                {
                    float t = (m + 0.5f) / 3f;
                    float mz = Mathf.Lerp(_boxRearZ + 0.8f, _boxFrontZ - 0.8f, t);
                    _r.lights.markerLights.Add(CreateLamp("TrailerMarker",
                        new Vector3(_s.boxWidth * 0.5f * side, _boxBottomY - 0.06f, mz),
                        new Vector3(0.05f, 0.07f, 0.13f), TruckMaterialLibrary.LensAmber));
                }
            }

            // Roof marker strip above the doors.
            for (int i = 0; i < 3; i++)
            {
                float t = (i + 0.5f) / 3f;
                float mx = Mathf.Lerp(-_s.boxWidth * 0.32f, _s.boxWidth * 0.32f, t);
                _r.lights.markerLights.Add(CreateLamp("TrailerRoofMarker",
                    new Vector3(mx, _boxTopY - 0.03f, _boxRearZ + 0.02f),
                    new Vector3(0.09f, 0.05f, 0.09f), TruckMaterialLibrary.LensAmber));
            }
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

        private void BuildWheels()
        {
            WheelPlacement[] wheels = _r.placements;
            if (wheels == null || wheels.Length == 0)
            {
                _r.wheelVisuals = new Transform[0];
                return;
            }

            GameObject group = new GameObject("TrailerWheelVisuals");
            group.transform.SetParent(_root.parent != null ? _root.parent : _root, false);

            Transform[] result = new Transform[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                GameObject go = TruckVisualUtility.CreateWheelObject(wheels[i], group.transform, _p, "TrailerWheel_" + i);
                result[i] = go.transform;
            }
            _r.wheelVisuals = result;
        }
    }
}
