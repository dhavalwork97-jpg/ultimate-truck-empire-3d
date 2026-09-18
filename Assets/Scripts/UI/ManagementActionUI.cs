using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Save;

namespace UltimateTruckEmpire.UI
{
    public sealed class ManagementActionUI : MonoBehaviour
    {
        private Text status;
        private InputField driverName;
        private InputField branchName;
        private AutoDispatcher dispatcher;
        private SaveManager save;

        private void Start()
        {
            dispatcher = AutoDispatcher.Instance ?? new GameObject("AutoDispatcher").AddComponent<AutoDispatcher>();
            save = FindFirstObjectByType<SaveManager>();
            Build();
        }

        private void Build()
        {
            var root = new GameObject("Management Actions"); var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; root.AddComponent<CanvasScaler>(); root.AddComponent<GraphicRaycaster>();
            var holder = new GameObject("Actions"); holder.transform.SetParent(root.transform, false); var rt = holder.AddComponent<RectTransform>(); rt.anchorMin=new Vector2(.58f,.08f);rt.anchorMax=new Vector2(.94f,.9f);rt.offsetMin=rt.offsetMax=Vector2.zero;
            var layout=holder.AddComponent<VerticalLayoutGroup>();layout.spacing=7;layout.padding=new RectOffset(12,12,12,12);
            AddButton(holder.transform,"DISPATCH BEST CONTRACTS",Dispatch); AddButton(holder.transform,"TOGGLE AUTO DISPATCH",ToggleDispatch);
            driverName=AddInput(holder.transform,"Driver name"); AddButton(holder.transform,"HIRE DRIVER",Hire);
            AddButton(holder.transform,"BUY UTE HAULER — ₹180,000",BuyTruck);
            AddButton(holder.transform,"UPGRADE HEADQUARTERS",UpgradeHQ);
            branchName=AddInput(holder.transform,"Branch city"); AddButton(holder.transform,"OPEN BRANCH",AddBranch);
            AddButton(holder.transform,"SAVE COMPANY",Save); AddButton(holder.transform,"LOAD COMPANY",Load); status=AddLabel(holder.transform,"Ready.");
        }

        private static Button AddButton(Transform parent,string label,UnityEngine.Events.UnityAction action){var go=new GameObject(label);go.transform.SetParent(parent,false);var b=go.AddComponent<Button>();var image=go.AddComponent<Image>();image.color=new Color(.12f,.14f,.18f,.98f);b.targetGraphic=image;b.onClick.AddListener(action);var text=new GameObject("Text").AddComponent<Text>();text.transform.SetParent(go.transform,false);text.text=label;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;go.AddComponent<LayoutElement>().minHeight=40;return b;}
        private static InputField AddInput(Transform parent,string placeholder){var go=new GameObject(placeholder);go.transform.SetParent(parent,false);go.AddComponent<Image>().color=new Color(.08f,.09f,.11f,1);var input=go.AddComponent<InputField>();var text=new GameObject("Text").AddComponent<Text>();text.transform.SetParent(go.transform,false);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.color=Color.white;text.text="";text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=new Vector2(10,0);text.rectTransform.offsetMax=new Vector2(-10,0);input.textComponent=text;go.AddComponent<LayoutElement>().minHeight=40;return input;}
        private static Text AddLabel(Transform parent,string value){var go=new GameObject("Status");go.transform.SetParent(parent,false);var text=go.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.color=Color.white;text.text=value;go.AddComponent<LayoutElement>().minHeight=55;return text;}
        private void Dispatch(){status.text=$"Dispatched {dispatcher.DispatchAvailable()} contract(s).";}
        private void ToggleDispatch(){dispatcher.SetEnabled(!dispatcher.Enabled);status.text="Auto dispatch: "+(dispatcher.Enabled?"ON":"OFF");}
        private void Hire(){var d=DriverManager.Instance?.HireDriver(string.IsNullOrWhiteSpace(driverName.text)?"New Driver":driverName.text);status.text=d==null?"Hire failed: driver capacity reached.":$"Hired {d.name}.";}
        private void BuyTruck(){var t=FleetManager.Instance?.BuyTruck("UTE Hauler 300",180000,30);status.text=t==null?"Purchase failed: check cash or fleet capacity.":$"Purchased {t.model} ({t.id}).";}
        private void UpgradeHQ(){status.text=CompanyManager.Instance?.UpgradeHeadquarters()==true?"Headquarters upgraded.":"HQ cannot be upgraded further.";}
        private void AddBranch(){var ok=CompanyManager.Instance?.AddBranch(branchName.text)==true;status.text=ok?$"Branch opened in {branchName.text.Trim()}.":"Branch failed: empty or duplicate city.";}
        private void Save(){if(save==null)save=FindFirstObjectByType<SaveManager>();save?.Save();status.text="Company saved.";}
        private void Load(){if(save==null)save=FindFirstObjectByType<SaveManager>();save?.Load();status.text="Company loaded.";}
    }
}
