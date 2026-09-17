using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public static class WorldVisualBuilder
    {
        public static void Build()
        {
            CreateGround();
            CreateRoadMarkings();
            CreateCityBlocks();
            CreateTrees();
            CreateStreetLights();
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "World Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 30f;
        }

        private static void CreateRoadMarkings()
        {
            for (int z = -140; z <= 140; z += 12) CreateMarking(new Vector3(0, .03f, z), new Vector3(7, .03f, .35f));
            for (int x = -170; x <= 170; x += 12) CreateMarking(new Vector3(x, .03f, 70), new Vector3(.35f, .03f, 7));
            for (int x = -170; x <= 170; x += 12) CreateMarking(new Vector3(x, .03f, -70), new Vector3(.35f, .03f, 7));
        }

        private static void CreateMarking(Vector3 position, Vector3 scale)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = "Lane Marking"; o.transform.position = position; o.transform.localScale = scale; Object.Destroy(o.GetComponent<Collider>());
        }

        private static void CreateCityBlocks()
        {
            for (int x = -120; x <= 120; x += 30)
            for (int z = -110; z <= 110; z += 40)
            {
                if (Mathf.Abs(z) < 18 || Mathf.Abs(x + 90) < 18) continue;
                float height = 4f + Mathf.Abs((x * 7 + z * 3) % 12);
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "City Building"; b.transform.position = new Vector3(x, height * .5f, z); b.transform.localScale = new Vector3(18, height, 22);
            }
        }

        private static void CreateTrees()
        {
            for (int i = 0; i < 40; i++)
            {
                float x = -160 + (i * 37) % 320; float z = -130 + (i * 61) % 260;
                if (Mathf.Abs(z) < 12 || Mathf.Abs(z - 70) < 12 || Mathf.Abs(z + 70) < 12) continue;
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder); trunk.name = "Roadside Tree"; trunk.transform.position = new Vector3(x, 2, z); trunk.transform.localScale = new Vector3(.7f, 2, .7f);
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Tree Crown"; crown.transform.position = new Vector3(x, 5, z); crown.transform.localScale = Vector3.one * 4f; Object.Destroy(crown.GetComponent<Collider>());
            }
        }

        private static void CreateStreetLights()
        {
            for (int z = -120; z <= 120; z += 30)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pole.name = "Street Light"; pole.transform.position = new Vector3(-8, 3, z); pole.transform.localScale = new Vector3(.12f, 3, .12f);
                var lamp = new GameObject("Street Lamp"); lamp.transform.position = new Vector3(-8, 6, z); var light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.range = 14; light.intensity = 2f;
            }
        }
    }
}
