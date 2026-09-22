using UnityEngine;
using UnityEngine.UI;

namespace UltimateTruckEmpire.UI
{
    /// Lightweight runtime main-menu/credits overlay. Credits are kept in-game so
    /// CC BY 4.0 asset attribution is visible without relying only on external docs.
    public sealed class MainMenuUI : MonoBehaviour
    {
        private GameObject root;
        private GameObject creditsPanel;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            root = new GameObject("Main Menu Canvas");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();

            GameObject panel = MakePanel(root.transform, new Color(.02f, .025f, .035f, .96f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(.12f,.10f), new Vector2(.88f,.90f));

            Text title = AddText(panel.transform, "ULTIMATE TRUCK EMPIRE", 42, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(.08f,.70f), new Vector2(.92f,.88f));

            Text subtitle = AddText(panel.transform, "BUILD • DRIVE • DELIVER", 16, TextAnchor.MiddleCenter);
            subtitle.color = new Color(.70f,.75f,.82f);
            SetRect(subtitle.rectTransform, new Vector2(.08f,.62f), new Vector2(.92f,.70f));

            AddButton(panel.transform, "START DRIVING", () => root.SetActive(false), new Vector2(.32f,.43f), new Vector2(.68f,.54f));
            AddButton(panel.transform, "CREDITS", ShowCredits, new Vector2(.32f,.29f), new Vector2(.68f,.40f));

            creditsPanel = MakePanel(root.transform, new Color(.018f,.022f,.03f,.985f));
            SetRect(creditsPanel.GetComponent<RectTransform>(), new Vector2(.16f,.12f), new Vector2(.84f,.88f));

            Text creditsTitle = AddText(creditsPanel.transform, "CREDITS", 34, TextAnchor.UpperCenter);
            SetRect(creditsTitle.rectTransform, new Vector2(.06f,.84f), new Vector2(.94f,.95f));

            Text credits = AddText(creditsPanel.transform,
                "ASSET ATTRIBUTION\n\n" +
                "Meshy-generated trailer and cargo assets\n" +
                "Model created with Meshy – CC BY 4.0 License\n\n" +
                "Covered Cargo Trailer • Tarp Covered Flatbed\n" +
                "Timber Hauler • Tanker candidates\n" +
                "Corrugated Steel Ware cargo\n\n" +
                "Meshy-generated output supplied by the project owner.\n" +
                "Attribution applies to the generated assets; source/reference\n" +
                "material rights remain the creator's responsibility.\n\n" +
                "Ultimate Truck Empire — Trailer System",
                18, TextAnchor.UpperCenter);
            credits.color = new Color(.88f,.90f,.94f);
            SetRect(credits.rectTransform, new Vector2(.08f,.25f), new Vector2(.92f,.80f));

            AddButton(creditsPanel.transform, "BACK", HideCredits, new Vector2(.35f,.08f), new Vector2(.65f,.18f));
            creditsPanel.SetActive(false);
        }

        private void ShowCredits()
        {
            creditsPanel.SetActive(true);
        }

        private void HideCredits()
        {
            creditsPanel.SetActive(false);
        }

        private static GameObject MakePanel(Transform parent, Color color)
        {
            GameObject go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action,
            Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(label);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(.12f,.15f,.20f,.98f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text text = AddText(go.transform, label, 16, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            SetRect(go.GetComponent<RectTransform>(), min, max);
            return button;
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
