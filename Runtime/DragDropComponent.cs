using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CineGame.MobileComponents {

	[ComponentReference ("Fire events/actions and replicate coordinates when this GameObject is dragged. This can be used as a simple analog joystick. You can specify whether the position should reset when dropped and how fast.")]
	[RequireComponent(typeof(RectTransform))]
	public class DragDropComponent : ReplicatedComponent, IDragHandler, IBeginDragHandler, IEndDragHandler {

        [Tooltip("Should dragged object spring back when drop/enddrag?")]
        public bool ResetPosition = false;
        [Tooltip("Duration for resetting position")]
        public float ResetPositionDuration = 0.1f;

        [Header("Replication")]
        [Tooltip("Smartfox uservariable name for x coordinate")]
        public string VariableNameX = "x";
        [Tooltip("Smartfox uservariable name for y coordinate")]
        public string VariableNameY = "y";
        [Tooltip("How often should coordinates be replicated")]
        public float UpdateInterval = 0.033f;

		public Vector2 Offset = new (.5f, .5f);
		public Vector2 Scale = new (.5f, .5f);

		[Tooltip("Key in objectMessage to gamehost when drag begins (true) or ends (false)")]
        public string DragDropKey = "";

		[Tooltip("Limit position inner-circularly to parent rect? Eg analog joystick handle")]
		public bool Circular = false;

		[Tooltip ("Size of deadzone in center. 0 is none, 1 is all of inner circle")]
		public float Deadzone = .1f;

		[HideInInspector]
		[SerializeField]
		private int eventMask = 0;

		[Tooltip("Event fired when user starts to drag the object")]
		public UnityEvent OnDragBegin;
		[Tooltip ("Event fired when user stops dragging the object")]
		public UnityEvent OnDragEnd;

		[Tooltip ("Event fired when the unit Vector2 changes")]
		public UnityEvent<Vector2> OnDragVector2;
		[Tooltip ("Event fired with a Vector3(X,Y,0) of the unit Vector2 when it changes")]
		public UnityEvent<Vector3> OnDragVector3_XY;
		[Tooltip ("Event fired with a Vector3(X,0,Y) of the unit Vector2 when it changes")]
		public UnityEvent<Vector3> OnDragVector3_XZ;
		[Tooltip ("Event fired with a Vector3(0,X,Y) of the unit Vector2 when it changes")]
		public UnityEvent<Vector3> OnDragVector3_YZ;

		[Tooltip ("Event fired when the distance between object and origin changes")]
		public UnityEvent<float> OnSpeedChange;
		[Tooltip ("Event fired when the angle between object-origin and origin up vector changes")]
		public UnityEvent<Quaternion> OnAngleChange;

		Vector2 currentNormalizedPosition = Vector2.zero;
		Vector2 prevSentPosition = Vector2.zero;
        float lastUpdateTime = 0f;

        GameObject dragObject;
        Vector2 dragOffset;
		Vector2 dropLocalPosition;
		Vector2 resetLocalPosition;
        GameObject resetPositionObject;
        float resetPositionStartTime;
        Camera eventCamera;
		int touchId;
		bool hasAngleListeners;

		void OnEnable () {
			dragObject = null;
			hasAngleListeners = OnAngleChange.GetPersistentEventCount () != 0;
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
			OnDragBegin.Invoke ();
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
			OnDragEnd.Invoke ();
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
					float t = (ResetPositionDuration != 0f) ? (Time.time - resetPositionStartTime) / ResetPositionDuration : 1f;
					localPos = Vector2.Lerp (dropLocalPosition, rtrect.center, Interpolation.EaseOutQuad (t));
					resetPositionObject.transform.localPosition = localPos;
					if (t >= 1f) {
						resetPositionObject = null;
						//Make sure position is updated one last time right now
						lastUpdateTime = Time.time - UpdateInterval;
					}
				}

				currentNormalizedPosition = new Vector2 ((localPos.x - rtrect.center.x) / (rtrect.width / 2f), (localPos.y - rtrect.center.y) / (rtrect.height / 2f));
				var mag = currentNormalizedPosition.magnitude;

				if (mag < Deadzone) {
					currentNormalizedPosition = Vector2.zero;
					mag = 0f;
				}

				//Debug.LogFormat ("x={0}, y={1}", currentPosition.x, currentPosition.y);
				var translatedPosition = currentNormalizedPosition * Scale + Offset;

				if (!string.IsNullOrEmpty (VariableNameX)
					&& !string.IsNullOrEmpty (VariableNameY)
					&& (lastUpdateTime + UpdateInterval) <= Time.time
					&& prevSentPosition != translatedPosition) {
					Send (VariableNameX, translatedPosition.x);
					Send (VariableNameY, translatedPosition.y);
					lastUpdateTime = Time.time;
					prevSentPosition = translatedPosition;
				}

				OnDragVector2.Invoke (translatedPosition);
				OnDragVector3_XY.Invoke (new Vector3 (translatedPosition.x, translatedPosition.y));
				OnDragVector3_XZ.Invoke (new Vector3 (translatedPosition.x, 0f, translatedPosition.y));
				OnDragVector3_YZ.Invoke (new Vector3 (0f, translatedPosition.x, translatedPosition.y));

				OnSpeedChange.Invoke (mag);

				if (hasAngleListeners && mag >= Deadzone) {
					var acos = Mathf.Acos (currentNormalizedPosition.y / mag) * 57.29578f;
					if (currentNormalizedPosition.x < 0) {
						acos = 360f - acos;
					}
					OnAngleChange.Invoke (Quaternion.AngleAxis (acos, Vector3.up));
				}
			}
        }
    }

}
