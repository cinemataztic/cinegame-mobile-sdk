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

		[Tooltip ("Property from host to listen for")]
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
			Rect,
		}
		[Tooltip ("Property type. Void if just triggering an action")]
		public EventType Type = EventType.Void;

		[Tooltip ("Interpolation time, 0=Snap")]
		public float InterpTime = 0f;

		public Interpolation.Type InterpType = Interpolation.Type.Linear;

		public UnityEvent onReceiveVoid;
		public UnityEvent<bool> onReceiveBool;
		public UnityEvent<int> onReceiveInt;
		public UnityEvent<float> onReceiveFloat;
		public UnityEvent<long> onReceiveLong;
		public UnityEvent<string> onReceiveString;
		public UnityEvent<Vector2> onReceiveVector2;
		public UnityEvent<Vector3> onReceiveVector3;
		public UnityEvent<Color> onReceiveColor;
		public UnityEvent<Quaternion> onReceiveQuaternion;
		public UnityEvent<Rect> onReceiveRect;

		//Interpolation variables
		//
		Quaternion cQuaternion, startQuaternion, destQuaternion;
		Color cColor, startColor, destColor;
		Vector3 cV3, startV3, destV3;
		Vector2 cV2, startV2, destV2;
		Rect cRect, startRect, destRect;
		int	cInt, startInt, destInt;
		long cLong, startLong, destLong;
		float cFloat, startFloat, destFloat;
		float startTime = float.MinValue;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			float[] floats;
			string s;
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
					destLong = dataObj.GetLong (Key);
					LogEvent (onReceiveLong, destLong);
					if (InterpTime == 0f) {
						startLong = cLong = destLong;
						onReceiveLong.Invoke (destLong);
					} else {
						startLong = cLong;
					}
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
				case EventType.Rect:
					floats = dataObj.GetFloatArray (Key);
					destRect = new Rect (floats [0], floats [1], floats [2], floats [3]);
					LogEvent (onReceiveRect, destRect);
					if (InterpTime == 0f) {
						startRect = cRect = destRect;
						onReceiveRect.Invoke (destRect);
					} else {
						startRect = cRect;
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
					case EventType.Long:
						cLong = (long)(.5f + Mathf.Lerp (startLong, destLong, ct));
						onReceiveLong.Invoke (cLong);
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
					case EventType.Rect:
						cRect.size = Vector2.LerpUnclamped (startRect.size, destRect.size, ct);
						cRect.center = Vector2.LerpUnclamped (startRect.center, destRect.center, ct);
						onReceiveRect.Invoke (cRect);
						break;
					default:
						break;
					}
				}
			}
		}
	}
}