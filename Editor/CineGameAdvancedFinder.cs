using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Text;

public class CineGameAdvancedFinder : EditorWindow {
	//Vector2 buildReportScrollPosition = Vector2.zero;
	private int LayerIndex;
	private string MethodName;
	private string TagName;
	private string TextString;
	private int AtlasName;

	[Serializable]
	public class Result {
		public UnityEngine.Object obj;
		public string text;
	}
	private List<Result> Results = new List<Result> ();
	private Vector2 resultScrollPosition;

	static CineGameAdvancedFinder instance;

	public CineGameAdvancedFinder() {
		instance = this;
	}

	void OnGUI () {
		Event e = Event.current;
		if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return) {
			switch (GUI.GetNameOfFocusedControl ()) {
			case "Method":
				FindInvokationsOfMethod (MethodName);
				break;
			case "Text":
				FindTextString (TextString);
				break;
			default:
				Debug.Log ("Unknown control " + GUI.GetNameOfFocusedControl ());
				break;
			}
		}

		var buttonMaxWidthOption = GUILayout.MaxWidth (100f);
		var inputMaxWidthOption = GUILayout.MaxWidth (400f);

		EditorGUILayout.BeginHorizontal ();
		var li = EditorGUILayout.LayerField (new GUIContent ("Layer:", "Find all gameobjects in the given layer"), LayerIndex, inputMaxWidthOption);
		if (li != LayerIndex || GUILayout.Button ("Find objects", buttonMaxWidthOption)) {
			LayerIndex = li;
			FindObjectsInLayer (LayerIndex);
		}
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		var tag = EditorGUILayout.TagField (new GUIContent ("Tag:","Find all gameobjects in a scene with the given tag"), TagName, inputMaxWidthOption);
		if (tag != TagName || GUILayout.Button ("Find objects", buttonMaxWidthOption)) {
			TagName = tag;
			FindObjectsWithTag (TagName);
		}
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		GUI.SetNextControlName ("Method");
		MethodName = EditorGUILayout.TextField (new GUIContent ("Method:", "Whole or partial method and class name to find references to"), MethodName, inputMaxWidthOption);
		if (GUILayout.Button ("Find references", buttonMaxWidthOption)) {
			FindInvokationsOfMethod (MethodName);
		}
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		var atlasLabelContent = new GUIContent ("Sprite Atlas:", "Find all sprites packed in the given atlas");
		if (Packer.atlasNames.Length == 0) {
			EditorGUILayout.PrefixLabel (atlasLabelContent);
			if (GUILayout.Button ("Rebuild Sprite Atlas", inputMaxWidthOption)) {
				Packer.RebuildAtlasCacheIfNeeded (EditorUserBuildSettings.activeBuildTarget, true);
			}
		} else {
			var selectedAtlas = EditorGUILayout.Popup (atlasLabelContent, AtlasName, Packer.atlasNames, inputMaxWidthOption);
			if (selectedAtlas != AtlasName || GUILayout.Button ("Find sprites", buttonMaxWidthOption)) {
				AtlasName = selectedAtlas;
				FindTextureAssetsInAtlas (Packer.atlasNames [selectedAtlas]);
			}
		}
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		GUI.SetNextControlName ("Text");
		TextString = EditorGUILayout.TextField (new GUIContent ("Text:", "Substring to search all loaded scenes' text components for"), TextString, inputMaxWidthOption);
		if (!string.IsNullOrWhiteSpace (TextString) && GUILayout.Button ("Find text", buttonMaxWidthOption)) {
			FindTextString (TextString);
		}
		EditorGUILayout.EndHorizontal ();

		if (GUILayout.Button (new GUIContent ("Find missing references","Find all missing references in open scenes"), inputMaxWidthOption)) {
			FindMissingReferences ();
		}

		EditorGUILayout.Separator ();

		var labelBoldStyle = new GUIStyle (GUI.skin.label);
		labelBoldStyle.fontStyle = FontStyle.Bold;
		labelBoldStyle.alignment = TextAnchor.MiddleLeft;

