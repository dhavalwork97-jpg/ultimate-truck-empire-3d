using UnityEngine;
using UnityEngine.UI;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.UI
{
    public sealed class DispatchAssignmentUI : MonoBehaviour
    {
        private Text details;
        private ContractOffer selectedContract;
        private DriverData selectedDriver;
        private FleetTruckData selectedTruck;

        private GameObject root;

        private void Start() { Build(); Refresh(); SetVisible(false); }

        // F10 toggles the dispatch panel; it covers half the screen, so it does not
        // stay open while driving.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10)) SetVisible(root == null || !root.activeSelf);
        }

        public void SetVisible(bool visible) { if (root != null) root.SetActive(visible); }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Dispatch Assignment Panel");
            root = panel;
            panel.transform.SetParent(transform, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(.025f, .03f, .04f, .97f);
            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(.04f, .04f); rt.anchorMax = new Vector2(.56f, .9f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 5;
            AddLabel(panel.transform, "DISPATCH CENTER — CONTRACT → DRIVER → TRUCK", 22);
            details = AddLabel(panel.transform, "Select a contract, available driver and available truck.", 16);
            AddLabel(panel.transform, "CONTRACTS", 18);
            var contracts = new GameObject("Contracts"); contracts.transform.SetParent(panel.transform, false); contracts.AddComponent<VerticalLayoutGroup>().spacing = 3;
            if (ContractMarket.Instance != null) foreach (var offer in ContractMarket.Instance.Offers) { var captured = offer; AddButton(contracts.transform, $"{offer.cargo} | {offer.pickup} → {offer.destination} | {offer.weightTons:0.0}t | ₹{offer.reward:0}", () => SelectContract(captured)); }
            AddLabel(panel.transform, "DRIVERS", 18);
            var drivers = new GameObject("Drivers"); drivers.transform.SetParent(panel.transform, false); drivers.AddComponent<VerticalLayoutGroup>().spacing = 3;
            if (DriverManager.Instance != null) foreach (var driver in DriverManager.Instance.Drivers) { var captured = driver; AddButton(drivers.transform, $"{driver.name} | Lv {driver.level} | Perf {DriverManager.Instance.GetEffectivePerformance(driver):0} | {(driver.available ? "AVAILABLE" : "BUSY")}", () => SelectDriver(captured)); }
            AddLabel(panel.transform, "TRUCKS", 18);
            var trucks = new GameObject("Trucks"); trucks.transform.SetParent(panel.transform, false); trucks.AddComponent<VerticalLayoutGroup>().spacing = 3;
            if (FleetManager.Instance != null) foreach (var truck in FleetManager.Instance.Trucks) { var captured = truck; AddButton(trucks.transform, $"{truck.model} | {truck.capacityTons:0}t | Fuel {truck.fuel:0} | {(truck.available ? "AVAILABLE" : "BUSY")}", () => SelectTruck(captured)); }
            AddButton(panel.transform, "DISPATCH SELECTED DELIVERY", Dispatch);
        }

        private void Refresh()
        {
            if (details == null) return;
            details.text = (selectedContract == null ? "Contract: —" : $"Contract: {selectedContract.cargo}  {selectedContract.pickup} → {selectedContract.destination}") + "\n" +
                           (selectedDriver == null ? "Driver: —" : $"Driver: {selectedDriver.name} (Perf {DriverManager.Instance.GetPerformance(selectedDriver):0})") + "\n" +
                           (selectedTruck == null ? "Truck: —" : $"Truck: {selectedTruck.model} ({selectedTruck.capacityTons:0}t)");
        }

        private void SelectContract(ContractOffer offer) { selectedContract = offer; Refresh(); }
        private void SelectDriver(DriverData driver) { selectedDriver = driver; Refresh(); }
        private void SelectTruck(FleetTruckData truck) { selectedTruck = truck; Refresh(); }

        private void Dispatch()
        {
            if (selectedContract == null || selectedDriver == null || selectedTruck == null) { details.text += "\nSelect all three items first."; return; }
            if (!DriverManager.Instance.CanDispatch(selectedDriver) || !selectedTruck.available)
            {
                details.text += "\nDriver is unavailable or too fatigued, or truck is already busy.";
                return;
            }
            if (selectedTruck.capacityTons < selectedContract.weightTons) { details.text += "\nSelected truck is too small for this cargo."; return; }
            DriverManager.Instance.BeginDelivery(selectedDriver);
            var job = AutomatedDeliveryManager.Instance?.StartDelivery(selectedTruck, selectedDriver, selectedContract);
            if (job == null)
            {
                selectedDriver.available = true;
                details.text += "\nDispatch failed.";
                return;
            }
            ContractMarket.Instance?.Remove(selectedContract);
            details.text = $"DISPATCHED {job.id}: {job.cargo}\n{job.origin} → {job.destination}\nETA {job.etaHours:0.0} h | ₹{job.reward:0}";
            selectedContract = null; selectedDriver = null; selectedTruck = null;
        }

        private static Text AddLabel(Transform parent, string value, int size)
        {
            var go = new GameObject("Label"); go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = Color.white; text.text = value;
            go.AddComponent<LayoutElement>().minHeight = size + 10; return text;
        }

        private static Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>(); image.color = new Color(.11f, .14f, .18f, 1f);
            var button = go.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
            var text = new GameObject("Text").AddComponent<Text>(); text.transform.SetParent(go.transform, false); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 14; text.alignment = TextAnchor.MiddleLeft; text.color = Color.white; text.text = label;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(8, 0); text.rectTransform.offsetMax = new Vector2(-8, 0);
            go.AddComponent<LayoutElement>().minHeight = 34; return button;
        }
    }
}
