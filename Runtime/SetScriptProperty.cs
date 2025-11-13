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
		Type PropertyType;
		readonly object [] MethodParams = new object [1];

		const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		void Start () {
			if (ScriptObject != null && !string.IsNullOrWhiteSpace (ScriptPropertyName)) {
				var type = ScriptObject.GetType ();
				FieldInfo = type.GetField (ScriptPropertyName, _bindingFlags);
				if (FieldInfo != null) {
					PropertyType = FieldInfo.FieldType;
				} else {
					PropertyInfo = type.GetProperty (ScriptPropertyName, _bindingFlags);
					if (PropertyInfo != null) {
						PropertyType = PropertyInfo.PropertyType;
					} else {
						MethodInfo = type.GetMethod (ScriptPropertyName, _bindingFlags);
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

		/// <summary>
        /// Set script property int value
        /// </summary>
		public void SetValue (int v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (int)) {
				LogError ($"Property {ScriptPropertyName} is not an integer!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		/// <summary>
		/// Set script property float value
		/// </summary>
		public void SetValue (float v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (float)) {
				LogError ($"Property {ScriptPropertyName} is not a float!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		/// <summary>
		/// Set script property bool value
		/// </summary>
		public void SetValue (bool v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (bool)) {
				LogError ($"Property {ScriptPropertyName} is not a bool!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		/// <summary>
		/// Set script property Vector2 value
		/// </summary>
		public void SetValue (Vector2 v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (Vector2)) {
				LogError ($"Property {ScriptPropertyName} is not a Vector2!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		/// <summary>
		/// Set script property Vector3 value
		/// </summary>
		public void SetValue (Vector3 v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (Vector3)) {
				LogError ($"Property {ScriptPropertyName} is not a Vector3!");
				return;
			}
#endif
			Log ("SetValue " + v);
			SetObjValue (v);
		}

		/// <summary>
		/// Set script property Object value
		/// </summary>
		public void SetValue (UnityEngine.Object v) {
#if UNITY_EDITOR
			if (PropertyType != typeof (UnityEngine.Object)) {
				LogError ($"Property {ScriptPropertyName} is not an Object!");
				return;
			}
#endif
			//Convenient transformation of GameObject to Transform and vice versa
			if (v is GameObject go && PropertyType == typeof (Transform)) {
				v = go.transform;
			} else if (v is Transform t && PropertyType == typeof (GameObject)) {
				v = t.gameObject;
			}
			Log("SetValue " + ((v != null) ? v.name : "null"));
			SetObjValue(v);
		}

	}

}