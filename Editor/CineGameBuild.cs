using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using CineGame.MobileComponents;
using CineGame.MobileComponents.GC;

namespace CineGameEditor.MobileComponents {
	public class CineGameBuild : EditorWindow {
		static CineGameBuild instance;

		static readonly string ProgressBarTitle = "CineGame Build";

		static string resultMessage = string.Empty;
		//contains result description from last scene build+upload

		static string Username;
		static string Password;
		static bool StayLoggedIn;
		public static string GameType;
		static bool IsSuperAdmin;

		static int MarketIndex;
		static int EnvironmentIndex;
		static int GameTypeIndex;
		static string [] GameTypesAvailable;

		static string [] _marketDisplayNames;
		static Util.APIRegion [] _regions;
		static string [] MarketDisplayNames {
			get {
				if (_marketDisplayNames == null) {
					_marketDisplayNames = Util.Markets.Select (m => $"{m.Value.Network}-{m.Value.Country}").ToArray ();
					_regions = Util.Markets.Select (m => m.Key).ToArray ();
				}
				return _marketDisplayNames;
			}
		}

		static readonly string [] BackendEnvironments = new string [] {
			"production",
			"staging",
		};

		static string AccessToken;
		static DateTime AccessTokenTime = DateTime.MinValue;

		static bool BuildOnlyCurrentPlatform;
		static bool IsGamecenterGame;
		static bool IsCanvasGame;
		public static bool IsARGame;

		static string progressMessage = "Waiting ...";

		static float buildProgress;
		static bool IsBuilding = false;
		static bool IsUploading = false;

		static string LastBuildReportString = string.Empty;
		static string builtScenePath;
		Vector2 buildReportScrollPosition = Vector2.zero;

		public delegate bool BoolEvent ();
		public static event BoolEvent onBeginBuild;

		public delegate void SwitchMarket (Util.APIRegion region);
		public static event SwitchMarket onSwitchMarket;

		public delegate void SwitchEnvironment (bool isStaging, bool isDev);
		public static event SwitchEnvironment onSwitchEnvironment;

		static bool HasIosBuildSupport, HasAndroidBuildSupport;

		static class ControlNames {
			public const string Password = "Password";
			public const string GameType = "GameType";
		}

		EditorCoroutine StartCoroutine (IEnumerator coroutine) {
			return EditorCoroutineUtility.StartCoroutine (coroutine, this);
		}

		void StopCoroutine (EditorCoroutine coroutine) {
			EditorCoroutineUtility.StopCoroutine (coroutine);
		}

		bool KeyDown (KeyCode keyCode) {
			return Event.current.type == EventType.KeyDown && Event.current.keyCode == keyCode;
		}

		void OnGUI () {
			EditorGUILayout.Space ();

			if (Application.internetReachability == NetworkReachability.NotReachable) {
				EditorGUILayout.HelpBox ("Internet not reachable.", MessageType.Error);
				return;
			}

			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS) {
				EditorGUILayout.HelpBox ($"Build target {EditorUserBuildSettings.activeBuildTarget} not supported!", MessageType.Error);

				var toAndroid = EditorUtility.DisplayDialog (instance.titleContent.text, "Current build target not supported, switch to Android or iOS build target?", "Android", "iOS");
				EditorUserBuildSettings.SwitchActiveBuildTargetAsync (toAndroid ? BuildTargetGroup.Android : BuildTargetGroup.iOS, toAndroid ? BuildTarget.Android : BuildTarget.iOS);
			}

			if (!HasAndroidBuildSupport) {
				if (!HasIosBuildSupport) {
					EditorGUILayout.HelpBox ("You need to install both iOS and Android build support!", MessageType.Error);
				} else {
					EditorGUILayout.HelpBox ("You need to install Android build support!", MessageType.Error);
				}
				return;
			} else if (!HasIosBuildSupport) {
				EditorGUILayout.HelpBox ("You need to install iOS build support!", MessageType.Error);
				return;
			}

			var enterKeyPressed = KeyDown (KeyCode.Return);
			var focusedControl = GUI.GetNameOfFocusedControl ();
			var passwordEntered = focusedControl == ControlNames.Password && enterKeyPressed;
			var gameTypeEntered = focusedControl == ControlNames.GameType && enterKeyPressed;

            var centeredStyle = new GUIStyle (GUI.skin.GetStyle ("Label")) {
				alignment = TextAnchor.UpperCenter,
				fontStyle = FontStyle.Bold
			};

            EditorGUILayout.BeginHorizontal ();

			var _marketIndex = Mathf.Clamp (MarketIndex, 0, MarketDisplayNames.Length);
			_marketIndex = EditorGUILayout.Popup (new GUIContent ("Market:"), _marketIndex, MarketDisplayNames);
			if (MarketIndex != _marketIndex) {
				MarketIndex = _marketIndex;
				onSwitchMarket?.Invoke (_regions [MarketIndex]);
				if (!string.IsNullOrWhiteSpace (Username) && !string.IsNullOrWhiteSpace (Password)) {
					if (!GetAccessToken (out AccessToken)) {
						EditorUtility.DisplayDialog (ProgressBarTitle, "Failed to login. Check username and password and that you are connected to the internet", "OK");
						return;
					}
					EditorPrefs.SetInt ("AssetMarketIndex", MarketIndex);
				}
			}

			EditorGUI.BeginDisabledGroup (!IsSuperAdmin);
			var _environmentIndex = Mathf.Clamp (EnvironmentIndex, 0, BackendEnvironments.Length);
			_environmentIndex = EditorGUILayout.Popup (new GUIContent ("Environment:"), _environmentIndex, BackendEnvironments, GUILayout.Width (250f));
			EditorGUI.EndDisabledGroup ();
			if (EnvironmentIndex != _environmentIndex) {
				EnvironmentIndex = _environmentIndex;
				onSwitchEnvironment?.Invoke (isStaging: _environmentIndex == 1, isDev: _environmentIndex == 2);
				if (!string.IsNullOrWhiteSpace (Username) && !string.IsNullOrWhiteSpace (Password)) {
					if (!GetAccessToken (out AccessToken)) {
						EditorUtility.DisplayDialog (ProgressBarTitle, "Failed to login. Check username and password and that you are connected to the internet", "OK");
						return;
					}
					EditorPrefs.SetInt ("CineGameEnvironmentIndex", EnvironmentIndex);
				}
			}

