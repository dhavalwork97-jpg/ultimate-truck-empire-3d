using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.UI
{
    /// Runtime garage screen. It intentionally uses the existing UGUI package so
    /// the game remains scene-light and can be wired to prefabs later.
    public sealed class GarageUI : MonoBehaviour
    {
        private GameObject root;
        private Text details;
        private int selected;
        private float nextRefresh;

        private void Start()
        {
            Build();
            Refresh();
            root.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
                root.SetActive(!root.activeSelf);

            if (root.activeSelf && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.35f;
                Refresh();
            }

            if (!root.activeSelf) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow)) Select(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) Select(1);
            if (Input.GetKeyDown(KeyCode.F8)) Repair();
            if (Input.GetKeyDown(KeyCode.F9)) Refuel();
        }

        private void Build()
        {
            root = new GameObject("Garage Canvas");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("Garage Panel");
            panel.transform.SetParent(root.transform, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(.025f, .03f, .04f, .97f);

            RectTransform panelRt = bg.rectTransform;
            panelRt.anchorMin = new Vector2(.06f, .08f);
            panelRt.anchorMax = new Vector2(.94f, .92f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            Text title = AddText(panel.transform, "TRUCK GARAGE", 26, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(.04f,.86f), new Vector2(.96f,.97f));

            Text help = AddText(panel.transform,
                "F7 close   ←/→ select   F8 repair   F9 refuel   Engine/Fuel/Reliability upgrades below",
                14, TextAnchor.UpperLeft);
            help.color = new Color(.65f,.69f,.76f);
            SetRect(help.rectTransform, new Vector2(.04f,.80f), new Vector2(.96f,.86f));

            GameObject list = new GameObject("Fleet List");
            list.transform.SetParent(panel.transform, false);
            VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            RectTransform listRt = list.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(.04f,.16f);
            listRt.anchorMax = new Vector2(.42f,.78f);
            listRt.offsetMin = listRt.offsetMax = Vector2.zero;

            // Buttons are rebuilt only once; fleet capacity is small at the early
            // game stage and the selected index is kept valid by Refresh().
            var fleet = FleetManager.Instance;
            if (fleet != null)
            {
                for (int i = 0; i < fleet.Trucks.Count; i++)
                {
                    int index = i;
                    AddButton(list.transform, "TRUCK " + (i + 1), () => { selected = index; Refresh(); });
                }
            }

            details = AddText(panel.transform, "", 17, TextAnchor.UpperLeft);
            SetRect(details.rectTransform, new Vector2(.47f,.28f), new Vector2(.96f,.78f));

            AddButton(panel.transform, "REPAIR", Repair).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.47f,.17f), new Vector2(.59f,.25f));
            AddButton(panel.transform, "REFUEL", Refuel).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.60f,.17f), new Vector2(.72f,.25f));
            AddButton(panel.transform, "ENGINE +", UpgradeEngine).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.73f,.17f), new Vector2(.82f,.25f));
            AddButton(panel.transform, "FUEL TANK +", UpgradeFuel).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.83f,.17f), new Vector2(.96f,.25f));
            AddButton(panel.transform, "RELIABILITY +", UpgradeReliability).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.47f,.07f), new Vector2(.62f,.15f));
            AddButton(panel.transform, "SET AS PLAYER TRUCK", SetActive).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.63f,.07f), new Vector2(.82f,.15f));
            AddButton(panel.transform, "CLOSE", () => root.SetActive(false)).GetComponent<RectTransform>().SetAnchors(
                new Vector2(.83f,.07f), new Vector2(.96f,.15f));
        }

        private void Refresh()
        {
            FleetManager fleet = FleetManager.Instance;
            if (details == null || fleet == null)
                return;

            if (fleet.Trucks.Count == 0)
            {
                selected = 0;
                details.text = "No trucks owned.\n\nBuy your first truck from the dealership.";
                return;
            }

            selected = Mathf.Clamp(selected, 0, fleet.Trucks.Count - 1);
            FleetTruckData truck = fleet.Trucks[selected];

            StringBuilder s = new StringBuilder(500);
            s.AppendLine(truck.model);
            s.AppendLine("────────────────────────");
            s.AppendLine("Fleet ID       " + truck.id);
            s.AppendLine("Capacity       " + truck.capacityTons.ToString("0.0") + " tons");
            s.AppendLine("Engine         " + truck.enginePower.ToString("0") + " HP");
            s.AppendLine("Top speed      " + truck.maxSpeedKph.ToString("0") + " km/h");
            s.AppendLine("Fuel           " + truck.fuel.ToString("0") + " / " + truck.fuelCapacity.ToString("0") + " L");
            s.AppendLine("Efficiency     " + truck.fuelEfficiency.ToString("0.0") + " km/L");
            s.AppendLine("Condition      " + truck.condition.ToString("0") + "%");
            s.AppendLine("Reliability    " + truck.reliability.ToString("0") + "%");
            s.AppendLine("Status         " + (truck.available ? "AVAILABLE" : "ON DELIVERY"));
            s.AppendLine("Player truck   " + (fleet.ActiveTruck == truck ? "ACTIVE" : "NOT ACTIVE"));
            s.AppendLine();
            s.AppendLine("Upgrades");
            s.AppendLine("Engine         Lv " + truck.engineUpgradeLevel + " / 5");
            s.AppendLine("Fuel tank      Lv " + truck.fuelUpgradeLevel + " / 3");
            s.AppendLine("Reliability    Lv " + truck.reliabilityUpgradeLevel + " / 3");
            s.AppendLine();
            s.AppendLine("Repair cost    ₹" + fleet.GetRepairCostPerConditionPoint(truck).ToString("0") + " / condition point");
            s.AppendLine("Fuel price     ₹" + fleet.GetFuelPricePerLitre().ToString("0") + " / litre");
            details.text = s.ToString();
        }

        private FleetTruckData SelectedTruck()
        {
            FleetManager fleet = FleetManager.Instance;
            if (fleet == null || fleet.Trucks.Count == 0) return null;
            selected = Mathf.Clamp(selected, 0, fleet.Trucks.Count - 1);
            return fleet.Trucks[selected];
        }

        private void Select(int delta)
        {
            FleetManager fleet = FleetManager.Instance;
            if (fleet == null || fleet.Trucks.Count == 0) return;
            selected = (selected + delta + fleet.Trucks.Count) % fleet.Trucks.Count;
            Refresh();
        }

        private void Repair() { var t = SelectedTruck(); if (t != null) FleetManager.Instance.Repair(t.id); Refresh(); }
        private void Refuel() { var t = SelectedTruck(); if (t != null) FleetManager.Instance.Refuel(t.id, t.fuelCapacity); Refresh(); }
        private void UpgradeEngine() { var t = SelectedTruck(); if (t != null) FleetManager.Instance.UpgradeEngine(t.id); Refresh(); }
        private void UpgradeFuel() { var t = SelectedTruck(); if (t != null) FleetManager.Instance.UpgradeFuelTank(t.id); Refresh(); }
        private void UpgradeReliability() { var t = SelectedTruck(); if (t != null) FleetManager.Instance.UpgradeReliability(t.id); Refresh(); }
        private void SetActive()
        {
            var t = SelectedTruck();
            if (t != null) FleetManager.Instance.SetActiveTruck(t.id);
            Refresh();
        }

        private static Text AddText(Transform parent, string value, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(.12f,.15f,.20f,.98f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text text = AddText(go.transform, label, 14, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            LayoutElement element = go.AddComponent<LayoutElement>();
            element.minHeight = 40f;
            return button;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    internal static class GarageRectExtensions
    {
        public static void SetAnchors(this RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
