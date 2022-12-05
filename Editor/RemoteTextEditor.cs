using UnityEngine;
using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor( typeof(RemoteTextComponent) )]
	public class RemoteTextEditor : Editor {
		System.Array TypeValuesArray;
		SerializedProperty TypesArray;
		bool KeysFoldedOut;

		public override void OnInspectorGUI ()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();

			if (TypeValuesArray == null) {
				TypeValuesArray = System.Enum.GetValues (typeof (RemoteTextComponent.ComponentType));
				TypesArray = serializedObject.FindProperty ("Types");
			}

			bool IsFormattedString = false;
			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "IsFormattedString") {
						IsFormattedString = obj.boolValue;
						break;
					}
				} while (obj.NextVisible (false));
			}

			obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "m_Script" || (!IsFormattedString && (obj.name == "Keys" || obj.name == "Types")) || (IsFormattedString && obj.name == "Key")) {
					} else {
						if (obj.name == "Keys") {
							KeysFoldedOut = EditorGUILayout.Foldout (KeysFoldedOut, "Keys");
							if (KeysFoldedOut) {
								EditorGUI.indentLevel++;

								obj.arraySize = TypesArray.arraySize = EditorGUILayout.IntField ("size", obj.arraySize);
								for (var i = 0; i < obj.arraySize; i++) {
									var itemKey = obj.GetArrayElementAtIndex (i);
									var itemType = TypesArray.GetArrayElementAtIndex (i);
									EditorGUILayout.BeginHorizontal ();
									EditorGUILayout.PrefixLabel (new GUIContent ("{" + i + "}"));
									itemKey.stringValue = EditorGUILayout.TextField (itemKey.stringValue);
									itemType.enumValueIndex = (int)(RemoteTextComponent.ComponentType)EditorGUILayout.EnumPopup ((RemoteTextComponent.ComponentType)TypeValuesArray.GetValue (itemType.enumValueIndex));
									EditorGUILayout.EndHorizontal ();
								}

								EditorGUI.indentLevel--;
							}
							EditorGUILayout.Space ();
						} else if (obj.name == "Types") {
							//Don't render, controlled from Keys
						} else {
							EditorGUILayout.PropertyField (obj, true);
						}
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}
	}

}