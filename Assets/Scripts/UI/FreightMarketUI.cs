using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Freight;

namespace UltimateTruckEmpire.UI
{
    /// <summary>
    /// Player-facing freight board. It exposes the generated FreightMarketService
    /// offers through touch-friendly buttons and keeps the driving HUD unobstructed
    /// until the player opens the board.
    /// </summary>
    public sealed class FreightMarketUI : MonoBehaviour
    {
        private GameObject root;
        private Transform offersRoot;
        private Text details;
        private Text active;
        private Button openButton;
        private Button acceptButton;
        private Font font;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            Refresh();
            SetVisible(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                SetVisible(root == null || !root.activeSelf);

            if (root != null && root.activeSelf)
                Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
            if (openButton != null) openButton.gameObject.SetActive(!visible);
            if (visible) Refresh();
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            openButton = AddButton(transform, "FREIGHT MARKET", () => SetVisible(true));
            RectTransform openRect = openButton.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(1f, 0.5f);
            openRect.anchorMax = new Vector2(1f, 0.5f);
            openRect.pivot = new Vector2(1f, 0.5f);
            openRect.anchoredPosition = new Vector2(-24f, 0f);
            openRect.sizeDelta = new Vector2(150f, 64f);

            root = new GameObject("Freight Market Panel");
            root.transform.SetParent(transform, false);
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.025f, 0.03f, 0.04f, 0.98f);
            RectTransform panel = background.rectTransform;
            panel.anchorMin = new Vector2(0.04f, 0.06f);
            panel.anchorMax = new Vector2(0.96f, 0.94f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            Text title = AddText(root.transform, "FREIGHT MARKET", 26, TextAnchor.MiddleLeft);
            Place(title.rectTransform, new Vector2(.04f, .92f), new Vector2(.96f, .99f));

            Button close = AddButton(root.transform, "CLOSE", () => SetVisible(false));
            Place(close.GetComponent<RectTransform>(), new Vector2(.86f, .92f), new Vector2(.96f, .99f));

            Button refresh = AddButton(root.transform, "REFRESH OFFERS", RefreshOffers);
            acceptButton = AddButton(root.transform, "ACCEPT SELECTED FREIGHT", AcceptSelected);
            Place(acceptButton.GetComponent<RectTransform>(), new Vector2(.28f, .08f), new Vector2(.52f, .14f));
            acceptButton.gameObject.SetActive(false);
            Place(refresh.GetComponent<RectTransform>(), new Vector2(.04f, .84f), new Vector2(.25f, .90f));

            details = AddText(root.transform, "Select a freight offer to inspect it.", 15, TextAnchor.UpperLeft);
            Place(details.rectTransform, new Vector2(.28f, .76f), new Vector2(.96f, .90f));

            active = AddText(root.transform, "ACTIVE JOB: none", 16, TextAnchor.UpperLeft);
            Place(active.rectTransform, new Vector2(.04f, .75f), new Vector2(.96f, .83f));

            GameObject scrollArea = new GameObject("Offer List");
            scrollArea.transform.SetParent(root.transform, false);
            RectTransform listRect = scrollArea.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(.04f, .08f);
            listRect.anchorMax = new Vector2(.96f, .73f);
            listRect.offsetMin = listRect.offsetMax = Vector2.zero;
            ScrollRect scroll = scrollArea.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollArea.transform, false);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.05f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform viewportRect = viewportImage.rectTransform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            scroll.viewport = viewportRect;

            GameObject content = new GameObject("Offers");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 8);
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            offersRoot = content.transform;
            scroll.content = contentRect;
        }

        private void RefreshOffers()
        {
            FreightMarketService market = FreightMarketService.Instance;
            if (market == null) return;
            market.Refresh("Ahmedabad");
            Refresh();
        }

        private void Refresh()
        {
            FreightMarketService market = FreightMarketService.Instance;
            if (market == null) return;

            if (active != null)
            {
                FreightJob job = market.ActiveJob;
                active.text = job == null
                    ? "ACTIVE JOB: none"
                    : $"ACTIVE JOB: {job.id} | {job.originCity} → {job.destinationCity} | " +
                      $"{(job.cargoLoaded ? "CARGO LOADED" : "GO TO PICKUP")} | ₹{job.reward:0}";
            }

            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (offersRoot == null) return;
            for (int i = offersRoot.childCount - 1; i >= 0; i--)
                Destroy(offersRoot.GetChild(i).gameObject);

            if (market.Offers.Count == 0)
            {
                AddText(offersRoot, "No offers. Tap REFRESH OFFERS.", 16, TextAnchor.MiddleLeft);
                return;
            }

            for (int i = 0; i < market.Offers.Count; i++)
            {
                FreightJob job = market.Offers[i];
                FreightJob captured = job;
                Button button = AddButton(offersRoot, FormatJob(job), () => Select(captured));
                button.GetComponent<LayoutElement>().minHeight = 82f;
            }
        }

        private void Select(FreightJob job)
        {
            if (details == null || job == null) return;
            bool canAccept = FreightMarketService.Instance != null && FreightMarketService.Instance.ActiveJob == null;
            StringBuilder s = new StringBuilder(240);
            s.Append(job.cargoId.Replace("_", " ")).AppendLine();
            s.Append(job.originCity).Append(" → ").Append(job.destinationCity).AppendLine();
            s.Append($"Distance {job.distanceKm:0} km | {job.weightTons:0.0} t | {job.trailerClass}").AppendLine();
            s.Append($"Reward ₹{job.reward:0} | XP {job.xp} | Deadline {job.deadlineHours:0.0} h").AppendLine();
            if (canAccept)
            {
                Button accept = AddButton(root.transform, "ACCEPT SELECTED FREIGHT", () =>
                {
                    if (FreightMarketService.Instance != null && FreightMarketService.Instance.Accept(job.id))
                        Refresh();
                });
                Place(accept.GetComponent<RectTransform>(), new Vector2(.28f, .08f), new Vector2(.52f, .14f));
            }
            details.text = s.ToString();
        }

        private void AcceptSelected() { }

        private void AcceptJob(string jobId)
        {
            if (FreightMarketService.Instance != null && FreightMarketService.Instance.Accept(jobId))
                Refresh();
        }

        private static string FormatJob(FreightJob job)
        {
            return $"{job.cargoId.Replace("_", " ")}  |  {job.originCity} → {job.destinationCity}" +
                   $"\n{job.distanceKm:0} km  |  {job.weightTons:0.0} t  |  {job.trailerClass}  |  ₹{job.reward:0}";
        }

        private Text AddText(Transform parent, string value, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<LayoutElement>().minHeight = size + 12;
            return text;
        }

        private Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(.11f, .14f, .18f, 1f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text text = AddText(go.transform, label, 14, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(12f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);
            text.raycastTarget = false;
            go.AddComponent<LayoutElement>().minHeight = 46f;
            return button;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