			EditorGUILayout.EndHorizontal ();

			if (string.IsNullOrEmpty (AccessToken) || AccessTokenTime < DateTime.Now.AddHours (-1.0)) {

				EditorGUILayout.HelpBox ("Not logged in!", MessageType.Error);

				Username = EditorGUILayout.TextField ("Username:", Username);
				GUI.SetNextControlName (ControlNames.Password);
				Password = EditorGUILayout.PasswordField ("Password:", Password);
				StayLoggedIn = EditorGUILayout.Toggle ("Stay logged in", StayLoggedIn);
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (" ");
				var loginPressed = GUILayout.Button ("Login", GUILayout.MaxWidth (200f));
				EditorGUILayout.EndHorizontal ();
				if (loginPressed || passwordEntered) {
					if (!GetAccessToken (out AccessToken)) {
						EditorUtility.DisplayDialog (ProgressBarTitle, "Failed to login. Check username and password and that you are connected to the internet", "OK");
						return;
					}
					EditorPrefs.SetString ("AssetBundleUser", Username);
				} else {
					return;
				}
			}

			if (IsGamecenterGame) {
				GUI.backgroundColor = Color.gray;
				EditorGUILayout.LabelField ("GameCenter Upload", centeredStyle);
			} else if (IsARGame) {
				GUI.backgroundColor = Color.magenta;
				EditorGUILayout.LabelField ("AR Upload", centeredStyle);
			} else if (!IsCanvasGame) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (" ");
				if (GUILayout.Button ("Log out")) {
					AccessToken = null;
					AccessTokenTime = DateTime.MinValue;
					Password = string.Empty;
					EditorPrefs.DeleteKey ("CGSP");
				}
				EditorGUILayout.EndHorizontal ();
				return;
			}

			if (IsSuperAdmin) {
				//Super-admins are allowed to specify any gametype
				GUI.SetNextControlName (ControlNames.GameType);
				GameType = EditorGUILayout.TextField ("GameType:", GameType);
			} else if (GameTypesAvailable.Length > 0) {
				//Other users only have a limited set of GameTypes to choose from
				var _gameTypeIndex = Mathf.Clamp (GameTypeIndex, 0, GameTypesAvailable.Length - 1);
				_gameTypeIndex = EditorGUILayout.Popup (new GUIContent ("GameType:"), _gameTypeIndex, GameTypesAvailable);
				if (GameTypeIndex != _gameTypeIndex) {
					GameTypeIndex = _gameTypeIndex;
					GameType = GameTypesAvailable [_gameTypeIndex];
					gameTypeEntered = true;
				}
			} else {
				//Not super-admin and no gametypes available, abandon
				EditorGUILayout.HelpBox ("You need access to one or more gametypes. Contact admin.", MessageType.Error);
				return;
			}

			if (gameTypeEntered && !string.IsNullOrWhiteSpace (GameType)) {
				UpdateGameTypeInGameProxy ();
			}

			if (EditorApplication.isCompiling) {
				EditorGUILayout.HelpBox ("Waiting for scripts to recompile ...", MessageType.Info);
				return;
			}

			var outputPath = GetOutputPathForCurrentScene ();
			var bundleName = GameType.ToLower ();
			var buildPathiOS = string.Format ("{0}/AssetBundles_iOS/{1}", outputPath, bundleName);
			var buildPathAndroid = string.Format ("{0}/AssetBundles_Android/{1}", outputPath, bundleName);

			var fileExistsIos = File.Exists (buildPathiOS);
			var fileExistsAndroid = File.Exists (buildPathAndroid);

			if (!fileExistsIos || !fileExistsAndroid) {
				EditorGUI.BeginDisabledGroup (true);
				BuildOnlyCurrentPlatform = false;
			}
			BuildOnlyCurrentPlatform = EditorGUILayout.Toggle (new GUIContent ($"Build only for {EditorUserBuildSettings.activeBuildTarget}", "Use with caution: This saves time in a short testing cycle, but builds will be out of sync."), BuildOnlyCurrentPlatform);
			if (!fileExistsIos || !fileExistsAndroid) {
				EditorGUI.EndDisabledGroup ();
			}

			if (!IsBuilding) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (" ");
				if (GUILayout.Button ("Build " + GameType)) {
					OnClickBuild ();
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (" ");
				if (GUILayout.Button ("Log out")) {
					AccessToken = null;
					AccessTokenTime = DateTime.MinValue;
					Password = string.Empty;
					EditorPrefs.DeleteKey ("CGSP");
				}
				EditorGUILayout.EndHorizontal ();
			} else {
				Rect r = EditorGUILayout.BeginVertical ();
				EditorGUI.ProgressBar (r, buildProgress, progressMessage);
				GUILayout.Space (18);
				EditorGUILayout.EndVertical ();

				if (GUILayout.Button ("Cancel")) {
					IsBuilding = false;
				}
			}

