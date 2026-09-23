using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UltimateTruckEmpire.UI
{
    /// <summary>
    /// Runtime-built Android/iOS driving controls. No scene authoring is required.
    /// The existing keyboard controls remain untouched for desktop/editor testing.
    /// </summary>
    public sealed class MobileDrivingControls : MonoBehaviour
    {
        [SerializeField] private bool forceShowInEditor;
        [SerializeField] private bool showUtilityControls = true;
        [SerializeField] private float buttonSize = 118f;

        private Font font;

        private void Start()
        {
            if (!Application.isMobilePlatform && !forceShowInEditor) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            Build();
        }

        private void Build()
        {
            GameObject canvasGo = new GameObject("Mobile Driving Controls Canvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Steering cluster
            CreateButton(canvasGo.transform, "LEFT", MobileControlButton.Control.SteerLeft,
                new Vector2(0f, 0f), new Vector2(24f, 28f), new Vector2(buttonSize, buttonSize));
            CreateButton(canvasGo.transform, "RIGHT", MobileControlButton.Control.SteerRight,
                new Vector2(0f, 0f), new Vector2(24f + buttonSize + 12f, 28f), new Vector2(buttonSize, buttonSize));

            // Pedals
            CreateButton(canvasGo.transform, "BRAKE", MobileControlButton.Control.Brake,
                new Vector2(1f, 0f), new Vector2(-24f - buttonSize, 28f), new Vector2(buttonSize, buttonSize));
            CreateButton(canvasGo.transform, "THROTTLE", MobileControlButton.Control.Throttle,
                new Vector2(1f, 0f), new Vector2(-36f - buttonSize * 2f, 28f), new Vector2(buttonSize, buttonSize));

            if (!showUtilityControls) return;

            // Compact utility row, deliberately above the pedals so it doesn't interfere with steering.
            float y = 180f;
            float x = -24f;
            CreateButton(canvasGo.transform, "ENGINE", MobileControlButton.Control.Engine,
                new Vector2(1f, 0f), new Vector2(x - 5f * 74f, y), new Vector2(64f, 56f));
            CreateButton(canvasGo.transform, "GEAR", MobileControlButton.Control.Gear,
                new Vector2(1f, 0f), new Vector2(x - 4f * 74f, y), new Vector2(64f, 56f));
            CreateButton(canvasGo.transform, "LIGHTS", MobileControlButton.Control.Headlights,
                new Vector2(1f, 0f), new Vector2(x - 3f * 74f, y), new Vector2(64f, 56f));
            CreateButton(canvasGo.transform, "L", MobileControlButton.Control.LeftIndicator,
                new Vector2(1f, 0f), new Vector2(x - 2f * 74f, y), new Vector2(64f, 56f));
            CreateButton(canvasGo.transform, "R", MobileControlButton.Control.RightIndicator,
                new Vector2(1f, 0f), new Vector2(x - 1f * 74f, y), new Vector2(64f, 56f));
            CreateButton(canvasGo.transform, "HAZ", MobileControlButton.Control.Hazards,
                new Vector2(1f, 0f), new Vector2(x, y), new Vector2(64f, 56f));
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("Mobile EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void CreateButton(Transform parent, string label, MobileControlButton.Control control,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject("Mobile " + label);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = control == MobileControlButton.Control.Throttle
                ? new Color(0.18f, 0.42f, 0.25f, 0.82f)
                : control == MobileControlButton.Control.Brake
                    ? new Color(0.48f, 0.20f, 0.18f, 0.82f)
                    : new Color(0.10f, 0.13f, 0.17f, 0.78f);
            image.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            MobileControlButton touch = go.AddComponent<MobileControlButton>();
            touch.Configure(control);

            RectTransform rt = image.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = size.x >= 100f ? 20 : 13;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }
    }
}