using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
    [CustomEditor (typeof (DragDropAnywhereComponent))]
    public class DragDropAnywhereComponentEditor : Editor {
        DragDropAnywhereComponent dragDropComponent;

        private void OnEnable () {
            dragDropComponent = target as DragDropAnywhereComponent;
        }

        private void OnSceneGUI () {
            Handles.color = Color.red;
            Handles.CircleHandleCap
            (
                0,
                dragDropComponent.transform.position,
                Quaternion.identity,
                dragDropComponent.Radius,
                EventType.Repaint
            );
        }
    }
}