		if (Results.Count != 0) {
			Rect rScroll = EditorGUILayout.BeginVertical ();
			resultScrollPosition = EditorGUILayout.BeginScrollView (resultScrollPosition, false, true, GUILayout.Height (rScroll.height));
			//var lineHeightOption = GUILayout.Height (GUI.skin.font.lineHeight * 1.5f);
			for (int i = 0; i < Results.Count; i++) {
				//EditorGUILayout.SelectableLabel (Results [i].text, lineHeightOption);
				if (GUILayout.Button (Results [i].text, labelBoldStyle)) {
					ShowObject (Results [i].obj);
				}
				EditorGUILayout.Separator ();
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndScrollView ();
			EditorGUILayout.EndVertical ();
		} else {
			EditorGUILayout.LabelField ("No results.");
		}
	}

	/// <summary>
	/// Change selection to the given object. If obj is a component, expand it in the inspector window.
	/// </summary>
	private void ShowObject (UnityEngine.Object obj) {
		if (PrefabUtility.IsPartOfPrefabAsset (obj)) {
			AssetDatabase.OpenAsset (AssetDatabase.LoadAssetAtPath<GameObject> (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot (obj)));
		}
		Selection.activeObject = obj;
		var component = obj as Component;
		if (component != null) {
			UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded (component, true);
			ActiveEditorTracker.sharedTracker.ForceRebuild ();
		}
	}

	[MenuItem ("CineGame/Advanced Finder %g")]
	static void Init () {
		if (instance == null) {
			instance = GetWindow<CineGameAdvancedFinder> ("Advanced Finder", true);
		}
		instance.Focus ();
	}

	static void RepaintWindow () {
		if (instance != null) {
			EditorUtility.SetDirty (instance);
			instance.Repaint ();
		}
	}

	void FindObjectsInLayer (int layerIndex) {
		Results.Clear ();
		var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
		foreach (var obj in allObjects) {
			if (obj.scene.isLoaded && obj.layer == layerIndex) {
				Results.Add (new Result { obj = obj, text = obj.GetScenePath () });
			}
		}
	}

	void FindObjectsWithTag (string tag) {
		Results.Clear ();
		var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
		foreach (var obj in allObjects) {
			if (obj.scene.isLoaded && obj.tag == tag) {
				Results.Add (new Result { obj = obj, text = obj.GetScenePath () });
			}
		}
	}

	void FindTextString (string textString) {
		Results.Clear ();
		var allObjects = Resources.FindObjectsOfTypeAll (typeof (Text)) as Text [];
		foreach (var obj in allObjects) {
			if (obj.gameObject.scene.isLoaded && obj.text.IndexOf (textString, StringComparison.InvariantCultureIgnoreCase) >= 0) {
				Results.Add (new Result { obj = obj, text = obj.gameObject.GetScenePath () });
			}
		}
	}

