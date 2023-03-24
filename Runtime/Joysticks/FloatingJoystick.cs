using UnityEngine.EventSystems;

namespace CineGame.MobileComponents
{
    public class FloatingJoystick : Joystick
    {
        protected override void Start() => base.Start();

        public override void OnPointerDown(PointerEventData eventData)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData) => base.OnPointerUp(eventData);
    }
}