using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;
using System.Diagnostics.Eventing.Reader;

namespace CineGameEditor.MobileComponents {
	[CanEditMultipleObjects]
	public class EventEditorBase : EditorBase {
		SerializedProperty EventMaskProperty;
		List<GUIContent> EventTypes = new List<GUIContent> ();

		GUIContent IconToolbarMinus;
		GUIContent AddButonContent;

		protected override void OnEnable () {
			base.OnEnable ();

			EventMaskProperty = serializedObject.FindProperty ("eventMask");
			AddButonContent = new GUIContent ("Add New Event Type");
			// Have to create a copy since otherwise the tooltip will be overwritten.
			IconToolbarMinus = new GUIContent (EditorGUIUtility.IconContent ("Toolbar Minus"));
			IconToolbarMinus.tooltip = "Remove all events in this list.";

			// Find all event properties and make sure they are expanded if they have listeners
			var eventMask = EventMaskProperty.intValue;

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				var idxOfEvent = 0;
				do {
					if (obj.propertyType == SerializedPropertyType.Generic && obj.FindPropertyRelative ("m_PersistentCalls") != null) {
						EventTypes.Add (new GUIContent (obj.displayName));

						if ((eventMask & 1 << idxOfEvent) == 0 && obj.FindPropertyRelative ("m_PersistentCalls").FindPropertyRelative ("m_Calls").arraySize != 0) {
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

							Rect removeButtonPos = new Rect (callbackRect.xMax - removeButtonSize.x - 8, callbackRect.y + 1, removeButtonSize.x, removeButtonSize.y);
							if (GUI.Button (removeButtonPos, IconToolbarMinus, GUIStyle.none)) {
								EventMaskProperty.intValue ^= 1 << idxOfEvent;
								obj.FindPropertyRelative ("m_PersistentCalls").FindPropertyRelative ("m_Calls").arraySize = 0;
							}
						}
						idxOfEvent++;
					} else {
						EditorGUILayout.PropertyField (obj, true);
					}
				} while (obj.NextVisible (false));
			}

			if (moreEventsToAdd) {
				EditorGUILayout.Space ();
				Rect btPosition = GUILayoutUtility.GetRect (AddButonContent, GUI.skin.button);
				const float addButonWidth = 200f;
				btPosition.x = btPosition.x + (btPosition.width - addButonWidth) / 2;
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
			GenericMenu menu = new GenericMenu ();
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

	public class EditorBase : Editor {
		private string ReferenceText;
		private GUIContent ReferenceIcon;
		private GUIStyle ReferenceButtonStyle;

		protected virtual void OnEnable () {
			var customAttributes = (ComponentReferenceAttribute [])target.GetType ().GetCustomAttributes (typeof (ComponentReferenceAttribute), true);
			if (customAttributes.Length > 0) {
				var myAttribute = customAttributes [0];
				ReferenceText = myAttribute.Text;
				ReferenceIcon = new GUIContent (EditorGUIUtility.IconContent ("P4_Conflicted"));
				ReferenceIcon.tooltip = ReferenceText;
			}
		}

		protected void DrawReferenceButton () {
			if (ReferenceIcon != null) {
				var rect = EditorGUILayout.GetControlRect (false, 0f);
				rect.height = EditorGUIUtility.singleLineHeight;
				rect.x = rect.xMax - 24f;
				rect.y -= 2f;
				rect.width = 24f;

				if (ReferenceButtonStyle == null) {
					//For some reason EditorStyles cannot be accessed from OnEnable
					ReferenceButtonStyle = new GUIStyle (EditorStyles.iconButton);
					ReferenceButtonStyle.padding = new RectOffset (0, 0, 0, 0);
				}

				if (GUI.Button (rect, ReferenceIcon, ReferenceButtonStyle)) {
					rect = GUILayoutUtility.GetLastRect ();
					rect.y += 24;
					PopupWindow.Show (rect, new ReferencePopup (target.GetType ().Name, ReferenceText));
				}
			}
		}

		private class ReferencePopup : PopupWindowContent {
			private string Title;
			private string Text;
			private GUIStyle TitleStyle, Style;

			public ReferencePopup (string title, string text) {
				Title = title;
				Text = text;
			}

			public override Vector2 GetWindowSize () {
				return new Vector2 (EditorGUIUtility.currentViewWidth, 150);
			}

			public override void OnGUI (Rect rect) {
				EditorGUILayout.LabelField (Title, TitleStyle);
				EditorGUILayout.LabelField (Text, Style, GUILayout.ExpandHeight (true));

				//var rt = GUILayoutUtility.GetLastRect ();
				//editorWindow.maxSize = new Vector2 (rt.width, rt.yMax);

				var evt = Event.current;
				if (evt.type == EventType.MouseDown || evt.type == EventType.ScrollWheel) {
					editorWindow.Close ();
				}
			}

			public override void OnOpen () {
				Style = new GUIStyle (EditorStyles.label);
				Style.wordWrap = true;
				Style.alignment = TextAnchor.UpperLeft;

				TitleStyle = new GUIStyle (EditorStyles.label);
				TitleStyle.alignment = TextAnchor.UpperCenter;
				TitleStyle.fontStyle = FontStyle.Bold;
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

			var obj = serializedObject.GetIterator ();
			if (obj.NextVisible (true)) {
				do {
					if (obj.name == "m_Script")
						continue;
					EditorGUILayout.PropertyField (obj, true);
				} while (obj.NextVisible (false));
			}

			serializedObject.ApplyModifiedProperties ();
		}
	}
}