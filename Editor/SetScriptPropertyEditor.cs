using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor (typeof (SetScriptProperty))]
	[CanEditMultipleObjects]
	public class SetScriptPropertyEditor : EditorBase {

		readonly List<Component> compList = new ();
		string [] compTypes;
		string [] memberNames;
		readonly GUIContent ScriptMemberContent = new ("Script Property", "Property, field or method to set value of");
		readonly GUIContent DropdownButtonContent = new ();

		static readonly Type _b = typeof (bool);
		static readonly Type _i = typeof (int);
		static readonly Type _f = typeof (float);
		static readonly Type _d = typeof (double);
		static readonly Type _eventType = typeof (UnityEngine.Events.UnityEventBase);

		Rect DropDownRect;

		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();
			DrawReferenceButton ();

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
								var fieldName = serializedObject.FindProperty ("ScriptPropertyName").stringValue;
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
						&& obj.name != "ScriptPropertyName"
					) {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}

		const BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		internal static GenericMenu BuildMenu (List<Component> components, GenericMenu.MenuFunction2 func, bool set = true) {
			var menu = new GenericMenu ();
			var _b = typeof (bool);
			var _i = typeof (int);
			var _f = typeof (float);
			var _d = typeof (double);
			foreach (var c in components) {
				var cType = c.GetType ();
				var cName = cType.Name;
				var props = cType.GetProperties (_bindingFlags);
				var fields = cType.GetFields (_bindingFlags);
				var methods = cType.GetMethods (_bindingFlags);
				for (int i = 0; i < props.Length; i++) {
					menu.AddItem (new GUIContent (cName + "/" + props [i].Name), false, func, new MemberFunctionParams (c, props [i].Name));
				}
				for (int i = 0; i < fields.Length; i++) {
					if (fields [i].FieldType.IsSubclassOf (_eventType))
						continue;
					menu.AddItem (new GUIContent (cName + "/" + fields [i].Name), false, func, new MemberFunctionParams (c, fields [i].Name));
				}
				for (int i = 0; i < methods.Length; i++) {
					var mi = methods [i];
					if (mi.GetParameters ().Length != 0 || mi.Name.StartsWith (set ? "set_" : "get_"))
						continue;
					var _type = mi.ReturnType;
					if (set || _type == _b || _type == _i || _type == _f || _type == _d) {
						menu.AddItem (new GUIContent (cName + "/" + mi.Name), false, func, new MemberFunctionParams (c, mi.Name));
					}
				}
			}
			return menu;
		}

		/// <summary>
		/// A member property or field was chosen. Update serialized properties and dropdown button text
		/// </summary>
		/// <param name="data"></param>
		void SetMemberFunction (object data) {
			var uef = (MemberFunctionParams)data;
			serializedObject.Update ();
			serializedObject.FindProperty ("ScriptObject").objectReferenceValue = uef.Component;
			serializedObject.FindProperty ("ScriptPropertyName").stringValue = uef.MemberName;
			serializedObject.ApplyModifiedProperties ();
			var typeName = uef.Component.GetType ().Name;
			DropdownButtonContent.text =
				DropdownButtonContent.tooltip =
				$"{typeName}.{uef.MemberName}";
		}

		internal struct MemberFunctionParams {
			public readonly Component Component;
			public readonly string MemberName;

			public MemberFunctionParams (Component component, string memberName) {
				Component = component;
				MemberName = memberName;
			}
		}

		/// <summary>
		/// Use reflection to get an array of all public properties, fields and methods which return bool, int, float or double
		/// </summary>
		internal static string [] GetValueMemberNames (Component c, bool set = true) {
			var cType = c.GetType ();
			var props = cType.GetProperties (_bindingFlags);
			var fields = cType.GetFields (_bindingFlags);
			var methods = cType.GetMethods (_bindingFlags);
			var fn = new List<string> (props.Length + fields.Length + methods.Length);
			for (int i = 0; i < props.Length; i++) {
				var _type = props [i].PropertyType;
				if (set || _type == _b || _type == _i || _type == _f || _type == _d) {
					fn.Add (props [i].Name);
				}
			}
			for (int i = 0; i < fields.Length; i++) {
				var _type = fields [i].FieldType;
				if (_type.IsSubclassOf (_eventType))
					continue;
				if (set || _type == _b || _type == _i || _type == _f || _type == _d) {
					fn.Add (fields [i].Name);
				}
			}
			for (int i = 0; i < methods.Length; i++) {
				var mi = methods [i];
				if (mi.GetParameters ().Length != 0 || mi.Name.StartsWith (set ? "set_" : "get_"))
					continue;
				var _type = mi.ReturnType;
				if (set || _type == _b || _type == _i || _type == _f || _type == _d) {
					fn.Add (mi.Name);
				}
			}
			return fn.ToArray ();
		}
	}

}
