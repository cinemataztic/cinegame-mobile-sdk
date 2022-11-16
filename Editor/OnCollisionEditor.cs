using UnityEngine;
using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
	[CustomEditor (typeof (OnCollision))]
	[CanEditMultipleObjects]
	public class OnCollisionEditor : Editor {
		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "m_Script") {
					} else if (obj.name == "FilterTags") {
						if (EditorGUILayout.PropertyField (obj, false)) {
							EditorGUI.indentLevel++;

							obj.arraySize = EditorGUILayout.IntField ("size", obj.arraySize);
							for (var i = 0; i < obj.arraySize; i++) {
								var item = obj.GetArrayElementAtIndex (i);
								item.stringValue = EditorGUILayout.TagField (new GUIContent ("Tag #" + (i+1)), item.stringValue);
							}

							EditorGUI.indentLevel--;
						}
					} else {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}
	}
}
