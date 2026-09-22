using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.UI
{
    public sealed class TrailerDealerUI : MonoBehaviour
    {
        private GameObject root;
        private Text details;
        private int selected;

        private void Start()
        {
            Build();
            Refresh();
            root.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10) && root != null)
                root.SetActive(!root.activeSelf);
            if (!root.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { selected--; Refresh(); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { selected++; Refresh(); }
        }

        private void Build()
        {
            root = new GameObject("Trailer Dealership Canvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Trailer Dealership Panel");
            panel.transform.SetParent(root.transform, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(.02f, .025f, .035f, .98f);
            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(.06f, .08f);
            rt.anchorMax = new Vector2(.94f, .92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var title = AddText(panel.transform, "TRAILER DEALERSHIP & MAINTENANCE", 25, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(.04f,.86f), new Vector2(.96f,.97f));
            var help = AddText(panel.transform, "F10 close   ←/→ select   Buy trailers, assign to the active truck, or repair owned trailers.", 14, TextAnchor.UpperLeft);
            help.color = new Color(.65f,.69f,.76f);
            SetRect(help.rectTransform, new Vector2(.04f,.80f), new Vector2(.96f,.86f));

            var list = new GameObject("Trailer Catalog");
            list.transform.SetParent(panel.transform, false);
            var layout = list.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            var lr = list.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(.04f,.18f);
            lr.anchorMax = new Vector2(.44f,.78f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;

            var dealer = TrailerDealer.Instance;
            if (dealer != null)
            {
                for (int i = 0; i < dealer.Catalog.Count; i++)
                {
                    int index = i;
                    AddButton(list.transform, dealer.Catalog[i].manufacturer + " — " + dealer.Catalog[i].model,
                        () => { selected = index; Refresh(); });
                }
            }

            details = AddText(panel.transform, "", 17, TextAnchor.UpperLeft);
            SetRect(details.rectTransform, new Vector2(.48f,.30f), new Vector2(.96f,.78f));

            AddButton(panel.transform, "BUY SELECTED", Buy).GetComponent<RectTransform>().SetAnchors(new Vector2(.48f,.18f), new Vector2(.63f,.26f));
            AddButton(panel.transform, "ASSIGN TO ACTIVE", Assign).GetComponent<RectTransform>().SetAnchors(new Vector2(.64f,.18f), new Vector2(.80f,.26f));
            AddButton(panel.transform, "REPAIR SELECTED", Repair).GetComponent<RectTransform>().SetAnchors(new Vector2(.81f,.18f), new Vector2(.96f,.26f));
            AddButton(panel.transform, "CLOSE", () => root.SetActive(false)).GetComponent<RectTransform>().SetAnchors(new Vector2(.81f,.08f), new Vector2(.96f,.15f));
        }

        private void Refresh()
        {
            var dealer = TrailerDealer.Instance;
            var fleet = TrailerFleetManager.Instance;
            if (details == null || dealer == null || fleet == null || dealer.Catalog.Count == 0) return;
            selected = (selected % dealer.Catalog.Count + dealer.Catalog.Count) % dealer.Catalog.Count;
            var entry = dealer.Catalog[selected];
            var s = new StringBuilder();
            s.AppendLine(entry.manufacturer + " " + entry.model);
            s.AppendLine("────────────────────────");
            s.AppendLine("Type: " + entry.type);
            s.AppendLine("Capacity: " + entry.capacityTons.ToString("0") + " tons");
            s.AppendLine("Price: ₹" + entry.price.ToString("0"));
            s.AppendLine("HQ requirement: " + entry.requiredCompanyLevel);
            s.AppendLine("Status: " + (dealer.IsUnlocked(entry) ? "AVAILABLE" : "LOCKED"));
            s.AppendLine(dealer.GetUnlockReason(entry));
            s.AppendLine();
            s.AppendLine("Owned trailers: " + fleet.Trailers.Count);
            foreach (var t in fleet.Trailers)
            {
                if (t == null) continue;
                s.AppendLine($"• {t.id} | {t.model} | {t.condition:0}% | {(t.available ? "AVAILABLE" : "ON CONTRACT")}");
            }
            var active = FleetManager.Instance?.ActiveTruck;
            if (active != null)
            {
                var assigned = fleet.GetAssigned(active.id);
                s.AppendLine();
                s.AppendLine("Active truck: " + active.model);
                s.AppendLine("Assigned trailer: " + (assigned == null ? "None" : assigned.model));
            }
            details.text = s.ToString();
        }

        private void Buy()
        {
            var dealer = TrailerDealer.Instance;
            if (dealer == null || dealer.Catalog.Count == 0) return;
            bool ok = dealer.Purchase(dealer.Catalog[Mathf.Clamp(selected, 0, dealer.Catalog.Count - 1)]);
            Refresh();
            if (details != null) details.text += ok ? "\n\nPURCHASE COMPLETE" : "\n\nPURCHASE FAILED — check HQ level, cash, or fleet state.";
        }

        private void Assign()
        {
            var dealer = TrailerDealer.Instance;
            var fleet = TrailerFleetManager.Instance;
            var truck = FleetManager.Instance?.ActiveTruck;
            if (dealer == null || fleet == null || truck == null) return;
            var type = dealer.Catalog[Mathf.Clamp(selected, 0, dealer.Catalog.Count - 1)].type;
            var owned = fleet.FindAvailableFor(type, truck.id);
            bool ok = owned != null && fleet.AssignToTruck(owned.id, truck.id);
            Refresh();
            if (details != null) details.text += ok ? "\n\nASSIGNED TO ACTIVE TRUCK" : "\n\nASSIGN FAILED — trailer may be unavailable or truck already has one.";
        }

        private void Repair()
        {
            var dealer = TrailerDealer.Instance;
            var fleet = TrailerFleetManager.Instance;
            if (dealer == null || fleet == null || dealer.Catalog.Count == 0) return;

            var entry = dealer.Catalog[Mathf.Clamp(selected, 0, dealer.Catalog.Count - 1)];
            FleetTrailerData trailer = null;

            if (entry.definition != null)
                trailer = fleet.FindByDefinitionId(entry.definition.id);

            if (trailer == null)
            {
                foreach (var owned in fleet.Trailers)
                {
                    if (owned != null && owned.type == entry.type)
                    {
                        trailer = owned;
                        break;
                    }
                }
            }

            bool ok = trailer != null && fleet.Repair(trailer.id);
            Refresh();
            if (details != null)
                details.text += ok
                    ? "\n\nREPAIR COMPLETE — " + trailer.id
                    : "\n\nREPAIR FAILED — no matching owned trailer, trailer may be on contract, or cash is insufficient.";
        }

        private static Text AddText(Transform parent, string value, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(.12f,.15f,.20f,.98f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var text = AddText(go.transform, label, 14, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            go.AddComponent<LayoutElement>().minHeight = 40;
            return button;
        }

        private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}