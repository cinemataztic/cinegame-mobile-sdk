using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Choice component, to reside inside a ChoicesComponent container.
	/// </summary>
	public class CompareComponent : MonoBehaviour, IGameComponentIcon {
		[Header ("Fire events based on current Value compared to Threshold")]
		[Space]
		[Tooltip ("Log events verbosely in editor and debug builds")]
		public bool VerboseDebug = true;

		public enum CompareFunction {
			Value,
			Distance,
			RightLeft,
			UpDown,
			FrontBack,
		}
		public CompareFunction Function = CompareFunction.Value;

		[Header ("Compare distance or dotproduct relative to this transform")]
		public Transform Other;

		[Header ("Value can be Set, Added, Subtracted, Multiplied or Divided")]
		public float Value;

		[Space]
		public Text Text;
		public string Format = "{0:#}";

		[Header ("Whether event should fire every update or only when crossing a threshold")]
		public bool Continuous = false;

		[Space]
		public UnityEvent<float> OnBelow;

		[System.Serializable]
		public class Threshold : IComparable<Threshold> {
			public float Value;
			public UnityEvent<float> OnAbove;

			public int CompareTo (Threshold b) {
				return Value.CompareTo (b.Value);
			}
		}

		public List<Threshold> Thresholds;

		int CurrentThresholdIndex = int.MinValue;

		public void Start () {
			Thresholds.Sort ();
			UpdateText ();
			VerboseDebug &= Debug.isDebugBuild;
		}

		public void SetValue (float v) {
			Value = v;
			FireEvent ();
		}

		public void SetOther (Transform t) {
			Other = t;
		}

		public void Add (float v) {
			Value += v;
			FireEvent ();
		}

		public void Subtract (float v) {
			Value -= v;
			FireEvent ();
		}

		public void Multiply (float v) {
			Value *= v;
			FireEvent ();
		}

		public void Divide (float v) {
			Value /= v;
			FireEvent ();
		}

		void FireEvent () {
			UpdateText ();
			int thresholdIndex = -1;
			foreach (var threshold in Thresholds) {
				if (Value < threshold.Value) {
					break;
				}
				thresholdIndex++;
			}
			if (!Continuous && thresholdIndex == CurrentThresholdIndex)
				return;
			if (thresholdIndex == -1) {
				if (VerboseDebug) {
					Debug.LogFormat ("{0} CompareComponent.OnBelow\n{1} {2}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnBelow), Value);
				}
				OnBelow?.Invoke (Value);
			} else {
				if (VerboseDebug) {
					Debug.LogFormat ("{0} CompareComponent.Thresholds [{1}].OnAbove\n{2} {3}", Util.GetObjectScenePath (gameObject), thresholdIndex, Util.GetEventPersistentListenersInfo (Thresholds [thresholdIndex].OnAbove), Value);
				}
				Thresholds [thresholdIndex].OnAbove?.Invoke (Value);
			}
			CurrentThresholdIndex = thresholdIndex;
		}

		void UpdateText () {
			if (Text != null) {
				var fmt = Format;
				if ((int)Value == 0 && fmt.Contains ("{0:#}", System.StringComparison.InvariantCultureIgnoreCase)) {
					fmt = fmt.Replace ("{0:#}", "0");
				}
				Text.text = string.Format (fmt, Value);
			}
		}

		void Update () {
			if (Function != CompareFunction.Value && Other != null) {
				switch (Function) {
				case CompareFunction.Distance:
					Value = (Other.position - transform.position).magnitude;
					break;
				case CompareFunction.RightLeft:
					Value = Vector3.Dot (transform.right, (Other.position - transform.position).normalized);
					break;
				case CompareFunction.UpDown:
					Value = Vector3.Dot (transform.up, (Other.position - transform.position).normalized);
					break;
				case CompareFunction.FrontBack:
					Value = Vector3.Dot (transform.forward, (Other.position - transform.position).normalized);
					break;
				}
				FireEvent ();
			}
		}
	}

}