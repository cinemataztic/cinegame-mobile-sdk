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

using Newtonsoft.Json;

using CineGame.MobileComponents;
using CineGame.MobileComponents.GC;

namespace CineGameEditor.MobileComponents {
	public class CineGameBuild : EditorWindow {
		static CineGameBuild instance;

		static string ProgressBarTitle = "CineGame Build";

		static string resultMessage = string.Empty;
		//contains result description from last scene build+upload

		static string Username;
		static string Password;
		static bool StayLoggedIn;
		public static string GameType;
		static string AppName;
		static bool IsSuperAdmin;

		static int MarketIndex;
		static int GameTypeIndex;
		static string [] GameTypesAvailable;

		/// <summary>
		/// Market IDs from cloud
		/// </summary>
		static class MarketID {
			public const string BioSpil = "57ff5b54359bc3000f1e1303";
			public const string KinoSpill = "57e79e40bb29b2000f22c704";
			public const string CineGame = "57e79e61bb29b2000f22c705";
			public const string Leffapeli = "5829676efd5ab2000f4eb252";
			public const string CineGame_AUS = "5ba2a95eb81b02b3d8198f89";
			public const string CineGame_IE = "618301a5be9b8d3befa0b589";
			public const string CineGame_IN = "627049112c827460088db3fd";
			public const string CineGame_NZ = "62a741d8709ea7ac02336c29";
			public const string CineGame_UAE = "5c12f1c58c2a1a5509cad589";
			public const string REDyPLAY = "5c44f3ba8c2a1a5509df3f6b";
			public const string ForumFun = "5ced2b5a8c2a1a5509b0116b";
			public const string CinesaFun = "5df786218c2a1a550974e19d";
			public const string Baltoppen = "58750bffb2928c000f2ff481";
			public const string DEMO_CineGame = "5b841697b81b02b3d8381244";
			public const string Cinemataztic_dev = "594be135e9678d3bb75fe7aa";
		}

		static string [] MarketDisplayNames = new string [] {
			"biospil-dk",
			//"kinospill-no",
			"cinegame-en",
			"finnkino-fi",
			//"cinegame-au",
			"cinegame-ie",
			"cinegame-in",
			//"cinegame-nz",
			//"cinegame-ae",
			"redyplay-de",
			//"forumfun-ee",
			//"cinesafun-es",
			//"Baltoppen (BioSpil)",
			//"DEMO CineGame",
			//"Cinemataztic-dev (CineGame)",
		};

		static string [] MarketIDs = new string [] {
			MarketID.BioSpil,
			//MarketID.KinoSpill,
			MarketID.CineGame,
			MarketID.Leffapeli,
			//MarketID.CineGame_AUS,
			MarketID.CineGame_IE,
			MarketID.CineGame_IN,
			//MarketID.CineGame_NZ,
			//MarketID.CineGame_UAE,
			MarketID.REDyPLAY,
			//MarketID.ForumFun,
			//MarketID.CinesaFun,
			//MarketID.Baltoppen,
			//MarketID.DEMO_CineGame,
			//MarketID.Cinemataztic_dev,
		};

