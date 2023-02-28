using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

namespace CineGame.MobileComponents {

	[RequireComponent(typeof(RectTransform),typeof(EventTrigger))]
	public class PointerEventComponent : ReplicatedComponent {

		[Tooltip("Limit object inner-circularly to parent rect?")]
		public bool Circular = false;

		[Serializable] public class Vector2Event : UnityEvent<Vector2> { }
		public Vector2Event onPointerEvent;

		[Header("Replication")]
        [Tooltip("Smartfox uservariable name for x coordinate")]
        public string VariableNameX = "x";
        [Tooltip("Smartfox uservariable name for y coordinate")]
        public string VariableNameY = "y";
        [Tooltip("How often should coordinates be replicated")]
        public float UpdateInterval = 0.033f;

		Vector2 currentNormalizedPosition = Vector2.zero;
		Vector2 prevNormalizedPosition = Vector2.zero;
        float lastUpdateTime = 0f;

        Camera eventCamera;
		int touchId;

		public void OnEvent (BaseEventData eventData) {
			var pointerEventData = eventData as PointerEventData;
			if (pointerEventData != null) {
				touchId = pointerEventData.pointerId;
				Vector2 localPos = (touchId >= 0)? Input.touches [touchId].position : pointerEventData.position;
				var rt = transform as RectTransform;
				var rtrect = rt.rect;

				RectTransformUtility.ScreenPointToLocalPointInRectangle (rt, localPos, eventCamera, out localPos);
				if (Circular) {
					//Ignore event if outside inner circle/ellipse of parent rect
					var lpProj = localPos - rtrect.center;
					var scalex = rtrect.center.x - rtrect.min.x;
					var scaley = rtrect.center.y - rtrect.min.y;
					lpProj.x /= scalex;
					lpProj.y /= scaley;
					float len = lpProj.magnitude;
					if (len > 1f) {
						return;
					}
				} else {
					localPos = Vector2.Max (rtrect.min, Vector2.Min (rtrect.max, localPos));
				}

				onPointerEvent.Invoke (localPos);

				currentNormalizedPosition = new Vector2 ((localPos.x - rtrect.min.x) / rtrect.width, (localPos.y - rtrect.min.y) / rtrect.height);
				if (currentNormalizedPosition != prevNormalizedPosition && (lastUpdateTime + UpdateInterval) <= Time.time) {
					Debug.LogFormat ("x={0}, y={1}", currentNormalizedPosition.x, currentNormalizedPosition.y);
					Send (VariableNameX, currentNormalizedPosition.x);
					Send (VariableNameY, currentNormalizedPosition.y);
					lastUpdateTime = Time.time;
					prevNormalizedPosition = currentNormalizedPosition;
				}
			}
        }
    }

}
