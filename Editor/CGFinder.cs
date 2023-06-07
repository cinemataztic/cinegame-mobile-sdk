using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;

using UnityEditor;
using UnityEditor.Sprites;

using Unity.EditorCoroutines.Editor;
using UnityEngine.UI;
using UnityEditor.UI;

namespace LaxityAssets {

	public class CGFinder : EditorWindow {
		//Vector2 buildReportScrollPosition = Vector2.zero;
		private int LayerIndex;
		private string MethodName;
		private string TagName;
		private string TextString;
		private int AtlasName;

		[Serializable]
		public class Result {
			public UnityEngine.Object obj;
			public string propertyPath;
			public string text;
		}
		private static string ResultsLabel;
		private static readonly List<Result> Results = new ();
		private Vector2 resultScrollPosition;
		private Rect ResultsScrollRect;
		private static EditorCoroutine ScrollToCoroutine;

		static CGFinder instance;

		readonly HashSet<string> ListInvokations = new ();
		readonly HashSet<string> ListStrings = new ();

		readonly MethodInfo miEndEditingActiveTextField;
		readonly Type InspectorWindowType, PropertyEditorType;
		EditorWindow InspectorWindow;
		Rect MethodRect, TextRect;
		Texture2D SolidBlackTexture;

		/*
		/// <summary>
		/// Build cached list of all Component types in all assemblies
		/// </summary>
		List<Type> GetComponentTypes () {
			ComponentTypes = new List<Type> (1024);
			ComponentTypes.Add (typeof(GameObject));
			ComponentTypes.AddRange (AppDomain.CurrentDomain.GetAssemblies ().Where (a => !a.IsDynamic).SelectMany (a => a.GetTypes ().Where (c => c.IsSubclassOf (typeof(Component)))));
			return ComponentTypes;
		}*/

		public CGFinder () {
			instance = this;

			miEndEditingActiveTextField = typeof (EditorGUI).GetMethod ("EndEditingActiveTextField", BindingFlags.Static | BindingFlags.NonPublic);
			InspectorWindowType = Type.GetType ("UnityEditor.InspectorWindow,UnityEditor");
			PropertyEditorType = Type.GetType ("UnityEditor.PropertyEditor,UnityEditor");

			wantsMouseMove = true;
		}

		void OnEnable () {
			SolidBlackTexture = new Texture2D (1, 1);
			SolidBlackTexture.SetPixel (0, 0, new Color32 (0x20, 0x70, 0xc0, 0xff));
			SolidBlackTexture.Apply ();
		}

		void OnHierarchyChange () {
			ListInvokations.Clear ();
			ListStrings.Clear ();
		}

		void OnGUI () {
			Event e = Event.current;
			if (ComboBoxPopup.Instance == null || !ComboBoxPopup.HandleEvent (e)) {
				if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return) {
					switch (GUI.GetNameOfFocusedControl ()) {
					case "Method":
						FindInvokationsOfMethod (MethodName);
						break;
					case "Text":
						FindString (TextString);
						break;
					default:
						Debug.Log ("Unknown control " + GUI.GetNameOfFocusedControl ());
						break;
					}
				}
			}

