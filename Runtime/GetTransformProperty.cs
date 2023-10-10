﻿using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	[ComponentReference ("Pass through a transform's position or rotation. Continuous or one-shot. Useful for eg setting and updating a NavMeshAgent destination")]
	public class GetTransformProperty : BaseEventComponent {

		public Transform Source;
		[Tooltip ("Update interval in msecs. 0=One-shot")]
		public float UpdateInterval = 0f;
		[Tooltip ("Invoked with the world position of the transform")]
		public UnityEvent<Vector3> OnUpdatePosition;
		[Tooltip ("Invoked with the world rotation of the transform")]
		public UnityEvent<Quaternion> OnUpdateRotation;
		[Tooltip ("Invoked with the world speed of the transform (units per second)")]
		public UnityEvent<float> OnUpdateSpeed;

		float lastSetTime = float.MinValue;
		Vector3 lastSourcePosition;
		float speed;

		private void OnEnable () {
			if (Source != null) {
				lastSourcePosition = Source.position;
				speed = 0f;
				UpdateNow ();
			}
		}

		public void SetSource (Transform s) {
			Source = s;
			OnEnable ();
		}

		public void UpdateNow () {
			lastSetTime = Time.time;
			Log ($"GetTransformProperty.UpdateNow {Source.gameObject.GetScenePath ()}");
			OnUpdatePosition?.Invoke (Source.position);
			OnUpdateRotation?.Invoke (Source.rotation);
			OnUpdateSpeed?.Invoke (speed);
		}

		private void Update () {
			if (Source != null) {
				if (OnUpdateSpeed.GetPersistentEventCount () != 0) {
					var position = Source.position;
					speed = speed * .7f + .3f * (position - lastSourcePosition).magnitude / Time.deltaTime;
					lastSourcePosition = position;
				}

				if (UpdateInterval > float.Epsilon && (lastSetTime + UpdateInterval) <= Time.time) {
					UpdateNow ();
				}
			}
		}
	}
}
