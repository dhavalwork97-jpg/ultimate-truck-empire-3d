using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Shared helpers for the visual pass: the wheel mesh cache (so a whole fleet
    /// shares a couple of Mesh assets) and a few small GameObject utilities.
    /// </summary>
    public static class TruckVisualUtility
    {
        private static readonly Quaternion AxisToX = Quaternion.Euler(0f, 0f, 90f);

        private class WheelMeshEntry
        {
            public Mesh mesh;
            public int[] slots;
        }

        private static readonly Dictionary<string, WheelMeshEntry> _wheelCache =
            new Dictionary<string, WheelMeshEntry>();

        public static void ClearCaches()
        {
            _wheelCache.Clear();
        }

        /// <summary>
        /// Creates one visible wheel. Submeshes are driven by the cached slot
        /// order, so tyre / hub / chrome materials always line up.
        /// </summary>
        public static GameObject CreateWheelObject(WheelPlacement w, Transform parent,
                                                   TruckMaterialLibrary.Palette palette, string objectName)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = w.localPosition;

            WheelMeshEntry entry = GetOrBuildWheelMesh(w);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = entry.mesh;

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            Material[] mats = new Material[entry.slots.Length];
            for (int i = 0; i < entry.slots.Length; i++) mats[i] = palette.Get(entry.slots[i]);
            mr.sharedMaterials = mats;
            mr.receiveShadows = true;

            return go;
        }

        private static WheelMeshEntry GetOrBuildWheelMesh(WheelPlacement w)
        {
            string key = string.Format("w{0:F3}_{1:F3}_{2}_{3}",
                w.radius, w.width, w.dual ? 1 : 0, w.isLeft ? 1 : 0);

            WheelMeshEntry cached;
            if (_wheelCache.TryGetValue(key, out cached) && cached != null && cached.mesh != null)
                return cached;

            MeshBuilder mb = new MeshBuilder();
            float outward = w.isLeft ? -1f : 1f;

            const int tyreSegments = 18;
            Mesh tyre = ProcMesh.Cylinder(w.radius, w.width, tyreSegments, true);
            Mesh tread = ProcMesh.Cylinder(w.radius * 1.008f, w.width * 0.80f, tyreSegments, false);
            Mesh hub = ProcMesh.Cylinder(w.radius * 0.56f, w.width * 1.04f, 12, true);
            Mesh cap = ProcMesh.Cylinder(w.radius * 0.20f, w.width * 0.20f, 10, true);
            Mesh nut = ProcMesh.Box(0.035f, 0.035f, 0.035f);

            float hubX;
            if (w.dual)
            {
                float gap = w.width * 1.08f;
                mb.Add(tyre, new Vector3(gap * 0.5f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(tread, new Vector3(gap * 0.5f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(tyre, new Vector3(-gap * 0.5f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(tread, new Vector3(-gap * 0.5f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(hub, new Vector3(gap * 0.5f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Aluminium);
                hubX = (gap * 0.5f + w.width * 0.53f) * outward;
            }
            else
            {
                mb.Add(tyre, Vector3.zero, AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(tread, Vector3.zero, AxisToX, TruckMaterialLibrary.Rubber);
                mb.Add(hub, Vector3.zero, AxisToX, TruckMaterialLibrary.Aluminium);
                hubX = w.width * 0.53f * outward;
            }

            mb.Add(cap, new Vector3(hubX + 0.02f * outward, 0f, 0f), AxisToX, TruckMaterialLibrary.Chrome);

            float nutRing = w.radius * 0.34f;
            for (int i = 0; i < 8; i++)
            {
                float a = (float)i / 8f * Mathf.PI * 2f;
                mb.Add(nut, new Vector3(hubX, Mathf.Sin(a) * nutRing, Mathf.Cos(a) * nutRing),
                       TruckMaterialLibrary.Chrome);
            }

            int[] slots;
            Mesh built = mb.Build("wheel_" + key, out slots);

            WheelMeshEntry entry = new WheelMeshEntry();
            entry.mesh = built;
            entry.slots = slots;
            _wheelCache[key] = entry;
            return entry;
        }

        /// <summary>
        /// Removes colliders from generated visual geometry. Generated parts never
        /// need one, and a stray MeshCollider on cosmetic geometry is a classic
        /// source of vehicles catching on their own bodywork.
        /// </summary>
        public static void StripColliders(GameObject root)
        {
            if (root == null) return;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                if (colliders[i] is WheelCollider) continue; // never touch physics wheels
                SafeDestroy(colliders[i]);
            }
        }

        public static void SetShadowCasting(GameObject root, bool cast)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            UnityEngine.Rendering.ShadowCastingMode mode = cast
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].shadowCastingMode = mode;
        }

        public static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
