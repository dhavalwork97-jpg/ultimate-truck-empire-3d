using UnityEngine;
using UnityEngine.EventSystems;
using UltimateTruckEmpire.CameraSystem;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.Visuals;
using UltimateTruckEmpire.World;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Save;
using UltimateTruckEmpire.UI;

namespace UltimateTruckEmpire.Bootstrap
{
    public sealed class WorldBootstrap : MonoBehaviour
    {
        private void Start() { EnsureSystems(); BuildWorld(); EnsureManagementUI(); }

        private static void EnsureSystems()
        {
            if (GameManager.Instance == null) new GameObject("GameManager").AddComponent<GameManager>();
            if (DeliveryManager.Instance == null) new GameObject("DeliveryManager").AddComponent<DeliveryManager>();
            if (ContractMarket.Instance == null) new GameObject("ContractMarket").AddComponent<ContractMarket>();
            if (MapManager.Instance == null) new GameObject("MapManager").AddComponent<MapManager>();
            if (MissionManager.Instance == null) new GameObject("MissionManager").AddComponent<MissionManager>();
            if (TimeWeatherManager.Instance == null) new GameObject("Time Weather Manager").AddComponent<TimeWeatherManager>();
            if (TrafficManager.Instance == null) new GameObject("Traffic Manager").AddComponent<TrafficManager>();
            if (CompanyManager.Instance == null) new GameObject("CompanyManager").AddComponent<CompanyManager>();
            if (DriverManager.Instance == null) new GameObject("DriverManager").AddComponent<DriverManager>();
            if (FleetManager.Instance == null) new GameObject("FleetManager").AddComponent<FleetManager>();
            if (TruckDealer.Instance == null) new GameObject("Truck Dealer").AddComponent<TruckDealer>();
            if (AutomatedDeliveryManager.Instance == null) new GameObject("AutomatedDeliveryManager").AddComponent<AutomatedDeliveryManager>();
            if (FinanceManager.Instance == null) new GameObject("FinanceManager").AddComponent<FinanceManager>();
            if (AutoDispatcher.Instance == null) new GameObject("AutoDispatcher").AddComponent<AutoDispatcher>();

            // Restore the persistent game before creating starter data, otherwise a
            // fresh company/truck would overwrite the player's saved fleet on boot.
            var saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager == null)
                saveManager = new GameObject("SaveManager").AddComponent<SaveManager>();
            saveManager.Load();

            if (!CompanyManager.Instance.IsCompanyCreated)
                CompanyManager.Instance.CreateCompany("My Trucking Company", "Ahmedabad", "General Freight");
            if (DriverManager.Instance.Drivers.Count == 0)
                DriverManager.Instance.HireDriver("Raj Patel");
            if (FleetManager.Instance.Trucks.Count == 0)
                FleetManager.Instance.BuyTruck("UTE Hauler 300", 180000f, 30f);
            FleetManager.Instance.EnsureActiveTruck();
            EnsureEventSystem();
        }

        /// UI Buttons and InputFields do nothing without an EventSystem in the
        /// scene, and the bootstrap scene is empty, so one is created here.
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureManagementUI()
        {
            if (FindFirstObjectByType<ManagementUI>() == null) new GameObject("Management UI").AddComponent<ManagementUI>();
            if (FindFirstObjectByType<ManagementActionUI>() == null) new GameObject("Management Actions").AddComponent<ManagementActionUI>();
            if (FindFirstObjectByType<DispatchAssignmentUI>() == null) new GameObject("Dispatch Assignment UI").AddComponent<DispatchAssignmentUI>();
            if (FindFirstObjectByType<TruckDealerUI>() == null) new GameObject("Truck Dealership UI").AddComponent<TruckDealerUI>();
            if (FindFirstObjectByType<GarageUI>() == null) new GameObject("Garage UI").AddComponent<GarageUI>();
            if (FindFirstObjectByType<TrafficSpawner>() == null) new GameObject("Traffic Spawner").AddComponent<TrafficSpawner>();
        }

