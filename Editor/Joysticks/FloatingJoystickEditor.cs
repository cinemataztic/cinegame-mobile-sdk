using CineGame.MobileComponents;
using UnityEditor;
using UnityEngine;

namespace CineGameEditor.MobileComponents
{
    [CustomEditor(typeof(FloatingJoystick))]
    public class FloatingJoystickEditor : JoystickEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (background.objectReferenceValue != null)
            {
                RectTransform backgroundRect = (RectTransform)background.objectReferenceValue;
                backgroundRect.anchorMax = Vector2.zero;
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.pivot = center;
            }
        }
    }
}