using UnityEngine;
using UnityEngine.UI;

namespace UltimateTruckEmpire
{
    public sealed class MobileHUD : MonoBehaviour
    {
        private Text speedText;
        private Text cashText;
        private MobileInput input;
        private TycoonStateService tycoon;

        private void Awake()
        {
            input = FindFirstObjectByType<MobileInput>();
            tycoon = FindFirstObjectByType<TycoonStateService>();
            Build();
        }

        private void Update()
        {
            var truck = FindFirstObjectByType<TruckController>();
            if (truck != null && speedText != null)
                speedText.text = $"{Mathf.RoundToInt(truck.SpeedKph)} KM/H";

            if (tycoon != null && cashText != null)
                cashText.text = $"$ {tycoon.State.cash:N0}";
        }

        private void Build()
        {
            var canvasObject = new GameObject("MobileHUDCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            speedText = CreateText(canvasObject.transform, "0 KM/H", 42, new Vector2(0.5f, 0.90f));
            cashText = CreateText(canvasObject.transform, "$ 25,000", 28, new Vector2(0.12f, 0.92f));

            if (input != null)
            {
                CreateButton(canvasObject.transform, "◀", new Vector2(0.12f, 0.18f), MobileDriveButton.ActionType.Left);
                CreateButton(canvasObject.transform, "▶", new Vector2(0.28f, 0.18f), MobileDriveButton.ActionType.Right);
                CreateButton(canvasObject.transform, "THROTTLE", new Vector2(0.83f, 0.22f), MobileDriveButton.ActionType.Throttle);
                CreateButton(canvasObject.transform, "BRAKE", new Vector2(0.83f, 0.09f), MobileDriveButton.ActionType.Brake);
            }
        }

        private Text CreateText(Transform parent, string value, int size, Vector2 anchor)
        {
            var obj = new GameObject("HUDText");
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(300f, 70f);
            return text;
        }

        private void CreateButton(Transform parent, string label, Vector2 anchor, MobileDriveButton.ActionType action)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.78f);
            var button = obj.AddComponent<MobileDriveButton>();
            var inputField = typeof(MobileDriveButton).GetField("input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var actionField = typeof(MobileDriveButton).GetField("action", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            inputField?.SetValue(button, input);
            actionField?.SetValue(button, action);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(150f, 100f);
        }
    }
}
