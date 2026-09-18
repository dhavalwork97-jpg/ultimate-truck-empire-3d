using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    public enum BuildingStyle { Warehouse, Factory, Office, Shop, Residential, Silhouette }

    /// <summary>
    /// Reusable procedural building styles. Each building is one GameObject with
    /// one welded mesh (a submesh per material) and one box collider, which keeps
    /// frustum culling useful while holding the object count down.
    ///
    /// Variation comes from a deterministic per-building seed, so the skyline is
    /// different everywhere but identical every run.
    /// </summary>
    public static class BuildingBuilder
    {
        /// <summary>Small deterministic generator - avoids touching UnityEngine.Random's global state.</summary>
        private struct Rng
        {
            private uint state;
            public Rng(int seed) { state = (uint)(seed * 747796405 + 2891336453); if (state == 0u) state = 1u; }
            public float Value
            {
                get
                {
                    state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                    return (state & 0xFFFFFF) / (float)0x1000000;
                }
            }
            public float Range(float a, float b) => a + (b - a) * Value;
            public int Range(int a, int b) => a + Mathf.FloorToInt(Value * (b - a));
            public bool Chance(float p) => Value < p;
        }

        public static GameObject Build(BuildingStyle style, Vector3 position, float yaw, Vector3 size, int seed, Transform parent)
        {
            MeshBuilder mb = new MeshBuilder();
            Rng rng = new Rng(seed);

            switch (style)
            {
                case BuildingStyle.Warehouse: Warehouse(mb, size, ref rng); break;
                case BuildingStyle.Factory: Factory(mb, size, ref rng); break;
                case BuildingStyle.Office: Office(mb, size, ref rng); break;
                case BuildingStyle.Shop: Shop(mb, size, ref rng); break;
                case BuildingStyle.Residential: Residential(mb, size, ref rng); break;
                default: Silhouette(mb, size, ref rng); break;
            }

            GameObject go = mb.Emit(style + " Building", parent, WorldPaletteAdapter.Palette);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Distant silhouettes are never driven into, so they stay collider-free.
            if (style != BuildingStyle.Silhouette)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, size.y * 0.5f, 0f);
                box.size = size;
            }

            return go;
        }

        private static int S(WorldSurface surface) => WorldPaletteAdapter.Slot(surface);

        // ==================================================================
        private static void Warehouse(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            int facade = S(rng.Chance(0.5f) ? WorldSurface.FacadeCream : WorldSurface.FacadeGrey);
            float h = size.y;

            mb.Add(ProcMesh.Box(size.x, h, size.z), new Vector3(0f, h * 0.5f, 0f), facade);

            // Shallow metal roof with an overhang.
            mb.Add(ProcMesh.Box(size.x + 0.7f, 0.35f, size.z + 0.7f), new Vector3(0f, h + 0.16f, 0f), S(WorldSurface.MetalRoof));
            mb.Add(ProcMesh.Frustum(size.x * 0.9f, size.z * 0.9f, size.x * 0.55f, size.z * 0.55f, 0.9f),
                   new Vector3(0f, h + 0.75f, 0f), S(WorldSurface.MetalRoof));

            // Vertical cladding ribs on the long faces.
            int ribs = Mathf.Clamp(Mathf.RoundToInt(size.x / 3.6f), 3, 16);
            Mesh rib = ProcMesh.Box(0.16f, h * 0.94f, 0.14f);
            for (int i = 0; i < ribs; i++)
            {
                float t = (i + 0.5f) / ribs;
                float x = Mathf.Lerp(-size.x * 0.46f, size.x * 0.46f, t);
                mb.Add(rib, new Vector3(x, h * 0.47f, size.z * 0.5f), S(WorldSurface.Metal));
                mb.Add(rib, new Vector3(x, h * 0.47f, -size.z * 0.5f), S(WorldSurface.Metal));
            }

            // Roller shutter doors along the front, with dock bumpers.
            int doors = Mathf.Clamp(Mathf.RoundToInt(size.x / 9f), 2, 6);
            float doorH = Mathf.Min(h * 0.62f, 4.6f);
            Mesh door = ProcMesh.Box(4.2f, doorH, 0.22f);
            Mesh bumper = ProcMesh.Box(0.30f, 0.34f, 0.30f);
            for (int i = 0; i < doors; i++)
            {
                float t = (i + 0.5f) / doors;
                float x = Mathf.Lerp(-size.x * 0.38f, size.x * 0.38f, t);
                mb.Add(door, new Vector3(x, doorH * 0.5f, size.z * 0.5f + 0.06f), S(WorldSurface.MetalPainted));
                mb.Add(bumper, new Vector3(x - 2.3f, 1.15f, size.z * 0.5f + 0.16f), S(WorldSurface.Tyre));
                mb.Add(bumper, new Vector3(x + 2.3f, 1.15f, size.z * 0.5f + 0.16f), S(WorldSurface.Tyre));
            }

            // Signage band and a personnel door.
            mb.Add(ProcMesh.Box(size.x * 0.44f, 1.1f, 0.18f),
                   new Vector3(0f, h * 0.84f, size.z * 0.5f + 0.06f), S(WorldSurface.SignBlue));
            mb.Add(ProcMesh.Box(1.1f, 2.2f, 0.16f),
                   new Vector3(-size.x * 0.46f, 1.1f, size.z * 0.5f + 0.06f), S(WorldSurface.MetalPainted));
        }

        private static void Factory(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            int facade = S(rng.Chance(0.5f) ? WorldSurface.FacadeGrey : WorldSurface.FacadeRust);
            float h = size.y;

            mb.Add(ProcMesh.Box(size.x, h, size.z), new Vector3(0f, h * 0.5f, 0f), facade);

            // Sawtooth roof - the silhouette that reads instantly as "factory".
            int teeth = Mathf.Clamp(Mathf.RoundToInt(size.x / 7f), 2, 8);
            float toothWidth = size.x / teeth;
            Mesh tooth = ProcMesh.Frustum(toothWidth * 0.98f, size.z, toothWidth * 0.98f, size.z, 1.5f);
            Mesh glazing = ProcMesh.Box(toothWidth * 0.86f, 1.2f, 0.2f);
            for (int i = 0; i < teeth; i++)
            {
                float x = -size.x * 0.5f + (i + 0.5f) * toothWidth;
                mb.Add(tooth, new Vector3(x, h + 0.75f, 0f), Quaternion.Euler(0f, 0f, 9f), S(WorldSurface.MetalRoof));
                mb.Add(glazing, new Vector3(x + toothWidth * 0.28f, h + 1.2f, -size.z * 0.5f + 0.2f), S(WorldSurface.GlassDark));
            }

            // Chimneys with banding.
            int stacks = rng.Range(1, 3);
            for (int i = 0; i < stacks; i++)
            {
                float x = Mathf.Lerp(-size.x * 0.3f, size.x * 0.3f, stacks == 1 ? 0.5f : i / (float)(stacks - 1));
                float stackHeight = rng.Range(h * 0.8f, h * 1.5f);
                mb.Add(ProcMesh.Cylinder(1.05f, stackHeight, 12, false),
                       new Vector3(x, h + stackHeight * 0.5f, -size.z * 0.26f), S(WorldSurface.Concrete));
                mb.Add(ProcMesh.Cylinder(1.18f, 0.5f, 12, true),
                       new Vector3(x, h + stackHeight - 0.4f, -size.z * 0.26f), S(WorldSurface.WarningStripe));
            }

            // External pipe run and vents.
            mb.Add(ProcMesh.Cylinder(0.32f, size.x * 0.8f, 10, false),
                   new Vector3(0f, h * 0.7f, size.z * 0.5f + 0.4f), Quaternion.Euler(0f, 0f, 90f), S(WorldSurface.Metal));
            for (int i = 0; i < 3; i++)
                mb.Add(ProcMesh.Box(1.6f, 1.2f, 1.6f),
                       new Vector3(Mathf.Lerp(-size.x * 0.3f, size.x * 0.3f, i / 2f), h + 0.6f, size.z * 0.3f),
                       S(WorldSurface.Metal));

            // Loading doors.
            Mesh door = ProcMesh.Box(5f, Mathf.Min(h * 0.55f, 5f), 0.22f);
            mb.Add(door, new Vector3(size.x * 0.22f, Mathf.Min(h * 0.55f, 5f) * 0.5f, size.z * 0.5f + 0.06f),
                   S(WorldSurface.MetalPainted));
            mb.Add(ProcMesh.Box(size.x * 0.4f, 1.2f, 0.18f),
                   new Vector3(-size.x * 0.2f, h * 0.8f, size.z * 0.5f + 0.06f), S(WorldSurface.SignGreen));
        }

        private static void Office(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            int facade = S(rng.Chance(0.5f) ? WorldSurface.FacadeBlue : WorldSurface.FacadeGrey);
            float h = size.y;

            mb.Add(ProcMesh.Box(size.x, h, size.z), new Vector3(0f, h * 0.5f, 0f), facade);

            // Continuous glazing bands, one per floor.
            int floors = Mathf.Clamp(Mathf.RoundToInt(h / 3.4f), 2, 9);
            float floorHeight = h / floors;
            Mesh bandX = ProcMesh.Box(size.x * 0.94f, floorHeight * 0.52f, 0.16f);
            Mesh bandZ = ProcMesh.Box(0.16f, floorHeight * 0.52f, size.z * 0.94f);
            for (int i = 0; i < floors; i++)
            {
                float y = (i + 0.55f) * floorHeight;
                mb.Add(bandX, new Vector3(0f, y, size.z * 0.5f + 0.02f), S(WorldSurface.Glass));
                mb.Add(bandX, new Vector3(0f, y, -size.z * 0.5f - 0.02f), S(WorldSurface.Glass));
                mb.Add(bandZ, new Vector3(size.x * 0.5f + 0.02f, y, 0f), S(WorldSurface.GlassDark));
                mb.Add(bandZ, new Vector3(-size.x * 0.5f - 0.02f, y, 0f), S(WorldSurface.GlassDark));
            }

            // Parapet, entrance canopy and a rooftop plant box.
            mb.Add(ProcMesh.Box(size.x + 0.5f, 0.55f, size.z + 0.5f), new Vector3(0f, h + 0.2f, 0f), S(WorldSurface.Concrete));
            mb.Add(ProcMesh.Box(size.x * 0.4f, 0.24f, 2.6f), new Vector3(0f, 3.2f, size.z * 0.5f + 1.2f), S(WorldSurface.Concrete));
            mb.Add(ProcMesh.Box(size.x * 0.32f, 2.6f, 0.18f), new Vector3(0f, 1.5f, size.z * 0.5f + 0.05f), S(WorldSurface.GlassDark));
            mb.Add(ProcMesh.Box(4f, 2.2f, 3.4f), new Vector3(size.x * 0.22f, h + 1.4f, 0f), S(WorldSurface.ConcreteDark));
        }

        private static void Shop(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            int facade = S(WorldPalette.FacadeFor((int)(rng.Value * 1000f)));
            float h = size.y;

            mb.Add(ProcMesh.Box(size.x, h, size.z), new Vector3(0f, h * 0.5f, 0f), facade);
            mb.Add(ProcMesh.Box(size.x + 0.4f, 0.3f, size.z + 0.4f), new Vector3(0f, h + 0.15f, 0f), S(WorldSurface.ConcreteDark));

            // Shopfront glazing and a striped awning.
            mb.Add(ProcMesh.Box(size.x * 0.8f, h * 0.42f, 0.14f),
                   new Vector3(0f, h * 0.32f, size.z * 0.5f + 0.04f), S(WorldSurface.Glass));
            mb.Add(ProcMesh.Box(size.x * 0.86f, 0.14f, 1.5f),
                   new Vector3(0f, h * 0.60f, size.z * 0.5f + 0.7f), Quaternion.Euler(-12f, 0f, 0f), S(WorldSurface.SignYellow));
            mb.Add(ProcMesh.Box(size.x * 0.7f, 0.8f, 0.14f),
                   new Vector3(0f, h * 0.76f, size.z * 0.5f + 0.05f), S(WorldSurface.SignWhite));
        }

        private static void Residential(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            int facade = S(WorldPalette.FacadeFor((int)(rng.Value * 1000f)));
            float h = size.y;

            mb.Add(ProcMesh.Box(size.x, h, size.z), new Vector3(0f, h * 0.5f, 0f), facade);

            int floors = Mathf.Clamp(Mathf.RoundToInt(h / 3.1f), 2, 8);
            int perFloor = Mathf.Clamp(Mathf.RoundToInt(size.x / 3.4f), 2, 7);
            float floorHeight = h / floors;
            Mesh window = ProcMesh.Box(1.25f, 1.35f, 0.14f);
            Mesh balcony = ProcMesh.Box(size.x * 0.82f, 0.16f, 1.1f);

            for (int f = 0; f < floors; f++)
            {
                float y = (f + 0.55f) * floorHeight;
                for (int i = 0; i < perFloor; i++)
                {
                    float t = (i + 0.5f) / perFloor;
                    float x = Mathf.Lerp(-size.x * 0.40f, size.x * 0.40f, t);
                    mb.Add(window, new Vector3(x, y, size.z * 0.5f + 0.03f), S(WorldSurface.GlassDark));
                    mb.Add(window, new Vector3(x, y, -size.z * 0.5f - 0.03f), S(WorldSurface.GlassDark));
                }
                if (f > 0 && rng.Chance(0.55f))
                    mb.Add(balcony, new Vector3(0f, y - floorHeight * 0.35f, size.z * 0.5f + 0.5f), S(WorldSurface.Concrete));
            }

            // Parapet and rooftop water tanks.
            mb.Add(ProcMesh.Box(size.x + 0.3f, 0.7f, size.z + 0.3f), new Vector3(0f, h + 0.25f, 0f), S(WorldSurface.Concrete));
            int tanks = rng.Range(1, 4);
            for (int i = 0; i < tanks; i++)
            {
                float x = rng.Range(-size.x * 0.3f, size.x * 0.3f);
                float z = rng.Range(-size.z * 0.3f, size.z * 0.3f);
                mb.Add(ProcMesh.Box(0.18f, 1.1f, 0.18f), new Vector3(x, h + 1.1f, z), S(WorldSurface.Metal));
                mb.Add(ProcMesh.Cylinder(0.75f, 1.0f, 10, true), new Vector3(x, h + 2.1f, z), S(WorldSurface.MetalPainted));
            }
        }

        private static void Silhouette(MeshBuilder mb, Vector3 size, ref Rng rng)
        {
            // Background only: a tapered slab, no detail, no collider, no windows.
            mb.Add(ProcMesh.Frustum(size.x, size.z, size.x * rng.Range(0.82f, 1f), size.z * rng.Range(0.82f, 1f), size.y),
                   new Vector3(0f, size.y * 0.5f, 0f), S(WorldSurface.FacadeGrey));
            mb.Add(ProcMesh.Box(size.x * 0.3f, size.y * 0.12f, size.z * 0.3f),
                   new Vector3(0f, size.y + size.y * 0.06f, 0f), S(WorldSurface.ConcreteDark));
        }
    }
}
