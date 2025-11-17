using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor (typeof (SetScriptField))]
	[CanEditMultipleObjects]
	public class SetScriptFieldEditor : EditorBase {

		readonly List<Component> compList = new ();
		string [] compTypes;
		IEnumerable<string> memberNames;
		readonly GUIContent ScriptMemberContent = new ("Script Field", "Field to set or interp value of");
		readonly GUIContent DropdownButtonContent = new ();

		static readonly Type _b = typeof (bool);
		static readonly Type _i = typeof (int);
		static readonly Type _f = typeof (float);
		static readonly Type _d = typeof (double);
		static readonly Type _c = typeof (Color);
		static readonly Type _q = typeof (Quaternion);
		static readonly Type _v2 = typeof (Vector2);
		static readonly Type _v3 = typeof (Vector3);
		static readonly Type _objType = typeof (UnityEngine.Object);
		static readonly Type _eventType = typeof (UnityEngine.Events.UnityEventBase);

		Rect DropDownRect;

		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();
			DrawReferenceButton ();

			var interpTime = serializedObject.FindProperty ("InterpTime").floatValue;
			var objReferenceValue = serializedObject.FindProperty ("ScriptObject").objectReferenceValue;
			var memberName = serializedObject.FindProperty ("ScriptFieldName").stringValue;

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "ScriptObject") {
						EditorGUI.BeginChangeCheck ();
						EditorGUILayout.PropertyField (obj, true);
						if (EditorGUI.EndChangeCheck () || compList.Count == 0) {
							Component comp = null;
							if (obj.objectReferenceValue is GameObject go) {
								go.GetComponents (compList);
							} else if (obj.objectReferenceValue is Component c) {
								c.GetComponents (compList);
								comp = c;
							} else {
								compList.Clear ();
							}
							compTypes = compList.Count != 0 ? compList.Select (c => c.GetType ().Name).ToArray () : null;
							var compTypeIndex = comp != null ? Mathf.Max (0, ArrayUtility.IndexOf (compTypes, comp.GetType ().Name)) : 0;
							memberNames = compList.Count != 0 ? GetValueMemberNames (compList.ElementAt (compTypeIndex)) : null;
							if (memberNames != null) {
								var fieldName = serializedObject.FindProperty ("ScriptFieldName").stringValue;
								if (memberNames.Contains (fieldName)) {
									DropdownButtonContent.text = compTypes [compTypeIndex] + "." + fieldName;
								} else {
									DropdownButtonContent.text = $"<Missing {fieldName}>";
								}
							} else {
								DropdownButtonContent.text = "-";
							}
						}
						if (compList.Count != 0) {
							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.PrefixLabel (ScriptMemberContent);
							if (EditorGUILayout.DropdownButton (DropdownButtonContent, FocusType.Passive, EditorStyles.popup)) {
								var menu = BuildMenu (compList, SetMemberFunction);
								menu.DropDown (DropDownRect);
							}
							if (Event.current.type == EventType.Repaint) {
								DropDownRect = GUILayoutUtility.GetLastRect ();
							}
							EditorGUILayout.EndHorizontal ();
						}
					} else if (obj.name != "m_Script"
						&& obj.name != "ScriptFieldName"
						&& !(obj.name == "InterpType" && interpTime == 0f)
						&& !((obj.name == "InterpTime" || obj.name == "InterpType") && (objReferenceValue == null || string.IsNullOrEmpty (memberName)))
					) {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}

		const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		/// <summary>
		/// A member property or field was chosen. Update serialized properties and dropdown button text
		/// </summary>
		/// <param name="data"></param>
		void SetMemberFunction (object data) {
			var uef = (MemberFunctionParams)data;
			serializedObject.Update ();
			serializedObject.FindProperty ("ScriptObject").objectReferenceValue = uef.Component;
			serializedObject.FindProperty ("ScriptFieldName").stringValue = uef.MemberName;
			serializedObject.ApplyModifiedProperties ();
			var typeName = uef.Component.GetType ().Name;
			DropdownButtonContent.text =
				DropdownButtonContent.tooltip =
				$"{typeName}.{uef.MemberName}";
		}

		internal static GenericMenu BuildMenu (List<Component> components, GenericMenu.MenuFunction2 func, bool set = true) {
			var menu = new GenericMenu ();
			foreach (var c in components) {
				var cType = c.GetType ();
				var cName = cType.Name;
				var fields = cType.GetFields (_bindingFlags).Where (f => IsMemberViable (f.FieldType, set));
				foreach (var p in fields) {
					menu.AddItem (new GUIContent ($"{cName}/{PrettyPrint (p.FieldType)} {p.Name}"), false, func, new MemberFunctionParams (c, p.Name));
				}
				if (!set) {
					var props = cType.GetProperties (_bindingFlags).Where (p => IsMemberViable (p.PropertyType, set));
					var methods = cType.GetMethods (_bindingFlags).Where (m => IsMethodViable (m, set));
					foreach (var p in props) {
						menu.AddItem (new GUIContent ($"{cName}/{PrettyPrint (p.PropertyType)} {p.Name}"), false, func, new MemberFunctionParams (c, p.Name));
					}
					foreach (var p in methods) {
						menu.AddItem (new GUIContent ($"{cName}/{p.Name} ({PrettyPrint (set ? p.GetParameters () [0].ParameterType : p.ReturnType)})"), false, func, new MemberFunctionParams (c, p.Name));
					}
				}
			}
			return menu;
		}

		static string PrettyPrint (Type t) {
			var tString = t.ToString ();
			tString = tString.Substring (tString.LastIndexOf ('.') + 1);
			switch (tString) {
			case "Boolean":
				return "bool";
			case "Int32":
				return "int";
			case "Int64":
				return "long";
			case "Single":
				return "float";
			default:
				break;
			}
			return tString;
		}

		internal struct MemberFunctionParams {
			public readonly Component Component;
			public readonly string MemberName;

			public MemberFunctionParams (Component component, string memberName) {
				Component = component;
				MemberName = memberName;
			}
		}

		static bool IsMemberViable (Type _type, bool set) {
			return _type == _b || _type == _i || _type == _f || _type == _d
				|| (set && (_type == _c || _type == _q || _type == _v2 || _type == _v3
						|| _type == _objType || _type == typeof (string)
						|| (_type.IsSubclassOf (_objType) && !_type.IsSubclassOf (_eventType)))
					);
		}

		static bool IsMethodViable (MethodInfo mi, bool set) {
			if (mi.Name.StartsWith ("set_") || mi.Name.StartsWith ("get_"))
				return false;
			Type _type;
			if (set) {
				var _params = mi.GetParameters ();
				if (_params.Length != 1)
					return false;
				_type = _params [0].ParameterType;
			} else {
				_type = mi.ReturnType;
			}
			return IsMemberViable (_type, set);
		}

		/// <summary>
		/// Use reflection to get an array of all public properties, fields and methods which return bool, int, float or double
		/// </summary>
		internal static IEnumerable<string> GetValueMemberNames (Component c, bool set = true) {
			var cType = c.GetType ();
			var fields = cType.GetFields (_bindingFlags).Where (f => IsMemberViable (f.FieldType, set)).Select (f => f.Name);
			if (!set) {
				var props = cType.GetProperties (_bindingFlags).Where (p => IsMemberViable (p.PropertyType, set)).Select (p => p.Name);
				var methods = cType.GetMethods (_bindingFlags).Where (m => IsMethodViable (m, set)).Select (m => m.Name);
				return props.Concat (fields).Concat (methods);
			}
			return fields;
		}
	}

}
