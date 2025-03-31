using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Compare values, distance, dot products, line-of-sight or angles and invoke events based on these. You can perform simple arithmetic operations (Add, Subtract, Multiply, Divide), boolean (And, Or, Xor) and specify whether the events should fire continuously at intervals or only when passing a threshold. You can sample a property, field or method of a SourceObject at intervals. If SourceObject is not specified, then this transform is used for spatial comparisons.
	/// </summary>
	[ComponentReference ("Compare values, distance, dot products, line-of-sight or angles and invoke events based on these. You can perform simple arithmetic operations (Add, Subtract, Multiply, Divide), boolean (And, Or, Xor) and specify whether the events should fire continuously at intervals or only when passing a threshold. You can sample a property, field or method of a SourceObject at intervals. If SourceObject is not specified, then this transform is used for spatial comparisons.")]
	public class LogicComponent : BaseComponent {

		public enum CompareFunction {
			Value,
			Distance,
			RightLeft,
			UpDown,
			FrontBack,
			LineOfSight,
			Angle,
		}

		public CompareFunction Function = CompareFunction.Value;

		[SerializeField]
		[Tooltip ("Initial value")]
		private float Value;

		[Tooltip ("Layers to intersect with during LineOfSight check")]
		public LayerMask LayerMask = -1;

		[Tooltip ("The source object to compare position, orientation or values from. If None then this GameObject's position and orientation will be used for spatial comparison")]
		public UnityEngine.Object SourceObject;
		public string SourceMemberName;

		[Tooltip ("Compare distance, dotproduct, angle or line-of-sight with this transform")]
		public Transform Other;

		[Tooltip ("How often to update. 0=every frame (can be expensive if using line-of-sight or source property)")]
		public float Interval;

		[Tooltip ("Whether event should fire every interval or only when crossing a threshold")]
		public bool Continuous = false;

		[Tooltip ("The tolerance of thresholds. Use this to dampen the events, eg if a distance value is \"flickering\" around a threshold, you may want a tolerance of +/- .1")]
		[Range (0f, 10f)]
		public float Tolerance = 0f;

		[Space]
		[Tooltip ("Format string to invoke OnUpdateString with")]
		[FormerlySerializedAs ("Format")]
		public string StringFormat = "{0:#}";

		[Tooltip ("Invoked with a formatted string using StringFormat property")]
		public UnityEvent<string> OnUpdateString;

		[Space]
		[Tooltip ("Invoked when the value is below the minimum threshold. If no thresholds are defined, this will always be invoked when Value changes.")]
		public UnityEvent<float> OnBelow;

		[Serializable]
		public class Threshold : IComparable<Threshold> {
			[Tooltip ("Threshold value")]
			public float Value;
			[Tooltip ("Invoked when the value is at or above the given threshold")]
			public UnityEvent<float> OnAbove;

			public int CompareTo (Threshold b) {
				return Value.CompareTo (b.Value);
			}
		}

		public List<Threshold> Thresholds;

		int CurrentThresholdIndex = int.MinValue;

		Transform sourceTransform;

		FieldInfo SourceFieldInfo;
		PropertyInfo SourcePropertyInfo;
		MethodInfo SourceMethodInfo;
		readonly object [] SourceMethodParams = new object [0];
		/// <summary>
        /// True if we should use reflection to update Value each Interval
        /// </summary>
		bool isValueDynamic;

		float _nextUpdateTime;

		void Start () {
			_nextUpdateTime = Time.time + UnityEngine.Random.Range(0f, Interval);

			Thresholds.Sort ();
			UpdateString ();

			if (Function != CompareFunction.Value) {
				sourceTransform = SourceObject != null ? SourceObject as Transform : transform;
			}

			if (SourceObject != null && !string.IsNullOrWhiteSpace (SourceMemberName)) {
				isValueDynamic = true;
				var type = SourceObject.GetType ();
				SourceFieldInfo = type.GetField (SourceMemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				if (SourceFieldInfo == null) {
					SourcePropertyInfo = type.GetProperty (SourceMemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					if (SourcePropertyInfo == null) {
						SourceMethodInfo = type.GetMethod (SourceMemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
						if (SourceMethodInfo == null) {
							LogError ($"{type.FullName}.{SourceMemberName} not found!");
							isValueDynamic = false;
						}
					}
				}
			}
		}

		/// <summary>
		/// Set the 'Other' transform used to compare relative position, angles and line-of-sight
		/// </summary>
		public void SetOther (Component c) {
			Log ($"LogicComponent.SetOther ({c.gameObject.GetScenePath ()})");
			Other = c.transform;
			FireEvent ();
		}

		/// <summary>
		/// Set the 'Other' transform used to compare relative position, angles and line-of-sight
		/// </summary>
		public void SetOther (GameObject go) {
			Log($"LogicComponent.SetOther ({go.GetScenePath ()})");
			Other = go.transform;
			FireEvent ();
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
		/// Update the current value to a constant. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void SetValue (int v) {
			//Log ($"LogicComponent.SetValue ({v})");
			Value = v;
			FireEvent ();
		}

		/// <summary>
		/// Update the current value to a constant (bool true=1, false=0). If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void SetValue (bool v) {
			//Log ($"LogicComponent.SetValue ({v})");
			Value = v ? 1f : 0f;
			FireEvent ();
		}

		/// <summary>
		/// Add to the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Add (float v) {
			Log ($"LogicComponent.Add ({v})");
			Value += v;
			FireEvent ();
		}

		/// <summary>
		/// Add an integer to the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Add (int v) {
			Log ($"LogicComponent.Add ({v})");
			Value += v;
			FireEvent ();
		}

		/// <summary>
		/// Subtract from the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Subtract (float v) {
			Log ($"LogicComponent.Subtract ({v})");
			Value -= v;
			FireEvent ();
		}

		/// <summary>
		/// Subtract an integer from the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Subtract (int v) {
			Log ($"LogicComponent.Subtract ({v})");
			Value -= v;
			FireEvent ();
		}

		/// <summary>
		/// Multiply the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Multiply (float v) {
			Log ($"LogicComponent.Multiply ({v})");
			Value *= v;
			FireEvent ();
		}

		/// <summary>
		/// Multiply the current value by an integer. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Multiply (int v) {
			Log ($"LogicComponent.Multiply ({v})");
			Value *= v;
			FireEvent ();
		}

		/// <summary>
		/// Divide the current value. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Divide (float v) {
			Log ($"LogicComponent.Divide ({v})");
			Value /= v;
			FireEvent ();
		}

		/// <summary>
		/// Divide the current value by an integer. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Divide (int v) {
			Log ($"LogicComponent.Divide ({v})");
			Value /= v;
			FireEvent ();
		}

		/// <summary>
		/// Logic AND the clamped value [0:1] with a constant bool. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void And (bool v) {
			Log ($"LogicComponent.And ({v})");
			if (Value < 0 || !v) Value = 0;
			else if (Value > 1) Value = 1;
			FireEvent ();
		}

		/// <summary>
		/// Logic OR the clamped value [0:1] with a constant bool. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Or (bool v) {
			Log ($"LogicComponent.Or ({v})");
			if (Value < 0) Value = 0;
			if (v) Value += 1;
			if (Value > 1) Value = 1;
			FireEvent ();
		}

		/// <summary>
		/// Logic XOR the clamped value [0:1] with a constant bool. If a threshold is passed, trigger the appropriate event
		/// </summary>
		public void Xor (bool v) {
			Log ($"LogicComponent.Xor ({v})");
			if (Value < 0) Value = 0;
			if (v) Value += 1;
			if (Value > 1) Value = 0;
			FireEvent ();
		}

		/// <summary>
		/// Call to retrigger the current event (useful if non-continuous)
		/// </summary>
		public void RetriggerEvent () {
			CurrentThresholdIndex = int.MinValue;
			if (!isValueDynamic) {
				FireEvent ();
			} else {
				//Value is dynamically read from a property and FireEvent called during next Update
				_nextUpdateTime = Time.time;
			}
		}

		/// <summary>
		/// Fire appropriate event acccording to the current value instantly
		/// </summary>
		public void FireEvent () {
			UpdateString ();
			int thresholdIndex = -1;
			foreach (var threshold in Thresholds) {
				if (CurrentThresholdIndex <= thresholdIndex) {
					if (Value < threshold.Value + Tolerance)
						break;
				} else if (Value < threshold.Value - Tolerance)
					break;
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
				if ((int)Value == 0 && fmt.Contains ("{0:#}", StringComparison.InvariantCultureIgnoreCase)) {
					fmt = fmt.Replace ("{0:#}", "0");
				}
				var str = string.Format (fmt, Value);
				//Log ($"LogicComponent.OnUpdateString \"{str}\"\n{Util.GetEventPersistentListenersInfo (OnUpdateString)}");
				OnUpdateString?.Invoke (str);
			}
		}

		/// <summary>
		/// Returns 1f if a raycast from sourceTransform's position to Other's position results in a hit on a Collider which contains Other's position.
		/// Otherwise returns 0f.
		/// </summary>
		float Raycast () {
			var sourcePosition = sourceTransform.position;
			var delta = Other.position - sourcePosition;
			var l = delta.magnitude;
			var dir = new Vector3 (delta.x / l, delta.y / l, delta.z / l);
			if (Physics.Raycast (new Ray (
				sourcePosition,
				dir
			), out RaycastHit hit, l, LayerMask)) {
				if (Other == hit.collider.transform || Other.IsChildOf (hit.collider.transform)) {
					DrawLine (sourcePosition, hit.point, Color.green);
					return 1f;
				}
                DrawLine (sourcePosition, hit.point, Color.red);
            }
            return 0f;
		}

		void Update () {
			var _time = Time.time;
			if (_nextUpdateTime > _time)
				return;
			_nextUpdateTime = _time + Interval;

			if (Function == CompareFunction.Value) {
				if (!isValueDynamic)
					return;
				object _v;
				if (SourceFieldInfo != null) {
					_v = SourceFieldInfo.GetValue (SourceObject);
				} else if (SourcePropertyInfo != null) {
					_v = SourcePropertyInfo.GetValue (SourceObject);
				} else {
					_v = SourceMethodInfo.Invoke (SourceObject, SourceMethodParams);
				}
				Value = Convert.ToSingle (_v);
				FireEvent ();
			} else if (Other != null) {
				switch (Function) {
				case CompareFunction.Distance:
					Value = (Other.position - sourceTransform.position).magnitude;
					break;
				case CompareFunction.RightLeft:
					Value = Vector3.Dot (sourceTransform.right, (Other.position - sourceTransform.position).normalized);
					break;
				case CompareFunction.UpDown:
					Value = Vector3.Dot (sourceTransform.up, (Other.position - sourceTransform.position).normalized);
					break;
				case CompareFunction.FrontBack:
					Value = Vector3.Dot (sourceTransform.forward, (Other.position - sourceTransform.position).normalized);
					break;
				case CompareFunction.LineOfSight:
					Value = Raycast ();
					break;
				case CompareFunction.Angle:
					Value = Vector3.Angle (sourceTransform.forward, Other.forward);
					break;
				}
				FireEvent ();
			}
		}
	}

}