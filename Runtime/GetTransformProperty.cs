using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	[ComponentReference ("Pass through a transform's position or rotation. Continuous or one-shot. Useful for eg setting and updating a NavMeshAgent destination")]
	public class GetTransformProperty : BaseComponent {

		public Transform Source;
		[Tooltip ("Update interval in msecs. 0=One-shot")]
		public float UpdateInterval = 0f;
		public UnityEvent<Vector3> OnUpdatePosition;
		public UnityEvent<Quaternion> OnUpdateRotation;

		float lastSetTime = float.MinValue;

		private void OnEnable () {
			if (Source != null) {
				UpdateNow ();
			}
		}

		public void SetSource (Transform s) {
			Source = s;
			UpdateNow ();
		}

		public void UpdateNow () {
			lastSetTime = Time.time;
			Log ($"GetTransformProperty.UpdateNow {Source.gameObject.GetScenePath ()}");
			OnUpdatePosition?.Invoke (Source.position);
			OnUpdateRotation?.Invoke (Source.rotation);
		}

		private void Update () {
			if (Source != null && UpdateInterval > float.Epsilon && (lastSetTime + UpdateInterval) >= Time.time) {
				UpdateNow ();
			}
		}
	}
}