	void FindInvokationsOfMethod (string partialMethodName) {
		Results.Clear ();
		if (string.IsNullOrEmpty (partialMethodName))
			return;
		var lioDot = partialMethodName.LastIndexOf ('.');
		string methodMatch, classMatch;
		if (lioDot > 0) {
			methodMatch = partialMethodName.Substring (lioDot + 1);
			classMatch = partialMethodName.Substring (0, lioDot);
		} else {
			methodMatch = classMatch = partialMethodName;
		}
		var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
		for (int j = 0; j < allObjects.Length; j++) {
			var gameObject = allObjects [j];
			if (!gameObject.scene.isLoaded)
				continue;
			var components = gameObject.GetComponents<Component> ();
			for (int i = 0; i < components.Length; i++) {
				var component = components [i];
				if (!component)
					continue;
				var componentType = component.GetType ();
				var fields = componentType.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields) {
					if (field.GetValue (component) is UnityEventBase unityEvent) {
						FindMatchingInvokation (component, field.Name, unityEvent, classMatch, methodMatch);
					} else if (field.GetValue (component) is List<UnityEngine.EventSystems.EventTrigger.Entry> eventTriggers) {
						foreach (var et in eventTriggers) {
							FindMatchingInvokation (component, $"{field.Name}.{et.eventID}", et.callback, classMatch, methodMatch);
						}
					}
				}
			}
		}
	}

	void FindMatchingInvokation (Component component, string fieldName, UnityEventBase unityEvent, string classMatch, string methodMatch) {
		for (int k = 0; k < unityEvent.GetPersistentEventCount (); k++) {
			var methodName = unityEvent.GetPersistentMethodName (k);
			var classPath = unityEvent.GetPersistentTarget (k).GetType ().FullName;
			var hasClassMatch = classPath.IndexOf (classMatch, StringComparison.InvariantCultureIgnoreCase) >= 0;
			var hasMethodMatch = methodName.IndexOf (methodMatch, StringComparison.InvariantCultureIgnoreCase) >= 0;
			if ((classMatch != methodMatch && hasClassMatch && hasMethodMatch)
			 || (classMatch == methodMatch && (hasClassMatch || hasMethodMatch))) {
				Results.Add (new Result { obj = component, text = component.gameObject.GetScenePath () + " " + component.GetType ().Name + "." + fieldName + " => " + classPath + "." + methodName });
				break;
			}
		}
	}

	/// <summary>
	/// Locates all textures packed as sprites in an atlas of a given name.
	/// </summary>
	void FindTextureAssetsInAtlas (string atlasName) {
		Results.Clear ();
		var indexOfParenth = atlasName.IndexOf ('(');
		var packingTag = indexOfParenth > 0 ? atlasName.Substring (0, indexOfParenth - 1) : atlasName;
		//var textures = Packer.GetTexturesForAtlas (packingTag);
		var assetPaths = AssetDatabase.FindAssets ("t:Texture2D").Select (guid => AssetDatabase.GUIDToAssetPath (guid));
		foreach (var assetPath in assetPaths) {
			var ti = AssetImporter.GetAtPath (assetPath) as TextureImporter;
			if (ti != null && ti.textureType == TextureImporterType.Sprite && ti.spritePackingTag == packingTag) {
				Results.Add (new Result { obj = AssetDatabase.LoadMainAssetAtPath (assetPath), text = assetPath });
			}
		}
	}

	Type GetType (string fullName) {
		var assemblies = AppDomain.CurrentDomain.GetAssemblies ();
		foreach (var assembly in assemblies) {
			var t = assembly.GetType (fullName);
			if (t != null) {
				Debug.Log ("Found type " + t.FullName);
				return t;
			}
		}
		return null;
	}

	/*
	UnityEditor.BuildPlayerWindow
	UnityEditor.ConsoleWindow
	UnityEditor.ObjectSelector
	UnityEditor.ProjectBrowser
	UnityEditor.SceneHierarchySortingWindow
	UnityEditor.SceneHierarchyWindow
	UnityEditor.InspectorWindow
	UnityEditor.PreviewWindow
	UnityEditor.PlayModeView
	UnityEditor.SearchableEditorWindow
	UnityEditor.LightingExplorerWindow
	UnityEditor.LightingWindow
	UnityEditor.LightmapPreviewWindow
	UnityEditor.SceneView,
	UnityEditor.SettingsWindow,
	UnityEditor.ProjectSettingsWindow,
	UnityEditor.PreferenceSettingsWindow,
	UnityEditor.SpriteUtilityWindow,
	*/

	void FindMissingReferences () {
		/*var type = GetType ("UnityEditor.InspectorWindow");
		var inspectorWindow = GetWindow (type);
		inspectorWindow.*/

		Results.Clear ();
		var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
		foreach (var go in allObjects) {
			var components = go.GetComponents<Component> ();
			foreach (var component in components) {
				if (component == null) {
					Debug.LogError ("Missing script found on: " + go.GetScenePath (), go);
				} else {
					var so = new SerializedObject (component);
					var sp = so.GetIterator ();

					while (sp.NextVisible (true)) {
						if (sp.propertyType != SerializedPropertyType.ObjectReference) {
							continue;
						}

						if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0) {
							Results.Add (new Result { obj = component, text = (PrefabUtility.IsPartOfPrefabAsset (component) ? "[PREFAB] " : string.Empty) + go.GetScenePath () + " " + component.GetType () + "." + sp.propertyPath });
						}
					}
				}
			}
		}
	}
}

static class ExtensionMethods {
	static void AppendParentName (ref StringBuilder sb, Transform t, char separator) {
		if (t.parent != null) {
			AppendParentName (ref sb, t.parent, separator);
		}
		sb.Append (separator);
		sb.Append (t.name);
	}

	/// <summary>
	/// Builds a 'path' to the specified GameObject in the scene hierarchy. Useful for logging/debugging
	/// </summary>
	public static string GetScenePath (this GameObject obj, char separator = '/') {
		var sb = new StringBuilder ();
		sb.Append (obj.scene.name);
		AppendParentName (ref sb, obj.transform, separator);
		return sb.ToString ();
	}
}
