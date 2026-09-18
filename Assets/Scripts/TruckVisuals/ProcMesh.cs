using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Anything that can hand a Material to a material slot index. Lets one
    /// MeshBuilder serve both the fixed vehicle palette and the much larger
    /// world palette without either knowing about the other.
    /// </summary>
    public interface IMaterialSlots
    {
        Material Get(int slot);
    }

    /// <summary>
    /// Small procedural geometry library used to build truck / trailer bodywork.
    /// Every mesh returned is centred on its own local origin so it can be placed
    /// with a single position + rotation, and every mesh uses flat (hard) normals,
    /// which is what reads best for panelled vehicle bodywork.
    ///
    /// Nothing here allocates colliders or GameObjects - callers decide that.
    /// </summary>
    public static class ProcMesh
    {
        // ------------------------------------------------------------------
        // Internal scratch builder for a single primitive
        // ------------------------------------------------------------------
        private class Scratch
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float uScale, float vScale)
            {
                Vector3 normal = Normal(a, b, c);
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                n.Add(normal); n.Add(normal); n.Add(normal); n.Add(normal);
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(uScale, 0f));
                uv.Add(new Vector2(uScale, vScale));
                uv.Add(new Vector2(0f, vScale));
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                Vector3 normal = Normal(a, b, c);
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c);
                n.Add(normal); n.Add(normal); n.Add(normal);
                uv.Add(ua); uv.Add(ub); uv.Add(uc);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            public Mesh ToMesh(string meshName)
            {
                Mesh m = new Mesh();
                m.name = meshName;
                m.vertices = v.ToArray();
                m.normals = n.ToArray();
                m.uv = uv.ToArray();
                m.triangles = t.ToArray();
                m.RecalculateBounds();
                return m;
            }
        }

        private static Vector3 Normal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 u = b - a;
            Vector3 w = c - a;
            Vector3 cr = new Vector3(
                u.y * w.z - u.z * w.y,
                u.z * w.x - u.x * w.z,
                u.x * w.y - u.y * w.x);
            float mag = cr.magnitude;
            if (mag < 1e-6f) return Vector3.up;
            return cr * (1f / mag);
        }

        // ------------------------------------------------------------------
        // Primitives
        // ------------------------------------------------------------------

        /// <summary>Axis aligned box centred on the origin.</summary>
        public static Mesh Box(Vector3 size)
        {
            return Frustum(size.x, size.z, size.x, size.z, size.y);
        }

        public static Mesh Box(float x, float y, float z)
        {
            return Frustum(x, z, x, z, y);
        }

        /// <summary>
        /// Box whose top face can differ in size from its bottom face. This is the
        /// workhorse for cab tapers, grille surrounds, bumper ends and sleeper caps.
        /// Centred on the origin, height runs along Y.
        /// </summary>
        public static Mesh Frustum(float bottomWidth, float bottomDepth, float topWidth, float topDepth, float height)
        {
            float hy = height * 0.5f;
            float bx = bottomWidth * 0.5f, bz = bottomDepth * 0.5f;
            float tx = topWidth * 0.5f, tz = topDepth * 0.5f;

            Vector3 b0 = new Vector3(-bx, -hy, -bz);
            Vector3 b1 = new Vector3(bx, -hy, -bz);
            Vector3 b2 = new Vector3(bx, -hy, bz);
            Vector3 b3 = new Vector3(-bx, -hy, bz);
            Vector3 t0 = new Vector3(-tx, hy, -tz);
            Vector3 t1 = new Vector3(tx, hy, -tz);
            Vector3 t2 = new Vector3(tx, hy, tz);
            Vector3 t3 = new Vector3(-tx, hy, tz);

            Scratch s = new Scratch();
            s.Quad(b3, b2, b1, b0, bottomWidth, bottomDepth);   // bottom (-Y)
            s.Quad(t0, t1, t2, t3, topWidth, topDepth);         // top (+Y)
            s.Quad(b0, b1, t1, t0, bottomWidth, height);        // back (-Z)
            s.Quad(b2, b3, t3, t2, bottomWidth, height);        // front (+Z)
            s.Quad(b1, b2, t2, t1, bottomDepth, height);        // right (+X)
            s.Quad(b3, b0, t0, t3, bottomDepth, height);        // left (-X)
            return s.ToMesh("proc_frustum");
        }

        /// <summary>Cylinder centred on the origin, axis along Y.</summary>
        public static Mesh Cylinder(float radius, float height, int segments, bool caps)
        {
            segments = Mathf.Max(3, segments);
            float hy = height * 0.5f;
            Scratch s = new Scratch();

            for (int i = 0; i < segments; i++)
            {
                float a0 = (float)i / segments * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, -hy, Mathf.Sin(a0) * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, -hy, Mathf.Sin(a1) * radius);
                Vector3 p2 = new Vector3(Mathf.Cos(a1) * radius, hy, Mathf.Sin(a1) * radius);
                Vector3 p3 = new Vector3(Mathf.Cos(a0) * radius, hy, Mathf.Sin(a0) * radius);
                s.Quad(p0, p1, p2, p3, 1f, 1f);
            }

            if (caps)
            {
                Vector3 topC = new Vector3(0f, hy, 0f);
                Vector3 botC = new Vector3(0f, -hy, 0f);
                for (int i = 0; i < segments; i++)
                {
                    float a0 = (float)i / segments * Mathf.PI * 2f;
                    float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
                    Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius, hy, Mathf.Sin(a0) * radius);
                    Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius, hy, Mathf.Sin(a1) * radius);
                    s.Tri(topC, t0, t1,
                          new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f),
                          new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f));

                    Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -hy, Mathf.Sin(a0) * radius);
                    Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -hy, Mathf.Sin(a1) * radius);
                    s.Tri(botC, b1, b0,
                          new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f + Mathf.Cos(a1) * 0.5f, 0.5f + Mathf.Sin(a1) * 0.5f),
                          new Vector2(0.5f + Mathf.Cos(a0) * 0.5f, 0.5f + Mathf.Sin(a0) * 0.5f));
                }
            }

            return s.ToMesh("proc_cylinder");
        }

        /// <summary>
        /// Extrudes a closed 2D profile (XY plane, counter-clockwise or clockwise)
        /// along Z. Handles concave profiles via ear clipping, so it can produce
        /// cab side panels with a raked windscreen, mudguard arcs and chassis rails.
        /// </summary>
        public static Mesh Extrude(Vector2[] profile, float depth)
        {
            Scratch s = new Scratch();
            if (profile == null || profile.Length < 3) return s.ToMesh("proc_extrude_empty");

            float hz = depth * 0.5f;
            List<Vector2> poly = new List<Vector2>(profile);
            List<int> capTris = Triangulate(poly);

            // Front cap (+Z)
            for (int i = 0; i < capTris.Count; i += 3)
            {
                Vector2 a = poly[capTris[i]];
                Vector2 b = poly[capTris[i + 1]];
                Vector2 c = poly[capTris[i + 2]];
                s.Tri(new Vector3(a.x, a.y, hz), new Vector3(c.x, c.y, hz), new Vector3(b.x, b.y, hz),
                      a, c, b);
            }

            // Back cap (-Z), reversed winding
            for (int i = 0; i < capTris.Count; i += 3)
            {
                Vector2 a = poly[capTris[i]];
                Vector2 b = poly[capTris[i + 1]];
                Vector2 c = poly[capTris[i + 2]];
                s.Tri(new Vector3(a.x, a.y, -hz), new Vector3(b.x, b.y, -hz), new Vector3(c.x, c.y, -hz),
                      a, b, c);
            }

            // Side walls
            int n = poly.Count;
            bool ccw = SignedArea(poly) > 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = poly[i];
                Vector2 p1 = poly[(i + 1) % n];
                float seg = (p1 - p0).magnitude;

                Vector3 a = new Vector3(p0.x, p0.y, -hz);
                Vector3 b = new Vector3(p1.x, p1.y, -hz);
                Vector3 c = new Vector3(p1.x, p1.y, hz);
                Vector3 d = new Vector3(p0.x, p0.y, hz);

                if (ccw) s.Quad(a, b, c, d, seg, depth);
                else s.Quad(d, c, b, a, seg, depth);
            }

            return s.ToMesh("proc_extrude");
        }

        // ------------------------------------------------------------------
        // Profile helpers
        // ------------------------------------------------------------------

        /// <summary>Rectangle profile with optional cut corners (cheap chamfer look).</summary>
        public static Vector2[] RectProfile(float width, float height, float chamfer)
        {
            float hx = width * 0.5f, hy = height * 0.5f;
            if (chamfer <= 0.0001f)
            {
                return new Vector2[]
                {
                    new Vector2(-hx, -hy), new Vector2(hx, -hy),
                    new Vector2(hx, hy), new Vector2(-hx, hy)
                };
            }
            float c = Mathf.Min(chamfer, Mathf.Min(hx, hy) * 0.9f);
            return new Vector2[]
            {
                new Vector2(-hx + c, -hy), new Vector2(hx - c, -hy),
                new Vector2(hx, -hy + c), new Vector2(hx, hy - c),
                new Vector2(hx - c, hy), new Vector2(-hx + c, hy),
                new Vector2(-hx, hy - c), new Vector2(-hx, -hy + c)
            };
        }

        /// <summary>
        /// Ring sector profile (an arch). Used for mudguards / wheel arches.
        /// Angles in degrees, measured from +X towards +Y.
        /// </summary>
        public static Vector2[] RingSector(float innerRadius, float outerRadius, float startDeg, float endDeg, int segments)
        {
            segments = Mathf.Max(2, segments);
            List<Vector2> pts = new List<Vector2>(segments * 2 + 2);
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(startDeg, endDeg, (float)i / segments) * Mathf.Deg2Rad;
                pts.Add(new Vector2(Mathf.Cos(a) * outerRadius, Mathf.Sin(a) * outerRadius));
            }
            for (int i = segments; i >= 0; i--)
            {
                float a = Mathf.Lerp(startDeg, endDeg, (float)i / segments) * Mathf.Deg2Rad;
                pts.Add(new Vector2(Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius));
            }
            return pts.ToArray();
        }

        /// <summary>
        /// Cab side profile: a rectangle whose upper front corner is raked back to
        /// form the windscreen angle, with an optional roof chamfer.
        /// X runs forward (+ = nose), Y runs up from the profile origin.
        /// </summary>
        public static Vector2[] CabSideProfile(float length, float height, float windscreenRake, float roofChamfer)
        {
            float hx = length * 0.5f;
            float rake = Mathf.Clamp(windscreenRake, 0f, length * 0.45f);
            float chamf = Mathf.Clamp(roofChamfer, 0f, height * 0.35f);
            // The screen rises roughly twice as fast as it leans back, which reads
            // as a heavy-truck windscreen rather than a sports car.
            float rise = Mathf.Min(rake * 2.1f, height * 0.55f);

            return new Vector2[]
            {
                new Vector2(-hx, 0f),                   // lower rear
                new Vector2(hx, 0f),                    // lower front
                new Vector2(hx, height - rise),         // top of the front face
                new Vector2(hx - rake, height),         // roof front edge
                new Vector2(-hx + chamf * 0.5f, height),// roof
                new Vector2(-hx, height - chamf)        // rear roof chamfer
            };
        }

        /// <summary>
        /// Returns the two profile points that form the windscreen segment of
        /// <see cref="CabSideProfile"/>, so the glass panel can be placed exactly on it.
        /// </summary>
        public static void CabWindscreenSegment(float length, float height, float windscreenRake,
                                                out Vector2 bottom, out Vector2 top)
        {
            float hx = length * 0.5f;
            float rake = Mathf.Clamp(windscreenRake, 0f, length * 0.45f);
            float rise = Mathf.Min(rake * 2.1f, height * 0.55f);
            bottom = new Vector2(hx, height - rise);
            top = new Vector2(hx - rake, height);
        }

        /// <summary>C-section chassis rail profile (web + two flanges), origin centred.</summary>
        public static Vector2[] ChannelProfile(float height, float flange, float thickness)
        {
            float hy = height * 0.5f;
            float t = Mathf.Min(thickness, height * 0.45f);
            return new Vector2[]
            {
                new Vector2(0f, -hy),
                new Vector2(flange, -hy),
                new Vector2(flange, -hy + t),
                new Vector2(t, -hy + t),
                new Vector2(t, hy - t),
                new Vector2(flange, hy - t),
                new Vector2(flange, hy),
                new Vector2(0f, hy)
            };
        }

        // ------------------------------------------------------------------
        // Ear clipping triangulation (supports simple concave polygons)
        // ------------------------------------------------------------------
        private static float SignedArea(List<Vector2> poly)
        {
            float area = 0f;
            int n = poly.Count;
            for (int p = n - 1, q = 0; q < n; p = q++)
                area += poly[p].x * poly[q].y - poly[q].x * poly[p].y;
            return area * 0.5f;
        }

        private static List<int> Triangulate(List<Vector2> poly)
        {
            List<int> result = new List<int>();
            int n = poly.Count;
            if (n < 3) return result;

            int[] order = new int[n];
            if (SignedArea(poly) > 0f)
                for (int i = 0; i < n; i++) order[i] = i;
            else
                for (int i = 0; i < n; i++) order[i] = (n - 1) - i;

            int nv = n;
            int guard = 2 * nv;
            for (int m = 0, vtx = nv - 1; nv > 2;)
            {
                if (guard-- <= 0) break; // non simple polygon - bail out gracefully

                int u = vtx; if (nv <= u) u = 0;
                vtx = u + 1; if (nv <= vtx) vtx = 0;
                int w = vtx + 1; if (nv <= w) w = 0;

                if (Snip(poly, u, vtx, w, nv, order))
                {
                    result.Add(order[u]);
                    result.Add(order[vtx]);
                    result.Add(order[w]);
                    m++;
                    for (int s = vtx, t = vtx + 1; t < nv; s++, t++) order[s] = order[t];
                    nv--;
                    guard = 2 * nv;
                }
            }

            result.Reverse();
            return result;
        }

        private static bool Snip(List<Vector2> poly, int u, int v, int w, int n, int[] order)
        {
            Vector2 a = poly[order[u]];
            Vector2 b = poly[order[v]];
            Vector2 c = poly[order[w]];

            if (((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 1e-7f) return false;

            for (int p = 0; p < n; p++)
            {
                if (p == u || p == v || p == w) continue;
                if (PointInTriangle(a, b, c, poly[order[p]])) return false;
            }
            return true;
        }

        private static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float d1 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
            float d2 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
            float d3 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            return d1 >= 0f && d2 >= 0f && d3 >= 0f;
        }
    }

    /// <summary>
    /// Accumulates many procedural parts into ONE mesh with one submesh per
    /// material slot. A whole cab or trailer body therefore costs a single
    /// MeshRenderer with a handful of submeshes instead of ~60 GameObjects,
    /// which is the main reason this pass stays cheap enough for mobile.
    /// </summary>
    public class MeshBuilder
    {
        private readonly List<Vector3> _verts = new List<Vector3>();
        private readonly List<Vector3> _norms = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly Dictionary<int, List<int>> _tris = new Dictionary<int, List<int>>();

        public int VertexCount { get { return _verts.Count; } }
        public int PartCount { get; private set; }

        public void Add(Mesh mesh, Vector3 position, int slot)
        {
            Add(mesh, position, Quaternion.identity, Vector3.one, slot);
        }

        public void Add(Mesh mesh, Vector3 position, Quaternion rotation, int slot)
        {
            Add(mesh, position, rotation, Vector3.one, slot);
        }

        public void Add(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, int slot)
        {
            if (mesh == null) return;

            Vector3[] mv = mesh.vertices;
            Vector3[] mn = mesh.normals;
            Vector2[] muv = mesh.uv;
            int[] mt = mesh.triangles;
            if (mv == null || mt == null) return;

            int baseIndex = _verts.Count;
            for (int i = 0; i < mv.Length; i++)
            {
                _verts.Add(rotation * Vector3.Scale(mv[i], scale) + position);
                _norms.Add(mn != null && i < mn.Length ? rotation * mn[i] : Vector3.up);
                _uvs.Add(muv != null && i < muv.Length ? muv[i] : Vector2.zero);
            }

            List<int> list;
            if (!_tris.TryGetValue(slot, out list))
            {
                list = new List<int>();
                _tris[slot] = list;
            }
            for (int i = 0; i < mt.Length; i++) list.Add(baseIndex + mt[i]);

            PartCount++;
        }

        /// <summary>
        /// Bakes everything added so far. <paramref name="slotOrder"/> receives the
        /// material slot for each submesh, in submesh order, so the caller can build
        /// a matching Material[] for the renderer.
        /// </summary>
        public Mesh Build(string meshName, out int[] slotOrder)
        {
            List<int> slots = new List<int>(_tris.Keys);
            slots.Sort();
            slotOrder = slots.ToArray();

            Mesh mesh = new Mesh();
            mesh.name = meshName;
            if (_verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = _verts.ToArray();
            mesh.normals = _norms.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.subMeshCount = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                mesh.SetTriangles(_tris[slots[i]], i);

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Creates a child GameObject carrying the baked mesh and its materials.</summary>
        public GameObject Emit(string objectName, Transform parent, IMaterialSlots palette)
        {
            int[] slots;
            Mesh mesh = Build(objectName + "_mesh", out slots);

            GameObject go = new GameObject(objectName);
            if (parent != null) go.transform.SetParent(parent, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Material[] mats = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++) mats[i] = palette.Get(slots[i]);
            mr.sharedMaterials = mats;

            return go;
        }
    }
}
