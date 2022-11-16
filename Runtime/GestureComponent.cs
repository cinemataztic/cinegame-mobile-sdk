using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	public class GestureComponent : MonoBehaviour, IGameComponentIcon {

		public enum _GestureType {
			SwipeUp,
			SwipeDown,
			SwipeLeft,
			SwipeRight
		}

		public _GestureType GestureType;

		[Tooltip("If >0 then more complex gestures than swipe or touch are supported")]
		public float TimeThreshold = 0f;

		[Tooltip("If bounding box cross-size < this then gesture is ignored")]
		public float AreaThreshold = 10f;

		[Tooltip("Amount of force to apply with the swipe")]
		public float ForceScale = 1f;

		public UnityEvent onFail;

		//This will invoke with a vector2 which can be used as an impulse on rigidbodies via eg SpawnComponent
		[Serializable] public class GestureEvent : UnityEvent<Vector2> { }
		public GestureEvent onSuccess;

		struct GestureSample {
			public Vector2 pos;
			public float time;
		}

		List<GestureSample> gestureSamples = new List<GestureSample> (100);
		Vector2 min, max;

		void Update () {
			if (Input.GetMouseButton(0)) {
				AddSample (Input.mousePosition);
			} else if (Input.GetMouseButtonUp(0)) {
				if (TimeThreshold > 0f) {
					Invoke ("Detect", TimeThreshold);
				} else {
					Detect ();
				}
			}
		}

		void AddSample (Vector2 pos) {
			gestureSamples.Add (new GestureSample { pos = Input.mousePosition, time = Time.time });
		}

		Vector2 GetMidPoint () {
			var mid = Vector2.zero;
			for (int i = 0; i < gestureSamples.Count; i++) {
				mid += gestureSamples [i].pos;
			}
			return mid / gestureSamples.Count;
		}

		void GetBoundingBox (out Vector2 min, out Vector2 max) {
			min = new Vector2 (float.MaxValue, float.MaxValue);
			max = new Vector2 (float.MinValue, float.MinValue);
			for (int i = 0; i < gestureSamples.Count; i++) {
				min = Vector2.Min (min, gestureSamples [i].pos);
				max = Vector2.Max (max, gestureSamples [i].pos);
			}
		}

		void Detect () {
			bool success = (gestureSamples.Count > 0);

			GetBoundingBox (out min, out max);
			success = ((min - max).sqrMagnitude >= AreaThreshold * AreaThreshold);

			if (success) {
				switch (GestureType) {
				case _GestureType.SwipeUp:
					success = DetectSwipeUp ();
					break;
				case _GestureType.SwipeDown:
					success = DetectSwipeDown ();
					break;
				case _GestureType.SwipeRight:
					success = DetectSwipeRight ();
					break;
				case _GestureType.SwipeLeft:
					success = DetectSwipeLeft ();
					break;
				default:
					Debug.LogErrorFormat ("GestureType {0} not supported!", GestureType);
					break;
				}
			}

			gestureSamples.Clear ();
			if (!success) {
				onFail.Invoke ();
			}
		}

		void Success (Vector2 delta, float dt) {
			onSuccess.Invoke (delta * ForceScale / dt);
		}

		bool DetectSwipeUp () {
			var first = gestureSamples [0];
			var last = gestureSamples [gestureSamples.Count - 1];
			if ((max.x - min.x < max.y - min.y) && last.pos.y > first.pos.y) {
				Success (last.pos - first.pos, last.time - first.time);
				return true;
			}
			return false;
		}

		bool DetectSwipeDown () {
			var first = gestureSamples [0];
			var last = gestureSamples [gestureSamples.Count - 1];
			if ((max.x - min.x < max.y - min.y) && last.pos.y < first.pos.y) {
				Success (last.pos - first.pos, last.time - first.time);
				return true;
			}
			return false;
		}

		bool DetectSwipeRight () {
			var first = gestureSamples [0];
			var last = gestureSamples [gestureSamples.Count - 1];
			if ((max.x - min.x < max.y - min.y) && last.pos.x > first.pos.x) {
				Success (last.pos - first.pos, last.time - first.time);
				return true;
			}
			return false;
		}

		bool DetectSwipeLeft () {
			var first = gestureSamples [0];
			var last = gestureSamples [gestureSamples.Count - 1];
			if ((max.x - min.x < max.y - min.y) && last.pos.x < first.pos.x) {
				Success (last.pos - first.pos, last.time - first.time);
				return true;
			}
			return false;
		}
	}

}