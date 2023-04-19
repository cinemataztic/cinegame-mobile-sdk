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
		[Header ("Threshold can be Set")]
		public float Threshold;
		[Header ("Epsilon for the threshold, .5f is good for integers")]
		public float Epsilon = .5f;

		public Text Text;
		public string Format = "{0:#}";

		public UnityEvent<float> OnBelow;
		public UnityEvent<float> OnAt;
		public UnityEvent<float> OnAbove;

		private enum ValueState {
			Unknown,
			Below,
			At,
			Above,
		}
		private ValueState state;

		public void Start () {
			UpdateText ();
			VerboseDebug &= Debug.isDebugBuild;
			state = ValueState.Unknown;
		}

		public void SetValue (float v) {
			Value = v;
			FireEvent ();
		}

		public void SetOther (Transform t) {
			Other = t;
		}

		public void SetThreshold (float v) {
			Threshold = v;
			FireEvent ();
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
			if (Value < Threshold - Epsilon) {
				if (state != ValueState.Below) {
					state = ValueState.Below;
					if (VerboseDebug) {
						Debug.LogFormat ("{0} CompareComponent.OnBelow\n{1} {2}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnBelow), Value);
					}
					OnBelow.Invoke (Value);
				}
			} else if (Value >= Threshold + Epsilon) {
				if (state != ValueState.Above) {
					state = ValueState.Above;
					if (VerboseDebug) {
						Debug.LogFormat ("{0} CompareComponent.OnAbove\n{1} {2}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnAbove), Value);
					}
					OnAbove.Invoke (Value);
				}
			} else {
				if (state != ValueState.At) {
					state = ValueState.At;
					if (VerboseDebug) {
						Debug.LogFormat ("{0} CompareComponent.OnAt\n{1} {2}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnAt), Value);
					}
					OnAt.Invoke (Value);
				}
			}
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