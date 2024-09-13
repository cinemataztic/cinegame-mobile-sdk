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
		int compTypeIndex, fieldIndex;
		string [] memberNames;
		GUIContent sourceMemberContent = new GUIContent ("Source Member", "The field or property to get value from every Interval");

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
							if (obj.objectReferenceValue is GameObject go) {
								go.GetComponents (compList);
							} else if (obj.objectReferenceValue is Component c) {
								c.GetComponents (compList);
							}
							compTypes = compList.Count != 0 ? compList.Select (c => c.GetType ().Name).ToArray () : null;
							compTypeIndex = 0;
							memberNames = compList.Count != 0 ? GetValueMemberNames (compList.ElementAt (0)) : null;
							fieldIndex = 0;
						}
						if (compTypes != null) {
							EditorGUILayout.BeginHorizontal ();
							var cti = EditorGUILayout.Popup (sourceMemberContent, compTypeIndex, compTypes);
							if (cti != compTypeIndex) {
								compTypeIndex = cti;
								var c = compList.ElementAt (compTypeIndex);
								obj.objectReferenceValue = c;
								memberNames = GetValueMemberNames (c);
								fieldIndex = 0;
							}
							if (memberNames != null) {
								var fi = EditorGUILayout.Popup (fieldIndex, memberNames);
								if (fi != fieldIndex) {
									fieldIndex = fi;
									serializedObject.FindProperty ("SourceMemberName").stringValue = memberNames [fieldIndex];
								}
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

		/// <summary>
        /// Use reflection to get an array of all public properties and fields of type bool, int or float
        /// </summary>
		string [] GetValueMemberNames (Component c) {
			var _b = typeof (bool);
			var _i = typeof (int);
			var _f = typeof (float);
			var props = c.GetType ().GetProperties (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fields = c.GetType ().GetFields (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			var fn = new List<string> (props.Length + fields.Length);
			for (int i = 0; i < props.Length; i++) {
				var _type = props [i].PropertyType;
				if (_type == _b || _type == _i || _type == _f) {
					fn.Add (props [i].Name);
				}
			}
			for (int i = 0; i < fields.Length; i++) {
				var _type = fields [i].FieldType;
				if (_type == _b || _type == _i || _type == _f) {
					fn.Add (fields [i].Name);
				}
			}
			return fn.ToArray ();
		}
	}

}