			if (!IsBuilding) {
				var lastBuildReportPath = GetLastBuildReportPath ();
				if (string.IsNullOrEmpty (LastBuildReportString) && File.Exists (lastBuildReportPath)) {
					LastBuildReportString = File.ReadAllText (lastBuildReportPath);
				}

				var hasBuildReport = !string.IsNullOrEmpty (LastBuildReportString) && LastBuildReportString.Contains ("Build report for " + bundleName);
				if (hasBuildReport) {
					Rect rScroll = EditorGUILayout.BeginVertical ();
					buildReportScrollPosition = EditorGUILayout.BeginScrollView (buildReportScrollPosition, false, true, GUILayout.Height (rScroll.height));
					EditorGUILayout.SelectableLabel (LastBuildReportString, EditorStyles.textArea, GUILayout.ExpandHeight (true));
					EditorGUILayout.EndScrollView ();
					EditorGUILayout.EndVertical ();
				}

				if (Application.platform != RuntimePlatform.OSXEditor || hasBuildReport) {
					if (IsUploading) {
						//Builds are now uploading, display progress
						Rect r = EditorGUILayout.BeginVertical ();
						EditorGUI.ProgressBar (r, buildProgress, progressMessage);
						GUILayout.Space (18);
						EditorGUILayout.EndVertical ();
					} else if (fileExistsAndroid && fileExistsIos && GUILayout.Button ($"Upload {GameType} for {MarketDisplayNames [MarketIndex]}")) {
						//Both platform builds exist and user has clicked on Upload
						OnClickUpload ();
					}
				}
			}
		}

		void OnHierarchyChange () {
			if (!IsBuilding && !string.IsNullOrWhiteSpace (AccessToken)) {
				var activeScenePath = EditorSceneManager.GetActiveScene ().path;
				if (/*EditorSceneManager.GetActiveScene ().isDirty || */activeScenePath != builtScenePath) {
					//Active scene has changed since build, nuke the build report
					//Debug.Log ("Nuking build report");
					try {
						File.Delete (GetLastBuildReportPath ());
					} catch (Exception) {
					}
				}
				var sceneName = EditorSceneManager.GetActiveScene ().name;
				var _gameProxy =
#if UNITY_2022_3_OR_NEWER
					FindAnyObjectByType<GameProxy> ();
#else
					FindObjectOfType<GameProxy> ();
#endif
				var _isCanvasGame = _gameProxy != null;
				if (IsCanvasGame != _isCanvasGame) {
					IsCanvasGame = _isCanvasGame && !sceneName.Equals ("MasterApp");
					Repaint ();
				}

				if (_isCanvasGame && GameType != _gameProxy.GameType) {
					if (IsSuperAdmin) {
						GameType = _gameProxy.GameType;
						if (string.IsNullOrWhiteSpace (GameType)) {
							GameType = sceneName;
							UpdateGameTypeInGameProxy ();
						}
					} else if (GameTypesAvailable.Length > 0) {
						GameTypeIndex = Array.IndexOf (GameTypesAvailable, _gameProxy.GameType);
						if (GameTypeIndex == -1) {
							GameTypeIndex = 0;
						}
						GameType = GameTypesAvailable [GameTypeIndex];
						var so = new SerializedObject (_gameProxy);
						so.FindProperty ("GameType").stringValue = GameType;
						so.ApplyModifiedProperties ();
						EditorUtility.DisplayDialog (ProgressBarTitle, $"GameType unavailable, forced to {GameType}", "OK");
					} else {
						EditorUtility.DisplayDialog (ProgressBarTitle, "No gametypes available. Contact admin", "OK");
					}
					Repaint ();
				}

				var _gcGameManager =
#if UNITY_2022_3_OR_NEWER
					FindAnyObjectByType<GC_GameManager> ();
#else
					FindObjectOfType<GC_GameManager> ();
#endif
				var _isGcGame = _gcGameManager != null;
				if (IsGamecenterGame != _isGcGame) {
					IsGamecenterGame = _isGcGame;
					Repaint ();
				}
				if (_isGcGame && GameType != _gcGameManager.gameObject.scene.name) {
					GameType = _gcGameManager.gameObject.scene.name;
					//_gcGameManager.Market
				}
			}
		}

		void UpdateGameTypeInGameProxy () {
			var _gameProxy =
#if UNITY_6000_0_OR_NEWER
				FindAnyObjectByType<GameProxy> ();
#else
				FindObjectOfType<GameProxy> ();
#endif
			if (_gameProxy != null && GameType != _gameProxy.GameType) {
				var so = new SerializedObject (_gameProxy);
				so.FindProperty ("GameType").stringValue = GameType;
				so.ApplyModifiedProperties ();
				Debug.Log ("Changed GameProxy.GameType to " + GameType);
			}
		}

		void OnClickBuild () {
			var builtTarget = EditorUserBuildSettings.activeBuildTarget;
			var notBuiltTarget = (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) ? BuildTarget.Android : BuildTarget.iOS;
			if (BuildOnlyCurrentPlatform && !EditorUtility.DisplayDialog (ProgressBarTitle, $"Building only {builtTarget}-- {notBuiltTarget} will be out of sync. Continue?", "OK", "Cancel")) {
				return;
			}

			StartCoroutine (E_BuildSingleSceneForAllPlatforms ());
		}

		void OnClickUpload () {
            var relOutputPath = GetOutputPathForCurrentScene ();
			var fiIOS = new FileInfo (relOutputPath + "/AssetBundles_iOS/" + GameType.ToLower ());
            var fiAndroid = new FileInfo (relOutputPath + "/AssetBundles_Android/" + GameType.ToLower ());
			var timeSinceIOSBuild = DateTime.Now - fiIOS.LastWriteTime;
            var timeSinceAndroidBuild = DateTime.Now - fiAndroid.LastWriteTime;

			var msg = "Are you sure you want to upload " + GameType + " to live server?\n";
            if (timeSinceAndroidBuild.TotalHours >= 10.0) {
				msg += $"\nAndroid build is more than {(int)timeSinceAndroidBuild.TotalHours} hours old.";
			}
            if (timeSinceIOSBuild.TotalHours >= 10.0) {
                msg += $"\niOS build is more than {(int)timeSinceIOSBuild.TotalHours} hours old.";
            }

            if (!EditorUtility.DisplayDialog (ProgressBarTitle, msg, "OK", "Cancel")) {
				return;
			}

			StartCoroutine (E_UploadSingleSceneForAllPlatforms ());
		}

		public void OnEnable () {
			var moduleManager = Type.GetType ("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
			var isPlatformSupportLoaded = moduleManager.GetMethod ("IsPlatformSupportLoaded", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			var getTargetStringFromBuildTarget = moduleManager.GetMethod ("GetTargetStringFromBuildTarget", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

			HasIosBuildSupport = (bool)isPlatformSupportLoaded.Invoke (null, new object [] { (string)getTargetStringFromBuildTarget.Invoke (null, new object [] {
				BuildTarget.iOS }) });
			HasAndroidBuildSupport = (bool)isPlatformSupportLoaded.Invoke (null, new object [] { (string)getTargetStringFromBuildTarget.Invoke (null, new object [] {
				BuildTarget.Android }) });

			EditorApplication.hierarchyChanged -= OnHierarchyChange;
			EditorApplication.hierarchyChanged += OnHierarchyChange;

			instance = this;
			var texturePanel = AssetDatabase.LoadAssetAtPath<Texture2D> (AssetDatabase.GUIDToAssetPath ("efec9089586ac4574995396036ef8524"));
			//var textures = PlayerSettings.GetIconsForTargetGroup (BuildTargetGroup.Unknown);
			//if (textures != null && textures.Length > 0 && textures [0] != null) {
			if (texturePanel != null) {
				//GUILayout.Label (textures[0], new GUIStyle () {alignment = TextAnchor.UpperRight});
				titleContent = new GUIContent (ProgressBarTitle, texturePanel, "Create assetbundle from current scene and upload as game");
			}

			MarketIndex = EditorPrefs.GetInt ("AssetMarketIndex", 0);
			if (MarketIndex >= MarketDisplayNames.Length) {
				Debug.LogError ($"MarketIndex was {MarketIndex} but array length is only {MarketDisplayNames.Length}");
				MarketIndex = 0;
			}

			EnvironmentIndex = EditorPrefs.GetInt ("CineGameEnvironmentIndex", 0);
			if (EnvironmentIndex >= BackendEnvironments.Length) {
				Debug.LogError ($"EnvironmentIndex was {EnvironmentIndex} but array length is only {BackendEnvironments.Length}");
				EnvironmentIndex = 0;
			}

			StayLoggedIn = EditorPrefs.GetBool ("CineGameStayLoggedIn");

			if (CGSP () && Application.internetReachability != NetworkReachability.NotReachable && !EditorApplication.isPlayingOrWillChangePlaymode) {
				GetAccessToken (out AccessToken);
				OnHierarchyChange ();
			}
		}

		public void OnDisable () {
			EditorApplication.hierarchyChanged -= OnHierarchyChange;
		}

#if UNITY_IOS || UNITY_ANDROID
		[MenuItem ("CinemaTaztic/Build And Upload")]
		public static void Init () {
			if (instance == null) {
				instance = GetWindow<CineGameBuild> (false, ProgressBarTitle, true);
			}
			instance.Focus ();
		}
#endif

		static void RepaintWindow () {
			if (instance != null) {
				EditorUtility.SetDirty (instance);
				instance.Repaint ();
			}
		}


		static List<BuildTarget> GetBuildTargets () {
			var targets = new List<BuildTarget> (2) {
				EditorUserBuildSettings.activeBuildTarget,
				EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? BuildTarget.Android : BuildTarget.iOS
			};
			return targets;
		}

#if UNITY_IOS || UNITY_ANDROID
		[MenuItem ("CinemaTaztic/Build All Scenes For Market")]
		static void BuildAllScenesForAllPlatforms () {
			if (EditorUtility.DisplayDialog (ProgressBarTitle, "Build all DLC scenes for region " + Util.GetRegion ().ToString () + "?", "OK", "Cancel")) {
				Init ();
				instance.StartCoroutine (instance.E_BuildAllScenesForAllPlatforms ());
			}
		}
#endif

		IEnumerator E_BuildAllScenesForAllPlatforms () {
			var region = Util.GetRegion ().ToString ();

			var gcAssetPaths = AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/GameCenter/Spil/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid));
			var arAssetPaths = AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/AR/Scenes/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid));
			var cvAssetPaths = AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/Scenes/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid));

			var targets = GetBuildTargets ();

			foreach (var buildTarget in targets) {
				if (buildTarget != EditorUserBuildSettings.activeBuildTarget) {
					Debug.LogFormat ("Switching build target to {0}", buildTarget);
					EditorUserBuildSettings.SwitchActiveBuildTarget ((buildTarget == BuildTarget.Android) ? BuildTargetGroup.Android : BuildTargetGroup.iOS, buildTarget);
				}

				Debug.LogFormat ("Building all canvas game scenes for {0} ...", buildTarget);
				yield return null;

				var unityVersionMajor = Application.unityVersion.Substring (0, Application.unityVersion.IndexOf ('.'));

				BuildAssetBundlesForScenes (cvAssetPaths, $"../builds/AssetBundles/cv/{region}/{unityVersionMajor}", buildTarget);

				Debug.LogFormat ("Building all GameCenter scenes for {0} ...", buildTarget);
				yield return null;

				BuildAssetBundlesForScenes (gcAssetPaths, $"../builds/AssetBundles/gc/{region}/{unityVersionMajor}", buildTarget);

				Debug.LogFormat ("Building all AR scenes for {0} ...", buildTarget);
				yield return null;

				BuildAssetBundlesForScenes (arAssetPaths, $"../builds/AssetBundles/ar/{region}/{unityVersionMajor}", buildTarget);
			}
			/*} catch (Exception e) {
				Debug.LogErrorFormat ("{0} while building all platforms: {1}\n{2}", e.ToString (), e.Message, e.StackTrace);
				EditorUtility.DisplayDialog (ProgressBarTitle, string.Format ("{0}: {1}\nSee log for details", e.ToString (), e.Message), "OK");
				yield break;
			}*/

			var totalAssets = gcAssetPaths.Count () + arAssetPaths.Count () + cvAssetPaths.Count ();
			EditorUtility.DisplayDialog (ProgressBarTitle, string.Format ("Built {0} assetbundles for both Android and iOS", totalAssets), "OK");
			EditorUtility.RevealInFinder (Path.GetFullPath (string.Format ("../builds/AssetBundles/{0}", region)));
		}



		static bool BuildAssetBundlesForScenes (IEnumerable<string> scenePaths, string outputPath, BuildTarget buildTarget) {
			var assetBundleBuilds = new List<AssetBundleBuild> ();
			foreach (var assetPath in scenePaths) {
				var sceneName = Path.GetFileNameWithoutExtension (assetPath);
				if (sceneName == "MasterApp")
					continue;
				var bundleName = sceneName.ToLower ();
				assetBundleBuilds.Add (new AssetBundleBuild { assetBundleName = bundleName, assetNames = new string [1] { assetPath } });
			}

			Debug.LogFormat ("Building {0} assetbundles for {1} and placing them in {2}...", assetBundleBuilds.Count, EditorUserBuildSettings.activeBuildTarget, outputPath);

			return BuildBundles (assetBundleBuilds, outputPath, buildTarget);
		}


		static bool BuildBundles (IEnumerable<AssetBundleBuild> assetBundleBuilds, string outputPath, BuildTarget buildTarget) {
			Debug.Log ($"Building {assetBundleBuilds.Count ()} AssetBundles for {buildTarget}");
			var relOutputPath = outputPath + "/AssetBundles_" + ((buildTarget == BuildTarget.Android) ? "Android" : "iOS");
			var fullOutputPath = Path.GetFullPath (relOutputPath);
			Directory.CreateDirectory (fullOutputPath);

			var manifest = BuildPipeline.BuildAssetBundles (relOutputPath, assetBundleBuilds.ToArray (), BuildAssetBundleOptions.None, buildTarget);
			if (manifest == null) {
				Debug.LogError ("Error during assetbundle build, or build was canceled");
				return false;
			}
			var builtAssetBundles = manifest.GetAllAssetBundles ().ToList ();
			var bundleBuilt = true;
			foreach (var abb in assetBundleBuilds) {
				if (!builtAssetBundles.Contains (abb.assetBundleName)) {
					bundleBuilt = false;
					break;
				}
				var filePath = Path.Combine (relOutputPath, abb.assetBundleName);
				try {
					File.Delete (filePath + ".manifest");
				} catch (Exception ex) {
					Debug.LogWarning ($"Exception while trying to delete manifest {filePath}.manifest: {ex}");
				}
			}
			if (!bundleBuilt) {
				Debug.LogError ("ERROR: Something went wrong, did not build all bundles. Bundles built: " + string.Join (",", builtAssetBundles));
				return false;
			}
			return true;
		}

		IEnumerator E_BuildSingleSceneForAllPlatforms () {
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ()) {
				Debug.Log ("User cancelled build.");
				yield break;
			}

			if (!CheckSceneIntegrity ()) {
				Debug.Log ("CheckSceneIntegrity returned false. Aborting build.");
				yield break;
			}

			IsBuilding = true;
			RepaintWindow ();
			yield return null;

			if (onBeginBuild != null && !onBeginBuild.Invoke ()) {
				Debug.Log ("onBeginBuild cancelled building");
				yield break;
			}

			var targets = GetBuildTargets ();

			buildProgress = 0f;
			float dpct = 1f / targets.Count;
			foreach (var target in targets) {
				var buildTarget = target;
				/*
				if (target != EditorUserBuildSettings.activeBuildTarget) {
					progressMessage = string.Format ("Switching target to {0} ...", target);
					RepaintWindow ();
					yield return null;
					RepaintWindow ();
					yield return null;
					EditorUserBuildSettings.SwitchActiveBuildTarget ((target == BuildTarget.Android)? BuildTargetGroup.Android : BuildTargetGroup.iOS, target);
				}
				*/
				progressMessage = string.Format ("Building {0} for {1} ...", GameType, buildTarget);
				RepaintWindow ();
				yield return null;
				RepaintWindow ();
				yield return null;

				var abb = new AssetBundleBuild { assetBundleName = GameType.ToLower (), assetNames = new [] { EditorSceneManager.GetActiveScene ().path } };
				var assetLoaderAbBuilds = ProcessAssetLoaders (prepareBuild: true);
				var abBuilds = new List<AssetBundleBuild> { abb }.Union (assetLoaderAbBuilds);

				if (!BuildBundles (abBuilds, GetOutputPathForCurrentScene (), buildTarget)) {
					resultMessage = "CreateAssetBundles: Build failed. See log for details.";
					Debug.LogError (resultMessage);
					IsBuilding = false;
					break;
				}

				RepaintWindow ();
				yield return null;
				builtScenePath = EditorSceneManager.GetActiveScene ().path;
				ExtractBuildReportFromEditorLog (target);

				if (BuildOnlyCurrentPlatform) {
					break;
				}
				buildProgress += dpct;
			}

			if (IsBuilding) {
				resultMessage = string.Format ("Build {0} succeeded", GameType);
				Debug.Log (resultMessage);
			}

			IsBuilding = false;
			RepaintWindow ();

			EditorUtility.DisplayDialog (ProgressBarTitle, resultMessage, "OK");
		}


		/// <summary>
		/// Checks if the scene meets specific requirements before building
		/// </summary>
		bool CheckSceneIntegrity () {
			var activeScene = EditorSceneManager.GetActiveScene ();
			if (!IsARGame && !IsGamecenterGame) {
				var gameProxies = Resources.FindObjectsOfTypeAll<GameProxy> ();
				if (gameProxies.Length != 1 || gameProxies [0].gameObject.scene != activeScene) {
					Debug.LogError ("Active scene must contain one and only one GameProxy component");
					return false;
				}
				//Check to make sure that the required pages are present in scene
				//var requiredScenePages = new string [] { "Lobby", "Win", "Lose"/*, "SupportLobby", "SupportWin", "SupportLose"*/ };
				//var canvasGroups = Resources.FindObjectsOfTypeAll<CanvasGroup> ();
				//var missingGroup = false;
				//var sb = new System.Text.StringBuilder ();
				//sb.AppendLine ("Required CanvasGroups missing from scene-- please add before building:");
				//foreach (var rsp in requiredScenePages) {
				//	if (!canvasGroups.Any (cg => cg.name == rsp)) {
				//		sb.AppendFormat ("Required CanvasGroup '{0}' not found in scene.\n", rsp);
				//		missingGroup = true;
				//	}
				//}
				//if (missingGroup) {
				//	Debug.LogError (sb.ToString ());
				//	return false;
				//}
				return true;
			} else if (IsARGame) {
				/*
				var arGameProxies = Resources.FindObjectsOfTypeAll<ARGameProxy> ();
				if (arGameProxies.Length != 1 || arGameProxies [0].gameObject.scene != activeScene) {
					Debug.LogError ("Active scene must contain one and only one ARGameProxy component");
					return false;
				}
				*/
				return true;
			} else if (IsGamecenterGame) {
				var hasMainCamera = Resources.FindObjectsOfTypeAll<Camera> ().Any (c => c.gameObject.scene == activeScene && c.gameObject.CompareTag ("MainCamera"));
				return hasMainCamera;
			}
			return false;
		}


		IEnumerator E_UploadSingleSceneForAllPlatforms () {
			Util.GetSdkVersionAndBuildTime (out Version sdkVersion, out DateTime sdkBuildTime);

			IsUploading = true;
			progressMessage = $"Uploading {GameType} for {MarketDisplayNames [MarketIndex]} to live server ...";
			buildProgress = 0f;
			RepaintWindow ();
			yield return null;

			var bundleName = GameType.ToLower ();
			var outputPath = GetOutputPathForCurrentScene ();

			//EditorUtility.RevealInFinder (fullOutputPath);
			var form = new WWWForm ();
			form.AddField ("gameType", GameType);
			form.AddField ("sdkVersion", sdkVersion.ToString ());
			form.AddField ("sdkBuildTime", sdkBuildTime.ToString ("u"));

			form.AddBinaryData ("iosBundle",
				File.ReadAllBytes (Path.Combine (outputPath, "AssetBundles_iOS", bundleName)),
				bundleName,
				MediaTypeNames.Application.Zip
			);
			form.AddBinaryData ("androidBundle",
				File.ReadAllBytes (Path.Combine (outputPath, "AssetBundles_Android", bundleName)),
				bundleName,
				MediaTypeNames.Application.Zip
			);

			var abbs = ProcessAssetLoaders (prepareBuild: false);
			var i = 1;
			foreach (var abb in abbs) {
				form.AddBinaryData ("iosBundle-" + i,
					File.ReadAllBytes (Path.Combine (outputPath, "AssetBundles_iOS", abb.assetBundleName)),
					abb.assetBundleName,
					MediaTypeNames.Application.Zip
				);
				form.AddBinaryData ("androidBundle-" + i,
					File.ReadAllBytes (Path.Combine (outputPath, "AssetBundles_Android", abb.assetBundleName)),
					abb.assetBundleName,
					MediaTypeNames.Application.Zip
				);
				i++;
			}

			var gameCategory = IsARGame ? "ar" : (IsGamecenterGame ? "gamecenter" : "canvas");

			var headers = form.headers;
			headers ["Authorization"] = "Bearer " + AccessToken;

			var cancelUpload = false;
			while (!cancelUpload) {
				var request = UnityWebRequest.Post (new Uri (Util.GetRegionBaseUri (_regions [MarketIndex], EnvironmentIndex == 1, EnvironmentIndex == 2), $"asset/{gameCategory}").AbsoluteUri, form);
				var enHeaders = headers.GetEnumerator ();
				while (enHeaders.MoveNext ()) {
					request.SetRequestHeader (enHeaders.Current.Key, enHeaders.Current.Value);
				}
				request.SendWebRequest ();

				var totalKB = form.data.Length / 1024;
				while (!request.isDone && !cancelUpload) {
					buildProgress = request.uploadProgress;
					if (EditorUtility.DisplayCancelableProgressBar (ProgressBarTitle, string.Format ("Uploading {0} of {1} KB ...", (int)(buildProgress * totalKB), totalKB), buildProgress)) {
						cancelUpload = true;
						//IsBuilding = false;
						request.Dispose ();
						request = null;
						break;
					}
					instance.Repaint ();
					yield return null;
				}
				EditorUtility.ClearProgressBar ();

				if (cancelUpload) {
					resultMessage = "Upload canceled!";
					Debug.Log (resultMessage);
				} else {
					var success = string.IsNullOrEmpty (request.error);
					if (!success) {
						resultMessage = $"Error while POSTing build to {request.uri.AbsoluteUri}: {request.responseCode} {request.downloadHandler.text}";
						Debug.LogError (resultMessage);
						yield return null;
						cancelUpload = !EditorUtility.DisplayDialog (ProgressBarTitle, string.Format ("{0}\n\nWant to try uploading again?", resultMessage), "Retry", "Cancel");
					} else {
						//Debug.Log ("CreateAssetBundles upload response: " + request.downloadHandler.text);
						var response = JsonConvert.DeserializeObject<UploadResponse> (request.downloadHandler?.text);
						resultMessage = $"Uploaded {response.gameType} Version {response.version} for {MarketDisplayNames [MarketIndex]} - total size: {totalKB} KB";
						Debug.Log (resultMessage);
						break;
					}
				}
			}

			IsUploading = false;
			RepaintWindow ();
			yield return null;

			EditorUtility.DisplayDialog (ProgressBarTitle, resultMessage, "OK");
		}


		private class UploadResponse {
			public string gameType;
			public long version;
		}


		public static Uri GetTokenUri () {
			var market = Util.Markets [_regions [MarketIndex]];
			var url = $"https://{market.Network}.{market.Country}.auth.iam.{market.Cluster}.cinemataztic.com";
			if (EnvironmentIndex != 0) {
				url = Regex.Replace (url, "(.+?)\\.[^.]+?\\.(cinemataztic\\.com)", EnvironmentIndex == 1 ? "$1.staging.$2" : "$1.dev.$2");
			}
			return new Uri (url);
		}


		static bool GetAccessToken (out string accessToken) {
			accessToken = null;
			var jsonReq = "{\"type\":\"user\","
								  + "\"email\":" + JsonConvert.SerializeObject (Username) + ","
								  + "\"password\":" + JsonConvert.SerializeObject (Password)
								  + "}";
			var request = new UnityWebRequest (
							  GetTokenUri (),
							  "POST",
							  new DownloadHandlerBuffer (),
							  new UploadHandlerRaw (Encoding.UTF8.GetBytes (jsonReq))
						  );
			request.uploadHandler.contentType = "application/json; charset=utf-8";
			request.SendWebRequest ();
			while (!request.isDone && !EditorApplication.isPlayingOrWillChangePlaymode) {
				System.Threading.Thread.Sleep (100);
			}
			if (request.result == UnityWebRequest.Result.ProtocolError) {
				Debug.LogError ($"Token service returned error: {request.responseCode} {request.downloadHandler?.text}");
				return false;
			}
			if (request.result != UnityWebRequest.Result.Success) {
				Debug.LogError ("Network error while getting upload token: " + request.error);
				return false;
			}
            if (StayLoggedIn) {
                var bytes = Encoding.UTF8.GetBytes (Password);
                for (int i = 0; i < bytes.Length; i++) {
                    bytes [i] ^= 0x5a;
                }
                EditorPrefs.SetString ("CGSP", Convert.ToBase64String (bytes));
            } else {
	            EditorPrefs.DeleteKey ("CGSP");
            }
            EditorPrefs.SetBool ("CineGameStayLoggedIn", StayLoggedIn);
			var responseBody = request.downloadHandler.text;
			try {
				var response = JsonConvert.DeserializeObject<AuthResponse> (responseBody);
				accessToken = response.access_token;
				AccessTokenTime = DateTime.Now;

				IsSuperAdmin = response.role.Contains ("super-admin");

				//TODO there's a security issue here because login is across markets, but game-access list should be per market.
				GameTypesAvailable = response.game_access ?? (new string [0]);

				instance.OnHierarchyChange ();

				Debug.Log ($"Logged into market {MarketDisplayNames [MarketIndex]} {BackendEnvironments [EnvironmentIndex]} environment");
			} catch (Exception e) {
				Debug.LogErrorFormat ("Exception while parsing JSON {0}: {1}", request.downloadHandler.text, e.ToString ());
				return false;
			}
			return true;
		}


		class AuthResponse {
			public string access_token;
			public string [] markets;
			public string [] role;
			[JsonProperty ("game-access")]
			public string [] game_access;
		}


		static string GetOutputPathForCurrentScene () {
			return string.Format ("../builds/AssetBundles/{0}/{1}", Util.Markets [_regions [MarketIndex]].MarketID, IsARGame ? "ARGameAssets" : (IsGamecenterGame ? "GameCenterAssets" : "GameAssets"));
		}


		static string GetLastBuildReportPath () {
			return "../builds/AssetBundles/buildreport.txt";
		}


		static void ExtractBuildReportFromEditorLog (BuildTarget target) {
			try {
				/*var outputPath = GetOutputPathForCurrentScene ();
				var bundleName = GameType.ToLower ();
				var bundlePath = string.Format ("{0}/AssetBundles_{1}/{2}", outputPath, target == BuildTarget.iOS ? "iOS" : "Android", bundleName);
				var bundle = AssetBundle.LoadFromFile (bundlePath);
				if (bundle != null) {
					var so = new SerializedObject (bundle);
					var sb = new StringBuilder ();

					sb.AppendLine ("Preload table:");
					foreach (SerializedProperty d in so.FindProperty ("m_PreloadTable")) {
						var o = d.objectReferenceValue;
						if (d.ob != null)
							sb.AppendLine ($"\t{d.objectReferenceValue.name} {d.objectReferenceValue.GetType ()}");
					}

					sb.AppendLine ("Container:");
					foreach (SerializedProperty d in so.FindProperty ("m_Container"))
						sb.AppendLine ($"\t{d.displayName}");

					bundle.Unload (false);

					LastBuildReportString = sb.ToString ();

					File.WriteAllText (GetLastBuildReportPath (), LastBuildReportString);

					if (instance != null) {
						instance.Focus ();
						RepaintWindow ();
					}
				} else {
					throw new Exception ("Could not load newly created AssetBundle " + bundlePath);
				}*/
				if (Application.platform == RuntimePlatform.WindowsEditor) {
					File.WriteAllText (GetLastBuildReportPath (), "Build report disabled on Windows Editor");
					return;
				}
				var logFileEditor = File.ReadAllText (Application.platform == RuntimePlatform.OSXEditor ?
					Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Library/Logs/Unity/Editor.log") :
					Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "Unity/Editor/Editor.log"));
				var bundleName = GameType.ToLower ();
				var buildReportHeader = "-------------------------------------------------------------------------------\nBundle Name: " + bundleName;
				var buildReportFooter = "-------------------------------------------------------------------------------\n";
				var idxOfBuildReport = logFileEditor.LastIndexOf (buildReportHeader);
				if (idxOfBuildReport >= 0) {
					var buildReport = logFileEditor.Substring (idxOfBuildReport + buildReportHeader.Length);

					var idxOfEndBuildReport = buildReport.IndexOf (buildReportFooter);
					if (idxOfEndBuildReport >= 0) {
						buildReport = buildReport.Substring (0, idxOfEndBuildReport);
					}
					//Filter out assets taking up less than 0.0% build size
					var sb = new StringBuilder (buildReport.Length);
					sb.AppendFormat ("Build report for {0} {1}:\n{2}\n", bundleName, target, DateTime.Now.ToString ("f"));

					var reader = new StringReader (buildReport);
					string line;
					if (reader != null) {
						while ((line = reader.ReadLine ()) != null) {
							if (line.StartsWith ("Other Assets ")) {
								line = line.Replace ("Other Assets ", "Other Assets (Movies, Fonts)");
							}
							if (line.IndexOf ("\t 0.0% Assets/") < 1) {
								sb.AppendLine (line);
							}
						}
					}
					LastBuildReportString = sb.ToString ();

					File.WriteAllText (GetLastBuildReportPath (), LastBuildReportString);

					if (instance != null) {
						instance.Focus ();
						RepaintWindow ();
					}
				} else {
					throw new Exception ("Did not find build report in Editor.log");
				}
			} catch (Exception e) {
				Debug.LogErrorFormat ("Exception while extracting build report: {0}", e);
			}
		}


		// -------------------------------------------------


		/// <summary>
		/// Build an enumeration of all assetbundles from AssetLoader Components in loaded scenes, and if prepareBuild=true then store the needed properties of the prefab in the serialized AssetLoader.
		/// </summary>
		static IEnumerable<AssetBundleBuild> ProcessAssetLoaders (bool prepareBuild) {
			var assetBundleNames = new HashSet<string> ();
			var assetLoaders =
#if UNITY_2022_3_OR_NEWER
				FindObjectsByType<AssetLoader> (FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
				FindObjectsOfType<AssetLoader> ();
#endif
			foreach (var assetLoader in assetLoaders) {
				var scenePath = assetLoader.gameObject.GetScenePath ();
				string assetPath = null;
				if (assetLoader.transform.childCount == 1) {
					var prefabInstance = assetLoader.transform.GetChild (0).gameObject;
					assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot (prefabInstance);
					if (!string.IsNullOrWhiteSpace (assetPath)) {
						var assetImporter = AssetImporter.GetAtPath (assetPath);
						if (string.IsNullOrWhiteSpace (assetImporter.assetBundleName)) {
							Debug.LogError ($"No assetbundle name assigned to prefab: {assetPath}\nInstanced from AssetLoader at {scenePath}");
						} else {
							assetBundleNames.Add (assetImporter.assetBundleName);
							if (prepareBuild) {
								Debug.Log ($"AssetLoader bundleName={assetImporter.assetBundleName} bundleVariant={assetImporter.assetBundleVariant} path={assetPath} at {scenePath}");

								//Set prefab instance tag to EditorOnly, so that the AssetLoader instance can async load and instantiate the prefab
								var originalTag = prefabInstance.tag;
								var so = new SerializedObject (prefabInstance);
								so.FindProperty ("m_TagString").stringValue = "EditorOnly";
								so.ApplyModifiedPropertiesWithoutUndo ();

								//Store properties for runtime instantiation
								var originalPosition = prefabInstance.transform.localPosition;
								var originalRotation = prefabInstance.transform.localRotation;
								var originalScale = prefabInstance.transform.localScale;
								so = new SerializedObject (assetLoader);
								so.FindProperty ("AssetName").stringValue = assetPath;
								so.FindProperty ("AssetBundleURL").stringValue = $"{GameType}/{assetImporter.assetBundleName}"; //-{assetImporter.assetBundleVariant}";
								so.FindProperty ("InstanceTag").stringValue = originalTag;
								so.FindProperty ("InstanceLocalPosition").vector3Value = originalPosition;
								so.FindProperty ("InstanceLocalRotation").quaternionValue = originalRotation;
								so.FindProperty ("InstanceLocalScale").vector3Value = originalScale;
								so.ApplyModifiedPropertiesWithoutUndo ();
								//assetImporter.SetAssetBundleNameAndVariant (bundleName, string.Empty);
								//AssetDatabase.GetAssetPathsFromAssetBundle (bundleName)
							}
						}
					}
				}
				if (string.IsNullOrWhiteSpace (assetPath)) {
					Debug.LogError ($"AssetLoader must have one child which is a prefab at {scenePath}");
				}
			}
			Debug.Log ($"Processed {assetLoaders.Count ()} AssetLoaders-- async AssetBundles to build: {assetBundleNames.Count ()}");

			return assetBundleNames.Select (abn => new AssetBundleBuild {
				assetBundleName = abn,
				assetNames = AssetDatabase.GetAssetPathsFromAssetBundle (abn),
			});
		}


        [MenuItem ("CinemaTaztic/Check VideoClips")]
        public static bool CheckVideoClips () {
			var allValid = true;
            var videoPaths = AssetDatabase.FindAssets ("t:VideoClip").Select (guid => AssetDatabase.GUIDToAssetPath (guid));
            Debug.Log ($"CheckVideoClips: checking {videoPaths.Count ()} VideoClips in project ...");
            foreach (var path in videoPaths) {
                var videoClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip> (path);
				if (videoClip == null)
					continue;
                var width = (int)videoClip.width;
				var height = (int)videoClip.height;
                var assetImporter = (VideoClipImporter)AssetImporter.GetAtPath (videoPaths.First ());
				var importSettings = assetImporter.GetTargetSettings ("Android") ?? assetImporter.defaultTargetSettings;
                if (importSettings.resizeMode == VideoResizeMode.CustomSize) {
					width = importSettings.customWidth;
					height = importSettings.customHeight;
				}
				var isPushing = (width > 1080) || (height > 1080);
				if (isPushing) {
                    var isWidth8 = (width >> 3) << 3 == width;
                    var isHeight8 = (height >> 3) << 3 == height;
					if (!isWidth8 || !isHeight8) {
                        Debug.LogWarning ($"VideoClip might not play on some Android devices due to hardware limitations (above 1080p): width={width} or height={height} not divisible by 8: {path}", videoClip);
						allValid = false;
                    }
                }
            }
            EditorUtility.UnloadUnusedAssetsImmediate ();
			if (allValid) {
				Debug.Log ($"CheckVideoClips: No invalid VideoClips found in project.");
			}
            return allValid;
        }


		private static bool CGSP () {
			Username = EditorPrefs.GetString ("AssetBundleUser");
			if (EditorPrefs.HasKey ("CGSP")) {
				var bytes = Convert.FromBase64String (EditorPrefs.GetString ("CGSP"));
				for (int i = 0; i < bytes.Length; i++) {
					bytes [i] ^= 0x5a;
				}
				Password = Encoding.UTF8.GetString (bytes);
				return true;
			}
			return false;
		}
	}
}