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
	[ComponentReference ("Raycast from screen position (mouse or touch)")]
	public class RaycastComponent : BaseEventComponent {

		public LayerMask LayerMask = -1;

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

				if (OnClickPosition.GetPersistentEventCount () != 0) {
					Log ($"RaycastComponent.OnClickPosition {hit.point}\n{Util.GetEventPersistentListenersInfo (OnClickPosition)}");
					OnClickPosition.Invoke (hit.point);
				}
				if (OnClickNormal.GetPersistentEventCount () != 0) {
					Log ($"RaycastComponent.OnClickNormal {hit.point}\n{Util.GetEventPersistentListenersInfo (OnClickNormal)}");
					OnClickNormal.Invoke (hit.normal);
				}
				if (OnClickTransform.GetPersistentEventCount () != 0) {
					Log ($"RaycastComponent.OnClickTransform {hit.point}\n{Util.GetEventPersistentListenersInfo (OnClickTransform)}");
					OnClickTransform.Invoke (hit.transform);
				}

			} else if ((hasDragListeners || hasHoverListeners) && RaycastFromScreen (out hit)) {
				if (Input.GetMouseButton (0)) {

					if (OnDragPosition.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnDragPosition {hit.point}\n{Util.GetEventPersistentListenersInfo (OnDragPosition)}");
						OnDragPosition.Invoke (hit.point);
					}
					if (OnDragNormal.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnDragNormal {hit.point}\n{Util.GetEventPersistentListenersInfo (OnDragNormal)}");
						OnDragNormal.Invoke (hit.normal);
					}
					if (OnDragTransform.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnDragTransform {hit.point}\n{Util.GetEventPersistentListenersInfo (OnDragTransform)}");
						OnDragTransform.Invoke (hit.transform);
					}

				} else {

					if (OnHoverPosition.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnHoverPosition {hit.point}\n{Util.GetEventPersistentListenersInfo (OnHoverPosition)}");
						OnHoverPosition.Invoke (hit.point);
					}
					if (OnHoverNormal.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnHoverNormal {hit.point}\n{Util.GetEventPersistentListenersInfo (OnHoverNormal)}");
						OnHoverNormal.Invoke (hit.normal);
					}
					if (OnHoverTransform.GetPersistentEventCount () != 0) {
						Log ($"RaycastComponent.OnHoverTransform {hit.point}\n{Util.GetEventPersistentListenersInfo (OnHoverTransform)}");
						OnHoverTransform.Invoke (hit.transform);
					}

				}
			}
		}
	}
}