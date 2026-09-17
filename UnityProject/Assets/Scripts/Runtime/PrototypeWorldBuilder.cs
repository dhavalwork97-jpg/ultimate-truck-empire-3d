using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class PrototypeWorldBuilder : MonoBehaviour
    {
        [SerializeField] private bool buildOnAwake = true;

        private void Awake()
        {
            if (buildOnAwake)
                Build();
        }

        public void Build()
        {
            if (FindFirstObjectByType<TruckController>() != null)
                return;

            BuildLighting();
            BuildGround();
            BuildRoads();
            BuildDepot();
            BuildTruck();
        }

        private static void BuildLighting()
        {
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.30f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.04f, 0.05f);
        }

        private static void BuildGround()
        {
            CreateCube("Ground", new Vector3(0f, -0.3f, 0f), new Vector3(180f, 0.5f, 180f), new Color(0.055f, 0.065f, 0.075f));
        }

        private static void BuildRoads()
        {
            CreateCube("MainRoad", new Vector3(0f, 0f, 0f), new Vector3(16f, 0.12f, 150f), new Color(0.10f, 0.11f, 0.12f));
            CreateCube("CrossRoad", new Vector3(0f, 0.01f, 34f), new Vector3(150f, 0.12f, 16f), new Color(0.10f, 0.11f, 0.12f));

            for (int z = -70; z <= 70; z += 8)
                CreateCube("LaneMark", new Vector3(0f, 0.08f, z), new Vector3(0.35f, 0.035f, 4.5f), new Color(0.85f, 0.78f, 0.52f));

            CreateRoadSideProps();
        }

        private static void CreateRoadSideProps()
        {
            for (int z = -60; z <= 60; z += 15)
            {
                CreateStreetLight(new Vector3(-11f, 0f, z));
                CreateStreetLight(new Vector3(11f, 0f, z + 7f));
            }

            for (int i = 0; i < 14; i++)
            {
                float x = i % 2 == 0 ? -25f : 25f;
                float z = -60f + i * 9f;
                float h = 5f + (i % 4) * 2.5f;
                CreateCube("CityBuilding", new Vector3(x, h * 0.5f, z), new Vector3(10f, h, 9f), new Color(0.12f, 0.14f, 0.18f));
            }
        }

        private static void BuildDepot()
        {
            CreateCube("DepotYard", new Vector3(35f, 0.05f, 34f), new Vector3(48f, 0.16f, 34f), new Color(0.16f, 0.16f, 0.15f));
            CreateCube("DepotWarehouse", new Vector3(45f, 5f, 45f), new Vector3(22f, 10f, 16f), new Color(0.20f, 0.23f, 0.26f));
            CreateCube("WarehouseDoor", new Vector3(33.8f, 3.5f, 36.7f), new Vector3(0.15f, 7f, 5.5f), new Color(0.035f, 0.04f, 0.045f));
        }

        private static void BuildTruck()
        {
            var root = new GameObject("StarterTruck");
            root.transform.position = new Vector3(0f, 0.9f, -45f);

            var body = CreatePrimitive(PrimitiveType.Cube, "Cab", root.transform, new Vector3(0f, 0.75f, 0f), new Vector3(2.7f, 1.5f, 4.2f), new Color(0.82f, 0.16f, 0.055f));
            var trailer = CreatePrimitive(PrimitiveType.Cube, "Trailer", root.transform, new Vector3(0f, 1.65f, 4.2f), new Vector3(2.65f, 2.25f, 6.5f), new Color(0.68f, 0.70f, 0.72f));
            trailer.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Cube, "Windshield", body.transform, new Vector3(0f, 0.25f, -2.12f), new Vector3(2.15f, 0.55f, 0.08f), new Color(0.045f, 0.10f, 0.15f));
            CreatePrimitive(PrimitiveType.Cube, "TrailerStripe", trailer.transform, new Vector3(0f, 0f, -0.05f), new Vector3(2.72f, 0.24f, 6.65f), new Color(0.82f, 0.16f, 0.055f));

            float[] wheelZ = { -1.35f, 1.45f, 4.2f, 5.65f };
            foreach (float z in wheelZ)
            {
                CreateWheel(root.transform, new Vector3(-1.42f, 0.2f, z));
                CreateWheel(root.transform, new Vector3(1.42f, 0.2f, z));
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 7000f;
            rb.drag = 0.15f;
            rb.angularDrag = 3f;

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.1f, 1.5f);
            collider.size = new Vector3(2.9f, 2.8f, 9.2f);

            root.AddComponent<TruckController>();

            var cameraObject = new GameObject("TruckCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            var follow = cameraObject.AddComponent<TruckCamera>();
            follow.SetTarget(root.transform);
            cameraObject.tag = "MainCamera";
        }

        private static void CreateWheel(Transform parent, Vector3 localPosition)
        {
            var wheel = CreatePrimitive(PrimitiveType.Cylinder, "Wheel", parent, localPosition, new Vector3(0.7f, 0.28f, 0.7f), new Color(0.025f, 0.025f, 0.03f));
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void CreateStreetLight(Vector3 position)
        {
            var pole = CreateCube("StreetLight", position + new Vector3(0f, 3f, 0f), new Vector3(0.12f, 6f, 0.12f), new Color(0.18f, 0.19f, 0.21f));
            var lamp = new GameObject("Lamp");
            lamp.transform.position = position + new Vector3(0f, 6.1f, 0f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 2.2f;
            light.color = new Color(1f, 0.72f, 0.42f);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            var cube = CreatePrimitive(PrimitiveType.Cube, name, null, position, scale, color);
            return cube;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            if (parent == null)
                obj.transform.position = position;
            else
                obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            var renderer = obj.GetComponent<Renderer>();
            renderer.material = CreateMaterial(color);
            return obj;
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }
    }
}
