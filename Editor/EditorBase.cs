using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {

	/// <summary>
	/// Custom editor for our components which need multiple events. The editor behaves exactly like the standard EventTrigger editor.
	/// </summary>
    [CanEditMultipleObjects]
	public class EventEditorBase : EditorBase {
		SerializedProperty EventMaskProperty;
		readonly List<GUIContent> EventTypes = new ();

		GUIContent IconToolbarMinus;
		GUIContent AddButonContent;

		//Persistent Listener Paths
		//internal const string kInstancePath = "m_Target";
		//internal const string kInstanceTypePath = "m_TargetAssemblyTypeName";
		//internal const string kCallStatePath = "m_CallState";
		//internal const string kArgumentsPath = "m_Arguments";
		//internal const string kModePath = "m_Mode";
		//internal const string kMethodNamePath = "m_MethodName";
		internal const string kCallsPath = "m_PersistentCalls.m_Calls";

		protected override void OnEnable () {
			base.OnEnable ();

            EventMaskProperty = serializedObject.FindProperty ("eventMask");
			AddButonContent = new GUIContent ("Add New Event Type");
			// Have to create a copy since otherwise the tooltip will be overwritten.
			IconToolbarMinus = new GUIContent (EditorGUIUtility.IconContent ("Toolbar Minus")) {
				tooltip = "Remove all listeners on this event"
			};

			// Find all event properties and make sure they are expanded if they have listeners
			var eventMask = EventMaskProperty.intValue;

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				var idxOfEvent = 0;
				do {
					if (obj.propertyType == SerializedPropertyType.Generic && obj.FindPropertyRelative ("m_PersistentCalls") != null) {
						EventTypes.Add (new GUIContent (obj.displayName));

						if ((eventMask & 1 << idxOfEvent) == 0 && obj.FindPropertyRelative (kCallsPath).arraySize != 0) {
							EventMaskProperty.intValue |= 1 << idxOfEvent;
						}

						idxOfEvent++;
					}
				} while (obj.NextVisible (false));
			}

			if (eventMask != EventMaskProperty.intValue) {
				serializedObject.ApplyModifiedPropertiesWithoutUndo ();
			}
		}

		public override void OnInspectorGUI () {
			serializedObject.Update ();
			DrawReferenceButton ();

			Vector2 removeButtonSize = GUIStyle.none.CalcSize (IconToolbarMinus);

			var eventMask = EventMaskProperty.intValue;
			var moreEventsToAdd = false;

			var instanceID = serializedObject.targetObject.GetInstanceID ();

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				var idxOfEvent = 0;
				do {
					if (obj.name == "m_Script") {
					} else if (EventTypes.Any (gc => gc.text == obj.displayName)) {
						if ((eventMask & 1 << idxOfEvent) == 0) {
							moreEventsToAdd = true;
						} else {
							EditorGUILayout.Space ();
							EditorGUILayout.PropertyField (obj);

                            Rect callbackRect = GUILayoutUtility.GetLastRect ();
							Rect removeButtonPos = new (callbackRect.xMax - removeButtonSize.x - 8, callbackRect.y + 1, removeButtonSize.x, removeButtonSize.y);
							if (GUI.Button (removeButtonPos, IconToolbarMinus, GUIStyle.none)) {
								EventMaskProperty.intValue ^= 1 << idxOfEvent;
								obj.FindPropertyRelative (kCallsPath).arraySize = 0;
							}
						}
						idxOfEvent++;
					} else {
                        EditorGUILayout.PropertyField (obj, true);
						//Highlighter.HighlightIdentifier (GUILayoutUtility.GetLastRect (), $"{instanceID}.{obj.propertyPath}");
					}
				} while (obj.NextVisible (false));
			}

			if (moreEventsToAdd) {
				EditorGUILayout.Space ();
				Rect btPosition = GUILayoutUtility.GetRect (AddButonContent, GUI.skin.button);
				const float addButonWidth = 200f;
				btPosition.x += (btPosition.width - addButonWidth) / 2;
				btPosition.width = addButonWidth;
				if (GUI.Button (btPosition, AddButonContent)) {
					ShowAddTriggermenu ();
				}
			}

			serializedObject.ApplyModifiedProperties ();
		}

        void ShowAddTriggermenu () {
			var eventMask = EventMaskProperty.intValue;

			// Now create the menu, add items and show it
			var menu = new GenericMenu ();
			for (int i = 0; i < EventTypes.Count; ++i) {
				bool active = true;

				if ((eventMask & 1 << i) != 0) {
					active = false;
				}

				if (active)
					menu.AddItem (EventTypes [i], false, OnAddNewSelected, i);
				else
					menu.AddDisabledItem (EventTypes [i]);
			}
			menu.ShowAsContext ();
			Event.current.Use ();
		}

		private void OnAddNewSelected (object index) {
			int selected = (int)index;

			EventMaskProperty.intValue |= 1 << selected;
			serializedObject.ApplyModifiedProperties ();
		}
	}

	/// <summary>
	/// Custom property drawer for Tag string values.
	/// </summary>
	[CustomPropertyDrawer (typeof (TagSelectorAttribute))]
	public class TagSelectorPropertyDrawer : PropertyDrawer {

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			if (property.propertyType == SerializedPropertyType.String) {
				EditorGUI.BeginProperty (position, label, property);
				property.stringValue = EditorGUI.TagField (position, label, property.stringValue);
				EditorGUI.EndProperty ();
			} else {
				EditorGUI.PropertyField (position, property, label);
			}
		}
	}

    /// <summary>
    /// Our own base editor for all our components. 
    /// </summary>
    public class EditorBase : Editor {
		private string ReferenceText;
		private bool ReferenceVisible;
		private GUIContent ReferenceIcon;
		private GUIStyle ReferenceButtonStyle;

		protected virtual void OnEnable () {
			var customAttributes = (ComponentReferenceAttribute [])target.GetType ().GetCustomAttributes (typeof (ComponentReferenceAttribute), true);
			if (customAttributes.Length > 0) {
				var myAttribute = customAttributes [0];
				ReferenceText = myAttribute.Text;
				ReferenceIcon = new GUIContent (EditorGUIUtility.IconContent ("d_UnityEditor.InspectorWindow")) {
					tooltip = ReferenceText
				};
			}
		}

		protected void DrawReferenceButton () {
			if (ReferenceIcon != null) {
				var rect = EditorGUILayout.GetControlRect (false, 0f);
				rect.height = EditorGUIUtility.singleLineHeight;
				rect.x = rect.xMax - 18f;
				rect.y += 2f;
				rect.width = 24f;

				if (ReferenceButtonStyle == null) {
					//For some reason EditorStyles cannot be accessed from OnEnable
					ReferenceButtonStyle = new GUIStyle (EditorStyles.iconButton) {
						padding = new RectOffset (0, 0, 0, 0)
					};
				}

				if (GUI.Button (rect, ReferenceIcon, ReferenceButtonStyle)) {
					ReferenceVisible = !ReferenceVisible;
				}
				if (ReferenceVisible) {
					EditorGUILayout.HelpBox (ReferenceText, MessageType.None);
				}
			}
		}
	}

	/// <summary>
	/// BaseComponent editor. Draws a reference button (if there is a ComponentReferenceAttribute on the class) and hides the Script property
	/// </summary>
	[CustomEditor (typeof (BaseComponent), editorForChildClasses: true)]
	[CanEditMultipleObjects]
	public class BaseComponentEditor : EditorBase {
		public override void OnInspectorGUI () {
			serializedObject.Update ();

			DrawReferenceButton ();

			//var instanceID = serializedObject.targetObject.GetInstanceID ();

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "m_Script")
						continue;
					EditorGUILayout.PropertyField (obj, true);
					//Highlighter.HighlightIdentifier (GUILayoutUtility.GetLastRect (), $"{instanceID}.{obj.propertyPath}");
				} while (obj.NextVisible (false));
			}

			serializedObject.ApplyModifiedProperties ();
		}
	}


    [CustomEditor (typeof (RaycastComponent))]
    [CanEditMultipleObjects]
    public class RaycastEditor : EventEditorBase {
    }

    [CustomEditor (typeof (GetTransformProperty))]
    [CanEditMultipleObjects]
    public class GetTransformPropertyEditor : EventEditorBase {
    }

	[CustomEditor (typeof (OnApplicationPlatform))]
	[CanEditMultipleObjects]
	public class OnApplicationPlatformEditor : EventEditorBase {
	}
}