        private static void BuildWorld()
        {
            CreateLight();

            // Roads, sites, city, skyline, vegetation and furniture are all built
            // by the world builder now, so there is one place that owns layout.
            WorldVisualBuilder.Build();

            CreateDeliveryZone(new Vector3(-55, 0, 16), "Ahmedabad Logistics Depot", DeliveryTrigger.TriggerType.Pickup,
                               new Vector3(-13, 0, 9));
            CreateDeliveryZone(new Vector3(55, 0, 16), "Vadodara Factory Warehouse", DeliveryTrigger.TriggerType.Destination,
                               new Vector3(13, 0, 9));

            var truck = CreateTruck(new Vector3(-20, 1.1f, 0));
            CreateCamera(truck);
            CreateHud(truck);

            // Give the scene its correct look on frame one; TimeWeatherManager
            // drives it from then on.
            var weather = TimeWeatherManager.Instance;
            EnvironmentAtmosphere.Ensure().Apply(weather != null ? weather.timeOfDay : 8f,
                                                 weather != null ? weather.Weather : WeatherState.Clear);
        }

        private static void CreateLight()
        {
            // Exactly one directional light in the scene; the atmosphere system
            // owns its angle, colour and intensity from here on.
            var existing = FindFirstObjectByType<EnvironmentAtmosphere>();
            if (existing != null && existing.sun != null) return;

            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, .96f, .89f);
            go.transform.rotation = Quaternion.Euler(48, 170, 0);
            RenderSettings.sun = light;
        }

        /// The load and unload zones now sit inside the yards rather than on the
        /// carriageway, so the player actually pulls into the site.
        private static void CreateDeliveryZone(Vector3 position, string zoneName, DeliveryTrigger.TriggerType type,
                                               Vector3 markerOffset)
        {
            var zone = new GameObject(zoneName + " Zone");
            zone.transform.position = position + Vector3.up;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(30, 4, 16);
            zone.AddComponent<DeliveryTrigger>().Configure(type);

            // Slim lit marker beside the gate, off the driving surface.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = zoneName + " Marker";
            marker.transform.position = new Vector3(position.x, 6f, 0f) + markerOffset;
            marker.transform.localScale = new Vector3(0.9f, 12f, 0.9f);
            Object.Destroy(marker.GetComponent<Collider>());
            var markerRenderer = marker.GetComponent<MeshRenderer>();
            if (markerRenderer != null)
                markerRenderer.sharedMaterial = TruckMaterialLibrary.MakeLens(
                    type == DeliveryTrigger.TriggerType.Pickup ? "markerPickup" : "markerDrop",
                    type == DeliveryTrigger.TriggerType.Pickup ? new Color(.20f, .60f, .95f) : new Color(.95f, .55f, .12f),
                    1.6f);
        }

        private static GameObject CreateTruck(Vector3 position)
        {
            var truck = new GameObject("Player Truck");
            SetPlayerTruckTag(truck);
            truck.transform.position = position;

            var body = truck.AddComponent<Rigidbody>();
            body.mass = 8000;
            body.centerOfMass = new Vector3(0, -.7f, 0);

            var collider = truck.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.2f, 0);
            collider.size = new Vector3(3, 2.4f, 7);

            // Placeholder blocks. The procedural visual pass below hides these and
            // builds proper bodywork; they remain as a fallback if it is removed.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "TruckCab";
            visual.transform.SetParent(truck.transform);
            visual.transform.localPosition = new Vector3(0, 1.2f, 1.3f);
            visual.transform.localScale = new Vector3(2.8f, 2.3f, 3);
            Object.Destroy(visual.GetComponent<Collider>());

            var trailer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trailer.name = "Dry Van Trailer";
            trailer.transform.SetParent(truck.transform);
            trailer.transform.localPosition = new Vector3(0, 1.35f, -2.35f);
            trailer.transform.localScale = new Vector3(2.75f, 2.8f, 4.2f);
            Object.Destroy(trailer.GetComponent<Collider>());

            truck.AddComponent<TrailerController>().Configure(TrailerType.DryVan);
            var controller = truck.AddComponent<TruckController>();
            truck.AddComponent<PlayerTruckFleetBinding>();
            truck.AddComponent<TruckPhysics>();
            truck.AddComponent<TruckInput>();
            var wheelRig = truck.AddComponent<TruckWheelRig>();

            var lights = truck.AddComponent<TruckLights>();
            BuildLamps(truck.transform, lights);
            controller.AttachLights(lights);

            ApplyTruckVisuals(truck, wheelRig);
            TruckCockpitBuilder.Build(truck.transform);
            CreateCameraAnchors(truck.transform);

            return truck;
        }

        private static void SetPlayerTruckTag(GameObject truck)
        {
            // The "PlayerTruck" tag only exists once it has been added in the Tag
            // Manager; assigning an undefined tag throws. Delivery triggers also
            // identify the truck by component, so this is best effort.
            try { truck.tag = "PlayerTruck"; }
            catch (UnityException) { Debug.LogWarning("[WorldBootstrap] Add a 'PlayerTruck' tag in Project Settings > Tags and Layers to enable tag based checks."); }
        }

        /// Builds the procedural cab, trailer, wheels and lamps, then hands the
        /// generated wheel meshes to the existing wheel rig so there is exactly one
        /// system moving them.
        private static void ApplyTruckVisuals(GameObject truck, TruckWheelRig wheelRig)
        {
            var spec = TruckVisualPresets.Resolve("nomad-aero");
            // Match the spec to this truck's actual chassis: 4.1 m wheelbase,
            // 0.62 m wheels, single drive axle.
            spec.frameFrontZ = 3.30f;
            spec.frameRearZ = -3.40f;
            spec.frameTopY = 1.05f;
            spec.wheelRadius = 0.62f;
            spec.wheelWidth = 0.38f;
            spec.trackWidth = 2.70f;
            spec.frontAxleZ = 2.05f;
            spec.rearAxleZ = -2.05f;
            spec.rearAxleCount = 1;
            spec.cabLength = 2.35f;
            spec.sleeperLength = 1.40f;
            spec.fifthWheelZ = -1.90f;
            // Screen bottom lands at ~2.29 m, which is what the cockpit geometry
            // and the driver eye point are aligned to.
            spec.windscreenRake = 0.47f;

            var visuals = TruckVisualAssembler.Apply(truck, spec, true);
            if (visuals == null) return;

            // TruckWheelRig already syncs wheels, so the assembler's own sync stays
            // disabled; instead the rig drives the new meshes.
            if (wheelRig != null && visuals.wheelVisuals != null)
                wheelRig.ReplaceVisuals(visuals.wheelVisuals);
        }

        private static void BuildLamps(Transform truck, TruckLights lights)
        {
            // Two realtime headlight spots only. Everything else is handled by the
            // emissive lamp lenses built by the visual pass.
            var left = CreateSpot(truck, "Headlight Left", new Vector3(-1.0f, 1.15f, 3.25f));
            var right = CreateSpot(truck, "Headlight Right", new Vector3(1.0f, 1.15f, 3.25f));
            lights.Configure(new[] { left, right }, new Light[0], new Light[0], new Light[0], new Light[0]);
        }

        private static Light CreateSpot(Transform parent, string lightName, Vector3 localPosition)
        {
            var go = new GameObject(lightName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 55f;
            light.spotAngle = 62f;
            light.intensity = 2.4f;
            light.color = new Color(1f, .97f, .88f);
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static void CreateCameraAnchors(Transform truck)
        {
            // Just clear of the nose, so neither view looks into the bodywork.
            CreateAnchor(truck, "HoodCameraAnchor", new Vector3(0f, 2.95f, 3.34f), -4f);
            CreateAnchor(truck, "BumperCameraAnchor", new Vector3(0f, 1.05f, 3.62f), -1f);
        }

        private static void CreateAnchor(Transform truck, string anchorName, Vector3 localPosition, float pitch)
        {
            var go = new GameObject(anchorName);
            go.transform.SetParent(truck, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static void CreateCamera(GameObject truck)
        {
            // One camera for every view. Re-uses an existing MainCamera if the scene
            // already has one so nothing ends up with two.
            var existing = Camera.main;
            GameObject camGo = existing != null ? existing.gameObject : new GameObject("Main Camera");
            if (existing == null) camGo.tag = "MainCamera";

            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = .08f;
            cam.farClipPlane = 900f;

            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();

            var rig = camGo.GetComponent<TruckCameraRig>() ?? camGo.AddComponent<TruckCameraRig>();
            var cockpit = truck.GetComponentInChildren<TruckCockpit>();
            rig.Bind(
                truck.transform,
                truck.GetComponent<Rigidbody>(),
                cockpit != null ? cockpit.eyeAnchor : null,
                truck.transform.Find("HoodCameraAnchor"),
                truck.transform.Find("BumperCameraAnchor"));
        }

        private static void CreateHud(GameObject truck)
        {
            var hudGo = FindFirstObjectByType<DrivingHUD>();
            if (hudGo == null)
            {
                var go = new GameObject("Driving HUD");
                hudGo = go.AddComponent<DrivingHUD>();
            }
            hudGo.Bind(truck.GetComponent<TruckController>(), truck.GetComponent<TruckLights>());
            DeliveryManager.Instance?.AcceptStarterContract();
        }
    }
}
