using UnityEngine;
using UnityEngine.EventSystems;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	[RequireComponent(typeof(RectTransform))]
	public class DragDropComponent : ReplicatedComponent, IDragHandler, IBeginDragHandler, IEndDragHandler {

        [Tooltip("Should dragged object spring back when drop/enddrag?")]
        public bool ResetPosition = false;
        [Tooltip("Interval for resetting position")]
        public float ResetPositionInterval = 0.1f;

        [Header("Replication")]
        [Tooltip("Smartfox uservariable name for x coordinate")]
        public string VariableNameX = "x";
        [Tooltip("Smartfox uservariable name for y coordinate")]
        public string VariableNameY = "y";
        [Tooltip("How often should coordinates be replicated")]
        public float UpdateInterval = 0.033f;

		[Tooltip("Key in objectMessage to gamehost when drag begin or end (bool true/false)")]
        public string DragDropKey = "";

		[Tooltip("Limit object inner-circularly to parent rect? Eg analog joystick")]
		public bool Circular = false;

		Vector2 currentNormalizedPosition = Vector2.zero;
		Vector2 prevNormalizedPosition = Vector2.zero;
        float lastUpdateTime = 0f;

        GameObject dragObject;
        Vector2 dragOffset;
		Vector2 dropLocalPosition;
		Vector2 resetLocalPosition;
        GameObject resetPositionObject;
        float resetPositionStartTime;
        Camera eventCamera;
		int touchId;

		void OnEnable () {
			dragObject = null;
			resetPositionObject = null;
		}

        public void OnBeginDrag (PointerEventData ped) {
            eventCamera = ped.pressEventCamera;
            dragObject = ped.pointerDrag;
			dragOffset = RectTransformUtility.WorldToScreenPoint (eventCamera, dragObject.transform.position) - ped.pressPosition;
			touchId = ped.pointerId;
            if (resetPositionObject == null) {
				resetLocalPosition = dragObject.transform.localPosition;
            }
            if (!string.IsNullOrEmpty (DragDropKey)) {
				Send (DragDropKey, true);
            }
        }

		public void OnEndDrag (PointerEventData ped) {
			if (!string.IsNullOrEmpty (DragDropKey)) {
				Send (DragDropKey, false);
			}
			if (ResetPosition) {
				dropLocalPosition = dragObject.transform.localPosition;
				resetPositionObject = dragObject;
				resetPositionStartTime = Time.time;
			}
			dragObject = null;
        }

		public void OnDrag (PointerEventData ped) {
			touchId = ped.pointerId;
		}

        void Update () {
			var obj = (dragObject != null) ? dragObject : resetPositionObject;
			if (obj != null) {
				Vector2 localPos;
				var rt = obj.transform.parent.transform as RectTransform;
				var rtrect = rt.rect;

				if (dragObject != null) {
					if (!Application.isEditor && touchId < Input.touches.Length) {
						localPos = Input.touches [touchId].position;
					} else {
						localPos = new Vector2 (Input.mousePosition.x, Input.mousePosition.y);
					}
					RectTransformUtility.ScreenPointToLocalPointInRectangle (rt, localPos + dragOffset, eventCamera, out localPos);
					if (Circular) {
						//Limit freedom to inner circle within parent rect
						var lpProj = localPos - rtrect.center;
						var scalex = rtrect.center.x - rtrect.min.x;
						var scaley = rtrect.center.y - rtrect.min.y;
						lpProj.x /= scalex;
						lpProj.y /= scaley;
						float len = lpProj.magnitude;
						if (len > 1f) {
							lpProj /= len;
							lpProj.x *= scalex;
							lpProj.y *= scaley;
							localPos = lpProj + rtrect.center;
						}
					} else {
						localPos = Vector2.Max (rtrect.min, Vector2.Min (rtrect.max, localPos));
					}

					dragObject.transform.localPosition = localPos;
				} else {
					//Smoothly interpolate to resetLocalPosition (eg to simulate a joystick)
					float t = (ResetPositionInterval != 0f) ? (Time.time - resetPositionStartTime) / ResetPositionInterval : 1f;
					localPos = Vector2.Lerp (dropLocalPosition, rtrect.center, Interpolation.EaseOutQuad (t));
					resetPositionObject.transform.localPosition = localPos;
					if (t >= 1f) {
						resetPositionObject = null;
						//Make sure position is updated one last time right now
						lastUpdateTime = Time.time - UpdateInterval;
					}
				}

				currentNormalizedPosition = new Vector2 ((localPos.x - rtrect.min.x) / rtrect.width, (localPos.y - rtrect.min.y) / rtrect.height);

				if (currentNormalizedPosition != prevNormalizedPosition && (lastUpdateTime + UpdateInterval) <= Time.time) {
					//Debug.LogFormat ("x={0}, y={1}", currentPosition.x, currentPosition.y);
					Send (VariableNameX, currentNormalizedPosition.x);
					Send (VariableNameY, currentNormalizedPosition.y);
					lastUpdateTime = Time.time;
					prevNormalizedPosition = currentNormalizedPosition;
				}
			}
        }
    }

}
