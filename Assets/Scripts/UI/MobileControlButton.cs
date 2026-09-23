using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateTruckEmpire.UI
{
    public sealed class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Control { SteerLeft, SteerRight, Throttle, Brake, Horn, Engine, Gear, Headlights, LeftIndicator, RightIndicator, Hazards }
        [SerializeField] private Control control;

        public void Configure(Control value) => control = value;

        public void OnPointerDown(PointerEventData eventData)
        {
            SetHeld(true);
            if (control == Control.Engine) UltimateTruckEmpire.Truck.MobileInputState.RequestEngineToggle();
            else if (control == Control.Gear) UltimateTruckEmpire.Truck.MobileInputState.RequestGear();
            else if (control == Control.Headlights) UltimateTruckEmpire.Truck.MobileInputState.RequestHeadlights();
            else if (control == Control.LeftIndicator) UltimateTruckEmpire.Truck.MobileInputState.RequestLeftIndicator();
            else if (control == Control.RightIndicator) UltimateTruckEmpire.Truck.MobileInputState.RequestRightIndicator();
            else if (control == Control.Hazards) UltimateTruckEmpire.Truck.MobileInputState.RequestHazards();
        }

        public void OnPointerUp(PointerEventData eventData) => SetHeld(false);
        public void OnPointerExit(PointerEventData eventData) => SetHeld(false);

        private void SetHeld(bool held)
        {
            switch (control)
            {
                case Control.SteerLeft: UltimateTruckEmpire.Truck.MobileInputState.Steering = held ? -1f : 0f; break;
                case Control.SteerRight: UltimateTruckEmpire.Truck.MobileInputState.Steering = held ? 1f : 0f; break;
                case Control.Throttle: UltimateTruckEmpire.Truck.MobileInputState.Throttle = held ? 1f : 0f; break;
                case Control.Brake: UltimateTruckEmpire.Truck.MobileInputState.Brake = held; break;
                case Control.Horn: UltimateTruckEmpire.Truck.MobileInputState.Horn = held; break;
            }
        }

        private void OnDisable() => SetHeld(false);
    }
}