using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	[CustomEditor (typeof (CompareComponent))]
	[CanEditMultipleObjects]
	public class CompareEditor : Editor {
		public override void OnInspectorGUI () {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();

			var funcProperty = serializedObject.FindProperty ("Function");
			var function = (CompareComponent.CompareFunction)funcProperty.enumValueIndex;
			var isValueFunction = (function == CompareComponent.CompareFunction.Value);

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name != "m_Script"
						&& !(isValueFunction && obj.name == "Other")
						&& !(!isValueFunction && obj.name == "Value")
					) {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}
	}

}