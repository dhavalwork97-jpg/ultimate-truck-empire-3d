using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Save;

namespace UltimateTruckEmpire.UI
{
    public sealed class ManagementActionUI : MonoBehaviour
    {
        private Text status;
        private InputField driverName;
        private AutoDispatcher dispatcher;

        private void Start()
        {
            dispatcher = AutoDispatcher.Instance;
            if (dispatcher == null) dispatcher = new GameObject("AutoDispatcher").AddComponent<AutoDispatcher>();
            Build();
        }

        private void Build()
        {
            var root = new GameObject("Management Actions");
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>(); root.AddComponent<GraphicRaycaster>();
            var holder = new GameObject("Actions"); holder.transform.SetParent(root.transform, false);
            var rt = holder.AddComponent<RectTransform>(); rt.anchorMin = new Vector2(.58f,.12f); rt.anchorMax = new Vector2(.92f,.86f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var layout = holder.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.padding = new RectOffset(12,12,12,12);
            AddButton(holder.transform, "DISPATCH BEST CONTRACTS", Dispatch);
            AddButton(holder.transform, "TOGGLE AUTO DISPATCH", ToggleDispatch);
            driverName = AddInput(holder.transform, "Driver name");
            AddButton(holder.transform, "HIRE DRIVER", Hire);
            AddButton(holder.transform, "SAVE COMPANY", Save);
            AddButton(holder.transform, "LOAD COMPANY", Load);
            status = AddLabel(holder.transform, "Ready.");
        }

        private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false); var b = go.AddComponent<Button>();
            var image = go.AddComponent<Image>(); image.color = new Color(.12f,.14f,.18f,.98f); b.targetGraphic = image; b.onClick.AddListener(action);
            var text = new GameObject("Text").AddComponent<Text>(); text.transform.SetParent(go.transform, false); text.text = label; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tr = text.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            var h = go.AddComponent<LayoutElement>(); h.minHeight = 42; return b;
        }

        private static InputField AddInput(Transform parent, string placeholder)
        {
            var go = new GameObject("Driver Input"); go.transform.SetParent(parent, false); var image = go.AddComponent<Image>(); image.color = new Color(.08f,.09f,.11f,1f); var input = go.AddComponent<InputField>();
            var text = new GameObject("Text").AddComponent<Text>(); text.transform.SetParent(go.transform, false); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.color = Color.white; text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(10,0); text.rectTransform.offsetMax = new Vector2(-10,0); input.textComponent = text;
            var h = go.AddComponent<LayoutElement>(); h.minHeight = 42; return input;
        }

        private static Text AddLabel(Transform parent, string value)
        {
            var go = new GameObject("Status"); go.transform.SetParent(parent, false); var text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.color = Color.white; text.text = value; var h = go.AddComponent<LayoutElement>(); h.minHeight = 70; return text;
        }

        private void Dispatch()
        {
            int count = dispatcher.DispatchAvailable();
            status.text = $"Dispatched {count} contract(s).";
        }

        private void ToggleDispatch()
        {
            dispatcher.SetEnabled(!dispatcher.Enabled);
            status.text = "Auto dispatch: " + (dispatcher.Enabled ? "ON" : "OFF");
        }

        private void Hire()
        {
            var name = string.IsNullOrWhiteSpace(driverName.text) ? "New Driver" : driverName.text;
            var driver = DriverManager.Instance?.HireDriver(name);
            status.text = driver == null ? "Cannot hire: capacity/company/cash condition failed." : $"Hired {driver.name} ({driver.id}).";
        }

        private void Save()
        {
            var go = new GameObject("Save Operation"); go.AddComponent<SaveManager>().Save(); Destroy(go);
            status.text = "Company saved.";
        }

        private void Load()
        {
            var go = new GameObject("Load Operation"); go.AddComponent<SaveManager>().Load(); Destroy(go);
            status.text = "Company loaded.";
        }
    }
}
