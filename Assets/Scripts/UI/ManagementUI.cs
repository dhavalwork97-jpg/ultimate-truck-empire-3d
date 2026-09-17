using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.UI
{
    public sealed class ManagementUI : MonoBehaviour
    {
        private Text panel;
        private int tab;
        private float nextRefresh;
        private readonly string[] tabs = { "FLEET", "DRIVERS", "CONTRACTS", "FINANCES", "COMPANY" };

        private void Start()
        {
            Build();
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + 0.5f; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F1)) { tab = 0; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F2)) { tab = 1; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F3)) { tab = 2; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F4)) { tab = 3; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F5)) { tab = 4; Refresh(); }
        }

        private void Build()
        {
            var canvasGo = new GameObject("Management Canvas");
            var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>(); canvasGo.AddComponent<GraphicRaycaster>();
            var bg = new GameObject("Management Panel"); bg.transform.SetParent(canvasGo.transform, false);
            var image = bg.AddComponent<Image>(); image.color = new Color(0.025f, 0.03f, 0.04f, 0.96f);
            var rect = image.rectTransform; rect.anchorMin = new Vector2(.05f,.08f); rect.anchorMax = new Vector2(.95f,.92f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel = new GameObject("Management Text").AddComponent<Text>(); panel.transform.SetParent(bg.transform, false);
            panel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); panel.fontSize = 20; panel.alignment = TextAnchor.UpperLeft;
            panel.color = Color.white; panel.rectTransform.anchorMin = new Vector2(.03f,.03f); panel.rectTransform.anchorMax = new Vector2(.97f,.97f); panel.rectTransform.offsetMin = panel.rectTransform.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (panel == null) return;
            var s = new StringBuilder();
            s.AppendLine("ULTIMATE TRUCK EMPIRE  //  MANAGEMENT");
            s.AppendLine("F1 Fleet   F2 Drivers   F3 Contracts   F4 Finances   F5 Company");
            s.AppendLine();
            s.AppendLine("────────────────────────────────────────────");
            s.AppendLine(tabs[tab]);
            s.AppendLine();
            switch (tab)
            {
                case 0: Fleet(s); break;
                case 1: Drivers(s); break;
                case 2: Contracts(s); break;
                case 3: Finances(s); break;
                default: Company(s); break;
            }
            panel.text = s.ToString();
        }

        private static void Fleet(StringBuilder s)
        {
            var fm = FleetManager.Instance;
            if (fm == null) return;
            s.AppendLine("TRUCKS");
            foreach (var t in fm.Trucks) s.AppendLine($"{t.id}  {t.model}  | {t.capacityTons:0}t | Fuel {t.fuel:0}/{t.fuelCapacity:0} | Condition {t.condition:0}% | {(t.available ? "AVAILABLE" : "ON DELIVERY")}");
            s.AppendLine(); s.AppendLine("Fleet assignments are Driver → Truck → Contract.");
        }

        private static void Drivers(StringBuilder s)
        {
            var dm = DriverManager.Instance;
            if (dm == null) return;
            foreach (var d in dm.Drivers) s.AppendLine($"{d.id}  {d.name}  | Lv {d.level} | XP {d.experience} | Perf {dm.GetPerformance(d):0} | Salary ₹{d.salary:0} | {(d.available ? "AVAILABLE" : "DRIVING")}");
        }

        private static void Contracts(StringBuilder s)
        {
            var market = ContractMarket.Instance;
            if (market == null) { s.AppendLine("Contract market unavailable."); return; }
            foreach (var c in market.Offers) s.AppendLine($"{c.cargo}  {c.pickup} → {c.destination}  | {c.weightTons:0.0}t | {c.distanceKm:0} km | ₹{c.reward:0} | Diff {c.difficulty}");
            s.AppendLine(); s.AppendLine("Automated dispatch API: AutomatedDeliveryManager.StartDelivery().");
        }

        private static void Finances(StringBuilder s)
        {
            var f = FinanceManager.Instance;
            var g = GameManager.Instance;
            if (f == null || g == null) return;
            s.AppendLine($"Cash             ₹{g.Money:0}");
            s.AppendLine($"Revenue          ₹{f.Data.revenue:0}");
            s.AppendLine($"Fuel expense     ₹{f.Data.fuelExpense:0}");
            s.AppendLine($"Payroll expense  ₹{f.Data.payrollExpense:0}");
            s.AppendLine($"Maintenance      ₹{f.Data.maintenanceExpense:0}");
            s.AppendLine($"Other expenses   ₹{f.Data.otherExpense:0}");
            s.AppendLine($"Debt             ₹{f.Data.debt:0}");
            s.AppendLine($"Net profit       ₹{f.NetProfit:0}");
        }

        private static void Company(StringBuilder s)
        {
            var c = CompanyManager.Instance?.Data;
            if (c == null) return;
            s.AppendLine($"{c.companyName}");
            s.AppendLine($"HQ: {c.headquarters}   Level: {c.level}");
            s.AppendLine($"Fleet capacity: {c.truckCapacity}   Driver capacity: {c.driverCapacity}");
            s.AppendLine($"Reputation: {c.reputation:0.0}/100");
            s.AppendLine($"Company value: ₹{c.companyValue:0}");
            s.AppendLine($"Branches: {(c.branches.Count == 0 ? "None" : string.Join(", ", c.branches))}");
        }
    }
}
