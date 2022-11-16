using UnityEditor;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
	[CustomEditor (typeof(DPadComponent))]
	[CanEditMultipleObjects]
	public class DPadComponentEditor : UnityEditor.EventSystems.EventTriggerEditor {
		public override void OnInspectorGUI() {
			serializedObject.Update ();

			//base.OnInspectorGUI();
			EditorGUILayout.PropertyField (serializedObject.FindProperty ("VariableName"));
			EditorGUILayout.PropertyField (serializedObject.FindProperty ("CooldownTime"));

			serializedObject.ApplyModifiedProperties ();
		}
	}
}