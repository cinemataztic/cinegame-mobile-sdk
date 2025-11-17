using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor (typeof (LogicComponent))]
	[CanEditMultipleObjects]
	public class LogicComponentEditor : EditorBase {

		readonly List<Component> compList = new ();
		string [] compTypes;
		IEnumerable<string> memberNames;
        readonly GUIContent SourceMemberContent = new ("Source Property", "Property or field to get value from every Interval");
        readonly GUIContent DropdownButtonContent = new ();

		Rect DropDownRect;

		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();
			DrawReferenceButton ();

			var funcProperty = serializedObject.FindProperty ("Function");
			var function = (LogicComponent.CompareFunction)funcProperty.enumValueIndex;
			var isValueFunction = (function == LogicComponent.CompareFunction.Value);
			var srcObjProperty = serializedObject.FindProperty ("SourceObject");

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "SourceObject") {
						EditorGUI.BeginChangeCheck ();
						EditorGUILayout.PropertyField (obj, true);
						if (!isValueFunction)
							continue;
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
							memberNames = compList.Count != 0 ? SetScriptFieldEditor.GetValueMemberNames (compList.ElementAt (compTypeIndex), set: false) : null;
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
						if (compList.Count != 0) {
							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.PrefixLabel (SourceMemberContent);
							if (EditorGUILayout.DropdownButton (DropdownButtonContent, FocusType.Passive, EditorStyles.popup)) {
								var menu = SetScriptFieldEditor.BuildMenu (compList, SetMemberFunction, set: false);
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
						&& !(obj.name == "Value" && (!isValueFunction || srcObjProperty.objectReferenceValue != null))
						&& !(function != LogicComponent.CompareFunction.LineOfSight && obj.name == "LayerMask")
					) {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}

		/// <summary>
        /// A member property or field was chosen. Update serialized properties and dropdown button text
        /// </summary>
        /// <param name="data"></param>
		void SetMemberFunction (object data) {
			var uef = (SetScriptFieldEditor.MemberFunctionParams)data;
			serializedObject.Update ();
			serializedObject.FindProperty ("SourceObject").objectReferenceValue = uef.Component;
			serializedObject.FindProperty ("SourceMemberName").stringValue = uef.MemberName;
			serializedObject.ApplyModifiedProperties ();
			var typeName = uef.Component.GetType ().Name;
			DropdownButtonContent.text =
				DropdownButtonContent.tooltip =
				$"{typeName}.{uef.MemberName}";
		}

	}

}