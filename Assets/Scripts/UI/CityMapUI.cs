using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.UI
{
    /// <summary>
    /// Lightweight mobile-friendly city/map overview built from the same MapManager
    /// and route progression data used by the contract market.
    /// </summary>
    public sealed class CityMapUI : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.M;
        private GameObject panel;
        private Text text;
        private bool visible;

        private void Start()
        {
            Build();
            SetVisible(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!visible);
            if (visible) Refresh();
        }

        private void Build()
        {
            var canvasGo = new GameObject("City Map Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            panel = new GameObject("City Map Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.035f, 0.055f, 0.075f, 0.96f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(.08f, .08f);
            rt.anchorMax = new Vector2(.92f, .92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var title = AddText(panel.transform, "GUJARAT FREIGHT MAP", 30);
            title.rectTransform.anchorMin = new Vector2(.04f, .90f);
            title.rectTransform.anchorMax = new Vector2(.96f, .98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            text = AddText(panel.transform, "", 20);
            text.alignment = TextAnchor.UpperLeft;
            text.rectTransform.anchorMin = new Vector2(.05f, .08f);
            text.rectTransform.anchorMax = new Vector2(.95f, .88f);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            var close = new GameObject("Close Map");
            close.transform.SetParent(panel.transform, false);
            var button = close.AddComponent<Button>();
            var closeImage = close.AddComponent<Image>();
            closeImage.color = new Color(.15f,.18f,.22f,.95f);
            button.targetGraphic = closeImage;
            button.onClick.AddListener(() => SetVisible(false));
            var cr = close.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(.86f,.01f);
            cr.anchorMax = new Vector2(.98f,.07f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            var label = AddText(close.transform, "CLOSE", 16);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (text == null) return;
            var map = MapManager.Instance;
            if (map == null) return;

            int completed = DeliveryManager.Instance == null ? 0 : DeliveryManager.Instance.CompletedContracts;
            var sb = new StringBuilder();
            sb.AppendLine("CITY NETWORK");
            sb.AppendLine("────────────────────────────────────────");
            sb.AppendLine($"Completed deliveries: {completed}");
            sb.AppendLine($"Route tier: {RouteProgression.GetTierLabel(RouteProgression.CurrentTier(completed))}");
            sb.AppendLine();

            foreach (var city in map.Cities)
            {
                bool unlocked = city.name.Equals("Ahmedabad", System.StringComparison.OrdinalIgnoreCase)
                    || completed >= UnlockRequirement(city.name);
                string services = (city.hasDepot ? "DEPOT " : "")
                    + (city.hasDealership ? "DEALER " : "")
                    + (city.hasService ? "SERVICE" : "");
                sb.AppendLine($"{(unlocked ? "●" : "○")} {city.name,-14} {(unlocked ? "UNLOCKED" : $"LOCKED — {UnlockRequirement(city.name)} deliveries")}");
                sb.AppendLine($"    Services: {services}");
            }

            sb.AppendLine();
            sb.AppendLine("Map data is shared with the contract/progression systems.");
            text.text = sb.ToString();
        }

        private static int UnlockRequirement(string city)
        {
            switch (city)
            {
                case "Vadodara": return 2;
                case "Surat": return 4;
                case "Rajkot": return 6;
                case "Indore": return 8;
                case "Kandla": return 10;
                case "Jaipur": return 12;
                case "Mumbai": return 14;
                case "Pune": return 16;
                case "Delhi": return 20;
                default: return 999;
            }
        }

        private static Text AddText(Transform parent, string value, int size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.text = value;
            t.raycastTarget = false;
            return t;
        }

        private void SetVisible(bool state)
        {
            visible = state;
            if (panel != null) panel.SetActive(state);
            if (state) Refresh();
        }
    }
}