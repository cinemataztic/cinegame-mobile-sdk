using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Component to proxy parameters or IK values into a specific Animator
	/// </summary>
	public class RaycastComponent : MonoBehaviour, IGameComponentIcon {
		[Header ("Raycast from screen position (mouse or touch)")]
		[Space]
		[Tooltip ("Log events verbosely in editor and debug builds")]
		public bool VerboseDebug = false;

		public LayerMask LayerMask = -1;

		[HideInInspector]
		[SerializeField]
		private int eventMask = 0;

		public UnityEvent<Vector3> OnClickPosition;
		public UnityEvent<Vector3> OnClickNormal;
		public UnityEvent<Transform> OnClickTransform;

		public UnityEvent<Vector3> OnDragPosition;
		public UnityEvent<Vector3> OnDragNormal;
		public UnityEvent<Transform> OnDragTransform;

		public UnityEvent<Vector3> OnHoverPosition;
		public UnityEvent<Vector3> OnHoverNormal;
		public UnityEvent<Transform> OnHoverTransform;

		bool hasDragListeners, hasHoverListeners;

		void Start () {
			VerboseDebug &= Util.IsDevModeActive || Debug.isDebugBuild;

			hasDragListeners =
				OnDragPosition.GetPersistentEventCount ()
				+ OnDragNormal.GetPersistentEventCount ()
				+ OnDragTransform.GetPersistentEventCount ()
				!= 0;

			hasHoverListeners = OnHoverPosition.GetPersistentEventCount ()
				+ OnHoverNormal.GetPersistentEventCount ()
				+ OnHoverTransform.GetPersistentEventCount ()
				!= 0;
		}

		bool RaycastFromScreen (out RaycastHit hit) {
			return Physics.Raycast (Camera.main.ScreenPointToRay (Input.mousePosition), out hit, 100, LayerMask);
		}

		void Update () {
			RaycastHit hit;
			if (Input.GetMouseButtonDown (0) && RaycastFromScreen (out hit)) {
				if (VerboseDebug) {
					Debug.LogFormat ("{0} RaycastComponent.OnClick {1}\n{2}", gameObject.GetScenePath (), hit.point, Util.GetEventPersistentListenersInfo (OnClickPosition));
				}
				OnClickPosition?.Invoke (hit.point);
				OnClickNormal?.Invoke (hit.normal);
				OnClickTransform?.Invoke (hit.transform);
			} else if ((hasDragListeners || hasHoverListeners) && RaycastFromScreen (out hit)) {
				if (VerboseDebug) {
					Debug.LogFormat ("{0} RaycastComponent.OnDrag {1}\n{2}", gameObject.GetScenePath (), hit.point, Util.GetEventPersistentListenersInfo (OnClickPosition));
				}
				if (Input.GetMouseButton (0)) {
					OnDragPosition?.Invoke (hit.point);
					OnDragNormal?.Invoke (hit.normal);
					OnDragTransform?.Invoke (hit.transform);
				} else {
					OnHoverPosition?.Invoke (hit.point);
					OnHoverNormal?.Invoke (hit.normal);
					OnHoverTransform?.Invoke (hit.transform);
				}
			}
		}
	}
}