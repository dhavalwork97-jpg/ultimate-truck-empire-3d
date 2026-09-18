using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// The places a truck driver actually stops at. Each site is built from a
    /// concrete apron, a recognisable building silhouette, painted markings and a
    /// small amount of clutter, batched into two or three meshes per site.
    ///
    /// Footprints are published as <see cref="Rect"/>s so the city generator can
    /// keep its blocks out of them - which is what stops buildings spawning inside
    /// the depot or across the road.
    /// </summary>
    public static class SiteBuilder
    {
        // Footprints in world XZ. Kept in one place so the city generator and the
        // site builder cannot disagree about who owns which patch of ground.
        public static readonly Rect DepotArea = new Rect(-88f, 7f, 66f, 38f);
        public static readonly Rect FactoryArea = new Rect(22f, 7f, 66f, 38f);
        public static readonly Rect TruckStopArea = new Rect(6f, -46f, 44f, 32f);
        public static readonly Rect ServiceArea = new Rect(-52f, -46f, 44f, 32f);

        private static int S(WorldSurface surface) => WorldPaletteAdapter.Slot(surface);
        private const float ApronY = -0.035f;   // above ground (-0.06), below asphalt (0.0)

        // ==================================================================
        public static void BuildLogisticsDepot(Transform parent, Vector3 origin)
        {
            MeshBuilder ground = new MeshBuilder();
            MeshBuilder props = new MeshBuilder();

            // Apron and yard surface.
            Apron(ground, new Vector3(0f, 0f, 24f), 60f, 32f, WorldSurface.Concrete);
            Apron(ground, new Vector3(0f, 0f, 9.5f), 22f, 6f, WorldSurface.AsphaltWorn);   // gate approach

            // Dock apron markings, bays and arrows.
            RoadBuilder.AddParkingBays(ground, new Vector3(-14f, 0f, 18f), 6, 4.2f, 14f, true);
            RoadBuilder.AddHatching(ground, new Vector3(16f, 0f, 14f), 14f, 8f, 9);
            RoadBuilder.AddArrow(ground, new Vector3(0f, 0f, 12.5f), 0f);

            // Warehouse, doors facing the road.
            BuildingBuilder.Build(BuildingStyle.Warehouse, origin + new Vector3(0f, 0f, 36f), 180f,
                                  new Vector3(48f, 9.5f, 15f), 4101, parent);

            // Office block beside it.
            BuildingBuilder.Build(BuildingStyle.Office, origin + new Vector3(-27f, 0f, 33f), 180f,
                                  new Vector3(12f, 10f, 12f), 4102, parent);

            // Gate posts, fence line and signage.
            Gate(props, new Vector3(0f, 0f, 9f), 9f);
            PropBuilder.AddFenceRun(props, new Vector3(-30f, 0f, 9f), new Vector3(-9f, 0f, 9f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(9f, 0f, 9f), new Vector3(30f, 0f, 9f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(-30f, 0f, 9f), new Vector3(-30f, 0f, 42f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(30f, 0f, 9f), new Vector3(30f, 0f, 42f), 2.4f);

            // Yard clutter: containers, pallets, a dumpster, bollards by the docks.
            for (int i = 0; i < 4; i++)
                PropBuilder.AddContainer(props, new Vector3(19f + (i % 2) * 7f, (i / 2) * 2.62f, 22f - (i / 2) * 8f), 90f, i, i % 2 == 0);
            PropBuilder.AddPalletStack(props, new Vector3(-20f, 0f, 27f), 6, 12f);
            PropBuilder.AddPalletStack(props, new Vector3(-18.2f, 0f, 27.6f), 4, -8f);
            PropBuilder.AddDumpster(props, new Vector3(-26f, 0f, 20f), 24f);
            for (int i = 0; i < 6; i++)
                PropBuilder.AddBollard(props, new Vector3(-24f + i * 3.4f, 0f, 28.5f));

            Emit(ground, "Depot Ground", parent, origin);
            Emit(props, "Depot Props", parent, origin);

            PropBuilder.CreateSign(parent, origin + new Vector3(-12f, 0f, 8.2f), 180f, 9f, 2.4f, 3.2f,
                                   WorldSurface.SignBlue, "ULTIMATE TRUCK EMPIRE\nAHMEDABAD LOGISTICS DEPOT");
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(-26f, 0f, 22f), 90f, false);
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(26f, 0f, 22f), 270f, false);
        }

        // ==================================================================
        public static void BuildFactory(Transform parent, Vector3 origin)
        {
            MeshBuilder ground = new MeshBuilder();
            MeshBuilder props = new MeshBuilder();

            Apron(ground, new Vector3(0f, 0f, 24f), 58f, 32f, WorldSurface.ConcreteDark);
            Apron(ground, new Vector3(0f, 0f, 9.5f), 20f, 6f, WorldSurface.AsphaltWorn);
            RoadBuilder.AddHatching(ground, new Vector3(-14f, 0f, 15f), 12f, 8f, 8);
            RoadBuilder.AddParkingBays(ground, new Vector3(14f, 0f, 17f), 5, 4.2f, 13f, true);
            RoadBuilder.AddArrow(ground, new Vector3(0f, 0f, 12.5f), 0f);

            BuildingBuilder.Build(BuildingStyle.Factory, origin + new Vector3(-4f, 0f, 36f), 180f,
                                  new Vector3(44f, 12f, 17f), 7311, parent);
            BuildingBuilder.Build(BuildingStyle.Warehouse, origin + new Vector3(26f, 0f, 34f), 180f,
                                  new Vector3(18f, 8f, 13f), 7312, parent);

            // Storage tanks give the destination an instantly readable silhouette.
            for (int i = 0; i < 3; i++)
            {
                float x = -26f + i * 7.5f;
                props.Add(ProcMesh.Cylinder(3.1f, 8f, 14, true), new Vector3(x, 4f, 20f), S(WorldSurface.Metal));
                props.Add(ProcMesh.Cylinder(3.25f, 0.4f, 14, false), new Vector3(x, 7.6f, 20f), S(WorldSurface.WarningStripe));
                props.Add(ProcMesh.Cylinder(0.25f, 6f, 8, false), new Vector3(x, 3f, 16.6f), S(WorldSurface.Metal));
            }
            props.Add(ProcMesh.Cylinder(0.35f, 22f, 8, false), new Vector3(-26f, 8.3f, 16.6f),
                      Quaternion.Euler(0f, 0f, 90f), S(WorldSurface.Metal));

            Gate(props, new Vector3(0f, 0f, 9f), 8f);
            PropBuilder.AddFenceRun(props, new Vector3(-29f, 0f, 9f), new Vector3(-8f, 0f, 9f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(8f, 0f, 9f), new Vector3(29f, 0f, 9f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(-29f, 0f, 9f), new Vector3(-29f, 0f, 42f), 2.4f);
            PropBuilder.AddFenceRun(props, new Vector3(29f, 0f, 9f), new Vector3(29f, 0f, 42f), 2.4f);

            for (int i = 0; i < 3; i++)
                PropBuilder.AddContainer(props, new Vector3(18f, i * 2.62f, 21f), 0f, i + 1, false);
            PropBuilder.AddDumpster(props, new Vector3(24f, 0f, 13f), -12f);

            Emit(ground, "Factory Ground", parent, origin);
            Emit(props, "Factory Props", parent, origin);

            PropBuilder.CreateSign(parent, origin + new Vector3(11f, 0f, 8.2f), 180f, 9f, 2.4f, 3.2f,
                                   WorldSurface.SignGreen, "VADODARA FACTORY\nGOODS RECEIVING");
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(-25f, 0f, 20f), 90f, false);
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(25f, 0f, 20f), 270f, false);
        }

        // ==================================================================
        public static void BuildTruckStop(Transform parent, Vector3 origin)
        {
            MeshBuilder ground = new MeshBuilder();
            MeshBuilder props = new MeshBuilder();

            Apron(ground, Vector3.zero, 40f, 28f, WorldSurface.AsphaltWorn);
            RoadBuilder.AddParkingBays(ground, new Vector3(-6f, 0f, -8f), 7, 4.4f, 16f, true);
            RoadBuilder.AddArrow(ground, new Vector3(14f, 0f, 8f), 180f);

            // Fuel canopy over four pump islands.
            props.Add(ProcMesh.Box(22f, 0.7f, 11f), new Vector3(0f, 6.2f, 6f), S(WorldSurface.MetalRoof));
            props.Add(ProcMesh.Box(22.4f, 0.5f, 11.4f), new Vector3(0f, 5.75f, 6f), S(WorldSurface.SignYellow));
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    props.Add(ProcMesh.Cylinder(0.32f, 6f, 8, false), new Vector3(i * 9f, 3f, 6f + j * 4f), S(WorldSurface.Metal));

            for (int i = 0; i < 2; i++)
            {
                float x = -5f + i * 10f;
                props.Add(ProcMesh.Box(6.5f, 0.35f, 2.2f), new Vector3(x, 0.17f, 6f), S(WorldSurface.Concrete));
                props.Add(ProcMesh.Box(1.0f, 1.9f, 0.8f), new Vector3(x - 1.6f, 1.1f, 6f), S(WorldSurface.MetalPainted));
                props.Add(ProcMesh.Box(1.0f, 1.9f, 0.8f), new Vector3(x + 1.6f, 1.1f, 6f), S(WorldSurface.MetalPainted));
            }

            BuildingBuilder.Build(BuildingStyle.Shop, origin + new Vector3(-13f, 0f, -11f), 0f,
                                  new Vector3(14f, 4.6f, 9f), 5150, parent);

            for (int i = 0; i < 5; i++)
                PropBuilder.AddBollard(props, new Vector3(-12f + i * 6f, 0f, 11.5f));
            PropBuilder.AddDumpster(props, new Vector3(17f, 0f, -10f), 90f);

            Emit(ground, "Truck Stop Ground", parent, origin);
            Emit(props, "Truck Stop Props", parent, origin);

            PropBuilder.CreateSign(parent, origin + new Vector3(16f, 0f, 12f), 0f, 6f, 2.2f, 5.5f,
                                   WorldSurface.SignYellow, "UTE TRUCK STOP\nDIESEL  \u2022  REST  \u2022  AIR");
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(-17f, 0f, 2f), 90f, false);
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(17f, 0f, 2f), 270f, false);
        }

        // ==================================================================
        public static void BuildServiceCentre(Transform parent, Vector3 origin)
        {
            MeshBuilder ground = new MeshBuilder();
            MeshBuilder props = new MeshBuilder();

            Apron(ground, Vector3.zero, 38f, 26f, WorldSurface.Concrete);
            RoadBuilder.AddParkingBays(ground, new Vector3(10f, 0f, -6f), 4, 4.4f, 13f, true);
            RoadBuilder.AddHatching(ground, new Vector3(-12f, 0f, 2f), 10f, 7f, 7);

            BuildingBuilder.Build(BuildingStyle.Warehouse, origin + new Vector3(-6f, 0f, 9f), 180f,
                                  new Vector3(26f, 7.5f, 13f), 6210, parent);

            // Tyre stacks, jack stands and a parts skip read as a workshop yard.
            for (int i = 0; i < 3; i++)
                for (int layer = 0; layer < 4; layer++)
                    props.Add(ProcMesh.Cylinder(0.55f, 0.28f, 10, true),
                              new Vector3(11f + i * 1.5f, 0.14f + layer * 0.28f, 6f), S(WorldSurface.Tyre));
            for (int i = 0; i < 4; i++)
                props.Add(ProcMesh.Frustum(0.6f, 0.6f, 0.2f, 0.2f, 1.0f), new Vector3(4f + i * 1.4f, 0.5f, 3f), S(WorldSurface.Metal));
            PropBuilder.AddDumpster(props, new Vector3(15f, 0f, 8f), 0f);
            PropBuilder.AddPalletStack(props, new Vector3(-16f, 0f, 3f), 5, 20f);

            Emit(ground, "Service Ground", parent, origin);
            Emit(props, "Service Props", parent, origin);

            PropBuilder.CreateSign(parent, origin + new Vector3(-2f, 0f, -12f), 0f, 7f, 2.2f, 3.4f,
                                   WorldSurface.SignBlue, "UTE SERVICE & REPAIR");
            PropBuilder.CreateStreetLight(parent, origin + new Vector3(-16f, 0f, -4f), 90f, false);
        }

        // ==================================================================
        private static void Apron(MeshBuilder mb, Vector3 localCentre, float sizeX, float sizeZ, WorldSurface surface)
        {
            mb.Add(ProcMesh.Box(sizeX, 0.22f, sizeZ), localCentre + new Vector3(0f, ApronY - 0.11f, 0f), S(surface));
        }

        private static void Gate(MeshBuilder mb, Vector3 localCentre, float openingWidth)
        {
            float half = openingWidth * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Add(ProcMesh.Box(0.5f, 4.2f, 0.5f), localCentre + new Vector3(side * half, 2.1f, 0f), S(WorldSurface.Concrete));
                mb.Add(ProcMesh.Box(0.7f, 0.4f, 0.7f), localCentre + new Vector3(side * half, 4.3f, 0f), S(WorldSurface.WarningStripe));
            }
            mb.Add(ProcMesh.Box(openingWidth + 1.2f, 0.4f, 0.35f), localCentre + new Vector3(0f, 4.9f, 0f), S(WorldSurface.Metal));
        }

        private static void Emit(MeshBuilder mb, string objectName, Transform parent, Vector3 origin)
        {
            GameObject go = mb.Emit(objectName, parent, WorldPaletteAdapter.Palette);
            go.transform.position = origin;
        }
    }
}
