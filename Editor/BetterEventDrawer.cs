using System;
using System.Reflection;
using System.Text;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace LaxityAssets {

    /// <summary>
    /// Custom override of the standard UnityEventDrawer.
	/// Most of this code has been copied from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/UnityEventDrawer.cs#L293
	/// Differences is in the layout of the fields in the event, and when you drop a new target,
	/// we try to find a match for the target-type and method, rather than just reset the event like the standard UnityEventDrawer does.
    /// </summary>
    [CustomPropertyDrawer (typeof (UnityEventBase), true)]
    public class BetterEventDrawer : UnityEventDrawer {

        private const string kNoFunctionString = "No Function";

        //Persistent Listener Paths
        internal const string kInstancePath = "m_Target";
        internal const string kInstanceTypePath = "m_TargetAssemblyTypeName";
        internal const string kCallStatePath = "m_CallState";
        internal const string kArgumentsPath = "m_Arguments";
        internal const string kModePath = "m_Mode";
        internal const string kMethodNamePath = "m_MethodName";
        internal const string kCallsPath = "m_PersistentCalls.m_Calls";

        //ArgumentCache paths
        internal const string kFloatArgument = "m_FloatArgument";
        internal const string kIntArgument = "m_IntArgument";
        internal const string kObjectArgument = "m_ObjectArgument";
        internal const string kStringArgument = "m_StringArgument";
        internal const string kBoolArgument = "m_BoolArgument";
        internal const string kObjectArgumentAssemblyTypeName = "m_ObjectArgumentAssemblyTypeName";

        private static readonly GUIContent s_MixedValueContent = EditorGUIUtility.TrTextContent ("\u2014", "Mixed Values");

        SerializedProperty m_ListenersArray;
        UnityEventBase m_DummyEvent;

        /// <summary>
        /// Internal Unity method for building the 'Component > Method' GenericMenu
        /// </summary>
        MethodInfo miBuildPopupList;

        /// <summary>
        /// Staging toggle menu item path
        /// </summary>
        private const string OpdMenuName = "Tools/Better Event Drawer";
        private const string OpdKey = "BetterEventDrawer";

        [MenuItem (OpdMenuName)]
        public static void ToggleOpd () {
            var opp = EditorPrefs.GetBool (OpdKey, true);
            EditorPrefs.SetBool (OpdKey, !opp);
        }

        [MenuItem (OpdMenuName, true)]
        private static bool ToggleOpdValidate () {
            Menu.SetChecked (OpdMenuName, EditorPrefs.GetBool (OpdKey, true));
            return true;
        }

        Rect [] GetRowRects (Rect rect, PersistentListenerMode mode) {
            Rect [] rects = new Rect [4];

            rect.height = EditorGUIUtility.singleLineHeight;

            Rect enabledRect = rect;
            enabledRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect goRect = rect;
            if (mode == PersistentListenerMode.Bool) {
                goRect.width -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            } else if (mode != PersistentListenerMode.Void && mode != PersistentListenerMode.EventDefined) {
                goRect.width *= .5f;
            }
            goRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect functionRect = rect;
            functionRect.xMin = enabledRect.xMax + EditorGUIUtility.standardVerticalSpacing; //+ EditorGUI.kSpacing;

            Rect argRect = functionRect;
            argRect.xMin = goRect.xMax + EditorGUIUtility.standardVerticalSpacing; //+ EditorGUI.kSpacing;
            argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            rects [0] = enabledRect;
            rects [1] = goRect;
            rects [2] = functionRect;
            rects [3] = argRect;
            return rects;
        }

        protected override void SetupReorderableList (ReorderableList list) {
            base.SetupReorderableList (list);

            if (!EditorPrefs.GetBool (OpdKey, true))
                return;

            m_ListenersArray = list.serializedProperty;

            //Squeeze elements a little tighter together compared to normal UnityEvents
            list.elementHeight = (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);

            var tUnityEventDrawer = typeof (UnityEventDrawer);
            miBuildPopupList = tUnityEventDrawer.GetMethod ("BuildPopupList", BindingFlags.NonPublic | BindingFlags.Static);
        }

        /// <summary>
        /// Couldn't find this using Reflection so I had to copy it from UnityCsReference. It's pretty much intact.
        /// </summary>
        private SerializedProperty GetArgument (SerializedProperty pListener) {
            var listenerTarget = pListener.FindPropertyRelative (kInstancePath);
            var methodName = pListener.FindPropertyRelative (kMethodNamePath);
            var mode = pListener.FindPropertyRelative (kModePath);
            var arguments = pListener.FindPropertyRelative (kArgumentsPath);

            SerializedProperty argument;
            var modeEnum = (PersistentListenerMode)mode.enumValueIndex;
            //only allow argument if we have a valid target / method
            if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty (methodName.stringValue))
                modeEnum = PersistentListenerMode.Void;

            switch (modeEnum) {
            case PersistentListenerMode.Float:
                argument = arguments.FindPropertyRelative (kFloatArgument);
                break;
            case PersistentListenerMode.Int:
                argument = arguments.FindPropertyRelative (kIntArgument);
                break;
            case PersistentListenerMode.Object:
                argument = arguments.FindPropertyRelative (kObjectArgument);
                break;
            case PersistentListenerMode.String:
                argument = arguments.FindPropertyRelative (kStringArgument);
                break;
            case PersistentListenerMode.Bool:
                argument = arguments.FindPropertyRelative (kBoolArgument);
                break;
            default:
                argument = arguments.FindPropertyRelative (kIntArgument);
                break;
            }

            return argument;
        }

        /// <summary>
        /// Couldn't find this using Reflection so I had to copy it from UnityCsReference. It's pretty much intact.
        /// </summary>
        private string GetFunctionDropdownText (SerializedProperty pListener) {
            var listenerTarget = pListener.FindPropertyRelative (kInstancePath);
            var methodName = pListener.FindPropertyRelative (kMethodNamePath);
            var mode = pListener.FindPropertyRelative (kModePath);
            var arguments = pListener.FindPropertyRelative (kArgumentsPath);
            var desiredArgTypeName = arguments.FindPropertyRelative (kObjectArgumentAssemblyTypeName).stringValue;
            var desiredType = typeof (UnityEngine.Object);
            if (!string.IsNullOrEmpty (desiredArgTypeName))
                desiredType = Type.GetType (desiredArgTypeName, false) ?? desiredType;

            var buttonLabel = new StringBuilder ();
            if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty (methodName.stringValue)) {
                buttonLabel.Append (kNoFunctionString);
            } else if (!IsPersistantListenerValid (m_DummyEvent, methodName.stringValue, listenerTarget.objectReferenceValue, (PersistentListenerMode)mode.enumValueIndex, desiredType)) {
                var instanceString = "UnknownComponent";
                var instance = listenerTarget.objectReferenceValue;
                if (instance != null)
                    instanceString = instance.GetType ().Name;

                buttonLabel.Append (string.Format ("<Missing {0}.{1}>", instanceString, methodName.stringValue));
            } else {
                buttonLabel.Append (listenerTarget.objectReferenceValue.GetType ().Name);

                if (!string.IsNullOrEmpty (methodName.stringValue)) {
                    buttonLabel.Append (".");
                    if (methodName.stringValue.StartsWith ("set_"))
                        buttonLabel.Append (methodName.stringValue.Substring (4));
                    else
                        buttonLabel.Append (methodName.stringValue);
                }
            }

            return buttonLabel.ToString ();
        }

        /// <summary>
        /// Attempt to find new target object to match the original type
        /// </summary>
        protected void FindMethod (SerializedProperty listenerTarget, string assemblyTypeName, SerializedProperty methodName) {
            var target = listenerTarget.objectReferenceValue;
            if (target != null) {
                var desiredType = Type.GetType (assemblyTypeName, false);
                if (desiredType != null && desiredType != typeof (GameObject)) {
                    if (target is GameObject go) {
                        target = go.GetComponent (desiredType);
                    } else if (target is Component c) {
                        target = c.GetComponent (desiredType);
                    }
                }
            }
            if (target != null) {
                listenerTarget.objectReferenceValue = target;
            } else {
                methodName.stringValue = null;
            }
        }

        protected override void DrawEvent (Rect rect, int index, bool isActive, bool isFocused) {
            if (!EditorPrefs.GetBool (OpdKey, true)) {
                base.DrawEvent (rect, index, isActive, isFocused);
                return;
            }

            var pListener = m_ListenersArray.GetArrayElementAtIndex (index);
            var mode = (PersistentListenerMode)pListener.FindPropertyRelative (kModePath).enumValueIndex;

            rect.y++;
            Rect [] subRects = GetRowRects (rect, mode);
            Rect enabledRect = subRects [0];
            Rect functionRect = subRects [1];
            Rect goRect = subRects [2];
            Rect argRect = subRects [3];

            Highlighter.HighlightIdentifier (rect, $"{pListener.serializedObject.targetObject.GetInstanceID ()}.{pListener.propertyPath}");

            // find the current event target...
            var callState = pListener.FindPropertyRelative (kCallStatePath);
            var arguments = pListener.FindPropertyRelative (kArgumentsPath);
            var listenerTarget = pListener.FindPropertyRelative (kInstancePath);
            var methodName = pListener.FindPropertyRelative (kMethodNamePath);

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            EditorGUI.PropertyField (enabledRect, callState, GUIContent.none);

            EditorGUI.BeginChangeCheck ();
            {
                GUI.Box (goRect, GUIContent.none);
                var targetAssemblyTypeName = pListener.FindPropertyRelative (kInstanceTypePath).stringValue;

                EditorGUI.PropertyField (goRect, listenerTarget, GUIContent.none);
                if (EditorGUI.EndChangeCheck ())
                    FindMethod (listenerTarget, targetAssemblyTypeName, methodName);
            }

            var argument = GetArgument (pListener);
            //only allow argument if we have a valid target / method
            if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty (methodName.stringValue))
                mode = PersistentListenerMode.Void;

            var desiredArgTypeName = arguments.FindPropertyRelative (kObjectArgumentAssemblyTypeName).stringValue;
            var desiredType = typeof (UnityEngine.Object);
            if (!string.IsNullOrEmpty (desiredArgTypeName))
                desiredType = Type.GetType (desiredArgTypeName, false) ?? desiredType;

            if (m_DummyEvent == null) {
                var tUnityEventDrawer = typeof (UnityEventDrawer);
                m_DummyEvent = tUnityEventDrawer.GetField ("m_DummyEvent", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (this) as UnityEventBase;
            }

            GUIContent functionContent;
            if (EditorGUI.showMixedValue) {
                functionContent = s_MixedValueContent;
            } else {
                var buttonLabel = GetFunctionDropdownText (pListener);
                functionContent = new GUIContent (buttonLabel, buttonLabel);
                functionContent.tooltip = buttonLabel;
            }

            var hasArgument = mode != PersistentListenerMode.Void && mode != PersistentListenerMode.EventDefined;

            functionRect.width = Mathf.Min (rect.width * (hasArgument ? .7f : 1f), EditorStyles.popup.CalcSize (functionContent).x);
            argRect.xMin = rect.xMin + functionRect.width + EditorGUIUtility.standardVerticalSpacing;

            if (mode == PersistentListenerMode.Object) {
                EditorGUI.BeginChangeCheck ();
                var result = EditorGUI.ObjectField (argRect, GUIContent.none, argument.objectReferenceValue, desiredType, true);
                if (EditorGUI.EndChangeCheck ())
                    argument.objectReferenceValue = result;
            } else if (hasArgument)
                EditorGUI.PropertyField (argRect, argument, GUIContent.none);

            using (new EditorGUI.DisabledScope (listenerTarget.objectReferenceValue == null)) {
                EditorGUI.BeginProperty (functionRect, GUIContent.none, methodName);
                {
                    if (EditorGUI.DropdownButton (functionRect, functionContent, FocusType.Passive, EditorStyles.popup)) {
                        var genericMenu = miBuildPopupList.Invoke (null, new object [] { listenerTarget.objectReferenceValue, m_DummyEvent, pListener }) as GenericMenu;
                        genericMenu.DropDown (functionRect);
                    }
                }
                EditorGUI.EndProperty ();
            }
            GUI.backgroundColor = c;
        }
    }

}
