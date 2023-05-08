using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using CineGame.MobileComponents;

namespace CineGameEditor.MobileComponents {
	[InitializeOnLoad]
	public class GameComponentsHierarchyDrawer {
		static Texture2D texturePanel;
		static Texture2D textureAppIcon;

		static List<IGameComponentIcon> gameComponentsList = new List<IGameComponentIcon> ();

		static GameComponentsHierarchyDrawer () {
			// Init
			texturePanel = AssetDatabase.LoadAssetAtPath (AssetDatabase.GUIDToAssetPath (Util.CinematazticIconGUID), typeof (Texture2D)) as Texture2D;
			ReloadAppIcon ();
			EditorApplication.hierarchyWindowItemOnGUI -= HierarchyItemCB;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
			SceneView.duringSceneGui -= SceneView_OnSceneGUIDelegate;
			SceneView.duringSceneGui += SceneView_OnSceneGUIDelegate;
		}

		static void SceneView_OnSceneGUIDelegate (SceneView sceneView) {
			if (textureAppIcon != null) {
				Handles.BeginGUI ();
				var r = new Rect (0f, 0f, 50f, 50f);
				GUI.Label (r, textureAppIcon);
				Handles.EndGUI ();
			}
		}

		/// <summary>
		/// Refresh app icon (eg after switching region in editor or via script.
		/// We prefer android icons as they contain alpha, but fall back on default if Android is not overridden.
		/// </summary>
		public static void ReloadAppIcon () {
			var textures = PlayerSettings.GetIconsForTargetGroup (BuildTargetGroup.Android);
			if (textures == null || textures.Length == 0 || textures [0] == null) {
				textures = PlayerSettings.GetIconsForTargetGroup (BuildTargetGroup.Unknown);
			}
			if (textures != null && textures.Length > 0 && textures [0] != null) {
				textureAppIcon = textures [0];
			}
			EditorApplication.RepaintHierarchyWindow ();
		}

		static Transform prevItemTransform;
		static bool prevItemHadGameComponentInChildren;
		static Rect prevItemIconRect;

		static void HierarchyItemCB (int instanceID, Rect selectionRect) {
			var go = EditorUtility.InstanceIDToObject (instanceID) as GameObject;

			if (gameComponentsList.Count != 0 && (go == null || go.transform.parent != prevItemTransform)) {
				//One or more children has a GameComponent. draw icon with half opacity
				var c = GUI.color;
				c.a = .5f;
				GUI.color = c;
				GUI.Label (prevItemIconRect, new GUIContent (texturePanel, string.Join ('\n', gameComponentsList.Select (gc => gc.GetType ().Name).Distinct ())));
				c.a = 1f;
				GUI.color = c;
				gameComponentsList.Clear ();
			}

			if (go != null) {
				// Item is a gameobject. if it has a GameComponent directly attached, draw icon with full opacity
				var xpos = selectionRect.x + selectionRect.width - 16;
				var r = new Rect (xpos, selectionRect.y, 18, 18);
				go.GetComponents<IGameComponentIcon> (gameComponentsList);
				if (gameComponentsList.Count != 0) {
					go.GetComponentsInChildren<IGameComponentIcon> (true, gameComponentsList);
					GUI.Label (r, new GUIContent (texturePanel, string.Join ('\n', gameComponentsList.Select (gc => gc.GetType ().Name).Distinct ())));
					gameComponentsList.Clear ();
				} else {
					go.GetComponentsInChildren<IGameComponentIcon> (true, gameComponentsList);
					prevItemIconRect = r;
					prevItemTransform = go.transform;
				}
			} else {
				/*// Item is a scene. Draw app icon and region
				size += 7f;
				Rect r;
				if (textureAppIcon != null) {
					r = new Rect (selectionRect.x + selectionRect.width - size, selectionRect.y - 2.5f, size, size);
					GUI.Label (r, textureAppIcon);
				}
				r = new Rect (selectionRect.x + selectionRect.width - size - 24, selectionRect.y, 24, size - 8f);
				GUI.Label (r, Util.GetRegion ().ToString ());*/
			}
		}
	}
}
