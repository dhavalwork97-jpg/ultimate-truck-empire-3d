using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Core;
namespace UltimateTruckEmpire.UI
{
 public sealed class ManagementUI:MonoBehaviour
 {
  private Text panel; private GameObject root; private int tab; private float nextRefresh; private readonly string[] tabs={"FLEET","DRIVERS","CONTRACTS","FINANCES","COMPANY","ACTIVE JOBS"};
  private void Start(){if(ContractMarket.Instance==null)new GameObject("ContractMarket").AddComponent<ContractMarket>();if(AutoDispatcher.Instance==null)new GameObject("AutoDispatcher").AddComponent<AutoDispatcher>();Build();Refresh();SetVisible(false);}
  // The management panel is full screen and opaque, so it starts hidden and the
  // F-keys open it. TAB or ESC closes it again and returns you to the road.
  private void Update()
  {
   if(root!=null&&root.activeSelf&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.5f;Refresh();}
   if(Input.GetKeyDown(KeyCode.F1))Open(0); if(Input.GetKeyDown(KeyCode.F2))Open(1); if(Input.GetKeyDown(KeyCode.F3))Open(2);
   if(Input.GetKeyDown(KeyCode.F4))Open(3); if(Input.GetKeyDown(KeyCode.F5))Open(4); if(Input.GetKeyDown(KeyCode.F6))Open(5);
   if(Input.GetKeyDown(KeyCode.Tab))SetVisible(!IsVisible);
   else if(Input.GetKeyDown(KeyCode.Escape))SetVisible(false);
  }
  private void Open(int index){tab=index;SetVisible(true);Refresh();}
  public void SetVisible(bool visible){if(root!=null)root.SetActive(visible);}
  public bool IsVisible => root!=null&&root.activeSelf;
  private void Build(){var c=new GameObject("Management Canvas");root=c;c.AddComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=c.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920f,1080f);scaler.matchWidthOrHeight=.5f;c.AddComponent<GraphicRaycaster>();var bg=new GameObject("Management Panel");bg.transform.SetParent(c.transform,false);var i=bg.AddComponent<Image>();i.color=new Color(.025f,.03f,.04f,.96f);var r=i.rectTransform;r.anchorMin=new Vector2(.05f,.08f);r.anchorMax=new Vector2(.95f,.92f);r.offsetMin=r.offsetMax=Vector2.zero;panel=new GameObject("Management Text").AddComponent<Text>();panel.transform.SetParent(bg.transform,false);panel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");panel.fontSize=20;panel.alignment=TextAnchor.UpperLeft;panel.color=Color.white;panel.rectTransform.anchorMin=new Vector2(.03f,.03f);panel.rectTransform.anchorMax=new Vector2(.97f,.97f);panel.rectTransform.offsetMin=panel.rectTransform.offsetMax=Vector2.zero;}
  private void Refresh(){if(panel==null)return;var s=new StringBuilder();s.AppendLine("ULTIMATE TRUCK EMPIRE  //  MANAGEMENT");s.AppendLine("F1 Fleet   F2 Drivers   F3 Contracts   F4 Finances   F5 Company   F6 Active Jobs");s.AppendLine();s.AppendLine("────────────────────────────────────────────");s.AppendLine(tabs[tab]);s.AppendLine();switch(tab){case 0:Fleet(s);break;case 1:Drivers(s);break;case 2:Contracts(s);break;case 3:Finances(s);break;case 4:Company(s);break;default:Jobs(s);break;}panel.text=s.ToString();}
  private static void Fleet(StringBuilder s){var fm=FleetManager.Instance;if(fm==null)return;foreach(var t in fm.Trucks)s.AppendLine($"{t.id}  {t.model} | {t.capacityTons:0}t | Fuel {t.fuel:0}/{t.fuelCapacity:0} | Condition {t.condition:0}% | {(t.available?"AVAILABLE":"ON DELIVERY")}");}
  private static void Drivers(StringBuilder s){var dm=DriverManager.Instance;if(dm==null)return;foreach(var d in dm.Drivers)s.AppendLine($"{d.id}  {d.name} | Lv {d.level} | XP {d.experience} | Performance {dm.GetPerformance(d):0} | Salary ₹{d.salary:0} | {(d.available?"AVAILABLE":"DRIVING")}");}
  private static void Contracts(StringBuilder s){var m=ContractMarket.Instance;if(m==null)return;int completed=DeliveryManager.Instance?.CompletedContracts??0;s.AppendLine($"Route Progression: {RouteProgression.GetTierLabel(RouteProgression.CurrentTier(completed))}");s.AppendLine($"Completed Deliveries: {completed}");s.AppendLine();foreach(var c in m.Offers)s.AppendLine($"[{RouteProgression.GetTierLabel(c.routeTier)}] {c.cargo} | {c.pickup} → {c.destination} | {c.weightTons:0.0}t | {c.distanceKm:0} km | ₹{c.reward:0} | Diff {c.difficulty}");}private static void Finances(StringBuilder s){var f=FinanceManager.Instance;var g=GameManager.Instance;if(f==null||g==null)return;s.AppendLine($"Cash ₹{g.Money:0}");s.AppendLine($"Revenue ₹{f.Data.revenue:0}");s.AppendLine($"Operating Expenses ₹{f.TotalOperatingExpenses:0}");s.AppendLine($"Operating Profit ₹{f.OperatingProfit:0}");s.AppendLine($"Capital Investment ₹{f.Data.capitalExpense:0}");s.AppendLine($"Total Cash Outflow ₹{f.TotalExpenses:0}");s.AppendLine($"Debt ₹{f.Data.debt:0}");}
  private static void Company(StringBuilder s){var c=CompanyManager.Instance?.Data;if(c==null)return;s.AppendLine(c.companyName);s.AppendLine($"HQ {c.headquarters} | Level {c.level}");s.AppendLine($"Capacity {c.truckCapacity} trucks / {c.driverCapacity} drivers");s.AppendLine($"Reputation {c.reputation:0.0}/100 | Value ₹{c.companyValue:0}");s.AppendLine($"Branches: {(c.branches.Count==0?"None":string.Join(", ",c.branches))}");}
  private static void Jobs(StringBuilder s){var jobs=AutomatedDeliveryManager.Instance?.ActiveDeliveries;if(jobs==null)return;foreach(var j in jobs)s.AppendLine($"{j.id} | {j.cargo} | {j.origin} → {j.destination} | Remaining {j.remainingKm:0} km | ETA {j.etaHours:0.0} h | ₹{j.reward:0}");if(jobs.Count==0)s.AppendLine("No active automated deliveries.");}
 }
}
