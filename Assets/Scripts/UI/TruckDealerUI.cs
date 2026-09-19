using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.UI
{
    public sealed class TruckDealerUI : MonoBehaviour
    {
        private Text details;
        private int selected;
        private TruckDealer dealer;
        private GameObject root;

        private void Start()
        {
            dealer = TruckDealer.Instance ?? new GameObject("Truck Dealer").AddComponent<TruckDealer>();
            Build(); Refresh(); root.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11) && root != null) root.SetActive(!root.activeSelf);
        }

        private void Build()
        {
            root = new GameObject("Truck Dealership Canvas");
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            root.AddComponent<CanvasScaler>(); root.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Dealership Panel"); panel.transform.SetParent(root.transform, false);
            var image = panel.AddComponent<Image>(); image.color = new Color(.02f,.025f,.035f,.97f);
            var rt = image.rectTransform; rt.anchorMin = new Vector2(.06f,.08f); rt.anchorMax = new Vector2(.94f,.92f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var title = AddText(panel.transform, "TRUCK DEALERSHIP\nF11 Close  •  Select a truck to compare specs", 24, TextAnchor.UpperLeft);
            title.rectTransform.anchorMin=new Vector2(.03f,.78f); title.rectTransform.anchorMax=new Vector2(.97f,.97f); title.rectTransform.offsetMin=title.rectTransform.offsetMax=Vector2.zero;

            var list = new GameObject("Truck List"); list.transform.SetParent(panel.transform,false);
            var layout=list.AddComponent<VerticalLayoutGroup>(); layout.spacing=6; layout.padding=new RectOffset(12,12,12,12);
            var lr=list.GetComponent<RectTransform>(); lr.anchorMin=new Vector2(.03f,.08f); lr.anchorMax=new Vector2(.46f,.76f); lr.offsetMin=lr.offsetMax=Vector2.zero;
            for(int i=0;i<dealer.Catalog.Count;i++)
            {
                int index=i;
                var button=AddButton(list.transform, dealer.Catalog[i].manufacturer+" — "+dealer.Catalog[i].model, ()=>{selected=index;Refresh();});
                button.GetComponent<LayoutElement>().minHeight=46;
            }

            details=AddText(panel.transform,"",18,TextAnchor.UpperLeft);
            details.rectTransform.anchorMin=new Vector2(.5f,.28f); details.rectTransform.anchorMax=new Vector2(.96f,.76f); details.rectTransform.offsetMin=details.rectTransform.offsetMax=Vector2.zero;
            AddButton(panel.transform,"BUY SELECTED TRUCK",Buy).GetComponent<RectTransform>().SetAnchors(new Vector2(.5f,.14f),new Vector2(.75f,.23f));
            AddButton(panel.transform,"CLOSE DEALERSHIP",Close).GetComponent<RectTransform>().SetAnchors(new Vector2(.76f,.14f),new Vector2(.96f,.23f));
        }

        private void Refresh()
        {
            if(details==null||dealer==null||dealer.Catalog.Count==0)return;
            selected=Mathf.Clamp(selected,0,dealer.Catalog.Count-1);
            var t=dealer.Catalog[selected]; var s=new StringBuilder();
            s.AppendLine(t.manufacturer+" "+t.model); s.AppendLine();
            s.AppendLine($"HQ Level Required: {t.requiredCompanyLevel}");
            s.AppendLine(dealer.IsUnlocked(t) ? "STATUS: AVAILABLE" : "STATUS: LOCKED");
            s.AppendLine(dealer.GetUnlockReason(t)); s.AppendLine();
            s.AppendLine($"Price: ₹{t.price:0}"); s.AppendLine($"Capacity: {t.capacityTons:0} tons");
            s.AppendLine($"Power: {t.powerHp:0} HP"); s.AppendLine($"Torque: {t.torqueNm:0} Nm");
            s.AppendLine($"Top speed: {t.maxSpeedKph:0} km/h"); s.AppendLine($"Fuel tank: {t.fuelCapacity:0} L");
            s.AppendLine($"Efficiency: {t.fuelEfficiency:0.0} km/L"); s.AppendLine($"Drivetrain: {t.drivetrain}");
            details.text=s.ToString();
        }

        private void Buy()
        {
            var t=dealer.Purchase(dealer.Catalog[Mathf.Clamp(selected,0,dealer.Catalog.Count-1)]);
            details.text += t!=null ? "\n\nPURCHASE COMPLETE\nFleet ID: "+t.id : "\n\nPURCHASE FAILED\nTruck may be locked, unaffordable, or fleet capacity may be full.";
        }
        private void Close(){if(root!=null)root.SetActive(false);}
        private static Text AddText(Transform p,string value,int size,TextAnchor anchor){var go=new GameObject("Text");go.transform.SetParent(p,false);var t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.alignment=anchor;t.color=Color.white;t.text=value;t.raycastTarget=false;return t;}
        private static Button AddButton(Transform p,string label,UnityEngine.Events.UnityAction action){var go=new GameObject(label);go.transform.SetParent(p,false);var b=go.AddComponent<Button>();var img=go.AddComponent<Image>();img.color=new Color(.12f,.15f,.2f,.98f);b.targetGraphic=img;b.onClick.AddListener(action);var text=AddText(go.transform,label,15,TextAnchor.MiddleCenter);text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;go.AddComponent<LayoutElement>().minHeight=40;return b;}
    }
    internal static class RectTransformExtensions
    {
        public static void SetAnchors(this RectTransform rt, Vector2 min, Vector2 max){rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;}
    }
}