using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
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
            if (!CompanyManager.Instance.IsCompanyCreated) CompanyManager.Instance.CreateCompany("My Trucking Company", "Ahmedabad", "General Freight");
            if (DriverManager.Instance.Drivers.Count == 0) DriverManager.Instance.HireDriver("Raj Patel");
            if (FleetManager.Instance.Trucks.Count == 0) FleetManager.Instance.BuyTruck("UTE Hauler 300", 180000f, 30f);
            if (FindFirstObjectByType<SaveManager>() == null) new GameObject("SaveManager").AddComponent<SaveManager>();
        }

        private static void EnsureManagementUI()
        {
            if (FindFirstObjectByType<ManagementUI>() == null) new GameObject("Management UI").AddComponent<ManagementUI>();
            if (FindFirstObjectByType<ManagementActionUI>() == null) new GameObject("Management Actions").AddComponent<ManagementActionUI>();
            if (FindFirstObjectByType<DispatchAssignmentUI>() == null) new GameObject("Dispatch Assignment UI").AddComponent<DispatchAssignmentUI>();
            if (FindFirstObjectByType<TruckDealerUI>() == null) new GameObject("Truck Dealership UI").AddComponent<TruckDealerUI>();
        }

        private static void BuildWorld()
        {
            RenderSettings.ambientIntensity = 1.1f; RenderSettings.fog = true; RenderSettings.fogDensity = .004f;
            CreateLight();
            CreateRoad(new Vector3(0, -.15f, 0), new Vector3(180, .3f, 14));
            CreateRoad(new Vector3(0, -.15f, 70), new Vector3(180, .3f, 14));
            CreateRoad(new Vector3(0, -.15f, -70), new Vector3(180, .3f, 14));
            CreateRoad(new Vector3(-90, -.15f, 35), new Vector3(14, .3f, 140));
            WorldVisualBuilder.Build();
            CreateDepot(new Vector3(-55, 0, 0), "Ahmedabad Logistics Depot", DeliveryTrigger.TriggerType.Pickup);
            CreateDepot(new Vector3(55, 0, 0), "Vadodara Factory Warehouse", DeliveryTrigger.TriggerType.Destination);
            CreateTruck(new Vector3(-20, 1.1f, 0)); CreateHud();
        }

        private static void CreateLight()
        {
            var go = new GameObject("Sun"); var light = go.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; go.transform.rotation = Quaternion.Euler(48, -30, 0);
        }

        private static void CreateRoad(Vector3 position, Vector3 scale)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube); road.name = "Road"; road.transform.position = position; road.transform.localScale = scale;
        }

        private static void CreateDepot(Vector3 position, string name, DeliveryTrigger.TriggerType type)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube); building.name = name; building.transform.position = position + new Vector3(0, 5, 12); building.transform.localScale = new Vector3(28, 10, 18);
            var zone = new GameObject(name + " Zone"); zone.transform.position = position + Vector3.up; var box = zone.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(22, 3, 12); zone.AddComponent<DeliveryTrigger>().Configure(type);
        }

        private static void CreateTruck(Vector3 position)
        {
            var truck = new GameObject("Player Truck"); truck.tag = "PlayerTruck"; truck.transform.position = position;
            var body = truck.AddComponent<Rigidbody>(); body.mass = 8000; body.centerOfMass = new Vector3(0, -.7f, 0);
            var collider = truck.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 1.2f, 0); collider.size = new Vector3(3, 2.4f, 7);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube); visual.name = "TruckCab"; visual.transform.SetParent(truck.transform); visual.transform.localPosition = new Vector3(0, 1.2f, 1.3f); visual.transform.localScale = new Vector3(2.8f, 2.3f, 3); Object.Destroy(visual.GetComponent<Collider>());
            var trailer = GameObject.CreatePrimitive(PrimitiveType.Cube); trailer.name = "Dry Van Trailer"; trailer.transform.SetParent(truck.transform); trailer.transform.localPosition = new Vector3(0, 1.35f, -2.35f); trailer.transform.localScale = new Vector3(2.75f, 2.8f, 4.2f); Object.Destroy(trailer.GetComponent<Collider>());
            truck.AddComponent<TrailerController>().Configure(TrailerType.DryVan);
            truck.AddComponent<TruckController>(); truck.AddComponent<TruckPhysics>(); truck.AddComponent<TruckInput>(); truck.AddComponent<TruckWheelRig>();
            var camGo = new GameObject("Truck Camera"); camGo.transform.SetParent(truck.transform); camGo.localPosition = new Vector3(0, 4, -8); camGo.LookAt(truck.transform.position + Vector3.up); camGo.AddComponent<Camera>();
        }

        private static void CreateHud()
        {
            var canvasGo = new GameObject("HUD"); var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvasGo.AddComponent<CanvasScaler>(); canvasGo.AddComponent<GraphicRaycaster>();
            var textGo = new GameObject("HUD Text"); textGo.transform.SetParent(canvasGo.transform); var text = textGo.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 22; text.alignment = TextAnchor.UpperLeft;
            text.text = "ULTIMATE TRUCK EMPIRE\n\nWASD / Arrows Drive   SPACE Brake   I Engine   L Lights   Q/R Indicators\nF1 Fleet   F2 Drivers   F3 Contracts   F4 Finances   F5 Company   F6 Active Jobs\nF7 Weather   D Dealership\n\nDRIVE THE TRUCK. BUILD THE COMPANY. CREATE THE EMPIRE.";
            var rt = text.rectTransform; rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(24, -24); rt.sizeDelta = new Vector2(800, 320); DeliveryManager.Instance?.AcceptStarterContract();
        }
    }
}
