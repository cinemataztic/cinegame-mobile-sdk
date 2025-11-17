using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// RemoteControl enables game host to control values or invoke methods on this client.
	/// Values can either be set or (if not void, bool or string) can be interpolated linearly over InterpTime duration.
    /// You can also use the interpolation feature locally.
	/// </summary>
	[ComponentReference ("Control methods and properties remotely from host. The property referenced by Key triggers the event with its value. Make sure to select the correct type (void if N/A).\nValues can either be set or (if not void, bool or string) can be interpolated linearly over InterpTime duration.\nYou can also use the Set methods to use the interpolation feature locally.")]
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

		public void SetInt (int v) {
			Log ("SetInt " + v);
			if (InterpTime != 0f) {
				destInt = v;
				startInt = cInt;
				startTime = Time.time;
			} else {
				cInt = v;
			}
		}

		public void SetFloat (float v) {
			Log ("SetFloat " + v);
			if (InterpTime != 0f) {
				destFloat = v;
				startFloat = cFloat;
				startTime = Time.time;
			} else {
				cFloat = v;
			}
		}

		public void SetVector2 (Vector2 v) {
			Log ("SetVector2 " + v);
			if (InterpTime != 0f) {
				destV2 = v;
				startV2 = cV2;
				startTime = Time.time;
			} else {
				cV2 = v;
			}
		}

		public void SetVector3 (Vector3 v) {
			Log ("SetVector3 " + v);
			if (InterpTime != 0f) {
				destV3 = v;
				startV3 = cV3;
				startTime = Time.time;
			} else {
				cV3 = v;
			}
		}

		public void SetQuaternion (Quaternion v) {
			Log ("SetQuaternion " + v);
			if (InterpTime != 0f) {
				destQuaternion = v;
				startQuaternion = cQuaternion;
				startTime = Time.time;
			} else {
				cQuaternion = v;
			}
		}

		public void SetColor (Color v) {
			Log ("SetColor " + v);
			if (InterpTime != 0f) {
				destColor = v;
				startColor = cColor;
				startTime = Time.time;
			} else {
				cColor = v;
			}
		}

		public void SetRect (Rect v) {
			Log ("SetRect " + v);
			if (InterpTime != 0f) {
				destRect = v;
				startRect = cRect;
				startTime = Time.time;
			} else {
				cRect = v;
			}
		}

		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
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
						cFloat = Mathf.LerpUnclamped (startFloat, destFloat, ct);
						onReceiveFloat.Invoke (cFloat);
						break;
					case EventType.Int:
						cInt = (int)(.5f + Mathf.LerpUnclamped (startInt, destInt, ct));
						onReceiveInt.Invoke (cInt);
						break;
					case EventType.Long:
						cLong = (long)(.5f + Mathf.LerpUnclamped (startLong, destLong, ct));
						onReceiveLong.Invoke (cLong);
						break;
					case EventType.Vector2:
						cV2 = Vector2.LerpUnclamped (startV2, destV2, ct);
						onReceiveVector2.Invoke (cV2);
						break;
					case EventType.Vector3:
						cV3 = Vector3.LerpUnclamped (startV3, destV3, ct);
						onReceiveVector3.Invoke (cV3);
						break;
					case EventType.Color:
						cColor = Color.LerpUnclamped (startColor, destColor, ct);
						onReceiveColor.Invoke (cColor);
						break;
					case EventType.Quaternion:
						cQuaternion = Quaternion.SlerpUnclamped (startQuaternion, destQuaternion, ct);
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