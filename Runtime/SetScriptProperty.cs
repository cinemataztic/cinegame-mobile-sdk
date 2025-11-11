using System;
using System.Reflection;
using UnityEngine;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Control a script property from events. For instance, you can route the value of a LogicComponent into a field, property or a single-argument method on a script
	/// </summary>
	[ComponentReference ("Control a script property from events. For instance, you can route the value of a LogicComponent into a field, property or a single-argument method on a script-- or you can route the collider from an OnCollision event into a script's Object property")]
	public class SetScriptProperty : BaseComponent {

		public UnityEngine.Object ScriptObject;
		public string ScriptPropertyName;

		FieldInfo FieldInfo;
		PropertyInfo PropertyInfo;
		MethodInfo MethodInfo;
		Type PropertyType;
		readonly object [] MethodParams = new object [1];

		void Start () {
			if (ScriptObject != null && !string.IsNullOrWhiteSpace (ScriptPropertyName)) {
				var type = ScriptObject.GetType ();
				FieldInfo = type.GetField (ScriptPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				if (FieldInfo != null) {
					PropertyType = FieldInfo.FieldType;
				} else {
					PropertyInfo = type.GetProperty (ScriptPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					if (PropertyInfo != null) {
						PropertyType = FieldInfo.FieldType;
					} else {
						MethodInfo = type.GetMethod (ScriptPropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
						if (MethodInfo != null) {
							PropertyType = MethodInfo.ReturnType;
						} else {
							LogError ($"{type.FullName}.{ScriptPropertyName} not found!");
						}
					}
				}
			}
		}

		void SetObjValue (object v) {
			if (FieldInfo != null) {
				FieldInfo.SetValue (ScriptObject, v);
			} else if (PropertyInfo != null) {
				PropertyInfo.SetValue (ScriptObject, v);
			} else {
				MethodParams [0] = v;
				MethodInfo.Invoke (ScriptObject, MethodParams);
			}
		}

		public void SetValue (int v) {
			if (PropertyType != typeof (int)) {
				LogError ($"Property {ScriptPropertyName} is not an integer!");
				return;
			}
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetValue (float v) {
			if (PropertyType != typeof (float)) {
				LogError ($"Property {ScriptPropertyName} is not a float!");
				return;
			}
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetValue (bool v) {
			if (PropertyType != typeof (bool)) {
				LogError ($"Property {ScriptPropertyName} is not a bool!");
				return;
			}
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetValue (Vector2 v) {
			if (PropertyType != typeof (Vector2)) {
				LogError ($"Property {ScriptPropertyName} is not a Vector2!");
				return;
			}
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetValue (Vector3 v) {
			if (PropertyType != typeof (Vector3)) {
				LogError ($"Property {ScriptPropertyName} is not a Vector3!");
				return;
			}
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		public void SetValue (UnityEngine.Object v) {
			if (PropertyType != typeof (UnityEngine.Object)) {
				LogError ($"Property {ScriptPropertyName} is not an Object!");
				return;
			}
			Log ("SetValue " + ((v != null) ? v.name : "null"));

			//Convenient transformation of GameObject to Transform and vice versa
			if (v is GameObject go && PropertyType == typeof (Transform)) {
				v = go.transform;
			} else if (v is Transform t && PropertyType == typeof (GameObject)) {
				v = t.gameObject;
			}

			SetObjValue (v);
		}

	}

}