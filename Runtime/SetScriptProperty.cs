using System;
using System.Reflection;
using UnityEngine;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Control a script property from events. For instance, you can route the value of a LogicComponent into a field, property or a single-argument method on a script
	/// </summary>
	[ComponentReference ("Control a script property from events. For instance, you can route the value of a LogicComponent into a field, property or a single-argument method on a script-- or you can route the collider from an OnCollision event into a script's Object property.\nThe SetValue method is a bit expensive so don't use it too often each frame, better to use an alternative if possible.")]
	public class SetScriptProperty : BaseComponent {

		public UnityEngine.Object ScriptObject;
		public string ScriptPropertyName;

		FieldInfo FieldInfo;
		PropertyInfo PropertyInfo;
		MethodInfo MethodInfo;

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
		readonly object [] MethodParams = new object [1];

		const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		void Start () {
			if (ScriptObject != null && !string.IsNullOrWhiteSpace (ScriptPropertyName)) {
				var type = ScriptObject.GetType ();
				Type pType;
				FieldInfo = type.GetField (ScriptPropertyName, _bindingFlags);
				if (FieldInfo != null) {
					pType = FieldInfo.FieldType;
				} else {
					PropertyInfo = type.GetProperty (ScriptPropertyName, _bindingFlags);
					if (PropertyInfo != null) {
						pType = PropertyInfo.PropertyType;
					} else {
						MethodInfo = type.GetMethod (ScriptPropertyName, _bindingFlags);
						if (MethodInfo != null) {
							pType = MethodInfo.GetParameters () [0].ParameterType;
						} else {
							LogError ($"{type.FullName}.{ScriptPropertyName} not found!");
							return;
						}
					}
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
			if (FieldInfo != null) {
				FieldInfo.SetValue (ScriptObject, v);
			} else if (PropertyInfo != null) {
				PropertyInfo.SetValue (ScriptObject, v);
			} else {
				MethodParams [0] = v;
				MethodInfo.Invoke (ScriptObject, MethodParams);
			}
		}

		public void SetInt (int v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Int32) {
				LogError ($"Property {ScriptPropertyName} is not an integer!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetFloat (float v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Single) {
				LogError ($"Property {ScriptPropertyName} is not a float!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetVector2 (Vector2 v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Vector2) {
				LogError ($"Property {ScriptPropertyName} is not a Vector2!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetVector3 (Vector3 v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Vector3) {
				LogError ($"Property {ScriptPropertyName} is not a Vector3!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetQuaternion (Quaternion v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Quaternion) {
				LogError ($"Property {ScriptPropertyName} is not a Quaternion!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetColor (Color v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Color) {
				LogError ($"Property {ScriptPropertyName} is not a Color!");
				return;
			}
#endif
			Log ("SetValue " + v);
				SetObjValue (v);
		}

		public void SetObject (UnityEngine.Object v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Object) {
				LogError ($"Property {ScriptPropertyName} is not an Object!");
				return;
			}
#endif
			//Convenient transformation of GameObject to Transform and vice versa
			if (v is GameObject go && propertyType == PropertyType.Transform) {
				v = go.transform;
			} else if (v is Transform t && propertyType == PropertyType.GameObject) {
				v = t.gameObject;
			}
			Log("SetValue " + ((v != null) ? v.name : "null"));
			SetObjValue(v);
		}

		public void SetBool (bool v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.Boolean) {
				LogError ($"Property {ScriptPropertyName} is not a bool!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetString (string v) {
#if UNITY_EDITOR
			if (propertyType != PropertyType.String) {
				LogError ($"Property {ScriptPropertyName} is not a string!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}
	}

}