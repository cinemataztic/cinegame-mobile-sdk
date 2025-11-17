using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	[ComponentReference ("Invoke methods with the Source Transform's world position, rotation, linear speed or angular speed. Continuous or one-shot. Useful for eg setting and updating a NavMeshAgent destination, or updating a speedometer. If no Source is specified, the GameObject's own Transform will be used.")]
	public class GetTransformProperty : BaseEventComponent {

		public Transform Source;
		[Tooltip ("Update interval in msecs. 0=One-shot")]
		public float UpdateInterval = 0f;
		[Tooltip ("Invoked with the world position of Source")]
		public UnityEvent<Vector3> OnUpdatePosition;
		[Tooltip ("Invoked with the world rotation of Source")]
		public UnityEvent<Quaternion> OnUpdateRotation;
		[Tooltip ("Invoked with the world speed of Source (units per second)")]
		public UnityEvent<float> OnUpdateSpeed;
		[Tooltip ("Invoked with the world angular speed of Source (degrees per second)")]
		public UnityEvent<float> OnUpdateAngularSpeed;

		float lastSetTime = float.MinValue;
		Vector3 lastSourcePosition;
		Quaternion lastSourceRotation;
		float speed, angularSpeed;

		private void OnEnable () {
			if (Source == null && UpdateInterval > float.Epsilon) {
				Source = transform;
			}
			if (Source != null) {
				lastSourcePosition = Source.position;
				lastSourceRotation = Source.rotation;
				speed = 0f;
				angularSpeed = 0f;
				if (UpdateInterval > float.Epsilon)
					UpdateNow ();
			}
		}

		/// <summary>
		/// Set Source transform, from which the properties will be used. Speed will be reset and calculated next frame.
		/// </summary>
		public void SetSource (Transform s) {
			Source = s;
			OnEnable ();
		}

		/// <summary>
		/// Set Source GameObject, from whose transform the properties will be used. Speed will be reset and calculated next frame.
		/// </summary>
		public void SetSource (GameObject s) {
			Source = s.transform;
			OnEnable ();
		}

		/// <summary>
		/// Invoke Update events immediately with the current values of Source
		/// </summary>
		public void UpdateNow () {
			lastSetTime = Time.time;
			Log ($"GetTransformProperty.UpdateNow {Source.gameObject.GetScenePath ()}");
			OnUpdatePosition?.Invoke (Source.position);
			OnUpdateRotation?.Invoke (Source.rotation);
			OnUpdateSpeed?.Invoke (speed);
			OnUpdateAngularSpeed?.Invoke (angularSpeed);
		}

		private void Update () {
			if (Source != null) {
				if (OnUpdateSpeed.GetPersistentEventCount () != 0) {
					var position = Source.position;
					speed = speed * .7f + .3f * (position - lastSourcePosition).magnitude / Time.deltaTime;
					lastSourcePosition = position;
				}
				if (OnUpdateAngularSpeed.GetPersistentEventCount () != 0) {
					var rotation = Source.rotation;
					angularSpeed = angularSpeed * .7f + .3f * Quaternion.Angle (rotation, lastSourceRotation) / Time.deltaTime;
					lastSourceRotation = rotation;
				}

				if (UpdateInterval > float.Epsilon && (lastSetTime + UpdateInterval) <= Time.time) {
					UpdateNow ();
				}
			}
		}
	}
}
