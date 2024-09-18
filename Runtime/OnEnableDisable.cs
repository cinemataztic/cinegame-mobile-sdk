using UnityEngine;
using UnityEngine.Events;
using System;

namespace CineGame.MobileComponents {

	[ComponentReference ("Trigger events when this GameObject becomes enabled or disabled. Optionally delay events, and optionally repeat the onEnable event (perform actions every n seconds) while GameObject is active")]
	public class OnEnableDisable : BaseComponent {

		public float OnEnableDelay = 0f;
		[Tooltip ("If >= 0 then OnEnable will invoke every RepeatInterval seconds while the GameObject is active.")]
		public float RepeatInterval = -1f;

		[Serializable] public class OnEnableDisableEvent : UnityEvent { }

		[Tooltip("Invoked when gameobject is activated or component is enabled")]
		public OnEnableDisableEvent onEnable;
		[Tooltip("Invoked when gameobject is deactivated or component is disabled")]
		public OnEnableDisableEvent onDisable;

		public void CancelEnable () {
			Log ("OnEnableDisable.CancelEnable");
			CancelInvoke (nameof(OnEnableInvoke));
		}

		void OnEnable () {
			if (RepeatInterval >= 0f)
				InvokeRepeating (nameof (OnEnableInvoke), OnEnableDelay, RepeatInterval);
			else if (OnEnableDelay > 0f) {
				Invoke (nameof(OnEnableInvoke), OnEnableDelay);
			} else {
				OnEnableInvoke ();
			}
		}

		void OnEnableInvoke () {
			Log ($"OnEnable:\n{Util.GetEventPersistentListenersInfo (onEnable)}");
			onEnable.Invoke ();
		}

		void OnDisable () {
			OnDisableInvoke ();
		}

		void OnDisableInvoke () {
			Log ($"OnDisable:\n{Util.GetEventPersistentListenersInfo (onDisable)}");
			onDisable.Invoke ();
		}

		void OnValidate(){
			OnEnableDelay = Math.Max (0f, OnEnableDelay);
			if (RepeatInterval < 0f)
				RepeatInterval = -1f;
		}
	}

}