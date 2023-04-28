using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;
using System.Diagnostics.Eventing.Reader;

namespace CineGameEditor.MobileComponents {
	[CanEditMultipleObjects]
	public class EventEditorBase : Editor {
		SerializedProperty EventMaskProperty;
		List<GUIContent> EventTypes = new List<GUIContent> ();

		GUIContent IconToolbarMinus;
		GUIContent AddButonContent;

		protected virtual void OnEnable () {
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
}