using UnityEngine;
using UnityEngine.Events;
using System;

namespace CineGame.MobileComponents {

	[ComponentReference ("Trigger events when this GameObject becomes enabled or disabled. Optionally delay events")]
	public class OnEnableDisable : BaseComponent {

		public float OnEnableDelay = 0f;

		[Serializable] public class OnEnableDisableEvent : UnityEvent { }

		[Header("When gameobject is activated")]
		public OnEnableDisableEvent onEnable;
		[Header("When gameobject is deactivated")]
		public OnEnableDisableEvent onDisable;

		private void Start() {
			VerboseDebug &= Debug.isDebugBuild || Util.IsDevModeActive;
		}

		public void CancelEnable () {
			CancelInvoke (nameof(OnEnableInvoke));
		}

		void OnEnable () {
			if (OnEnableDelay > 0f) {
				Invoke (nameof(OnEnableInvoke), OnEnableDelay);
			} else {
				OnEnableInvoke ();
			}
		}

		void OnEnableInvoke () {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} OnEnable:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (onEnable));
			}
			onEnable.Invoke ();
		}

		void OnDisable () {
			OnDisableInvoke ();
		}

		void OnDisableInvoke () {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} OnDisable:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (onDisable));
			}
			onDisable.Invoke ();
		}

		void OnValidate(){
			OnEnableDelay = Math.Max (0f, OnEnableDelay);
		}
	}

}