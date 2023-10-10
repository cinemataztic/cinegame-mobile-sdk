using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	[ComponentReference ("Pass through one of a transform's children. Each interval or each time UpdateNow is called, a new child is selected. 'Mode' determines in which order.")]
	public class GetTransformChild : BaseComponent {

		public Transform Source;
		[Tooltip ("Update interval in msecs. 0=One-shot")]
		public float UpdateInterval = 0f;

		public enum SequenceMode {
			Random,
			Sequential,
		}
		public SequenceMode Mode = SequenceMode.Random;

		[Tooltip("Invoked with the selected child")]
		public UnityEvent<Transform> OnUpdate;

		float lastSetTime = float.MinValue;
		int lastIndex = -1;

		private void OnEnable () {
			if (Source == null && UpdateInterval > float.Epsilon) {
				Source = transform;
			}
			if (Source != null) {
				UpdateNow ();
			}
		}

		public void SetSource (Transform s) {
			Source = s;
			OnEnable ();
		}

		public void UpdateNow () {
			lastSetTime = Time.time;
			var index = (lastIndex + ((Mode == SequenceMode.Random) ? Random.Range (1, Source.childCount) : 1)) % Source.childCount;
			var child = Source.GetChild (index);
			Log ($"GetTransformChild.UpdateNow {child.gameObject.GetScenePath ()}");
			lastIndex = index;
			OnUpdate?.Invoke (child);
		}

		private void Update () {
			if (Source != null) {
				if (UpdateInterval > float.Epsilon && (lastSetTime + UpdateInterval) <= Time.time) {
					UpdateNow ();
				}
			}
		}
	}
}