		public static Dictionary<string, string> MarketTokenUris = new Dictionary<string, string> {
			{ MarketID.BioSpil,         "https://drf.dk.auth.iam.drf-1.cinemataztic.com/" },
			{ MarketID.CineGame,        "https://cinemataztic.en.auth.iam.eu-1.cinemataztic.com" },
			{ MarketID.Leffapeli,       "https://finnkino.fi.auth.iam.eu-1.cinemataztic.com" },
			{ MarketID.CineGame_IE,     "https://wideeyemedia.ie.auth.iam.eu-2.cinemataztic.com" },
			{ MarketID.CineGame_IN,     "https://itv.in.auth.iam.asia-1.cinemataztic.com" },
			{ MarketID.REDyPLAY,        "https://weischer.de.auth.iam.eu-2.cinemataztic.com" },
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

			var centeredStyle = GUI.skin.GetStyle ("Label");
			centeredStyle.alignment = TextAnchor.UpperCenter;
			centeredStyle.fontStyle = FontStyle.Bold;

			var _marketIndex = Mathf.Clamp (MarketIndex, 0, MarketIDs.Length);
			_marketIndex = EditorGUILayout.Popup (new GUIContent ("Market:"), _marketIndex, MarketDisplayNames);
			if (MarketIndex != _marketIndex) {
				MarketIndex = _marketIndex;
				if (!string.IsNullOrWhiteSpace (Username) && !string.IsNullOrWhiteSpace (Password)) {
					if (!GetAccessToken (out AccessToken)) {
						EditorUtility.DisplayDialog (ProgressBarTitle, "Failed to login. Check username and password and that you are connected to the internet", "OK");
						return;
					}
					EditorPrefs.SetString ("AssetMarketId", MarketIDs [_marketIndex]);
				}
			}

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
				EditorGUILayout.LabelField ("Please select a game scene!", centeredStyle);
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

			if (EditorApplication.isCompiling) {
				EditorGUILayout.HelpBox ("Waiting for scripts to recompile ...", MessageType.Info);
			} else if (!IsBuilding) {
				var lastBuildReportPath = GetLastBuildReportPath ();
				if (string.IsNullOrEmpty (LastBuildReportString) && File.Exists (lastBuildReportPath)) {
					LastBuildReportString = File.ReadAllText (lastBuildReportPath);
				}

				var hasBuildReport = !string.IsNullOrEmpty (LastBuildReportString);
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
					} else if (fileExistsAndroid && fileExistsIos && GUILayout.Button ("Upload " + GameType + " for " + AppName)) {
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
				var _gameProxy = FindObjectOfType<GameProxy> ();
				var _isCanvasGame = _gameProxy != null;
				if (IsCanvasGame != _isCanvasGame) {
					IsCanvasGame = _isCanvasGame;
					Repaint ();
				}

				if (_isCanvasGame && GameType != _gameProxy.GameType) {
					if (IsSuperAdmin) {
						GameType = _gameProxy.GameType;
						if (string.IsNullOrWhiteSpace (GameType)) {
							GameType = EditorSceneManager.GetActiveScene ().name;
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

				var _gcGameManager = FindObjectOfType<GC_GameManager> ();
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
			var _gameProxy = FindObjectOfType<GameProxy> ();
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

			MarketIndex = Mathf.Clamp (Array.IndexOf (MarketIDs, EditorPrefs.GetString ("AssetMarketId", MarketIDs [0])), 0, MarketIDs.Length - 1);
			EditorPrefs.SetString ("AssetMarketId", MarketIDs [MarketIndex]);

			StayLoggedIn = EditorPrefs.GetBool ("CineGameStayLoggedIn");

			if (CGSP () && Application.internetReachability != NetworkReachability.NotReachable) {
				GetAccessToken (out AccessToken);
				OnHierarchyChange ();
			}
		}

		public void OnDisable () {
			EditorApplication.hierarchyChanged -= OnHierarchyChange;
		}

		[MenuItem ("CinemaTaztic/Build And Upload")]
		public static void Init () {
			if (instance == null) {
				instance = GetWindow<CineGameBuild> (false, ProgressBarTitle, true);
			}
			instance.Focus ();
		}

		static void RepaintWindow () {
			if (instance != null) {
				EditorUtility.SetDirty (instance);
				instance.Repaint ();
			}
		}


		static List<BuildTarget> GetBuildTargets () {
			var targets = new List<BuildTarget> (2);
			targets.Add (EditorUserBuildSettings.activeBuildTarget);
			targets.Add ((EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) ? BuildTarget.Android : BuildTarget.iOS);
			return targets;
		}


		[MenuItem ("CinemaTaztic/Build All Scenes For Market")]
		static void BuildAllScenesForAllPlatforms () {
			if (EditorUtility.DisplayDialog (ProgressBarTitle, "Build all DLC scenes for region " + Util.GetRegion ().ToString () + "?", "OK", "Cancel")) {
				Init ();
				instance.StartCoroutine (instance.E_BuildAllScenesForAllPlatforms ());
			}
		}

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
			Debug.LogFormat ("Building AssetBundles for platform {0}", buildTarget);
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
				Debug.Log ("User cancelled build");
				yield break;
			}

			if (!CheckSceneIntegrity ()) {
				Debug.Log ("CheckSceneIntegrity returned false");
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
			float dpct = 1f / (float)targets.Count;
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

				if (!BuildBundles (new [] { abb }, GetOutputPathForCurrentScene (), buildTarget)) {
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
				return true;
			}
			return false;
		}


		IEnumerator E_UploadSingleSceneForAllPlatforms () {
			Util.GetSdkVersionAndBuildTime (out Version sdkVersion, out DateTime sdkBuildTime);

			IsUploading = true;
			progressMessage = string.Format ("Uploading {0} for {1} to live server ...", GameType, AppName);
			buildProgress = 0f;
			RepaintWindow ();
			yield return null;

			var bundleName = GameType.ToLower ();
			var outputPath = GetOutputPathForCurrentScene ();

			//EditorUtility.RevealInFinder (fullOutputPath);
			var form = new WWWForm ();
			form.AddBinaryData ("iosBundle",
				File.ReadAllBytes (string.Format ("{0}/AssetBundles_iOS/{1}", outputPath, bundleName)),
				bundleName,
				MediaTypeNames.Application.Zip
			);
			form.AddBinaryData ("androidBundle",
				File.ReadAllBytes (string.Format ("{0}/AssetBundles_Android/{1}", outputPath, bundleName)),
				bundleName,
				MediaTypeNames.Application.Zip
			);
			form.AddField ("gameType", GameType);
			form.AddField ("sdkVersion", sdkVersion.ToString ());
			form.AddField ("sdkBuildTime", sdkBuildTime.ToString ("u"));

			var gameCategory = IsARGame ? "ar" : (IsGamecenterGame ? "gamecenter" : "canvas");

			var headers = form.headers;
			headers ["Authorization"] = "Bearer " + AccessToken;

			var cancelUpload = false;
			while (!cancelUpload) {
				var request = UnityWebRequest.Post (new Uri (Util.GetRegionBaseUri (MarketIDs [MarketIndex]), $"asset/{gameCategory}").AbsoluteUri, form);
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
						resultMessage = $"Uploaded {response.gameType} Version {response.version} for {AppName} - total size: {totalKB} KB";
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
			return new Uri (MarketTokenUris [MarketIDs [MarketIndex]]);
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
			while (!request.isDone) {
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
				GameTypesAvailable = (response.game_access != null) ? response.game_access : new string [0];

				instance.OnHierarchyChange ();

				Debug.Log ($"Logged into market {MarketDisplayNames [MarketIndex]}");
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
			return string.Format ("../builds/AssetBundles/{0}/{1}", MarketIDs [MarketIndex], IsARGame ? "ARGameAssets" : (IsGamecenterGame ? "GameCenterAssets" : "GameAssets"));
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
		/// Pre-build method for Cloud Build which assigns the proper assetbundle names to all DLC
		/// </summary>
		[MenuItem ("CinemaTaztic/Assign AssetBundle Names")]
		public static void AssignAssetBundleNames () {
#if UNITY_CLOUD_BUILD
			//CinemaMobileBuild.InitializeRegion ();
#endif
			var region = Util.GetRegion ().ToString ();
			Debug.Log ($"Assigning assetbundles for region {region} ...");
			AssetDatabase.RemoveUnusedAssetBundleNames ();
			AssignAssetBundleNames (region, AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/Scenes/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid)));
			AssignAssetBundleNames (region, AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/GameCenter/Spil/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid)));
			//AssignAssetBundleNames("gc", region, AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/GameCenter/Spil/" }).Select(guid => AssetDatabase.GUIDToAssetPath(guid)));
			AssignAssetBundleNames (region, AssetDatabase.FindAssets ("t:Scene", new string [] { "Assets/AR/Scenes/" + region }).Select (guid => AssetDatabase.GUIDToAssetPath (guid)));

			CheckAssetBundleScenes ();
		}

		/// <summary>
		/// Checks the asset bundles for multiple scenes and output error if any present.
		/// </summary>
		[MenuItem ("CinemaTaztic/Check AssetBundle Scenes")]
		public static void CheckAssetBundleScenes () {
			Debug.Log ("Checking AssetBundle Scenes ...");
			var region = Util.GetRegion ().ToString ();
			var assetBundleNames = AssetDatabase.GetAllAssetBundleNames ();
			var listSceneNames = new HashSet<string> ();
			foreach (var abName in assetBundleNames) {
				var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle (abName);
				Debug.LogFormat ("Checking AssetBundle '{0}' ...", abName);
				listSceneNames.Clear ();
				foreach (var aPath in assetPaths) {
					if (Path.GetExtension (aPath) == ".unity") {
						//Asset is a scenefile, log name
						var sceneName = Path.GetFileNameWithoutExtension (aPath);
						if (!listSceneNames.Add (sceneName)) {
							Debug.LogErrorFormat ("ERROR: AssetBundle '{0}' contains multiple scenes with the name '{1}'. Scene names in each Asset Bundle must be unique", abName, sceneName);
						}
					}
				}
			}
		}

		static void AssignAssetBundleNames (string marketId, IEnumerable<string> assetPaths) {
			Debug.Log ("Removing bundlenames for downloadable scenes ...");
			foreach (var assetPath in assetPaths) {
				var sceneName = Path.GetFileNameWithoutExtension (assetPath);
				var bundleName = sceneName.ToLower ();
				AssetDatabase.RemoveAssetBundleName (bundleName, forceRemove: true);
			}
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();

			Debug.Log ("Assigning bundlenames for downloadable scenes ...");
			var i = 0;
			foreach (var assetPath in assetPaths) {
				var sceneName = Path.GetFileNameWithoutExtension (assetPath);
				var bundleName = sceneName.ToLower ();
				EditorUtility.DisplayProgressBar ($"Assigning {marketId} bundlenames ...", assetPath, (float)i / assetPaths.Count ());
				var assetImporter = AssetImporter.GetAtPath (assetPath);
				assetImporter.SetAssetBundleNameAndVariant (bundleName, string.Empty);
				//assetImporter.SaveAndReimport ();
				i++;
			}
			EditorUtility.ClearProgressBar ();
		}

		/// <summary>
		/// Map of original script guids in mobile sdk runtime. These should NEVER change.
		/// </summary>
		static Dictionary<string, string> GuidDict = new Dictionary<string, string> {
			{ "TimingComponent.cs", "a584f9c43c8364f49bb9fe2a0e8f84f5" },
			{ "SendVariableEvent.cs", "330f0810a95a11d47a520951fbeb3146" },
			{ "Supporter.cs", "4b77932d9977b483580fe3b666729bda" },
			{ "Vibrate.cs", "90e5e87c145124627a1cbb7394f35b0e" },
			{ "ThrowComponent.cs", "39ad5dd4450e54526be26021eeaa26d8" },
			{ "TextInputComponent.cs", "4ec468fea26b54dd487ea01d0414e68a" },
			{ "SwipeDirectionComponent.cs", "551a5488302ea714e81ec1c6d90de91a" },
			{ "SwipeComponent.cs", "c7deefd2f731cf149aebbe68b5a25954" },
			{ "SpawnComponent.cs", "e5704d5e14eef4767b52b489559f10ed" },
			{ "SliderComponent.cs", "d53e99312a4974ee5ace6c2e52e52aa1" },
			{ "SetTextComponent.cs", "297e197a819704cb99e8d5932166d81a" },
			{ "SetParticlesColor.cs", "7c20d9a009599490fbab4aed1c760230" },
			{ "SendVariableComponent.cs", "b2c122fc170c24695abd11767c61efea" },
			{ "ReplicatedComponent.cs", "3d59a6db02a9549b98fd389eb616de26" },
			{ "RenderConfig.cs", "8af72a6d7c2b44ba6aca576dbcc2e4d4" },
			{ "RemoteTextComponent.cs", "f64f24ad31c70436abc2ec8635a056b4" },
			{ "RemoteSpriteComponent.cs", "8e28a58e3594b4f2e9f6fe925e3dfb65" },
			{ "RemoteControl.cs", "546ec569191b0428981532da17a34b76" },
			{ "PointerEventComponent.cs", "c89d64cb538764722870310bd54cf525" },
			{ "PlaySound.cs", "091a2f98215b249199e701dd0bad8ed5" },
			{ "PhysicsConfig.cs", "63017d3eb48a54e138cb5be84c521fa1" },
			{ "OnLogin.cs", "580b8ede9eafe4579bc685643161d87d" },
			{ "OnEnableDisable.cs", "3333ae0926977417092a70b35c7f8732" },
			{ "OnCollision.cs", "276c960910d194b68b28b491a48876b1" },
			{ "OnApplicationPlatform.cs", "5cd271b10ceca487f84b5d4a82517130" },
			{ "LookAt.cs", "93bc52955bb1a40a5aafdaeb32f37cfe" },
			{ "GyroComponent.cs", "4b2e7470ca5cd4ba28ba7ca9f9432a98" },
			{ "GravityComponent.cs", "a5f37167862804f4085906d19efb4f6d" },
			{ "GestureComponent.cs", "8c84447952b4a4e42b6547948392fa93" },
			{ "GameProxy.cs", "6e2d78771f4b44644a63252576974c3b" },
			{ "DragDropComponent.cs", "04691ed229dff4af08511d9ceb0dfb29" },
			{ "DragDropAnywhereComponent.cs", "36662550c2ffaa941b549c13cda4fec0" },
			{ "DPadComponent.cs", "bc396f5091ed84c3591030cd477bbef9" },
			{ "ChoicesComponent.cs", "5a0c36823542b4a3faf55597b502316c" },
			{ "ChoiceComponent.cs", "75ef6897842c3468ea6ec8e9cf6c8f08" },
			{ "AngularPointerComponent.cs", "7814844a3c70f4ceb88a8b90fd1833f2" },
			{ "AnchorPositionFrom3D.cs", "29803c86459594ae388d0f536654bc6d" },
			{ "AlignToAxes.cs", "bcde88b88311a4a6e87eed4799886cde" },
			{ "LogicComponent.cs", "cc7fbe9cb65b24476b1426f19a438a63" },
			{ "JoystickComponent.cs", "e12223f1cfe254b5fa8b051b88c642a2" },
			{ "GetTransformChild.cs", "bbd2c6c6ead9d4f09ad1a45f3dbedcf1" },
			{ "GetTransformProperty.cs", "5ac1a4302dfe6468e8cbe43ec944b6cc" },
			{ "Destroy.cs", "05255282a40314348a18c91f3f18ffc4" },
			{ "FollowComponent.cs", "6ce1fb93fd2ad47d599ce2dc1e36d627" },
			{ "AnimationEventListener.cs", "13d77d1e33ee27f428f0d7175a2076b2" },
			{ "AnimatorParameter.cs", "40fd1fe7494a34ddba76ed6c1897cd53" },
			{ "TimerComponent.cs", "1a13d68ca6f1f4f209d3ffcd1b467e3b" },
		};

		/// <summary>
		/// Check to make sure that SDK scripts still have the original GUIDs. If not, uploads will not work, so we must enforce it
		/// </summary>
		[InitializeOnLoadMethod]
		static void CheckSDKIntegrity () {
			var scriptGuids = AssetDatabase.FindAssets ("t:Script", new string [] { "Packages/com.cinegamesdk.mobile/Runtime" });
			var resetGuid = false;
			foreach (var guid in scriptGuids) {
				var path = AssetDatabase.GUIDToAssetPath (guid);
				var filename = Path.GetFileName (path);
				if (GuidDict.TryGetValue (filename, out string originalGuid) && guid != originalGuid) {
					resetGuid = true;
					Debug.LogWarning ($"GUID changed {filename} {originalGuid} => {guid} - resetting");
					var metaPath = path + ".meta";
					var metaLines = File.ReadAllLines (metaPath);
					for (var i = 0; i < metaLines.Length; i++) {
						if (metaLines [i].StartsWith ("guid: ")) {
							metaLines [i] = "guid: " + originalGuid;
							File.WriteAllLines (metaPath, metaLines);
							break;
						}
					}
				}
			}
			if (resetGuid) {
				AssetDatabase.Refresh ();
			} else {
				Debug.Log ("SDK guids checked OK");
			}
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