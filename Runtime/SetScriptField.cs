using System;
using System.Reflection;
using UnityEngine;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Set or interpolate a script field from events. For instance, you can route the value of a LogicComponent into a field on a script-- or you can route the colliding GameObject from an OnCollision event into a script's Object field.\nThe interpolation feature is a bit expensive so don't use it too much each and with short intervals, better to use an alternative if possible.
	/// </summary>
	[ComponentReference ("Control a script field from events. For instance, you can route the value of a LogicComponent into a field on a script-- or you can route the collider from an OnCollision event into a script's Object field.\nThe interpolation feature is a bit expensive so don't use it too much and with short intervals, better to use an alternative if possible.")]
	public class SetScriptField : BaseComponent {

		public UnityEngine.Object ScriptObject;
		public string ScriptFieldName;

		[Tooltip ("Interpolation time, 0=Snap")]
		public float InterpTime = 0f;
		public Interpolation.Type InterpType = Interpolation.Type.Linear;

		FieldInfo FieldInfo;

		public enum PropertyType {
			Boolean,
			Int32,
			Int64,
			Single,
			String,
			Vector2,
			Vector3,
			Color,
			Quaternion,
			Rect,
			Object,
			Transform,
			GameObject,
		}
		PropertyType propertyType;

		//Interpolation variables
		//
		Quaternion cQuaternion, startQuaternion, destQuaternion;
		Color cColor, startColor, destColor;
		Vector3 cV3, startV3, destV3;
		Vector2 cV2, startV2, destV2;
		Rect cRect, startRect, destRect;
		int cInt, startInt, destInt;
		long cLong, startLong, destLong;
		float cFloat, startFloat, destFloat;
		float startTime = float.MinValue;

		const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		void Start () {
			if (ScriptObject != null && !string.IsNullOrWhiteSpace (ScriptFieldName)) {
				var type = ScriptObject.GetType ();
				Type pType;
				FieldInfo = type.GetField (ScriptFieldName, _bindingFlags);
				if (FieldInfo != null) {
					pType = FieldInfo.FieldType;
				} else {
					LogError ($"{type.FullName}.{ScriptFieldName} field not found!");
					return;
				}
				var pTypeString = pType.ToString ();
				pTypeString = pTypeString.Substring (pTypeString.LastIndexOf ('.') + 1);
				if (!Enum.TryParse (pTypeString, out propertyType)) {
					propertyType = PropertyType.Object;
				}
			}
		}

		void SetObjValue (object v) {
			if (!enabled)
				return;
			FieldInfo.SetValue (ScriptObject, v);
		}

		public void SetInt (int v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Int32) {
				LogError ($"{ScriptFieldName} is not an integer!");
				return;
			}
#endif
			Log ("SetInt " + v);
			if (InterpTime != 0f) {
				destInt = v;
				startInt = cInt;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetLong (long v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Int64) {
				LogError ($"{ScriptFieldName} is not a long!");
				return;
			}
#endif
			Log ("SetLong " + v);
			if (InterpTime != 0f) {
				destLong = v;
				startLong = cLong;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetFloat (float v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Single) {
				LogError ($"{ScriptFieldName} is not a float!");
				return;
			}
#endif
			Log ("SetFloat " + v);
			if (InterpTime != 0f) {
				destFloat = v;
				startFloat = cFloat;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetVector2 (Vector2 v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Vector2) {
				LogError ($"{ScriptFieldName} is not a Vector2!");
				return;
			}
#endif
			Log ("SetVector2 " + v);
			if (InterpTime != 0f) {
				destV2 = v;
				startV2 = cV2;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetVector3 (Vector3 v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Vector3) {
				LogError ($"{ScriptFieldName} is not a Vector3!");
				return;
			}
#endif
			Log ("SetVector3 " + v);
			if (InterpTime != 0f) {
				destV3 = v;
				startV3 = cV3;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetQuaternion (Quaternion v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Quaternion) {
				LogError ($"{ScriptFieldName} is not a Quaternion!");
				return;
			}
#endif
			Log ("SetQuaternion " + v);
			if (InterpTime != 0f) {
				destQuaternion = v;
				startQuaternion = cQuaternion;
				startTime = Time.time;
			} else {
				SetObjValue(v);
			}
		}

		public void SetColor (Color v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Color) {
				LogError ($"{ScriptFieldName} is not a Color!");
				return;
			}
#endif
			Log ("SetColor " + v);
			if (InterpTime != 0f) {
				destColor = v;
				startColor = cColor;
				startTime = Time.time;
			} else {
				SetObjValue (v);
			}
		}

		public void SetObject (UnityEngine.Object v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Object) {
				LogError ($"{ScriptFieldName} is not an Object!");
				return;
			}
#endif
			//Convenient transformation of GameObject to Transform and vice versa
			if (v is GameObject go && propertyType == PropertyType.Transform) {
				v = go.transform;
			} else if (v is Transform t && propertyType == PropertyType.GameObject) {
				v = t.gameObject;
			}
			Log("SetObject " + ((v != null) ? v.name : "null"));
			SetObjValue(v);
		}

		public void SetBool (bool v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Boolean) {
				LogError ($"{ScriptFieldName} is not a bool!");
				return;
			}
#endif
			Log ("SetBool " + v);
			SetObjValue (v);
		}

		public void SetString (string v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.String) {
				LogError ($"{ScriptFieldName} is not a string!");
				return;
			}
#endif
			Log ("SetString " + v);
			SetObjValue (v);
		}

		void Update () {
			if (InterpTime > float.Epsilon) {
				float t = (Time.time - startTime);
				//Interpolate until we have reached at least t=1f
				if (t - Time.deltaTime < InterpTime) {
					t /= InterpTime;
					var ct = Interpolation.Interp (Mathf.Min (t, 1f), InterpType);
					switch (propertyType) {
					case PropertyType.Single:
						cFloat = Mathf.LerpUnclamped (startFloat, destFloat, ct);
						SetObjValue (cFloat);
						break;
					case PropertyType.Int32:
						cInt = (int)(.5f + Mathf.LerpUnclamped (startInt, destInt, ct));
						SetObjValue (cInt);
						break;
					case PropertyType.Int64:
						cLong = (long)(.5f + Mathf.LerpUnclamped (startLong, destLong, ct));
						SetObjValue (cLong);
						break;
					case PropertyType.Vector2:
						cV2 = Vector2.LerpUnclamped (startV2, destV2, ct);
						SetObjValue (cV2);
						break;
					case PropertyType.Vector3:
						cV3 = Vector3.LerpUnclamped (startV3, destV3, ct);
						SetObjValue (cV3);
						break;
					case PropertyType.Color:
						cColor = Color.LerpUnclamped (startColor, destColor, ct);
						SetObjValue (cColor);
						break;
					case PropertyType.Quaternion:
						cQuaternion = Quaternion.SlerpUnclamped (startQuaternion, destQuaternion, ct);
						SetObjValue (cQuaternion);
						break;
					case PropertyType.Rect:
						cRect.size = Vector2.LerpUnclamped (startRect.size, destRect.size, ct);
						cRect.center = Vector2.LerpUnclamped (startRect.center, destRect.center, ct);
						SetObjValue (cRect);
						break;
					default:
						break;
					}
				}
			}
		}
	}

}