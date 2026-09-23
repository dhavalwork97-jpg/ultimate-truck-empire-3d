using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.UI
{
    /// <summary>
    /// Touch-first driving controls for Android/iOS.
    /// The existing keyboard path remains authoritative on desktop.
    /// </summary>
    public sealed class MobileDrivingControls : MonoBehaviour
    {
        [SerializeField] private bool showInEditor = false;

        private TruckController truck;
        private TruckLights lights;
        private Canvas canvas;
        private SteeringPad steering;
        private HoldButton throttle;
        private HoldButton brake;

        private void Start()
        {
            if (!Application.isMobilePlatform && !showInEditor)
            {
                enabled = false;
                return;
            }

            FindTruck();
            Build();
        }

        private void Update()
        {
            if (truck == null) FindTruck();
            if (truck == null) return;

            truck.SetMobileDrivingInput(steering != null ? steering.Value : 0f,
                                        throttle != null && throttle.IsPressed ? 1f : 0f,
                                        brake != null && brake.IsPressed);
        }

        private void FindTruck()
        {
            truck = FindFirstObjectByType<TruckController>();
            if (truck != null) lights = truck.GetComponent<TruckLights>();
        }

        private void Build()
        {
            if (canvas != null) return;

            var go = new GameObject("Mobile Driving Controls");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            steering = CreateSteeringPad(go.transform);
            throttle = CreateHoldButton(go.transform, "THROTTLE", new Vector2(1f, 0f), new Vector2(-112f, 168f), new Vector2(120f, 88f), false);
            brake = CreateHoldButton(go.transform, "BRAKE", new Vector2(1f, 0f), new Vector2(-112f, 68f), new Vector2(120f, 88f), true);

            CreateActionButton(go.transform, "GEAR", new Vector2(1f, 1f), new Vector2(-86f, -76f), new Vector2(112f, 62f), () => truck?.MobileCycleGear());
            CreateActionButton(go.transform, "LIGHTS", new Vector2(1f, 1f), new Vector2(-208f, -76f), new Vector2(112f, 62f), () => lights?.SetHeadlights(!(lights != null && lights.HeadlightsOn)));
            CreateActionButton(go.transform, "CRUISE", new Vector2(1f, 1f), new Vector2(-330f, -76f), new Vector2(112f, 62f), () => truck?.MobileToggleCruise());
            CreateActionButton(go.transform, "HORN", new Vector2(0f, 1f), new Vector2(86f, -76f), new Vector2(112f, 62f), null, true);
        }

        private SteeringPad CreateSteeringPad(Transform parent)
        {
            var root = new GameObject("Steering Pad");
            root.transform.SetParent(parent, false);
            var image = root.AddComponent<Image>();
            image.color = new Color(0.03f, 0.05f, 0.08f, 0.72f);
            image.raycastTarget = true;

            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(42f, 58f);
            rt.sizeDelta = new Vector2(280f, 280f);

            var pad = root.AddComponent<SteeringPad>();
            pad.Configure(truck);
            return pad;
        }

        private HoldButton CreateHoldButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, bool warning)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = warning ? new Color(0.48f, 0.16f, 0.12f, 0.86f) : new Color(0.10f, 0.18f, 0.27f, 0.86f);
            var rt = image.rectTransform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = position; rt.sizeDelta = size;

            var text = MakeText(go.transform, label, 18);
            Stretch(text.rectTransform);

            var button = go.AddComponent<HoldButton>();
            button.Configure(truck);
            return button;
        }

        private void CreateActionButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool horn = false)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.06f, 0.10f, 0.15f, 0.84f);
            var rt = image.rectTransform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = position; rt.sizeDelta = size;

            var text = MakeText(go.transform, label, 14);
            Stretch(text.rectTransform);

            var button = go.AddComponent<HoldButton>();
            button.Configure(truck);
            button.ClickAction = action;
            button.IsHorn = horn;
        }

        private static Text MakeText(Transform parent, string value, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }

    public sealed class SteeringPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private TruckController truck;
        private RectTransform rect;
        private float value;

        public float Value => value;

        public void Configure(TruckController controller) => truck = controller;

        private void Awake() => rect = transform as RectTransform;

        public void OnPointerDown(PointerEventData eventData) => UpdateValue(eventData.position);
        public void OnDrag(PointerEventData eventData) => UpdateValue(eventData.position);
        public void OnPointerUp(PointerEventData eventData) => value = 0f;

        private void UpdateValue(Vector2 screen)
        {
            if (rect == null) rect = transform as RectTransform;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out local);
            float half = Mathf.Max(1f, rect.rect.width * 0.5f);
            value = Mathf.Clamp(local.x / half, -1f, 1f);
            if (Mathf.Abs(value) < 0.12f) value = 0f;
        }
    }

    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private TruckController truck;
        public bool IsPressed { get; private set; }
        public bool IsHorn { get; set; }
        public UnityEngine.Events.UnityAction ClickAction { get; set; }

        public void Configure(TruckController controller) => truck = controller;

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            if (IsHorn) truck?.SetMobileHorn(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) { if (IsHorn) Release(); }

        private void Release()
        {
            bool wasPressed = IsPressed;
            IsPressed = false;
            if (IsHorn) truck?.SetMobileHorn(false);
            if (wasPressed && !IsHorn) ClickAction?.Invoke();
        }
    }
}
