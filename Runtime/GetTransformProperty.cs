using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	[ComponentReference ("Invoke methods with the Source Transform's world position, rotation or speed. Continuous or one-shot. Useful for eg setting and updating a NavMeshAgent destination, or updating a speedometer. If no Source is specified, the GameObject's own Transform will be used.")]
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

		float lastSetTime = float.MinValue;
		Vector3 lastSourcePosition;
		float speed;

		private void OnEnable () {
			if (Source == null && UpdateInterval > float.Epsilon) {
				Source = transform;
			}
			if (Source != null) {
				lastSourcePosition = Source.position;
				speed = 0f;
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
		/// Invoke Update events immediately with the current values of Source
		/// </summary>
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
