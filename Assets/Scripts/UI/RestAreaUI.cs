using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Gameplay.RestArea;

namespace UltimateTruckEmpire.UI
{
    public sealed class RestAreaUI : MonoBehaviour
    {
        private GameObject root;
        private Text status;
        private Button restButton;
        private float nextRefresh;

        private void Start() { Build(); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E) && RestAreaManager.Instance != null && RestAreaManager.Instance.CanInteract)
                RestAreaManager.Instance.BeginRest();

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f;
            Refresh();
        }

        private void Build()
        {
            var canvas = new GameObject("Rest Area Canvas");
            canvas.transform.SetParent(transform, false);
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvas.AddComponent<GraphicRaycaster>();

            root = new GameObject("Rest Area Panel");
            root.transform.SetParent(canvas.transform, false);
            var image = root.AddComponent<Image>();
            image.rectTransform.anchorMin = new Vector2(.30f, .05f);
            image.rectTransform.anchorMax = new Vector2(.70f, .18f);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;

            status = new GameObject("Rest Status").AddComponent<Text>();
            status.transform.SetParent(root.transform, false);
            status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize = 22;
            status.alignment = TextAnchor.MiddleCenter;
            status.color = Color.white;
            status.rectTransform.anchorMin = new Vector2(.03f, .42f);
            status.rectTransform.anchorMax = new Vector2(.97f, .95f);
            status.rectTransform.offsetMin = status.rectTransform.offsetMax = Vector2.zero;

            restButton = new GameObject("Rest Button").AddComponent<Button>();
            restButton.transform.SetParent(root.transform, false);
            restButton.gameObject.AddComponent<Image>();
            restButton.GetComponent<Image>().raycastTarget = true;
            restButton.GetComponent<RectTransform>().anchorMin = new Vector2(.35f, .05f);
            restButton.GetComponent<RectTransform>().anchorMax = new Vector2(.65f, .40f);
            restButton.GetComponent<RectTransform>().offsetMin = restButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            restButton.onClick.AddListener(() => RestAreaManager.Instance?.BeginRest());
            var label = new GameObject("Button Label").AddComponent<Text>();
            label.transform.SetParent(restButton.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "REST";
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            if (root == null || status == null) return;
            var manager = RestAreaManager.Instance;
            if (manager == null) { root.SetActive(false); return; }

            bool visible = manager.IsResting || manager.CurrentArea != null;
            root.SetActive(visible);
            if (!visible) return;

            if (manager.IsResting)
            {
                status.text = string.Format("RESTING  •  {0:0.0}h remaining  •  Fatigue {1:0}%",
                    manager.RemainingHours, manager.CurrentDriver != null ? manager.CurrentDriver.fatigue : 0f);
                restButton.gameObject.SetActive(false);
            }
            else
            {
                var area = manager.CurrentArea;
                status.text = string.Format("{0}  •  Bays {1}  •  ₹{2:0}/h\n{3}",
                    area.DisplayName, area.AvailableBays, area.GetCost(1f), manager.StatusMessage);
                restButton.gameObject.SetActive(manager.CanInteract);
            }
        }
    }
}
