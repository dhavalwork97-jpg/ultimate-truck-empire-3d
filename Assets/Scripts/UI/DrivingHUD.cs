using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.UI
{
    public sealed class DrivingHUD : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.045f, 0.055f, 0.07f, 0.84f);
        private static readonly Color AccentColor = new Color(0.98f, 0.62f, 0.12f);
        private static readonly Color DimText = new Color(0.62f, 0.66f, 0.72f);
        private static readonly Color OnText = new Color(0.55f, 0.95f, 0.65f);
        private static readonly Color OffText = new Color(0.30f, 0.33f, 0.38f);

        private TruckController truck;
        private TruckLights lights;
        private Text speedValue, gearValue, statusLine, jobPanel, hint;
        private RectTransform fuelFill;
        private Text leftArrow, rightArrow;
        private Button starterButton;
        private Transform pickupPoint, destinationPoint;
        private float nextJobRefresh;
        private Font font;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(); FindTruck(); FindDeliveryPoints(); RefreshJob();
        }

        public void Bind(TruckController controller, TruckLights truckLights) { truck = controller; lights = truckLights; }

        private void FindTruck()
        {
            if (truck == null) truck = FindFirstObjectByType<TruckController>();
            if (lights == null && truck != null) lights = truck.GetComponent<TruckLights>();
        }

        private void FindDeliveryPoints()
        {
            DeliveryTrigger[] triggers = FindObjectsByType<DeliveryTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null) continue;
                if (triggers[i].Type == DeliveryTrigger.TriggerType.Pickup) pickupPoint = triggers[i].transform;
                else destinationPoint = triggers[i].transform;
            }
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject("Driving HUD Canvas"); canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            BuildSpeedPanel(canvasGo.transform); BuildJobPanel(canvasGo.transform); BuildIndicators(canvasGo.transform); BuildHint(canvasGo.transform);
        }

        private void BuildSpeedPanel(Transform parent)
        {
            RectTransform panel = MakePanel(parent, "Speed Panel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(430f, 150f));
            speedValue = MakeText(panel, "Speed", "0", 62, TextAnchor.LowerLeft, Color.white); Place(speedValue.rectTransform, Vector2.zero, Vector2.zero, new Vector2(22f, 52f), new Vector2(210f, 72f));
            Text kph = MakeText(panel, "Kph", "KPH", 18, TextAnchor.LowerLeft, DimText); Place(kph.rectTransform, Vector2.zero, Vector2.zero, new Vector2(180f, 60f), new Vector2(70f, 24f));
            RectTransform gearBox = MakeBox(panel, "Gear Box", new Color(0.10f, 0.12f, 0.16f, 0.95f)); Place(gearBox, Vector2.one, Vector2.one, new Vector2(-92f, 52f), new Vector2(88f, 76f));
            gearValue = MakeText(gearBox, "Gear", "D", 44, TextAnchor.MiddleCenter, AccentColor); Stretch(gearValue.rectTransform);
            statusLine = MakeText(panel, "Status", "", 17, TextAnchor.LowerLeft, DimText); Place(statusLine.rectTransform, new Vector2(0f, 0f), Vector2.one, new Vector2(22f, 28f), new Vector2(-22f, 22f), true);
            RectTransform track = MakeBox(panel, "Fuel Track", new Color(0.14f, 0.16f, 0.19f, 0.95f)); Place(track, new Vector2(0f, 0f), Vector2.one, new Vector2(22f, 14f), new Vector2(-22f, 8f), true);
            GameObject fill = new GameObject("Fuel Fill"); fill.transform.SetParent(track, false); Image fillImage = fill.AddComponent<Image>(); fillImage.color = new Color(0.30f, 0.78f, 0.45f, 0.95f); fuelFill = fillImage.rectTransform; fuelFill.anchorMin = Vector2.zero; fuelFill.anchorMax = Vector2.one; fuelFill.offsetMin = Vector2.zero; fuelFill.offsetMax = Vector2.zero;
        }

        private void BuildJobPanel(Transform parent)
        {
            RectTransform panel = MakePanel(parent, "Job Panel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-448f, 28f), new Vector2(420f, 190f));
            Text header = MakeText(panel, "Job Header", "CURRENT JOB", 16, TextAnchor.UpperLeft, AccentColor); Place(header.rectTransform, Vector2.up, Vector2.one, new Vector2(18f, -26f), new Vector2(-18f, 20f), true);
            jobPanel = MakeText(panel, "Job Text", "", 17, TextAnchor.UpperLeft, Color.white); Place(jobPanel.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 14f), new Vector2(-18f, -34f), true);
            starterButton = MakeButton(panel, "ACCEPT STARTER CONTRACT", AcceptStarterContract);
            Place(starterButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 8f), new Vector2(-18f, 46f), true);
        }

        private void BuildIndicators(Transform parent)
        {
            leftArrow = MakeText(parent, "Left Indicator", "\u25C0", 34, TextAnchor.MiddleCenter, OffText); Place(leftArrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-64f, -46f), new Vector2(54f, 54f));
            rightArrow = MakeText(parent, "Right Indicator", "\u25B6", 34, TextAnchor.MiddleCenter, OffText); Place(rightArrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(64f, -46f), new Vector2(54f, 54f));
        }

        private void BuildHint(Transform parent)
        {
            hint = MakeText(parent, "Controls Hint", "WASD drive   SPACE brake   G gear   V cruise   I engine   L lights   Q/R indicators   Z hazards   H horn   C camera\nF1-F6 management   F8 actions   F10 dispatch   F11 dealership   F7 weather   TAB hide panels", 15, TextAnchor.UpperLeft, new Color(0.72f, 0.76f, 0.82f, 0.85f));
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(980f, 48f));
        }

        private void Update()
        {
            if (truck == null) { FindTruck(); if (truck == null) return; }
            if (speedValue != null) speedValue.text = Mathf.RoundToInt(truck.SpeedKph).ToString();
            if (gearValue != null) { gearValue.text = truck.GearLabel; gearValue.color = truck.Gear == GearState.Drive ? AccentColor : truck.Gear == GearState.Reverse ? new Color(0.95f, 0.45f, 0.35f) : DimText; }
            if (fuelFill != null) { float fraction = Mathf.Clamp01(truck.Fuel / 100f); fuelFill.anchorMax = new Vector2(fraction, 1f); Image image = fuelFill.GetComponent<Image>(); if (image != null) image.color = fraction < 0.15f ? new Color(0.9f, 0.35f, 0.3f, 0.95f) : new Color(0.30f, 0.78f, 0.45f, 0.95f); }
            if (statusLine != null) statusLine.text = BuildStatusLine(); UpdateIndicatorArrows();
            if (Time.unscaledTime >= nextJobRefresh) { nextJobRefresh = Time.unscaledTime + 0.25f; RefreshJob(); }
        }

        private string BuildStatusLine()
        {
            StringBuilder s = new StringBuilder(96); Append(s, "ENG", truck.EngineRunning); Append(s, "BRK", truck.Braking); Append(s, "LGT", lights != null && lights.HeadlightsOn); Append(s, "CRZ", truck.CruiseActive); Append(s, "LIM", truck.LimiterActive); Append(s, "HZD", lights != null && lights.HazardsOn); return s.ToString();
        }
        private static void Append(StringBuilder s, string label, bool on) { if (s.Length > 0) s.Append("   "); s.Append(on ? label : label.ToLowerInvariant()); }
        private void UpdateIndicatorArrows() { bool left = lights != null && lights.LeftIndicatorOn && lights.BlinkPhase; bool right = lights != null && lights.RightIndicatorOn && lights.BlinkPhase; if (leftArrow != null) leftArrow.color = left ? AccentColor : OffText; if (rightArrow != null) rightArrow.color = right ? AccentColor : OffText; }

        private void AcceptStarterContract()
        {
            DeliveryManager.Instance?.AcceptStarterContract();
            RefreshJob();
        }

        private void RefreshJob()
        {
            if (jobPanel == null) return;
            DeliveryManager delivery = DeliveryManager.Instance;
            bool active = delivery != null && delivery.ContractAccepted;
            if (starterButton != null) starterButton.gameObject.SetActive(!active);
            if (!active) { jobPanel.text = "No active contract.\nAccept the starter contract below."; return; }
            bool loaded = delivery.CargoLoaded; Transform targetPoint = loaded ? destinationPoint : pickupPoint; string targetName = loaded ? delivery.Destination : delivery.Pickup;
            StringBuilder s = new StringBuilder(220); s.Append("Contract   ").AppendLine(delivery.ContractId); s.Append("Cargo      ").AppendLine(delivery.CargoName); s.Append("Route      ").Append(delivery.Pickup).Append("  →  ").AppendLine(delivery.Destination); s.Append("Status     ").AppendLine(loaded ? "LOADED - deliver to destination" : "EMPTY - drive to pickup"); s.Append("Next stop  ").AppendLine(targetName);
            if (targetPoint != null && truck != null) { Vector3 a = truck.transform.position; Vector3 b = targetPoint.position; a.y = 0f; b.y = 0f; s.Append("Distance   ").Append(Vector3.Distance(a, b).ToString("0")).AppendLine(" m"); }
            s.Append("Reward     ₹").Append(delivery.Reward.ToString("0")).Append("  |  XP ").Append(delivery.RewardXp).Append("  |  Diff ").Append(delivery.ContractDifficulty); jobPanel.text = s.ToString();
        }

        private static RectTransform MakeBox(Transform parent, string boxName, Color color) { GameObject go = new GameObject(boxName); go.transform.SetParent(parent, false); Image image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image.rectTransform; }
        private Button MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label); go.transform.SetParent(parent, false); Image image = go.AddComponent<Image>(); image.color = new Color(0.12f, 0.14f, 0.18f, 0.98f); Button button = go.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            Text text = new GameObject("Text").AddComponent<Text>(); text.transform.SetParent(go.transform, false); text.font = font; text.fontSize = 15; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.text = label; text.raycastTarget = false; Stretch(text.rectTransform); return button;
        }
        private RectTransform MakePanel(Transform parent, string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size) { RectTransform rt = MakeBox(parent, panelName, PanelColor); rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(anchorMin.x, anchorMin.y); rt.anchoredPosition = offset; rt.sizeDelta = size; return rt; }
        private Text MakeText(Transform parent, string textName, string value, int size, TextAnchor anchor, Color color) { GameObject go = new GameObject(textName); go.transform.SetParent(parent, false); Text text = go.AddComponent<Text>(); text.font = font; text.fontSize = size; text.alignment = anchor; text.color = color; text.text = value; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow; return text; }
        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 a, Vector2 b, bool stretchMode = false) { rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; if (stretchMode) { rt.offsetMin = a; rt.offsetMax = b; return; } rt.pivot = new Vector2(anchorMin.x, anchorMin.y); rt.anchoredPosition = a; rt.sizeDelta = b; }
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    }
}
