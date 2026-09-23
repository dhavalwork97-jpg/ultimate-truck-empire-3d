using UnityEngine;
using UltimateTruckEmpire.Visuals;
using UltimateTruckEmpire.Gameplay.Toll;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Assembles the whole procedural environment: terrain, road network, the
    /// trucking sites, city blocks, background skyline, vegetation and street
    /// furniture.
    ///
    /// Two rules keep this affordable:
    ///  * small repeated things are welded into batched meshes (one renderer for
    ///    every tree in a region, one for every fence and bollard on a site);
    ///  * anything the player can hit is a box collider, and nothing else has one.
    /// </summary>
    public static class WorldVisualBuilder
    {
        private const float GroundExtent = 600f;   // plane scale 60 => 600 m across

        // Road corridors, used to keep buildings and trees off the carriageway.
        private static readonly float[] EastWestRoads = { 0f, 70f, -70f };
        private const float NorthSouthRoadX = -90f;
        private const float RoadHalfWidth = 7f;
        private const float RoadClearance = 11f;

        public static void Build()
        {
            RoadBuilder.Reset();
            RoadNetwork.Reset();

            Transform root = new GameObject("World").transform;

            CreateGround(root);
            BuildRoadNetwork(root);
            BuildSites(root);
            BuildCityBlocks(root);
            BuildBackgroundSkyline(root);
            BuildVegetation(root);
            BuildStreetFurniture(root);

            EnvironmentAtmosphere.Ensure();
            StreetLightManager.Ensure();
            TollPlazaBuilder.BuildDrivenMapPlaza(root);
        }

        private static int S(WorldSurface surface) => WorldPaletteAdapter.Slot(surface);

        // ==================================================================
        private static void CreateGround(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "World Ground";
            ground.transform.SetParent(root, false);
            // Sits just below the asphalt so road and terrain are never coplanar.
            ground.transform.position = new Vector3(0f, RoadBuilder.GroundY, 0f);
            ground.transform.localScale = Vector3.one * (GroundExtent / 10f);
            WorldPalette.Paint(ground, WorldSurface.GrassDry);

            // Scattered dirt and lush patches stop the terrain reading as one flat colour.
            MeshBuilder mb = new MeshBuilder();
            for (int i = 0; i < 26; i++)
            {
                float x = -220f + (i * 83f) % 440f;
                float z = -190f + (i * 137f) % 380f;
                if (!IsClearOfRoads(new Vector3(x, 0f, z), new Vector3(40f, 0f, 30f))) continue;
                WorldSurface surface = i % 3 == 0 ? WorldSurface.Dirt
                                     : i % 3 == 1 ? WorldSurface.GrassLush
                                     : WorldSurface.Gravel;
                PropBuilder.AddGroundPatch(mb, new Vector3(x, 0f, z), 26f + (i % 4) * 9f, 18f + (i % 3) * 11f, surface);
            }
            mb.Emit("Ground Patches", root, WorldPaletteAdapter.Palette);
        }

        // ==================================================================
        private static void BuildRoadNetwork(Transform root)
        {
            RoadBuilder.RoadOptions highway = RoadBuilder.RoadOptions.Highway();
            RoadBuilder.RoadOptions street = RoadBuilder.RoadOptions.Street();
            RoadBuilder.RoadOptions access = RoadBuilder.RoadOptions.Access();

            // Main east-west highway, split around the junction with the north road.
            RoadBuilder.BuildEastWest("Highway West", -160f, -98f, 0f, 14f, highway);
            RoadBuilder.BuildEastWest("Highway East", -82f, 160f, 0f, 14f, highway);

            RoadBuilder.BuildEastWest("North Highway West", -160f, -98f, 70f, 14f, highway);
            RoadBuilder.BuildEastWest("North Highway East", -82f, 160f, 70f, 14f, highway);

            RoadBuilder.BuildEastWest("South Highway", -160f, 160f, -70f, 14f, highway);

            // Connecting road, split around both junctions it crosses.
            RoadBuilder.BuildNorthSouth("Link Road South", -35f, -8f, NorthSouthRoadX, 14f, street);
            RoadBuilder.BuildNorthSouth("Link Road Middle", 8f, 62f, NorthSouthRoadX, 14f, street);
            RoadBuilder.BuildNorthSouth("Link Road North", 78f, 105f, NorthSouthRoadX, 14f, street);

            RoadBuilder.BuildIntersection("Junction South", new Vector3(NorthSouthRoadX, 0f, 0f), 16f, 16f);
            RoadBuilder.BuildIntersection("Junction North", new Vector3(NorthSouthRoadX, 0f, 70f), 16f, 16f);

            // Shared semantic metadata mirrors the same coordinates used above.
            RoadNetwork.RegisterCorridor("Highway South", true, -70f, -160f, 160f, 14f);
            RoadNetwork.RegisterCorridor("Highway Main", true, 0f, -160f, 160f, 14f);
            RoadNetwork.RegisterCorridor("Highway North", true, 70f, -160f, 160f, 14f);
            RoadNetwork.RegisterCorridor("Link Road", false, NorthSouthRoadX, -35f, 105f, 14f);
            RoadNetwork.RegisterJunction("Junction South", new Vector3(NorthSouthRoadX, 0f, 0f), 16f, 16f, true);
            RoadNetwork.RegisterJunction("Junction North", new Vector3(NorthSouthRoadX, 0f, 70f), 16f, 16f, true);

            // Site access roads. They start exactly at the highway edge so no two
            // asphalt surfaces overlap.
            RoadBuilder.BuildNorthSouth("Depot Access", RoadHalfWidth, 12f, -55f, 12f, access);
            RoadBuilder.BuildNorthSouth("Factory Access", RoadHalfWidth, 12f, 55f, 12f, access);
            RoadBuilder.BuildNorthSouth("Truck Stop Access", -17f, -RoadHalfWidth, 28f, 12f, access);
            RoadBuilder.BuildNorthSouth("Service Access", -18f, -RoadHalfWidth, -30f, 12f, access);

            // Painted turn arrows on the approaches to each site.
            MeshBuilder paint = new MeshBuilder();
            RoadBuilder.AddArrow(paint, new Vector3(-70f, 0f, -3.5f), 0f);
            RoadBuilder.AddArrow(paint, new Vector3(40f, 0f, -3.5f), 0f);
            RoadBuilder.AddArrow(paint, new Vector3(70f, 0f, 3.5f), 180f);
            RoadBuilder.AddArrow(paint, new Vector3(-40f, 0f, 3.5f), 180f);
            paint.Emit("Road Arrows", root, WorldPaletteAdapter.Palette);
        }

        // ==================================================================
        private static void BuildSites(Transform root)
        {
            SiteBuilder.BuildLogisticsDepot(root, new Vector3(-55f, 0f, 0f));
            SiteBuilder.BuildFactory(root, new Vector3(55f, 0f, 0f));
            SiteBuilder.BuildTruckStop(root, new Vector3(28f, 0f, -30f));
            SiteBuilder.BuildServiceCentre(root, new Vector3(-30f, 0f, -30f));
        }

        // ==================================================================
        private static void BuildCityBlocks(Transform root)
        {
            int seed = 0;
            for (float x = -146f; x <= 146f; x += 29f)
            {
                for (float z = -112f; z <= 112f; z += 34f)
                {
                    seed++;
                    int hash = Mathf.Abs(Mathf.RoundToInt(x) * 73856093 ^ Mathf.RoundToInt(z) * 19349663);

                    float jitterX = ((hash % 7) - 3) * 1.6f;
                    float jitterZ = ((hash / 7 % 7) - 3) * 1.6f;
                    Vector3 position = new Vector3(x + jitterX, 0f, z + jitterZ);

                    BuildingStyle style = StyleFor(hash, position);
                    Vector3 size = SizeFor(style, hash);

                    if (!IsClearOfRoads(position, size)) continue;
                    if (OverlapsSite(position, size)) continue;

                    float yaw = (hash % 4) * 90f;
                    BuildingBuilder.Build(style, position, yaw, size, hash + seed, root);
                }
            }
        }

        private static BuildingStyle StyleFor(int hash, Vector3 position)
        {
            // Industry clusters near the highway, housing and offices further out.
            bool nearHighway = Mathf.Abs(position.z) < 48f;
            switch (hash % 5)
            {
                case 0: return nearHighway ? BuildingStyle.Warehouse : BuildingStyle.Residential;
                case 1: return nearHighway ? BuildingStyle.Factory : BuildingStyle.Residential;
                case 2: return BuildingStyle.Office;
                case 3: return BuildingStyle.Shop;
                default: return nearHighway ? BuildingStyle.Warehouse : BuildingStyle.Office;
            }
        }

        private static Vector3 SizeFor(BuildingStyle style, int hash)
        {
            float a = (hash % 5) / 4f;
            float b = (hash / 5 % 5) / 4f;
            switch (style)
            {
                case BuildingStyle.Warehouse: return new Vector3(Mathf.Lerp(24f, 38f, a), Mathf.Lerp(7f, 11f, b), Mathf.Lerp(14f, 20f, b));
                case BuildingStyle.Factory: return new Vector3(Mathf.Lerp(26f, 40f, a), Mathf.Lerp(9f, 14f, b), Mathf.Lerp(16f, 22f, a));
                case BuildingStyle.Office: return new Vector3(Mathf.Lerp(14f, 22f, a), Mathf.Lerp(12f, 30f, b), Mathf.Lerp(13f, 19f, b));
                case BuildingStyle.Shop: return new Vector3(Mathf.Lerp(12f, 20f, a), Mathf.Lerp(4f, 6f, b), Mathf.Lerp(10f, 14f, a));
                default: return new Vector3(Mathf.Lerp(14f, 22f, a), Mathf.Lerp(9f, 24f, b), Mathf.Lerp(12f, 18f, b));
            }
        }

        // ==================================================================
        /// <summary>
        /// A ring of plain slabs well beyond the playable area. They give the
        /// horizon depth through the fog and cost almost nothing - no colliders,
        /// no detail, no windows.
        /// </summary>
        private static void BuildBackgroundSkyline(Transform root)
        {
            for (int i = 0; i < 34; i++)
            {
                float angle = i / 34f * Mathf.PI * 2f;
                int hash = Mathf.Abs(i * 733 % 977) + i * 13;
                float radius = 215f + (hash % 60);
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 size = new Vector3(18f + hash % 22, 16f + hash % 46, 16f + hash % 18);
                BuildingBuilder.Build(BuildingStyle.Silhouette, position, (hash % 4) * 90f, size, hash, root);
            }
        }

        // ==================================================================
        private static void BuildVegetation(Transform root)
        {
            MeshBuilder near = new MeshBuilder();
            MeshBuilder far = new MeshBuilder();

            // Roadside planting along the highways.
            for (int i = 0; i < 64; i++)
            {
                int hash = Mathf.Abs(i * 374761393 % 9973);
                float x = -150f + (i * 23.7f) % 300f;
                float roadZ = EastWestRoads[i % EastWestRoads.Length];
                float side = (i % 2 == 0) ? 1f : -1f;
                float z = roadZ + side * (12.5f + (hash % 5));

                Vector3 position = new Vector3(x, RoadBuilder.GroundY, z);
                if (OverlapsSite(position, new Vector3(6f, 0f, 6f))) continue;

                float scale = 0.8f + (hash % 60) / 100f;
                PropBuilder.AddTree(near, position, scale, hash, hash % 3 == 0);
                if (hash % 4 == 0) PropBuilder.AddBush(near, position + new Vector3(2.4f, 0f, 1.1f), 0.7f);
            }

            // Looser scatter further out for midground interest.
            for (int i = 0; i < 46; i++)
            {
                int hash = Mathf.Abs(i * 668265263 % 7919);
                float x = -190f + (hash % 380);
                float z = -170f + (hash / 3 % 340);
                Vector3 position = new Vector3(x, RoadBuilder.GroundY, z);
                if (!IsClearOfRoads(position, new Vector3(8f, 0f, 8f))) continue;
                if (OverlapsSite(position, new Vector3(8f, 0f, 8f))) continue;
                PropBuilder.AddTree(far, position, 0.9f + (hash % 70) / 100f, hash, hash % 2 == 0);
            }

            near.Emit("Roadside Vegetation", root, WorldPaletteAdapter.Palette);
            far.Emit("Scatter Vegetation", root, WorldPaletteAdapter.Palette);
        }

        // ==================================================================
        private static void BuildStreetFurniture(Transform root)
        {
            MeshBuilder batch = new MeshBuilder();

            // Street lights alternate sides along the main highway.
            for (int i = 0; i < 13; i++)
            {
                float x = -138f + i * 23f;
                bool north = i % 2 == 0;
                float z = north ? 11.5f : -11.5f;
                PropBuilder.CreateStreetLight(root, new Vector3(x, 0f, z), north ? 180f : 0f, false);
            }

            // Signals on both junctions.
            PropBuilder.CreateTrafficLight(root, new Vector3(NorthSouthRoadX + 9f, 0f, -9f), 180f);
            PropBuilder.CreateTrafficLight(root, new Vector3(NorthSouthRoadX - 9f, 0f, 9f), 0f);
            PropBuilder.CreateTrafficLight(root, new Vector3(NorthSouthRoadX + 9f, 0f, 61f), 180f);
            PropBuilder.CreateTrafficLight(root, new Vector3(NorthSouthRoadX - 9f, 0f, 79f), 0f);

            // Direction signs on the approach to each site.
            PropBuilder.CreateSign(root, new Vector3(-72f, 0f, 10.5f), 180f, 7f, 2.0f, 3.6f,
                                   WorldSurface.SignGreen, "AHMEDABAD DEPOT  1 km");
            PropBuilder.CreateSign(root, new Vector3(38f, 0f, 10.5f), 180f, 7f, 2.0f, 3.6f,
                                   WorldSurface.SignGreen, "VADODARA  \u2192");
            PropBuilder.CreateSign(root, new Vector3(16f, 0f, -10.5f), 0f, 6f, 1.8f, 3.4f,
                                   WorldSurface.SignBlue, "TRUCK STOP  \u2193");

            // Guard rails on the outside of the long straights, utility poles along
            // the link road, and bollards protecting the junction corners.
            PropBuilder.AddGuardRail(batch, new Vector3(-150f, 0f, -11.2f), new Vector3(-100f, 0f, -11.2f));
            PropBuilder.AddGuardRail(batch, new Vector3(100f, 0f, 11.2f), new Vector3(150f, 0f, 11.2f));
            PropBuilder.AddGuardRail(batch, new Vector3(-150f, 0f, -81f), new Vector3(-90f, 0f, -81f));

            for (int i = 0; i < 8; i++)
                PropBuilder.AddUtilityPole(batch, new Vector3(NorthSouthRoadX - 11.5f, RoadBuilder.GroundY, -28f + i * 17f));

            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? 1f : -1f;
                float sz = (i < 2) ? 1f : -1f;
                PropBuilder.AddBollard(batch, new Vector3(NorthSouthRoadX + sx * 9.5f, 0f, sz * 9.5f));
            }

            batch.Emit("Street Furniture", root, WorldPaletteAdapter.Palette);
        }

        // ==================================================================
        // Placement rules
        // ==================================================================
        private static bool IsClearOfRoads(Vector3 position, Vector3 size)
        {
            float halfZ = size.z * 0.5f;
            for (int i = 0; i < EastWestRoads.Length; i++)
                if (Mathf.Abs(position.z - EastWestRoads[i]) < halfZ + RoadClearance) return false;

            float halfX = size.x * 0.5f;
            if (Mathf.Abs(position.x - NorthSouthRoadX) < halfX + RoadClearance &&
                position.z > -50f && position.z < 120f) return false;

            return true;
        }

        private static bool OverlapsSite(Vector3 position, Vector3 size)
        {
            Rect footprint = new Rect(position.x - size.x * 0.5f, position.z - size.z * 0.5f, size.x, size.z);
            return Overlaps(footprint, SiteBuilder.DepotArea)
                || Overlaps(footprint, SiteBuilder.FactoryArea)
                || Overlaps(footprint, SiteBuilder.TruckStopArea)
                || Overlaps(footprint, SiteBuilder.ServiceArea);
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return a.x < b.x + b.width && b.x < a.x + a.width &&
                   a.y < b.y + b.height && b.y < a.y + a.height;
        }
    }
}
