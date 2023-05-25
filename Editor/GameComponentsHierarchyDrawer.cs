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

		static List<IGameComponentIcon> gameComponentsList = new ();

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
			var textures = PlayerSettings.GetIcons (UnityEditor.Build.NamedBuildTarget.Android, IconKind.Application);
			if (textures == null || textures.Length == 0 || textures [0] == null) {
				textures = PlayerSettings.GetIcons (UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Application);
			}
			if (textures != null && textures.Length > 0 && textures [0] != null) {
				var t = textures [0];
				textureAppIcon = new Texture2D (t.width, t.height, t.format, true);
				Graphics.CopyTexture (t, 0, 0, textureAppIcon, 0, 0);
				textureAppIcon.Apply (true, true);
				AppIconContent = new GUIContent (textureAppIcon) {
					tooltip = $"{PlayerSettings.productName} {Util.GetRegion ()}"
				};
			}
			EditorApplication.RepaintHierarchyWindow ();
		}

		static GUIContent AppIconContent;
		static Transform prevItemTransform;
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
				go.GetComponents (gameComponentsList);
				if (gameComponentsList.Count != 0) {
					go.GetComponentsInChildren (true, gameComponentsList);
					GUI.Label (r, new GUIContent (texturePanel, string.Join ('\n', gameComponentsList.Select (gc => gc.GetType ().Name).Distinct ())));
					gameComponentsList.Clear ();
				} else {
					go.GetComponentsInChildren (true, gameComponentsList);
					prevItemIconRect = r;
					prevItemTransform = go.transform;
				}
			} else {
				// Item is a scene. Draw app icon and region, if available
				if (AppIconContent != null) {
					var size = selectionRect.height + 6f;
					var r = new Rect (selectionRect.x - 3f, selectionRect.y - 2f, size, size);
					GUI.Label (r, AppIconContent);
				}
			}
		}
	}
}
