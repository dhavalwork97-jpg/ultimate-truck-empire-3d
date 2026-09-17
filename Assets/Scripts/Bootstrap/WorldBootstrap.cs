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
 public sealed class WorldBootstrap:MonoBehaviour
 {
  private void Start(){EnsureSystems();BuildWorld();}
  private static void EnsureSystems(){
   if(GameManager.Instance==null)new GameObject("GameManager").AddComponent<GameManager>();
   if(DeliveryManager.Instance==null)new GameObject("DeliveryManager").AddComponent<DeliveryManager>();
   if(CompanyManager.Instance==null)new GameObject("CompanyManager").AddComponent<CompanyManager>();
   if(DriverManager.Instance==null)new GameObject("DriverManager").AddComponent<DriverManager>();
   if(FleetManager.Instance==null)new GameObject("FleetManager").AddComponent<FleetManager>();
   if(AutomatedDeliveryManager.Instance==null)new GameObject("AutomatedDeliveryManager").AddComponent<AutomatedDeliveryManager>();
   if(FinanceManager.Instance==null)new GameObject("FinanceManager").AddComponent<FinanceManager>();
   if(!CompanyManager.Instance.IsCompanyCreated)CompanyManager.Instance.CreateCompany("My Trucking Company","Ahmedabad");
   if(DriverManager.Instance.Drivers.Count==0)DriverManager.Instance.HireDriver("Raj Patel");
   if(FleetManager.Instance.Trucks.Count==0)FleetManager.Instance.BuyTruck("UTE Hauler 300",180000f,30f);
   if(FindFirstObjectByType<SaveManager>()==null)new GameObject("SaveManager").AddComponent<SaveManager>();
  }
  private static void BuildWorld(){RenderSettings.ambientIntensity=1.1f;RenderSettings.fog=true;RenderSettings.fogDensity=.004f;CreateLight();CreateRoad(new Vector3(0,-.15f,0),new Vector3(180,.3f,14));CreateRoad(new Vector3(0,-.15f,70),new Vector3(180,.3f,14));CreateRoad(new Vector3(0,-.15f,-70),new Vector3(180,.3f,14));CreateDepot(new Vector3(-55,0,0),"Ahmedabad Logistics Depot",DeliveryTrigger.TriggerType.Pickup);CreateDepot(new Vector3(55,0,0),"Vadodara Factory Warehouse",DeliveryTrigger.TriggerType.Destination);CreateTruck(new Vector3(-20,1.1f,0));CreateHud();var ui=new GameObject("Management UI");ui.AddComponent<ManagementUI>();}
  private static void CreateLight(){var go=new GameObject("Sun");var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;go.transform.rotation=Quaternion.Euler(48,-30,0);}
  private static void CreateRoad(Vector3 position,Vector3 scale){var road=GameObject.CreatePrimitive(PrimitiveType.Cube);road.name="Road";road.transform.position=position;road.transform.localScale=scale;}
  private static void CreateDepot(Vector3 position,string name,DeliveryTrigger.TriggerType type){var building=GameObject.CreatePrimitive(PrimitiveType.Cube);building.name=name;building.transform.position=position+new Vector3(0,5,12);building.transform.localScale=new Vector3(28,10,18);var zone=new GameObject(name+" Zone");zone.transform.position=position+Vector3.up;var box=zone.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(22,3,12);zone.AddComponent<DeliveryTrigger>().Configure(type);}
  private static void CreateTruck(Vector3 position){var truck=new GameObject("Player Truck");truck.tag="PlayerTruck";truck.transform.position=position;var body=truck.AddComponent<Rigidbody>();body.mass=8000;body.centerOfMass=new Vector3(0,-.7f,0);var collider=truck.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.2f,0);collider.size=new Vector3(3,2.4f,7);var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="TruckCab";visual.transform.SetParent(truck.transform);visual.transform.localPosition=new Vector3(0,1.2f,1.3f);visual.transform.localScale=new Vector3(2.8f,2.3f,3);Object.Destroy(visual.GetComponent<Collider>());truck.AddComponent<TruckController>();truck.AddComponent<TruckPhysics>();truck.AddComponent<TruckInput>();truck.AddComponent<TruckWheelRig>();var camGo=new GameObject("Truck Camera");camGo.transform.SetParent(truck.transform);camGo.transform.localPosition=new Vector3(0,4,-8);camGo.transform.LookAt(truck.transform.position+Vector3.up);camGo.AddComponent<Camera>();}
  private static void CreateHud(){var canvasGo=new GameObject("HUD");var canvas=canvasGo.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvasGo.AddComponent<CanvasScaler>();canvasGo.AddComponent<GraphicRaycaster>();var textGo=new GameObject("HUD Text");textGo.transform.SetParent(canvasGo.transform);var text=textGo.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=24;text.alignment=TextAnchor.UpperLeft;text.text="ULTIMATE TRUCK EMPIRE\n\nWASD / Arrows  Drive\nSPACE Brake   I Engine   L Lights\nF1 Fleet   F2 Drivers   F3 Contracts   F4 Finances   F5 Company\n\nDRIVE THE TRUCK. BUILD THE COMPANY. CREATE THE EMPIRE.";var rt=text.rectTransform;rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(24,-24);rt.sizeDelta=new Vector2(760,300);DeliveryManager.Instance?.AcceptStarterContract();}
 }
}
