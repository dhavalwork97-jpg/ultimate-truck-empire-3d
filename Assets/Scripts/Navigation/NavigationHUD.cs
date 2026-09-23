using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.Navigation
{
    public sealed class NavigationHUD : MonoBehaviour
    {
        private Text instruction;
        private Text distance;
        private Text arrow;
        private NavigationManager navigation;
        private TruckController truck;

        private void Awake()
        {
            navigation = NavigationManager.Instance;
            if (navigation == null)
            {
                var go = new GameObject("GPS Navigation");
                navigation = go.AddComponent<NavigationManager>();
            }

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("GPS Panel");
            panel.transform.SetParent(transform, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -18f);
            rt.sizeDelta = new Vector2(430f, 108f);

            instruction = MakeText(panel.transform, "Instruction", 28, new Vector2(0f, 26f), new Vector2(390f, 40f));
            distance = MakeText(panel.transform, "Distance", 20, new Vector2(0f, -8f), new Vector2(390f, 30f));
            arrow = MakeText(panel.transform, "Arrow", 38, new Vector2(0f, -37f), new Vector2(390f, 34f));
            instruction.alignment = TextAnchor.MiddleCenter;
            distance.alignment = TextAnchor.MiddleCenter;
            arrow.alignment = TextAnchor.MiddleCenter;
        }

        private Text MakeText(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            return text;
        }

        private void Update()
        {
            if (navigation == null) return;
            if (truck == null) truck = FindFirstObjectByType<TruckController>();

            instruction.text = navigation.Instruction;
            distance.text = navigation.HasRoute
                ? "GPS  •  " + navigation.DistanceRemainingKm.ToString("0.0") + " km"
                : "GPS  •  No active route";

            if (navigation.HasRoute && truck != null)
            {
                Vector3 to = navigation.NextWaypoint - truck.transform.position;
                to.y = 0f;
                float signed = Vector3.SignedAngle(truck.transform.forward, to.normalized, Vector3.up);
                arrow.text = Mathf.Abs(signed) < 18f ? "↑" : signed > 0f ? "↗" : "↖";
            }
            else arrow.text = "—";
        }
    }
}