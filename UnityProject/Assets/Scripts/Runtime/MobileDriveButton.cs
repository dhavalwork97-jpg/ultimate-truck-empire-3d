using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateTruckEmpire
{
    public sealed class MobileDriveButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum ActionType
        {
            Left,
            Right,
            Throttle,
            Brake
        }

        private MobileInput input;
        private ActionType action;

        public void Initialize(MobileInput mobileInput, ActionType buttonAction)
        {
            input = mobileInput;
            action = buttonAction;
        }

        private void Awake()
        {
            if (input == null)
                input = FindFirstObjectByType<MobileInput>();
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

        private void SetPressed(bool pressed)
        {
            if (input == null) return;

            switch (action)
            {
                case ActionType.Left:
                    input.SetSteering(pressed ? -1f : 0f);
                    break;
                case ActionType.Right:
                    input.SetSteering(pressed ? 1f : 0f);
                    break;
                case ActionType.Throttle:
                    input.SetThrottle(pressed ? 1f : 0f);
                    break;
                case ActionType.Brake:
                    input.SetBraking(pressed);
                    break;
            }
        }
    }
}
