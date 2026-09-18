using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    public enum TrafficVehicleKind { Hatchback, Sedan, Van, Bus, BoxTruck }

    /// <summary>
    /// Builds the traffic fleet. Each vehicle is one welded mesh with a submesh
    /// per material and no collider at all - traffic is presentation, the AI is
    /// unchanged and does not use physics.
    ///
    /// Meshes are cached per (kind, colour), so fourteen vehicles share a handful
    /// of Mesh assets, and every material comes from the shared cache.
    ///
    /// Local origin sits at the spawner's cruise height, with the wheels reaching
    /// down to the road from there.
    /// </summary>
    public static class TrafficVisualFactory
    {
        private const float Hover = 0.55f;    // matches TrafficSpawner's route height
        private const float WheelRadius = 0.33f;

        private static readonly Color[] BodyColours =
        {
            new Color(0.78f, 0.78f, 0.80f), new Color(0.12f, 0.13f, 0.15f),
            new Color(0.62f, 0.16f, 0.14f), new Color(0.14f, 0.30f, 0.52f),
            new Color(0.20f, 0.40f, 0.28f), new Color(0.85f, 0.72f, 0.20f),
            new Color(0.45f, 0.47f, 0.50f), new Color(0.90f, 0.90f, 0.88f)
        };

        /// <summary>Slot table for one vehicle: 0 body, 1 glass, 2 tyre, 3 trim, 4 lamp.</summary>
        private sealed class VehicleSlots : IMaterialSlots
        {
            private readonly Material body;
            public VehicleSlots(Color colour)
            {
                body = TruckMaterialLibrary.Make("trafficBody", colour, 0.3f, 0.68f);
            }

            public Material Get(int slot)
            {
                switch (slot)
                {
                    case 1: return WorldPalette.Get(WorldSurface.GlassDark);
                    case 2: return WorldPalette.Get(WorldSurface.Tyre);
                    case 3: return WorldPalette.Get(WorldSurface.Metal);
                    case 4: return TruckMaterialLibrary.MakeLens("trafficLamp", new Color(0.6f, 0.1f, 0.08f), 0.6f);
                    default: return body;
                }
            }
        }

        private const int Body = 0, Glass = 1, Tyre = 2, Trim = 3, Lamp = 4;

        public static TrafficVehicleKind KindFor(int index)
        {
            switch (index % 7)
            {
                case 0: return TrafficVehicleKind.BoxTruck;
                case 1: return TrafficVehicleKind.Sedan;
                case 2: return TrafficVehicleKind.Van;
                case 3: return TrafficVehicleKind.Hatchback;
                case 4: return TrafficVehicleKind.Bus;
                case 5: return TrafficVehicleKind.Sedan;
                default: return TrafficVehicleKind.Hatchback;
            }
        }

        public static GameObject Create(int index, Transform parent)
        {
            TrafficVehicleKind kind = KindFor(index);
            Color colour = BodyColours[Mathf.Abs(index * 37 + (int)kind * 11) % BodyColours.Length];

            MeshBuilder mb = new MeshBuilder();
            switch (kind)
            {
                case TrafficVehicleKind.Bus: Bus(mb); break;
                case TrafficVehicleKind.BoxTruck: BoxTruck(mb); break;
                case TrafficVehicleKind.Van: Van(mb); break;
                case TrafficVehicleKind.Sedan: Sedan(mb); break;
                default: Hatchback(mb); break;
            }

            GameObject go = mb.Emit("Traffic " + kind, parent, new VehicleSlots(colour));
            return go;
        }

        // ------------------------------------------------------------------
        private static void AddWheels(MeshBuilder mb, float wheelbase, float track, float width)
        {
            Mesh wheel = ProcMesh.Cylinder(WheelRadius, width, 10, true);
            Quaternion axis = Quaternion.Euler(0f, 0f, 90f);
            float y = -Hover + WheelRadius;
            for (int zi = -1; zi <= 1; zi += 2)
                for (int xi = -1; xi <= 1; xi += 2)
                    mb.Add(wheel, new Vector3(xi * track * 0.5f, y, zi * wheelbase * 0.5f), axis, Tyre);
        }

        private static void AddLamps(MeshBuilder mb, float halfLength, float halfWidth, float y)
        {
            Mesh lamp = ProcMesh.Box(0.26f, 0.13f, 0.08f);
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Add(lamp, new Vector3(side * halfWidth * 0.68f, y, halfLength), Trim);
                mb.Add(lamp, new Vector3(side * halfWidth * 0.68f, y, -halfLength), Lamp);
            }
        }

        private static void Hatchback(MeshBuilder mb)
        {
            const float length = 3.9f, width = 1.72f;
            mb.Add(ProcMesh.Box(width, 0.62f, length), new Vector3(0f, -0.09f, 0f), Body);
            mb.Add(ProcMesh.Frustum(width * 0.94f, length * 0.52f, width * 0.80f, length * 0.42f, 0.56f),
                   new Vector3(0f, 0.48f, -0.22f), Body);
            mb.Add(ProcMesh.Box(width * 0.86f, 0.34f, length * 0.44f), new Vector3(0f, 0.50f, -0.20f), Glass);
            AddWheels(mb, length * 0.62f, width * 0.86f, 0.22f);
            AddLamps(mb, length * 0.5f, width * 0.5f, -0.05f);
        }

        private static void Sedan(MeshBuilder mb)
        {
            const float length = 4.6f, width = 1.80f;
            mb.Add(ProcMesh.Box(width, 0.60f, length), new Vector3(0f, -0.10f, 0f), Body);
            mb.Add(ProcMesh.Frustum(width * 0.92f, length * 0.46f, width * 0.74f, length * 0.34f, 0.52f),
                   new Vector3(0f, 0.46f, -0.15f), Body);
            mb.Add(ProcMesh.Box(width * 0.84f, 0.32f, length * 0.40f), new Vector3(0f, 0.48f, -0.14f), Glass);
            AddWheels(mb, length * 0.60f, width * 0.86f, 0.22f);
            AddLamps(mb, length * 0.5f, width * 0.5f, -0.06f);
        }

        private static void Van(MeshBuilder mb)
        {
            const float length = 5.4f, width = 1.96f;
            mb.Add(ProcMesh.Box(width, 0.55f, length), new Vector3(0f, -0.14f, 0f), Body);
            mb.Add(ProcMesh.Box(width * 0.98f, 1.35f, length * 0.62f), new Vector3(0f, 0.80f, -0.60f), Body);
            mb.Add(ProcMesh.Frustum(width * 0.96f, length * 0.34f, width * 0.86f, length * 0.30f, 0.95f),
                   new Vector3(0f, 0.60f, length * 0.32f), Body);
            mb.Add(ProcMesh.Box(width * 0.82f, 0.42f, 0.10f), new Vector3(0f, 0.86f, length * 0.47f), Glass);
            mb.Add(ProcMesh.Box(0.10f, 0.50f, length * 0.30f), new Vector3(width * 0.49f, 0.82f, 0.30f), Glass);
            mb.Add(ProcMesh.Box(0.10f, 0.50f, length * 0.30f), new Vector3(-width * 0.49f, 0.82f, 0.30f), Glass);
            AddWheels(mb, length * 0.62f, width * 0.84f, 0.24f);
            AddLamps(mb, length * 0.5f, width * 0.5f, -0.10f);
        }

        private static void Bus(MeshBuilder mb)
        {
            const float length = 10.6f, width = 2.42f;
            mb.Add(ProcMesh.Box(width, 2.55f, length), new Vector3(0f, 0.85f, 0f), Body);
            mb.Add(ProcMesh.Box(width + 0.06f, 0.18f, length * 0.97f), new Vector3(0f, 2.08f, 0f), Trim);

            // Window strips both sides plus a windscreen.
            Mesh window = ProcMesh.Box(0.10f, 0.78f, length * 0.80f);
            mb.Add(window, new Vector3(width * 0.5f, 1.30f, -0.3f), Glass);
            mb.Add(window, new Vector3(-width * 0.5f, 1.30f, -0.3f), Glass);
            mb.Add(ProcMesh.Box(width * 0.88f, 0.95f, 0.10f), new Vector3(0f, 1.35f, length * 0.5f), Glass);
            mb.Add(ProcMesh.Box(width * 0.84f, 0.75f, 0.10f), new Vector3(0f, 1.30f, -length * 0.5f), Glass);

            // Door recess and skirt.
            mb.Add(ProcMesh.Box(0.12f, 1.55f, 1.10f), new Vector3(width * 0.5f, 0.95f, length * 0.30f), Trim);
            mb.Add(ProcMesh.Box(width * 1.01f, 0.35f, length * 0.9f), new Vector3(0f, -0.28f, 0f), Trim);

            Mesh wheel = ProcMesh.Cylinder(0.46f, 0.28f, 10, true);
            Quaternion axis = Quaternion.Euler(0f, 0f, 90f);
            float y = -Hover + 0.46f;
            foreach (float z in new[] { length * 0.34f, -length * 0.30f })
                for (int xi = -1; xi <= 1; xi += 2)
                    mb.Add(wheel, new Vector3(xi * width * 0.42f, y, z), axis, Tyre);

            AddLamps(mb, length * 0.5f, width * 0.5f, 0.05f);
        }

        private static void BoxTruck(MeshBuilder mb)
        {
            const float length = 7.8f, width = 2.30f;
            // Chassis and cab.
            mb.Add(ProcMesh.Box(width * 0.8f, 0.30f, length * 0.92f), new Vector3(0f, -0.15f, 0f), Trim);
            mb.Add(ProcMesh.Box(width, 1.85f, 2.30f), new Vector3(0f, 0.85f, length * 0.5f - 1.15f), Body);
            mb.Add(ProcMesh.Box(width * 0.86f, 0.70f, 0.10f), new Vector3(0f, 1.35f, length * 0.5f - 0.02f), Glass);

            // Cargo box with corner posts.
            mb.Add(ProcMesh.Box(width * 1.02f, 2.35f, length * 0.62f), new Vector3(0f, 1.15f, -length * 0.16f), Body);
            mb.Add(ProcMesh.Box(width * 1.04f, 0.14f, length * 0.63f), new Vector3(0f, 2.30f, -length * 0.16f), Trim);
            mb.Add(ProcMesh.Box(width * 0.9f, 1.9f, 0.10f), new Vector3(0f, 1.10f, -length * 0.47f), Trim);

            Mesh wheel = ProcMesh.Cylinder(0.44f, 0.30f, 10, true);
            Quaternion axis = Quaternion.Euler(0f, 0f, 90f);
            float y = -Hover + 0.44f;
            foreach (float z in new[] { length * 0.32f, -length * 0.26f })
                for (int xi = -1; xi <= 1; xi += 2)
                    mb.Add(wheel, new Vector3(xi * width * 0.40f, y, z), axis, Tyre);

            AddLamps(mb, length * 0.5f, width * 0.5f, 0.0f);
        }
    }
}
