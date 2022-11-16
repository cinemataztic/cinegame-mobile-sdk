using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using System;

namespace CineGame.MobileComponents {

	/// <summary>
	/// RemoteControl enables game host to control values or invoke methods on this client.
	/// Values can either be set or (if not void or string) can be interpolated linearly over InterpTime duration.
	/// </summary>
	public class RemoteControl : ReplicatedComponent {

		[Tooltip("If this key is in ObjectMessage from host then invoke onReceive")]
		public string Key;

		public enum EventType {
			Void,
			Bool,
			Int,
			Float,
			Long,
			String,
			Vector2,
			Vector3,
			Color,
		}
		public EventType Type = EventType.Void;

		[Tooltip("If this is not 0 the value will be interpolated (if applicable)")]
		public float InterpTime = 0f;

		public Interpolation.Type InterpType = Interpolation.Type.Linear;

		public UnityEvent onReceiveVoid;

		[Serializable] public class BoolEvent : UnityEvent<bool> { }
		public BoolEvent onReceiveBool;

		[Serializable] public class IntEvent : UnityEvent<int> { }
		public IntEvent onReceiveInt;

		[Serializable] public class FloatEvent : UnityEvent<float> { }
		public FloatEvent onReceiveFloat;

		[Serializable] public class LongEvent : UnityEvent<long> { }
		public LongEvent onReceiveLong;

		[Serializable] public class StringEvent : UnityEvent<string> { }
		public StringEvent onReceiveString;

		[Serializable] public class Vector2Event : UnityEvent<Vector2> { }
		public Vector2Event onReceiveVector2;

		[Serializable] public class Vector3Event : UnityEvent<Vector3> { }
		public Vector3Event onReceiveVector3;

		[Serializable] public class ColorEvent : UnityEvent<Color> { }
		public ColorEvent onReceiveColor;

		Color cColor, startColor, destColor;
		Vector3 cV3, startV3, destV3;
		Vector2 cV2, startV2, destV2;
		int	cInt, startInt, destInt;
		float cFloat, startFloat, destFloat;
		float startTime;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			float[] floats;
			//If key is present invoke onReceive
			if (dataObj.ContainsKey (Key)) {
				startTime = Time.time;
				switch (Type) {
				case EventType.Bool:
					LogEventListeners (onReceiveBool);
					onReceiveBool.Invoke (dataObj.GetBool (Key));
					break;
				case EventType.Int:
					destInt = dataObj.GetInt (Key);
					if (InterpTime == 0f) {
						startInt = cInt = destInt;
						LogEventListeners (onReceiveInt);
						onReceiveInt.Invoke (destInt);
					} else {
						startInt = cInt;
					}
					break;
				case EventType.Float:
					destFloat = dataObj.GetFloat (Key);
					if (InterpTime == 0f) {
						startFloat = cFloat = destFloat;
						LogEventListeners (onReceiveFloat);
						onReceiveFloat.Invoke (destFloat);
					} else {
						startFloat = cFloat;
					}
					break;
				case EventType.Long:
					LogEventListeners (onReceiveLong);
					onReceiveLong.Invoke(dataObj.GetLong(Key));
					break;
				case EventType.String:
					LogEventListeners (onReceiveString);
					onReceiveString.Invoke (dataObj.GetUtfString (Key));
					break;
				case EventType.Vector2:
					floats = dataObj.GetFloatArray (Key);
					destV2 = new Vector2 (floats [0], floats [1]);
					if (InterpTime == 0f) {
						startV2 = cV2 = destV2;
						LogEventListeners (onReceiveVector2);
						onReceiveVector2.Invoke (destV2);
					} else {
						startV2 = cV2;
					}
					break;
				case EventType.Vector3:
					floats = dataObj.GetFloatArray (Key);
					destV3 = new Vector3 (floats [0], floats [1], floats[2]);
					if (InterpTime == 0f) {
						startV3 = cV3 = destV3;
						LogEventListeners (onReceiveVector3);
						onReceiveVector3.Invoke (destV3);
					} else {
						startV3 = cV3;
					}
					break;
				case EventType.Color:
					floats = dataObj.GetFloatArray (Key);
					destColor = (floats.Length == 3)? new Color (floats [0], floats [1], floats[2]) : new Color (floats[0], floats[1], floats[2], floats[3]);
					if (InterpTime == 0f) {
						startColor = cColor = destColor;
						LogEventListeners (onReceiveColor);
						onReceiveColor.Invoke (destColor);
					} else {
						startColor = cColor;
					}
					break;
				default:
					LogEventListeners (onReceiveVoid);
					onReceiveVoid.Invoke ();
					break;
				}
			}
		}


		/// <summary>
		/// If this is a debug build, we would like to know all the listeners of a particular event when it happens, formatted as a single multiline log entry
		/// </summary>
		void LogEventListeners (UnityEventBase e) {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} <b>RemoteControl</b> '{1}':\n{2}", Util.GetObjectScenePath (gameObject), Key, Util.GetEventPersistentListenersInfo (e));
			}
		}


		void Update () {
			if (InterpTime > 0f) {
				float t = (Time.time - startTime);
				//Interpolate until we have reached at least t=1f
				if (t - Time.deltaTime < InterpTime) {
					t /= InterpTime;
					var ct = Interpolation.Interp (t, InterpType);
					switch (Type) {
					case EventType.Float:
						cFloat = Mathf.Lerp (startFloat, destFloat, ct);
						onReceiveFloat.Invoke (cFloat);
						break;
					case EventType.Int:
						cInt = (int)(.5f + Mathf.Lerp ((float)startInt, (float)destInt, ct));
						onReceiveInt.Invoke (cInt);
						break;
					case EventType.Vector2:
						cV2 = Vector2.Lerp (startV2, destV2, ct);
						onReceiveVector2.Invoke (cV2);
						break;
					case EventType.Vector3:
						cV3 = Vector3.Lerp (startV3, destV3, ct);
						onReceiveVector3.Invoke (cV3);
						break;
					case EventType.Color:
						cColor = Color.Lerp (startColor, destColor, ct);
						onReceiveColor.Invoke (cColor);
						break;
					default:
						break;
					}
				}
			}
		}
	}
}