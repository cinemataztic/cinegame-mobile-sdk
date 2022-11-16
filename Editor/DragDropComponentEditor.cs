using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
	[CustomEditor (typeof(DragDropComponent))]
	[CanEditMultipleObjects]
	public class DragDropComponentEditor : UnityEditor.EventSystems.EventTriggerEditor {
		public override void OnInspectorGUI() {
			serializedObject.Update ();

			//base.OnInspectorGUI();
			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "m_Script" || obj.name == "m_Delegates" || obj.name == "delegates") {
					} else {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			serializedObject.ApplyModifiedProperties ();
		}
	}
}