			var buttonMaxWidthOption = GUILayout.MaxWidth (100f);
			var bigButtonMaxWidthOption = GUILayout.MaxWidth (160f);
			var expandWidthOption = GUILayout.ExpandWidth (true);

			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal ();
			var li = EditorGUILayout.LayerField (new GUIContent ("Layer:", "Find all gameobjects in the given layer"), LayerIndex, expandWidthOption);
			if (li != LayerIndex || GUILayout.Button ("Find objects", buttonMaxWidthOption)) {
				LayerIndex = li;
				FindObjectsInLayer (LayerIndex);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			var tag = EditorGUILayout.TagField (new GUIContent ("Tag:", "Find all gameobjects in a scene with the given tag"), TagName, expandWidthOption);
			if (tag != TagName || GUILayout.Button ("Find objects", buttonMaxWidthOption)) {
				TagName = tag;
				FindObjectsWithTag (TagName);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (new GUIContent ("Method:", "Whole or partial method and class name to find references to"));
			GUI.SetNextControlName ("Method");
			EditorGUI.BeginChangeCheck ();
			MethodName = EditorGUILayout.TextField (MethodName, expandWidthOption);
			if (Event.current.type == EventType.Repaint) {
				MethodRect = GUILayoutUtility.GetLastRect ();
			}
			if (EditorGUI.EndChangeCheck ()) {
				IEnumerable<string> matches = null;
				if (MethodName.Trim ().Length > 1) {
					var str = MethodName.Trim ();
					if (ListInvokations.Count == 0) {
						FindAllInvokations ();
					}
					var indexOfDot = str.LastIndexOf ('.');
					if (indexOfDot > 0) {
						var compName = str.Substring (0, indexOfDot + 1);
						var methMatch = str.Substring (indexOfDot + 1);
						matches = ListInvokations.Where (i => i.StartsWith (compName) && i.Substring (indexOfDot + 1).Contains (methMatch, StringComparison.InvariantCultureIgnoreCase));
					} else {
						matches = ListInvokations.Where (i => i.Contains (str, StringComparison.InvariantCultureIgnoreCase));
					}
				}
				if (matches != null && matches.Count () != 0) {
					ComboBoxPopup.Show (matches, MethodRect, (value) => {
						MethodName = value;
						miEndEditingActiveTextField.Invoke (null, null);
						FindInvokationsOfMethod (MethodName);
						EditorGUI.FocusTextInControl ("Method");
						SendEvent (new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow });
					});
				} else if (ComboBoxPopup.Instance != null) {
					ComboBoxPopup.Instance.Close ();
				}
			}
			if (GUILayout.Button ("Find references", buttonMaxWidthOption)) {
				FindInvokationsOfMethod (MethodName);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			var atlasLabelContent = new GUIContent ("Sprite Atlas:", "Find all sprites packed in the given atlas");
			if (Packer.atlasNames.Length == 0) {
				EditorGUILayout.PrefixLabel (atlasLabelContent);
				if (GUILayout.Button ("Rebuild Sprite Atlas", bigButtonMaxWidthOption)) {
					Packer.RebuildAtlasCacheIfNeeded (EditorUserBuildSettings.activeBuildTarget, true);
				}
			} else {
				var selectedAtlas = EditorGUILayout.Popup (atlasLabelContent, AtlasName, Packer.atlasNames, expandWidthOption);
				if (selectedAtlas != AtlasName || GUILayout.Button ("Find sprites", buttonMaxWidthOption)) {
					AtlasName = selectedAtlas;
					FindTextureAssetsInAtlas (Packer.atlasNames [selectedAtlas]);
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (new GUIContent ("String:", "Whole or partial string to search for"));
			GUI.SetNextControlName ("Text");
			EditorGUI.BeginChangeCheck ();
			TextString = EditorGUILayout.TextField (TextString, expandWidthOption);
			if (Event.current.type == EventType.Repaint) {
				TextRect = GUILayoutUtility.GetLastRect ();
			}
			if (EditorGUI.EndChangeCheck ()) {
				IEnumerable<string> matches = null;
				if (TextString.Trim ().Length > 1) {
					var str = TextString.Trim ();
					if (ListStrings.Count == 0) {
						FindAllStrings ();
					}
					matches = ListStrings.Where (i => i.Contains (str, StringComparison.InvariantCultureIgnoreCase));
				}
				if (matches != null && matches.Count () != 0) {
					ComboBoxPopup.Show (matches, TextRect, (value) => {
						TextString = value;
						miEndEditingActiveTextField.Invoke (null, null);
						FindString (TextString);
						EditorGUI.FocusTextInControl ("Text");
						SendEvent (new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow });
					});
				} else if (ComboBoxPopup.Instance != null) {
					ComboBoxPopup.Instance.Close ();
				}
			}
			if (GUILayout.Button ("Find string", buttonMaxWidthOption)) {
				FindString (TextString);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.PrefixLabel (new GUIContent ("Missing references:", "Find all missing references in open scenes"));
			if (GUILayout.Button (new GUIContent ("Find missing references", "Find all missing references in open scenes"), bigButtonMaxWidthOption)) {
				FindMissingReferences ();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();

			var buttonHoverStyle = new GUIStyle (GUI.skin.button);
			buttonHoverStyle.alignment = TextAnchor.MiddleLeft;
			buttonHoverStyle.onHover.background = SolidBlackTexture;
			buttonHoverStyle.hover.textColor = Color.cyan;
			buttonHoverStyle.richText = true;

			Result SelectedResult = null;
			if (!string.IsNullOrWhiteSpace (ResultsLabel)) {
				EditorGUILayout.LabelField (ResultsLabel);
			}
			var cnt = Results.Count;
			if (cnt != 0) {
				ResultsScrollRect = EditorGUILayout.BeginVertical ();
				resultScrollPosition = EditorGUILayout.BeginScrollView (resultScrollPosition, false, false, GUILayout.ExpandHeight (true));
				for (int i = 0; i < Results.Count; i++) {
					if (GUILayout.Button (Results [i].text, buttonHoverStyle)) {
						SelectedResult = Results [i];
					}
				}
				GUI.backgroundColor = Color.white;
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndScrollView ();
				EditorGUILayout.EndVertical ();
				GUI.Box (ResultsScrollRect, GUIContent.none);
			}

			if (SelectedResult != null) {
				ShowResult (SelectedResult);
			}
		}

		/// <summary>
		/// Change selection to the given object. If obj is a component, expand it in the inspector window and scroll to it.
		/// </summary>
		private void ShowResult (Result result) {
			if (PrefabUtility.IsPartOfPrefabAsset (result.obj)) {
				AssetDatabase.OpenAsset (AssetDatabase.LoadAssetAtPath<GameObject> (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot (result.obj)));
			}
			Selection.activeObject = result.obj;
			var component = result.obj as Component;
			if (component != null) {
				UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded (component, true);
				ActiveEditorTracker.sharedTracker.ForceRebuild ();

				//Scroll to component editor. This is not possible through the public API so we use reflection to dig through the internals
				InspectorWindow = GetWindow (InspectorWindowType);
				typeof (EditorWindow).GetMethod ("RepaintImmediately", BindingFlags.NonPublic | BindingFlags.Instance).Invoke (InspectorWindow, null);

				var tr = (ActiveEditorTracker)PropertyEditorType.GetProperty ("tracker")?.GetValue (InspectorWindow);
				if (tr != null) {
					var editors = tr.activeEditors;
					for (int i = 0; i < editors.Length; i++) {
						if (editors [i].targets.Any (t => t == result.obj)) {
							var root = InspectorWindow.rootVisualElement.Q (className: "unity-inspector-editors-list");
							if (root != null && root.childCount > i) {
								var child = root [i];
								if (ScrollToCoroutine != null) {
									EditorCoroutineUtility.StopCoroutine (ScrollToCoroutine);
								}
								var highlightIdentifier = !string.IsNullOrWhiteSpace (result.propertyPath) ? $"{component.GetInstanceID ()}.{result.propertyPath}" : null;
								ScrollToCoroutine = EditorCoroutineUtility.StartCoroutine (E_ScrollTo (child, highlightIdentifier), this);
							}
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// The ScrollTo method usually takes two tries for some odd reason, probably dependant on some repainting.
		/// </summary>
		IEnumerator E_ScrollTo (VisualElement visualElement, string highlightIdentifier) {
			Highlighter.Stop ();
			var inspectorScrollView = (ScrollView)PropertyEditorType.GetField ("m_ScrollView", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue (InspectorWindow);
			yield return null;
			inspectorScrollView?.ScrollTo (visualElement);
			yield return null;
			inspectorScrollView?.ScrollTo (visualElement);

			if (!string.IsNullOrWhiteSpace (highlightIdentifier)) {
				Highlighter.Highlight ("Inspector", highlightIdentifier, HighlightSearchMode.Identifier);
				yield return new WaitForSecondsRealtime (3f);
				Highlighter.Stop ();
			}
		}

		[MenuItem ("CineGame/CG Finder %g")]
		static void Init () {
			if (instance == null) {
				instance = GetWindow<CGFinder> ("CG Finder", true);
			}
			instance.Focus ();
		}

		[MenuItem (itemName: "GameObject/CGFinder/Find references", isValidateFunction: false, priority: 0)]
		[MenuItem (itemName: "Assets/CGFinder/Find references", isValidateFunction: false, priority: 0)]
		static void FindReferencesToSelectedObject () {
			var activeObject = Selection.activeObject;
			activeObject = (activeObject is Component c) ? c.gameObject : activeObject;
			Init ();
			Results.Clear ();
			var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			var allComponents = allObjects.Where (o => o.scene.isLoaded).SelectMany (o => o.GetComponents<Component> ());
			foreach (var component in allComponents) {
				if (component == null)
					continue;
				var go = component.gameObject;
				var so = new SerializedObject (component);
				var sp = so.GetIterator ();
				SerializedProperty pCalls;
				if (!sp.NextVisible (true))
					continue;
				bool enterChildren;
				do {
					enterChildren = true;
					if (sp.propertyType == SerializedPropertyType.Generic && (pCalls = sp.FindPropertyRelative ("m_PersistentCalls.m_Calls")) != null) {
						var len = pCalls.arraySize;
						for (int i = 0; i < len; i++) {
							//Check if object is used as a target
							var pCall = pCalls.GetArrayElementAtIndex (i);
							var oa = pCall.FindPropertyRelative ("m_Target").objectReferenceValue;
							if (oa != null) {
								var parentObject = (oa is Component co) ? co.gameObject : oa;
								if (parentObject == activeObject)
									AddResult (component, pCall.propertyPath, NicePropertyPath (sp));
							}
							//Check if object is used as an argument
							oa = pCall.FindPropertyRelative ("m_Arguments.m_ObjectArgument").objectReferenceValue;
							if (oa != null) {
								var parentObject = (oa is Component co) ? co.gameObject : oa;
								if (parentObject == activeObject)
									AddResult (component, pCall.propertyPath, NicePropertyPath (sp));
							}
						}
						enterChildren = false;
					} else if (sp.propertyType == SerializedPropertyType.ObjectReference) {
						//Check if object is used as a reference
						var oa = sp.objectReferenceValue;
						var parentObject = (oa is Component co) ? co.gameObject : oa;
						if (parentObject == activeObject)
							AddResult (component, sp.propertyPath, NicePropertyPath (sp));
					}
				} while (sp.NextVisible (enterChildren));
			}
			if (activeObject is GameObject ago) {
				ResultsLabel = $"Found {Results.Count} references to {ago.GetScenePath ()}.";
			} else {
				ResultsLabel = $"Found {Results.Count} references to {AssetDatabase.GetAssetPath (activeObject)}.";
			}
			if (Results.Count == 1) {
				instance.ShowResult (Results [0]);
			}
			instance.Repaint ();
		}

		static string NicePropertyPath (SerializedProperty sp) {
			var pp = sp.propertyPath;
			return pp.Replace (".Array.data[", "[");
		}

		void FindObjectsInLayer (int layerIndex) {
			Results.Clear ();
			var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			foreach (var obj in allObjects) {
				if (obj.scene.isLoaded && obj.layer == layerIndex) {
					Results.Add (new Result { obj = obj, text = obj.GetScenePath () });
			}
			}
			ResultsLabel = $"Found {Results.Count} GameObjects in {LayerMask.LayerToName (layerIndex)} layer.";
			if (Results.Count == 1)
				ShowResult (Results [0]);
		}

		void FindObjectsWithTag (string tag) {
			Results.Clear ();
			var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			foreach (var obj in allObjects) {
				if (obj.scene.isLoaded && obj.tag == tag) {
					AddResult (obj);
				}
			}
			ResultsLabel = $"Found {Results.Count} GameObjects with {tag} tag.";
			if (Results.Count == 1)
				ShowResult (Results [0]);
		}

		void FindString (string textString) {
			Results.Clear ();
			var allGOs = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			var allComponents = allGOs.Where (o => o.scene.isLoaded).SelectMany (o => o.GetComponents<Component> ());
			foreach (var component in allComponents) {
				if (component == null)
					continue;
				var so = new SerializedObject (component);
				var sp = so.GetIterator ();
				SerializedProperty pCalls;
				if (!sp.NextVisible (true))
					continue;
				bool enterChildren;
				do {
					enterChildren = true;
					if (sp.propertyType == SerializedPropertyType.Generic && (pCalls = sp.FindPropertyRelative ("m_PersistentCalls.m_Calls")) != null) {
						var len = pCalls.arraySize;
						for (int i = 0; i < len; i++) {
							var pCall = pCalls.GetArrayElementAtIndex (i);
							var sv = pCall.FindPropertyRelative ("m_Arguments.m_StringArgument").stringValue;
							if (sv.Replace ('\n', ' ').Contains (textString, StringComparison.InvariantCultureIgnoreCase))
								AddResult (component, pCall.propertyPath, NicePropertyPath (sp));
						}
						enterChildren = false;
					} else if (sp.propertyType == SerializedPropertyType.String
					 && sp.stringValue.Replace ('\n', ' ').Contains (textString, StringComparison.InvariantCultureIgnoreCase)) {
						AddResult (component, sp.propertyPath, NicePropertyPath (sp));
					}
				} while (sp.NextVisible (enterChildren));
			}
			ResultsLabel = $"Found {Results.Count} instances of the string '{textString.Replace ('\n', ' ').Truncate (32)}'";
			if (Results.Count == 1)
				ShowResult (Results [0]);
		}

		/// <summary>
		/// Build HashSet of all serialized strings in the loaded scenes for the ComboBox
		/// </summary>
		void FindAllStrings () {
			var allGOs = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			var allComponents = allGOs.Where (o => o.scene.isLoaded).SelectMany (o => o.GetComponents<Component> ());
			ListStrings.Clear ();
			foreach (var component in allComponents) {
				if (component == null)
					continue;
				var so = new SerializedObject (component);
				var sp = so.GetIterator ();
				SerializedProperty pCalls;
				if (!sp.NextVisible (true))
					continue;
				bool enterChildren;
				do {
					enterChildren = true;
					if (sp.propertyType == SerializedPropertyType.Generic && (pCalls = sp.FindPropertyRelative ("m_PersistentCalls.m_Calls")) != null) {
						var len = pCalls.arraySize;
						for (int i = 0; i < len; i++) {
							var sv = pCalls.GetArrayElementAtIndex (i).FindPropertyRelative ("m_Arguments.m_StringArgument").stringValue;
							if (!string.IsNullOrWhiteSpace (sv))
								ListStrings.Add (sv.Replace ('\n', ' '));
						}
						enterChildren = false;
					} else if (sp.propertyType == SerializedPropertyType.String) {
						var sv = sp.stringValue;
						if (!string.IsNullOrWhiteSpace (sv))
							ListStrings.Add (sv.Replace ('\n', ' '));
					}
				} while (sp.NextVisible (enterChildren));
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
			var allComponents = allObjects.Where (o => o.scene.isLoaded).SelectMany (o => o.GetComponents<Component> ());
			foreach (var component in allComponents) {
				if (component == null)
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
			ResultsLabel = $"Found {Results.Count} event listeners matching '{partialMethodName}'";
			if (Results.Count == 1)
				ShowResult (Results [0]);
		}

		/// <summary>
		/// Build HashSet of all event listeners in the loaded scenes for the ComboBox
		/// </summary>
		void FindAllInvokations () {
			var allObjects = Resources.FindObjectsOfTypeAll (typeof (GameObject)) as GameObject [];
			var allComponents = allObjects.Where (o => o.scene.isLoaded).SelectMany (o => o.GetComponents<Component> ());
			ListInvokations.Clear ();
			foreach (var component in allComponents) {
				if (component == null)
					continue;
				var componentType = component.GetType ();
				var fields = componentType.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields) {
					if (field.GetValue (component) is UnityEventBase unityEvent) {
						AddInvokations (ListInvokations, unityEvent);
					} else if (field.GetValue (component) is List<UnityEngine.EventSystems.EventTrigger.Entry> eventTriggers) {
						foreach (var et in eventTriggers) {
							AddInvokations (ListInvokations, et.callback);
						}
					}
				}
			}
		}

		void AddInvokations (HashSet<string> Invokations, UnityEventBase unityEvent) {
			for (int k = 0; k < unityEvent.GetPersistentEventCount (); k++) {
				var methodName = unityEvent.GetPersistentMethodName (k);
				var classPath = unityEvent.GetPersistentTarget (k).GetType ().Name;
				Invokations.Add ($"{classPath}.{methodName}");
			}
		}

		void FindMatchingInvokation (Component component, string fieldName, UnityEventBase unityEvent, string classMatch, string methodMatch) {
			for (int k = 0; k < unityEvent.GetPersistentEventCount (); k++) {
				var methodName = unityEvent.GetPersistentMethodName (k);
				var classPath = unityEvent.GetPersistentTarget (k).GetType ().Name;
				var hasClassMatch = classPath.IndexOf (classMatch, StringComparison.InvariantCultureIgnoreCase) >= 0;
				var hasMethodMatch = methodName.IndexOf (methodMatch, StringComparison.InvariantCultureIgnoreCase) >= 0;
				if ((classMatch != methodMatch && hasClassMatch && hasMethodMatch)
				 || (classMatch == methodMatch && (hasClassMatch || hasMethodMatch))) {
					var path = $"{fieldName}.m_PersistentCalls.m_Calls.Array.data[{k}]";
					AddResult (component, path, fieldName);
					break;
				}
			}
		}

		static void AddResult (Component component, string PropertyPath, string FieldName, string Suffix = null) {
			Results.Add (new Result { obj = component, propertyPath = PropertyPath, text = $"{(PrefabUtility.IsPartOfPrefabAsset (component) ? "[PREFAB] " : string.Empty)}{component.gameObject.GetScenePath ()} <color=#44ffcc>{component.GetType ().Name}</color>.<color=#ffffcc>{FieldName}</color>{Suffix ?? string.Empty}" });
		}

		static void AddResult (GameObject GameObject, string PropertyPath = null, string Suffix = null) {
			Results.Add (new Result { obj = GameObject, propertyPath = PropertyPath, text = $"{(PrefabUtility.IsPartOfPrefabAsset (GameObject) ? "[PREFAB] " : string.Empty)}{GameObject.GetScenePath ()}{Suffix ?? string.Empty}" });
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
			ResultsLabel = $"Found {Results.Count} sprite textures with the packing tag '{packingTag}'";
			if (Results.Count == 1)
				ShowResult (Results [0]);
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
				if (!go.scene.isLoaded)
					continue;
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
								AddResult (component, sp.propertyPath, NicePropertyPath (sp));
							}
						}
					}
				}
			}
			ResultsLabel = $"Found {Results.Count} missing references in loaded scenes.";
			if (Results.Count == 1)
				ShowResult (Results [0]);
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

		/// <summary>
		/// Builds a 'path' to the specified Component's GameObject in the scene hierarchy. Useful for logging/debugging
		/// </summary>
		public static string GetScenePath (this Component comp, char separator = '/') {
			var sb = new StringBuilder ();
			sb.Append (comp.gameObject.scene.name);
			AppendParentName (ref sb, comp.transform, separator);
			return sb.ToString ();
		}

		/// <summary>
		/// Cap string length and if capped, add a postfix (defaults to ellipsis character)
		/// </summary>
		public static string Truncate (this string s, int maxLength, string postfix = "…") {
			if (s.Length <= maxLength) return s;
			return s.Substring (0, maxLength) + postfix;
		}

	[CustomPropertyDrawer (typeof (UnityEngine.Object), true)]
	public class ObjectPropertyEditor : PropertyDrawer {
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var identifier = $"{property.serializedObject.targetObject.GetInstanceID ()}.{property.propertyPath}";
			Highlighter.HighlightIdentifier (position, identifier);
			EditorGUI.PropertyField (position, property, label);
		}
	}

	[CustomPropertyDrawer (typeof (string), true)]
	public class StringPropertyEditor : PropertyDrawer {
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var identifier = $"{property.serializedObject.targetObject.GetInstanceID ()}.{property.propertyPath}";
			Highlighter.HighlightIdentifier (position, identifier);
			EditorGUI.PropertyField (position, property, label);
		}
	}

	[CustomEditor (typeof (Text), true)]
	[CanEditMultipleObjects]
	/// <summary>
	/// Custom Editor for the Text Component.
	/// </summary>
	public class CustomTextEditor : UnityEditor.UI.TextEditor {
		SerializedProperty m_Text;
		SerializedProperty m_FontData;

		protected override void OnEnable () {
			base.OnEnable ();
			m_Text = serializedObject.FindProperty ("m_Text");
			m_FontData = serializedObject.FindProperty ("m_FontData");
		}

		public override void OnInspectorGUI () {
			serializedObject.Update ();

			EditorGUILayout.PropertyField (m_Text);
			var identifier = $"{serializedObject.targetObject.GetInstanceID ()}.{m_Text.propertyPath}";
			Highlighter.HighlightIdentifier (GUILayoutUtility.GetLastRect (), identifier);

			EditorGUILayout.PropertyField (m_FontData);

			AppearanceControlsGUI ();
			RaycastControlsGUI ();
			serializedObject.ApplyModifiedProperties ();
		}
	}
}