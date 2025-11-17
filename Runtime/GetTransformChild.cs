using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {

	[ComponentReference ("Pass through one of a transform's children. Each interval or each time UpdateNow is called, a new child is selected. 'Mode' determines in which order.")]
	public class GetTransformChild : BaseEventComponent {

		[Tooltip ("The parent of the children. If none specified, this gameobject is used as Source")]
		public Transform Source;
		[Tooltip ("Update interval in msecs. 0=One-shot")]
		public float UpdateInterval = 0f;

		public enum SequenceMode {
			Random,
			Sequential,
			BackSequential,
			RandomSequential,
		}
		public SequenceMode Mode = SequenceMode.Random;

		[Tooltip("Invoked with the selected child")]
		[FormerlySerializedAs ("OnUpdate")]
		[FormerlySerializedAs ("OnSelect")]
		public UnityEvent<Transform> OnSelectTransform;

		[Tooltip ("Invoked with the selected child")]
		public UnityEvent<GameObject> OnSelectGameObject;

		[Tooltip ("Invoked with the previously selected child when a new one is selected. Eg for activating only one at a time")]
		[FormerlySerializedAs ("OnDeselect")]
		public UnityEvent<Transform> OnDeselectTransform;

		[Tooltip ("Invoked with the previously selected child when a new one is selected. Eg for activating only one at a time")]
		public UnityEvent<GameObject> OnDeselectGameObject;

		float lastSetTime = float.MinValue;
		int lastIndex = -1;
		bool backSequentialUpdate;

		int [] RandomSequentialIndices;
		int RandomSequentialCounter;

		void OnEnable () {
			if (Source == null && UpdateInterval > float.Epsilon) {
				Source = transform;
			}
			if (Mode == SequenceMode.RandomSequential) {
				InitRandomSequentialIndices ();
			}
			lastIndex = -1;
			backSequentialUpdate = false;
			if (Source != null) {
				UpdateNow ();
			}
		}

		/// <summary>
        /// Set a new Source transform to get children from
        /// </summary>
		public void SetSource (Transform s) {
			Source = s;
			OnEnable ();
		}

		/// <summary>
		/// Set a new Source transform to get children from
		/// </summary>
		public void SetSource (GameObject s) {
			Source = s.transform;
			OnEnable ();
		}

		/// <summary>
		/// Choose a new child now
		/// </summary>
		public void UpdateNow () {
			lastSetTime = Time.time;
			var newIndex = 0;
			switch (Mode) {
			case SequenceMode.Sequential:
				newIndex = (lastIndex + 1) % Source.childCount;
				break;
			case SequenceMode.BackSequential:
				newIndex = lastIndex;
				if (backSequentialUpdate) newIndex--;
				backSequentialUpdate = true;
				if (newIndex < 0)
					newIndex += Source.childCount;
				break;
			case SequenceMode.RandomSequential:
				newIndex = RandomSequentialIndices [RandomSequentialCounter++];
				if (RandomSequentialCounter >= RandomSequentialIndices.Length)
					RandomSequentialCounter = 0;
				break;
			case SequenceMode.Random:
				newIndex = Random.Range (0, Source.childCount);
				break;
			}
			var child = Source.GetChild (newIndex);
			Log ($"GetTransformChild.UpdateNow lastIndex={lastIndex} newIndex={newIndex} name={child.name}");
			if (lastIndex >= 0 && lastIndex < Source.childCount) {
				var lastChild = Source.GetChild (lastIndex);
				OnDeselectTransform?.Invoke (lastChild);
				OnDeselectGameObject?.Invoke (lastChild.gameObject);
			}
			lastIndex = newIndex;
			OnSelectTransform?.Invoke (child);
			OnSelectGameObject?.Invoke (child.gameObject);
		}

		/// <summary>
		/// Activate the specified transform and deactivate all its siblings
		/// </summary>
		public void ActivateSingle (Transform t) {
			var parent = t.parent;
			var index = t.GetSiblingIndex ();
			for (int i = 0; i < parent.childCount; i++) {
				parent.GetChild (i).gameObject.SetActive (i == index);
			}
		}

		/// <summary>
        /// Activate the transform's gameobject
		/// </summary>
		public void Activate (Transform t) {
			t.gameObject.SetActive (true);
		}

		/// <summary>
		/// Deactivate the transform's gameobject
		/// </summary>
		public void Deactivate (Transform t) {
			t.gameObject.SetActive (false);
		}

		/// <summary>
		/// All children of Source will be activated or deactivated
		/// </summary>
		public void SetActiveChildren (bool active) {
			for (int i = 0; i < Source.childCount; i++) {
				Source.GetChild (i).gameObject.SetActive (active);
			}
		}

		void Update () {
			if (Source != null) {
				if (UpdateInterval > float.Epsilon && (lastSetTime + UpdateInterval) <= Time.time) {
					UpdateNow ();
				}
			}
		}

		/// <summary>
        /// Init array of child indices for the random sequential mode
        /// </summary>
		void InitRandomSequentialIndices () {
			var num = Source.childCount;
			RandomSequentialIndices = new int [num];
			RandomSequentialCounter = 0;
			for (int i = 0; i < num; i++) {
				RandomSequentialIndices [i] = i;
			}
			for (int i = 0; i < num; i++) {
				var randomIndex = Random.Range (0, num);
				if (i == randomIndex)
					continue;
				var idx = RandomSequentialIndices [randomIndex];
				RandomSequentialIndices [randomIndex] = RandomSequentialIndices [i];
				RandomSequentialIndices [i] = idx;
			}
		}

		void OnTransformChildrenChanged () {
			if (Source == transform && Mode == SequenceMode.RandomSequential) {
				InitRandomSequentialIndices ();
			}
        }
    }
}
