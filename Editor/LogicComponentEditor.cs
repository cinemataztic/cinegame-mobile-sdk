using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor (typeof (LogicComponent))]
	[CanEditMultipleObjects]
	public class LogicComponentEditor : EditorBase {

		readonly List<Component> compList = new ();
		string [] compTypes;
		string [] memberNames;
        readonly GUIContent SourceMemberContent = new ("Source Property", "Property or field to get value from every Interval");
        readonly GUIContent DropdownButtonContent = new ();

		readonly Type _b = typeof (bool);
		readonly Type _i = typeof (int);
		readonly Type _f = typeof (float);
		readonly Type _d = typeof (double);
		readonly Type _mbType = typeof (MonoBehaviour);
		readonly Type _tType = typeof (Transform);

		Rect DropDownRect;

		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();
			DrawReferenceButton ();

			var funcProperty = serializedObject.FindProperty ("Function");
			var function = (LogicComponent.CompareFunction)funcProperty.enumValueIndex;
			var isValueFunction = (function == LogicComponent.CompareFunction.Value);

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "SourceObject") {
						EditorGUI.BeginChangeCheck ();
						EditorGUILayout.PropertyField (obj, true);
						if (!isValueFunction)
							continue;
						if (EditorGUI.EndChangeCheck () || compTypes == null) {
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
								var fieldName = serializedObject.FindProperty ("SourceMemberName").stringValue;
								if (memberNames.Contains (fieldName)) {
									DropdownButtonContent.text = compTypes [compTypeIndex] + "." + fieldName;
								} else {
									DropdownButtonContent.text = $"<Missing {fieldName}>";
								}
							} else {
								DropdownButtonContent.text = "-";
							}
						}
						if (compTypes != null) {
							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.PrefixLabel (SourceMemberContent);
							if (EditorGUILayout.DropdownButton (DropdownButtonContent, FocusType.Passive, EditorStyles.popup)) {
								var menu = BuildMenu (compList);
								menu.DropDown (DropDownRect);
							}
							if (Event.current.type == EventType.Repaint) {
								DropDownRect = GUILayoutUtility.GetLastRect ();
							}
							EditorGUILayout.EndHorizontal ();
						}
					} else if (obj.name == "Thresholds") {
						/*EditorGUILayout.BeginVertical (GUI.skin.box);
						EditorGUILayout.PropertyField (obj.FindPropertyRelative ("Array.size"), new GUIContent ("Thresholds"));
						EditorGUI.indentLevel += 1;
						for (int i = 0; i < obj.arraySize; i++) {
							EditorGUILayout.PropertyField (obj.GetArrayElementAtIndex (i), new GUIContent ($"Threshold {i+1}"));
						}
						EditorGUI.indentLevel -= 1;
						EditorGUILayout.EndVertical ();*/
						EditorGUILayout.PropertyField (obj, true);
					} else if (obj.name != "m_Script"
						&& obj.name != "SourceMemberName"
						&& !(isValueFunction && obj.name == "Other")
						&& !(!isValueFunction && obj.name == "Value")
						&& !(function != LogicComponent.CompareFunction.LineOfSight && obj.name == "LayerMask")
					) {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}

		GenericMenu BuildMenu (List<Component> components) {
			var menu = new GenericMenu ();
			var _b = typeof (bool);
			var _i = typeof (int);
			var _f = typeof (float);
			var _d = typeof (double);
			foreach (var c in components) {
				var cType = c.GetType ();
				var cName = cType.Name;
				var props = cType.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				var fields = cType.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				var methods = cType.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				for (int i = 0; i < props.Length; i++) {
					var _type = props [i].PropertyType;
					if (props [i].DeclaringType.IsSubclassOf (_mbType) && (_type == _b || _type == _i || _type == _f || _type == _d)) {
						menu.AddItem (new GUIContent (cName + "/" + props [i].Name), false, SetMemberFunction, new MemberFunctionParams (c, props [i].Name));
					}
				}
				for (int i = 0; i < fields.Length; i++) {
					var _type = fields [i].FieldType;
					if (fields [i].DeclaringType.IsSubclassOf (_mbType) && (_type == _b || _type == _i || _type == _f || _type == _d)) {
						menu.AddItem (new GUIContent (cName + "/" + fields [i].Name), false, SetMemberFunction, new MemberFunctionParams (c, fields [i].Name));
					}
				}
				for (int i = 0; i < methods.Length; i++) {
					var mi = methods [i];
					if ((mi.DeclaringType != _tType && !mi.DeclaringType.IsSubclassOf (_mbType)) || mi.GetParameters ().Length != 0 || mi.Name.StartsWith ("get_"))
						continue;
					var _type = mi.ReturnType;
					if (_type == _b || _type == _i || _type == _f || _type == _d) {
						menu.AddItem (new GUIContent (cName + "/" + mi.Name), false, SetMemberFunction, new MemberFunctionParams (c, mi.Name));
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
			serializedObject.FindProperty ("SourceObject").objectReferenceValue = uef.Component;
			serializedObject.FindProperty ("SourceMemberName").stringValue = uef.MemberName;
			serializedObject.ApplyModifiedProperties ();
			var typeName = uef.Component.GetType ().Name;
			DropdownButtonContent.text =
				DropdownButtonContent.tooltip =
				$"{typeName}.{uef.MemberName}";
		}

		struct MemberFunctionParams {
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
		string [] GetValueMemberNames (Component c) {
			var cType = c.GetType ();
			var props = cType.GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fields = cType.GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var methods = cType.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fn = new List<string> (props.Length + fields.Length + methods.Length);
			for (int i = 0; i < props.Length; i++) {
				var _type = props [i].PropertyType;
				if (props [i].DeclaringType.IsSubclassOf (_mbType) && (_type == _b || _type == _i || _type == _f || _type == _d)) {
					fn.Add (props [i].Name);
				}
			}
			for (int i = 0; i < fields.Length; i++) {
				var _type = fields [i].FieldType;
				if (fields [i].DeclaringType.IsSubclassOf (_mbType) && (_type == _b || _type == _i || _type == _f || _type == _d)) {
					fn.Add (fields [i].Name);
				}
			}

			for (int i = 0; i < methods.Length; i++) {
				var mi = methods [i];
				if ((mi.DeclaringType != _tType && !mi.DeclaringType.IsSubclassOf (_mbType)) || mi.GetParameters ().Length != 0 || mi.Name.StartsWith ("get_"))
					continue;
				var _type = mi.ReturnType;
				if (_type == _b || _type == _i || _type == _f || _type == _d) {
					fn.Add (mi.Name);
				}
			}
			return fn.ToArray ();
		}
	}

}