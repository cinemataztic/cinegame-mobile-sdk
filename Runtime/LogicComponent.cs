using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Choice component, to reside inside a ChoicesComponent container.
	/// </summary>
	[ComponentReference ("Compare values, distance, line-of-sight or angles and trigger events/actions based on these. You can perform simple arithmetic operations (Add, Subtract, Multiply, Divide) and specify whether the events should fire continuously each frame or only when passing a threshold.")]
	public class LogicComponent : BaseComponent {

		public enum CompareFunction {
			Value,
			Distance,
			RightLeft,
			UpDown,
			FrontBack,
			LineOfSight,
		}

		public CompareFunction Function = CompareFunction.Value;

		[Header ("Compare distance or dotproduct relative to this transform")]
		public Transform Other;

		[Header ("Value can be Set, Added, Subtracted, Multiplied or Divided")]
		public float Value;

		[Header ("Whether event should fire every update or only when crossing a threshold")]
		public bool Continuous = false;

		[Space]
		[Header ("Format string to invoke OnUpdateString with")]
		[FormerlySerializedAs ("Format")]
		public string StringFormat = "{0:#}";

		[Tooltip ("Invoked with a formatted string using StringFormat property")]
		public UnityEvent<string> OnUpdateString;

		[Space]
		[Tooltip ("Invoked when the value is below the minimum thresholds. If no thresholds are defined, this will always be invoked when Value changes.")]
		public UnityEvent<float> OnBelow;

		[Serializable]
		public class Threshold : IComparable<Threshold> {
			[Tooltip ("Threshold value")]
			public float Value;
			[Tooltip ("Invoked when the value is above the given threshold")]
			public UnityEvent<float> OnAbove;

			public int CompareTo (Threshold b) {
				return Value.CompareTo (b.Value);
			}
		}

		public List<Threshold> Thresholds;

		int CurrentThresholdIndex = int.MinValue;

		public void Start () {
			Thresholds.Sort ();
			UpdateString ();
		}

		/// <summary>
		/// Update the current value to a constant. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void SetValue (float v) {
			//Log ($"LogicComponent.SetValue ({v})");
			Value = v;
			FireEvent ();
		}

		/// <summary>
		/// Set the 'Other' parameter which is used to compare relative position and line-of-sight
		/// </summary>
		public void SetOther (Transform t) {
			Log ($"LogicComponent.SetOther ({t.gameObject.GetScenePath ()})");
			Other = t;
		}

		/// <summary>
		/// Add a constant to the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Add (float v) {
			Log ($"LogicComponent.Add ({v})");
			Value += v;
			FireEvent ();
		}

		/// <summary>
		/// Subtract a constant from the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Subtract (float v) {
			Log ($"LogicComponent.Subtract ({v})");
			Value -= v;
			FireEvent ();
		}

		/// <summary>
		/// Multiply the current value by a constant. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Multiply (float v) {
			Log ($"LogicComponent.Multiply ({v})");
			Value *= v;
			FireEvent ();
		}

		/// <summary>
		/// Divide the current value by a constant. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Divide (float v) {
			Log ($"LogicComponent.Divide ({v})");
			Value /= v;
			FireEvent ();
		}

		/// <summary>
		/// Call to retrigger the current event (useful if non-continuous)
		/// </summary>
		public void RetriggerEvent () {
			CurrentThresholdIndex = int.MinValue;
			FireEvent ();
		}

		/// <summary>
		/// Fire appropriate event acccording to the current value instantly
		/// </summary>
		public void FireEvent () {
			UpdateString ();
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
				Log ($"LogicComponent.OnBelow Value={Value}\n{Util.GetEventPersistentListenersInfo (OnBelow)}");
				//DrawListenersLines (OnBelow, Color.yellow);
				OnBelow.Invoke (Value);
			} else {
				Log ($"LogicComponent.Thresholds [{thresholdIndex}].OnAbove Value={Value}\n{Util.GetEventPersistentListenersInfo (Thresholds [thresholdIndex].OnAbove)}");
                //DrawListenersLines (Thresholds [thresholdIndex].OnAbove, Color.yellow);
                Thresholds [thresholdIndex].OnAbove.Invoke (Value);
			}
			CurrentThresholdIndex = thresholdIndex;
		}

		void UpdateString () {
			if (!string.IsNullOrWhiteSpace (StringFormat)) {
				var fmt = StringFormat;
				if ((int)Value == 0 && fmt.Contains ("{0:#}", System.StringComparison.InvariantCultureIgnoreCase)) {
					fmt = fmt.Replace ("{0:#}", "0");
				}
				var str = string.Format (fmt, Value);
				//Log ($"LogicComponent.OnUpdateString \"{str}\"\n{Util.GetEventPersistentListenersInfo (OnUpdateString)}");
				OnUpdateString?.Invoke (str);
			}
		}

		/// <summary>
		/// Returns 1f if a raycast from this transform's position to Other's position results in a hit on a Collider which contains Other's position.
		/// Otherwise returns 0f.
		/// </summary>
		float Raycast () {
			var thisPosition = transform.position;
			var otherPosition = Other.position;
			if (Physics.Raycast (new Ray (
				thisPosition,
				(otherPosition - thisPosition).normalized
			), out RaycastHit hit, 100, -1)) {
				if (Other == hit.collider.transform || Other.IsChildOf (hit.collider.transform)) {
					DrawLine (thisPosition, hit.point, Color.green);
					return 1f;
				}
                DrawLine (thisPosition, hit.point, Color.red);
            }
            return 0f;
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
				case CompareFunction.LineOfSight:
					Value = Raycast ();
					break;
				}
				FireEvent ();
			}
		}
	}

}