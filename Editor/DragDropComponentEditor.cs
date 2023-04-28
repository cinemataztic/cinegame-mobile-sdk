using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
	[CustomEditor (typeof (DragDropComponent))]
	[CanEditMultipleObjects]
	public class DragDropComponentEditor : EventEditorBase {
	}
}