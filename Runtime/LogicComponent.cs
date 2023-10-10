﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Choice component, to reside inside a ChoicesComponent container.
	/// </summary>
	[ComponentReference ("Compare values, distance, angles and trigger events/actions based on these. You can perform simple arithmetic operations and specify whether the events should fire continuously each frame or only when passing a threshold.")]
	public class LogicComponent : BaseComponent {

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

		[Header ("Whether event should fire every update or only when crossing a threshold")]
		public bool Continuous = false;

		[Space]
		[Header ("Format string to invoke OnUpdateString with")]
		[FormerlySerializedAs ("Format")]
		public string StringFormat = "{0:#}";

		[Tooltip ("Invoked with a formatted string using StringFormat property")]
		public UnityEvent<string> OnUpdateString;

		[Space]
		[Tooltip ("Invoked when the value is below the minimum thresholds")]
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

		public void SetValue (float v) {
			//Log ($"LogicComponent.SetValue ({v})");
			Value = v;
			FireEvent ();
		}

		public void SetOther (Transform t) {
			Log ($"LogicComponent.SetOther ({t.gameObject.GetScenePath ()})");
			Other = t;
		}

		public void Add (float v) {
			Log ($"LogicComponent.Add ({v})");
			Value += v;
			FireEvent ();
		}

		public void Subtract (float v) {
			Log ($"LogicComponent.Subtract ({v})");
			Value -= v;
			FireEvent ();
		}

		public void Multiply (float v) {
			Log ($"LogicComponent.Multiply ({v})");
			Value *= v;
			FireEvent ();
		}

		public void Divide (float v) {
			Log ($"LogicComponent.Divide ({v})");
			Value /= v;
			FireEvent ();
		}

		void FireEvent () {
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
				OnBelow?.Invoke (Value);
			} else {
				Log ($"LogicComponent.Thresholds [{thresholdIndex}].OnAbove Value={Value}\n{Util.GetEventPersistentListenersInfo (Thresholds [thresholdIndex].OnAbove)}");
				Thresholds [thresholdIndex].OnAbove?.Invoke (Value);
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