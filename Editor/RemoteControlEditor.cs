using UnityEngine;
using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor ( typeof(RemoteControl) )]
	[CanEditMultipleObjects]
	public class RemoteControlEditor : EditorBase {
		public override void OnInspectorGUI ()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();
			DrawReferenceButton ();

			var typeProperty = serializedObject.FindProperty ("Type");
			var typeName = string.Format ("onReceive{0}", typeProperty.enumNames [typeProperty.enumValueIndex]);
			bool isInterpolative = 
			(
				typeName != "onReceiveVoid" &&
				typeName != "onReceiveString" &&
				typeName != "onReceiveBool"
			);

			var interpTime = serializedObject.FindProperty ("InterpTime").floatValue;

			var guiContentOnReceive = new GUIContent ("On Receive");
			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (
						!obj.name.StartsWith ("onReceive") 
						&& obj.name != "m_Script" 
						&& (isInterpolative || !obj.name.StartsWith ("Interp"))
						&& (obj.name != "InterpType" || (isInterpolative && interpTime != 0f))
					) {
						EditorGUILayout.PropertyField (obj, true);
					} else if (obj.name == typeName) {
						EditorGUILayout.PropertyField (obj, guiContentOnReceive, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}
	}

}