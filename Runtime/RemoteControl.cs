using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System;

namespace CineGame.MobileComponents {

	/// <summary>
	/// RemoteControl enables game host to control values or invoke methods on this client.
	/// Values can either be set or (if not void or string) can be interpolated linearly over InterpTime duration.
	/// </summary>
	[ComponentReference ("Control unity objects and properties remotely from host. The property referenced by Key triggers the event with its value. Make sure to select the correct type (void if N/A)")]
	public class RemoteControl : ReplicatedComponent {

		[Header ("Property from host")]
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
			Quaternion,
		}
		[Header ("Property type. Void if just triggering an action")]
		public EventType Type = EventType.Void;

		[Header ("Interpolation time (if applicable), 0=Snap")]
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

		[Serializable] public class QuaternionEvent : UnityEvent<Quaternion> { }
		public QuaternionEvent onReceiveQuaternion;

		//Interpolation variables
		//
		Quaternion cQuaternion, startQuaternion, destQuaternion;
		Color cColor, startColor, destColor;
		Vector3 cV3, startV3, destV3;
		Vector2 cV2, startV2, destV2;
		int	cInt, startInt, destInt;
		float cFloat, startFloat, destFloat;
		float startTime = float.MinValue;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			float[] floats;
			string s;
			long l;
			bool b;
			//If key is present invoke onReceive
			if (dataObj.ContainsKey (Key)) {
				startTime = Time.time;
				switch (Type) {
				case EventType.Bool:
					b = dataObj.GetBool (Key);
					LogEvent (onReceiveBool, b);
					onReceiveBool.Invoke (b);
					break;
				case EventType.Int:
					destInt = dataObj.GetInt (Key);
					LogEvent (onReceiveInt, destInt);
					if (InterpTime == 0f) {
						startInt = cInt = destInt;
						onReceiveInt.Invoke (destInt);
					} else {
						startInt = cInt;
					}
					break;
				case EventType.Float:
					destFloat = dataObj.GetFloat (Key);
					LogEvent (onReceiveFloat, destFloat);
					if (InterpTime == 0f) {
						startFloat = cFloat = destFloat;
						onReceiveFloat.Invoke (destFloat);
					} else {
						startFloat = cFloat;
					}
					break;
				case EventType.Long:
					l = dataObj.GetLong (Key);
					LogEvent (onReceiveLong, l);
					onReceiveLong.Invoke(l);
					break;
				case EventType.String:
					s = dataObj.GetUtfString (Key);
					LogEvent (onReceiveString, s);
					onReceiveString.Invoke (s);
					break;
				case EventType.Vector2:
					floats = dataObj.GetFloatArray (Key);
					destV2 = new Vector2 (floats [0], floats [1]);
					LogEvent (onReceiveVector2, destV2);
					if (InterpTime == 0f) {
						startV2 = cV2 = destV2;
						onReceiveVector2.Invoke (destV2);
					} else {
						startV2 = cV2;
					}
					break;
				case EventType.Vector3:
					floats = dataObj.GetFloatArray (Key);
					destV3 = new Vector3 (floats [0], floats [1], floats[2]);
					LogEvent (onReceiveVector3, destV3);
					if (InterpTime == 0f) {
						startV3 = cV3 = destV3;
						onReceiveVector3.Invoke (destV3);
					} else {
						startV3 = cV3;
					}
					break;
				case EventType.Color:
					floats = dataObj.GetFloatArray (Key);
					destColor = (floats.Length == 3)? new Color (floats [0], floats [1], floats[2]) : new Color (floats[0], floats[1], floats[2], floats[3]);
					LogEvent (onReceiveColor, destColor);
					if (InterpTime == 0f) {
						startColor = cColor = destColor;
						onReceiveColor.Invoke (destColor);
					} else {
						startColor = cColor;
					}
					break;
				case EventType.Quaternion:
					floats = dataObj.GetFloatArray (Key);
					destQuaternion = new Quaternion (floats [0], floats [1], floats [2], floats [3]);
					LogEvent (onReceiveQuaternion, destQuaternion);
					if (InterpTime == 0f) {
						startQuaternion = cQuaternion = destQuaternion;
						onReceiveQuaternion.Invoke (destQuaternion);
					} else {
						startQuaternion = cQuaternion;
					}
					break;
				default:
					LogEvent (onReceiveVoid, null);
					onReceiveVoid.Invoke ();
					break;
				}
			}
		}


		/// <summary>
		/// If this is a debug build, we would like to know all the listeners of a particular event when it happens, formatted as a single multiline log entry
		/// </summary>
		void LogEvent (UnityEventBase e, object value) {
			Log ("RemoteControl{0} {1}={2}\n{3}", (InterpTime > float.Epsilon)? " Interpolate" : string.Empty,  Key, value, Util.GetEventPersistentListenersInfo (e));
		}


		void Update () {
			if (InterpTime > float.Epsilon) {
				float t = (Time.time - startTime);
				//Interpolate until we have reached at least t=1f
				if (t - Time.deltaTime < InterpTime) {
					t /= InterpTime;
					var ct = Interpolation.Interp (Mathf.Min (t, 1f), InterpType);
					switch (Type) {
					case EventType.Float:
						cFloat = Mathf.Lerp (startFloat, destFloat, ct);
						onReceiveFloat.Invoke (cFloat);
						break;
					case EventType.Int:
						cInt = (int)(.5f + Mathf.Lerp (startInt, destInt, ct));
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
					case EventType.Quaternion:
						cQuaternion = Quaternion.Slerp (startQuaternion, destQuaternion, ct);
						onReceiveQuaternion.Invoke (cQuaternion);
						break;
					default:
						break;
					}
				}
			}
		}
	}
}