using UnityEngine;
using UnityEngine.EventSystems;

namespace CineGame.MobileComponents {

	public class DPadComponent : ReplicatedComponent, IPointerDownHandler, IPointerUpHandler {
        [Header ("Replicated PointerUp/PointerDown. CooldownTime will limit how often the event will replicate.")]

        [Tooltip("Name of smartfox uservariable (bool) to replicate")]
        public string VariableName = "fire";
        [Tooltip("How often should a 'keydown' be able to trigger")]
        public float CooldownTime = .1f;

		//bool State = false;
        float lastUpdateTime = 0f;
/*
		public void OnClick () {
			if (lastUpdateTime + CooldownTime <= Time.time) {
				SmartfoxClient.Send (VariableName, true);
				lastUpdateTime = Time.time;
			}
		}
*/
		public void OnPointerDown (PointerEventData eventData) {
            if (lastUpdateTime + CooldownTime <= Time.time) {
                //State = true;
                Send (VariableName, true);
                lastUpdateTime = Time.time;
                Util.PerformHapticFeedback (Util.HapticFeedbackConstants.VIRTUAL_KEY);
            }
        }

		public void OnPointerUp (PointerEventData eventData) {
            //State = false;
			Send (VariableName, false);
            Util.PerformHapticFeedback (Util.HapticFeedbackConstants.VIRTUAL_KEY_RELEASE);
        }

        public void SetState (bool state) {
            if (state) {
                OnPointerDown (null);
            } else {
                OnPointerUp (null);
            }
        }
    }

}