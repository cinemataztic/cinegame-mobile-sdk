using UnityEngine;
using UnityEngine.Events;
using System;

namespace CineGame.MobileComponents {

	public class OnEnableDisable : MonoBehaviour, IGameComponentIcon {
		[Header ("Trigger events when this GameObject becomes enabled or disabled. Optionally delay events")]

		public float OnEnableDelay = 0f;
		public float OnDisableDelay = 0f;

		[Serializable] public class OnEnableDisableEvent : UnityEvent { }

		[Header("When gameobject is activated")]
		public OnEnableDisableEvent onEnable;
		[Header("When gameobject is deactivated")]
		public OnEnableDisableEvent onDisable;

		public void CancelEnable () {
			CancelInvoke ("OnEnableInvoke");
		}

		public void CancelDisable () {
			CancelInvoke ("OnDisableInvoke");
		}

		void OnEnable () {
			if (OnEnableDelay > 0f) {
				Invoke ("OnEnableInvoke", OnEnableDelay);
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
			if (OnDisableDelay > 0f) {
				Invoke ("OnDisableInvoke", OnDisableDelay);
			} else {
				OnDisableInvoke ();
			}
		}

		void OnDisableInvoke () {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} OnDisable:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (onDisable));
			}
			onDisable.Invoke ();
		}

		void OnValidate(){
			OnEnableDelay = Math.Max (0f, OnEnableDelay);
			OnDisableDelay = Math.Max (0f, OnDisableDelay);
		}
	}

}