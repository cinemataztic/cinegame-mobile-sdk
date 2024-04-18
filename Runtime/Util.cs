using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System.Linq;

namespace CineGame.MobileComponents {

	public static class Util {
		public const string CinematazticIconGUID = "efec9089586ac4574995396036ef8524";

		[Serializable]
		public enum APIRegion {
			DK = 0, //drf-dk
			NO = 1, //mdn-no
			EN = 2, //cinemataztic-en
			FI = 3, //finnkino-fi
			AU = 4, //valmorgan-au
			AE = 5, //cinemataztic-ae
			DE = 6, //weischer-de
			EE = 7, //forumkino-ee
			ES = 8, //cinesa-es
			IE = 9, //wideeyemedia-ie
			IN = 10,//itv-in
			NZ = 11,//valmorgan-nz
			SE = 12,//filmstaden-se
		};

		public class Market {
			public APIRegion Region;
			public string MarketID;
			public string Market_Slug;
			public string Network;
			public string Country;
			public string Cluster;
			public string DefaultLocale;
		}

		public static readonly Dictionary<APIRegion, Market> Markets = new () {
			{ APIRegion.DK, new Market { Region = APIRegion.DK, MarketID = "57ff5b54359bc3000f1e1303", Network = "drf", Country = "dk", Cluster = "drf-1", DefaultLocale = "da" } },
			{ APIRegion.NO,	new Market { Region = APIRegion.NO, MarketID = "57e79e40bb29b2000f22c704", Network = "mdn",	Country = "no", Cluster = "drf-1",	DefaultLocale = "no" } },
			{ APIRegion.EN, new Market { Region = APIRegion.EN, MarketID = "57e79e61bb29b2000f22c705", Network = "cinemataztic", Country = "en", Cluster = "eu-1", DefaultLocale = "en" } },
			{ APIRegion.FI, new Market { Region = APIRegion.FI, MarketID = "5829676efd5ab2000f4eb252", Network = "finnkino", Country = "fi", Cluster = "eu-1", DefaultLocale = "fi" } },
			{ APIRegion.AU, new Market { Region = APIRegion.AU, MarketID = "5ba2a95eb81b02b3d8198f89", Network = "valmorgan", Country = "au", Cluster = "au-1", DefaultLocale = "en-AU" } },
			{ APIRegion.AE, new Market { Region = APIRegion.AE, MarketID = "5c12f1c58c2a1a5509cad589", Network = "cinemataztic", Country = "ae", Cluster = "au-1", DefaultLocale = "en-US" } },
			{ APIRegion.DE, new Market { Region = APIRegion.DE, MarketID = "5c44f3ba8c2a1a5509df3f6b", Network = "weischer", Country = "de", Cluster = "eu-2", DefaultLocale = "de" } },
			{ APIRegion.EE, new Market { Region = APIRegion.EE, MarketID = "5ced2b5a8c2a1a5509b0116b", Network = "forumkino", Country = "ee", Cluster = "eu-1", DefaultLocale = "et" } },
			{ APIRegion.ES, new Market { Region = APIRegion.ES, MarketID = "5df786218c2a1a550974e19d", Network = "cinesa", Country = "es", Cluster = "eu-2", DefaultLocale = "es" } },
			{ APIRegion.IE, new Market { Region = APIRegion.IE, MarketID = "618301a5be9b8d3befa0b589", Network = "wideeyemedia", Country = "ie", Cluster = "eu-2", DefaultLocale = "en-IE" } },
			{ APIRegion.IN, new Market { Region = APIRegion.IN, MarketID = "627049112c827460088db3fd", Network = "itv", Country = "in", Cluster = "asia-1", DefaultLocale = "en-IN" } },
			{ APIRegion.NZ, new Market { Region = APIRegion.NZ, MarketID = "62a741d8709ea7ac02336c29", Network = "valmorgan", Country = "nz", Cluster = "au-1", DefaultLocale = "en-NZ" } },
			{ APIRegion.SE, new Market { Region = APIRegion.SE, MarketID = "653676850c50fc8ecda86b43", Network = "filmstaden", Country = "se", Cluster = "eu-1", DefaultLocale = "sv" } },
		};

		public delegate void HapticEvent (HapticFeedbackConstants feedbackConstant);
		public delegate void VibrationEvent (string pattern);
		/// <summary>
		/// App hooks up here so SDK can play haptics via PerformHapticFeedback
		/// </summary>
		public static event HapticEvent OnPlayHapticFeedback;
		/// <summary>
		/// App hooks up here so SDK can play custom vibration patterns
		/// </summary>
		public static event VibrationEvent OnPlayVibrationEffect;

		public delegate UnityWebRequest AssetBundleEvent (string assetURL);
		/// <summary>
		/// App hooks up here so SDK can download hosted AssetBundles
		/// </summary>
		public static event AssetBundleEvent OnLoadAssetBundle;

		/// <summary>
		/// Determine API Region based on Application.identifier
		/// </summary>
		public static APIRegion GetRegion () {
			switch (Application.identifier) {
			case "com.oxmond.biospil":
			case "air.com.oxmond.biospil":
				return APIRegion.DK;
			case "com.cinemataztic.cinemagame":
				return APIRegion.EN;
			case "com.cinemataztic.cinegameau":
			case "com.cinegameau.cinegameau":
				return APIRegion.AU;
			case "com.cinemataztic.cinegameger":
			case "com.redyplay.redyplay":
				return APIRegion.DE;
			case "com.cinemataztic.kinospill":
				return APIRegion.NO;
			case "com.cinemataztic.leffapeli":
				return APIRegion.FI;
			case "com.wideeyemedia.cinegameie":
				return APIRegion.IE;
			case "com.valmorgan.cinegamenz":
				return APIRegion.NZ;
			case "com.se.filmstaden.cinegame":
			case "com.se.filmstaden.cinegamese":
				return APIRegion.SE;
			default:
				Debug.LogErrorFormat ("Unexpected application identifier {0}. App will NOT work!", Application.identifier);
				Debug.Break ();
				return APIRegion.DK;
			}
		}


		public static string GetRegionProfanityUrl () {
			return $"https://profanity.cinemataztic.com/{Markets [GetRegion ()].MarketID}/txt-file";
		}


		/// <summary>
		/// Get API Base URI based on applicationIdentifier and whether app is in staging mode
		/// </summary>
		public static Uri GetRegionBaseUri (bool isStaging = false, bool isDev = false) {
			//return new Uri ("http://localhost:5000/api/");
			var market = Markets [GetRegion ()];
			var uri = new Uri ($"https://{market.Network}-{market.Country}.cinegamecore.{market.Cluster}.cinemataztic.com/api/");
			if (isStaging || isDev) {
				return new Uri (Regex.Replace (uri.AbsoluteUri, "(.+?)\\.[^.]+?\\.(cinemataztic\\.com.+)", isStaging ? "$1.staging.$2" : "$1.dev.$2"));
			}
			return uri;
		}


		/// <summary>
		/// Get API Base URI based on Cloud marketId
		/// </summary>
		public static Uri GetRegionBaseUri (APIRegion region, bool isStaging = false, bool isDev = false) {
			var market = Markets [region];
			var uri = new Uri ($"https://{market.Network}-{market.Country}.cinegamecore.{market.Cluster}.cinemataztic.com/api/");
			if (isStaging || isDev) {
				return new Uri (Regex.Replace (uri.AbsoluteUri, "(.+?)\\.[^.]+?\\.(cinemataztic\\.com.+)", isStaging ? "$1.staging.$2" : "$1.dev.$2"));
			}
			return uri;
		}


		/// <summary>
		/// Get default language for region/market
		/// </summary>
		public static string GetRegionDefaultLanguage () {
			return Markets [GetRegion ()].DefaultLocale;
		}


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
		public static string GetObjectScenePath (GameObject obj, char separator = '/') {
			var sb = new StringBuilder ();
			sb.Append (obj.scene.name);
			AppendParentName (ref sb, obj.transform, separator);
			return sb.ToString ();
		}

		/// <summary>
		/// Builds a list of the persistent listeners on the specified events for logging/debugging
		/// </summary>
		public static string GetEventPersistentListenersInfo (UnityEventBase e) {
			var sb = new StringBuilder ();
			var numListeners = e.GetPersistentEventCount ();
			for (int i = 0; i < numListeners; i++) {
				var obj = e.GetPersistentTarget (i);
				if (obj != null) {
					sb.AppendFormat ("{0} > {1}.{2}\n", obj.name, obj.GetType (), e.GetPersistentMethodName (i));
				}
			}
			return sb.ToString ();
		}

        /// <summary>
        /// Draws a Debug line to the transform of each persistent event listener
        /// </summary>
        public static void DrawLinesToPersistentEventListeners (UnityEventBase e, Vector3 start, Color color) {
            var numListeners = e.GetPersistentEventCount ();
            for (int i = 0; i < numListeners; i++) {
                var obj = e.GetPersistentTarget (i);
				Vector3 end;
				if (obj is GameObject go) {
					end = go.transform.position;
				} else {
					var c = obj as Component;
					end = c.transform.position;
                }
				if (start != end) {
					Debug.DrawLine (start, end, color);
				}
            }
        }

		public static string ComputeMD5Hash (string s) {
			return ComputeMD5Hash (Encoding.Default.GetBytes (s));
		}

		public static string ComputeMD5Hash (byte [] data) {
			using (var md5h = System.Security.Cryptography.MD5.Create ()) {
				var hash = md5h.ComputeHash (data);
				var sb = new StringBuilder ();
				for (var i = 0; i < hash.Length; ++i) {
					sb.Append (hash [i].ToString ("x2"));
				}
				return sb.ToString ();
			}
		}

		/// <summary>
		/// Crops texture so it has same aspect ratio as imageWidth:imageHeight
		/// </summary>
		public static void CropTextureToImage (Texture2D texture, int imageWidth, int imageHeight, out Texture2D croppedTexture) {
			var pHeight = imageHeight * texture.width / imageWidth;
			Color [] pixels;
			if (pHeight <= texture.height) {
				croppedTexture = new Texture2D (texture.width, pHeight, texture.format, false);
				pixels = texture.GetPixels (0, (texture.height - pHeight) / 2, texture.width, pHeight);
			} else {
				var pWidth = imageWidth * texture.height / imageHeight;
				croppedTexture = new Texture2D (pWidth, texture.height, texture.format, false);
				pixels = texture.GetPixels ((texture.width - pWidth) / 2, 0, pWidth, texture.height);
			}
			croppedTexture.SetPixels (pixels);
			croppedTexture.Apply ();
		}

		/// <summary>
		/// Blit non-readable texture to rendertexture and copy results to readable texture. Remember to release texture when done with it!
		/// </summary>
		public static Texture2D CreateReadableTexture2D (Texture2D texture) {
			// Create a temporary RenderTexture of the same size as the texture
			var tmpRenderTexture = RenderTexture.GetTemporary (texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
			Graphics.Blit (texture, tmpRenderTexture);
			var prevRenderTexture = RenderTexture.active;
			RenderTexture.active = tmpRenderTexture;

			// Create a readable Texture2D and copy the pixels to it
			var readableTexture = new Texture2D (texture.width, texture.height);
			readableTexture.ReadPixels (new Rect (0, 0, tmpRenderTexture.width, tmpRenderTexture.height), 0, 0);
			readableTexture.Apply ();
			RenderTexture.active = prevRenderTexture;
			RenderTexture.ReleaseTemporary (tmpRenderTexture);
			return readableTexture;
		}

		public static T TryGetValue<T> (Dictionary<string, object> d, string key) {
			object objValue;
			if (d.TryGetValue (key, out objValue)) {
				return (T)objValue;
			}
			return default (T);
		}

		/// <summary>
		/// Find all components of a given type in all loaded scenes, whether active or not.
		/// This is slow as we have to manually scan all loaded scenes for the component type, so don't use it often.
		/// </summary>
		public static T [] FindAllLoaded<T> (bool includeInactive = false) {
			var list = new List<T> ();
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt (i);
				if (scene.isLoaded) {
					var roots = scene.GetRootGameObjects ();
					foreach (var root in roots) {
						list.AddRange (root.GetComponentsInChildren<T> (includeInactive));
					}
				}
			}
			return list.ToArray ();
		}

		/// <summary>
		/// Download a hosted AssetBundle using the relative URL specified as argument
		/// </summary>
		public static UnityWebRequest DownloadAssetBundle (string relativeAssetURL) {
			return OnLoadAssetBundle?.Invoke (relativeAssetURL);
		}

		private static AndroidJavaObject _androidVibrator;
		public static AndroidJavaObject AndroidVibrator {
			get {
				if (_androidVibrator == null) {
					using (var unityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer"))
					using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject> ("currentActivity")) {
						_androidVibrator = currentActivity.Call<AndroidJavaObject> ("getSystemService", "vibrator");
					}
				}
				return _androidVibrator;
			}
		}

		private static AndroidJavaClass _androidVibrationEffect;
		public static AndroidJavaClass AndroidVibrationEffect {
			get {
				if (_androidVibrationEffect == null) {
					_androidVibrationEffect = new AndroidJavaClass ("android.os.VibrationEffect");
				}
				return _androidVibrationEffect;
			}
		}

		private static int _androidAPILevel;
		public static int AndroidAPILevel {
			get {
				if (_androidAPILevel == default) {
					using (var version = new AndroidJavaClass ("android.os.Build$VERSION")) {
						_androidAPILevel = version.GetStatic<int> ("SDK_INT");
					}
				}
				return _androidAPILevel;
			}
		}

		/// <summary>
		/// Vibrate the phone for the specified amount of ms (only implemented on Android, iOS will vibrate default time)
		/// </summary>
		public static void Vibrate (long milliseconds = 500) {
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor) {
				AndroidVibrator.Call ("vibrate", milliseconds);
			} else {
				Handheld.Vibrate ();
			}
		}

		/// <summary>
		/// Vibrate the phone in a specific pattern (long array of ms, even are vibrate, odd are pause). Only implemented on Android, iOS will vibrate once default time
		/// </summary>
		public static void Vibrate (long [] pattern, int repeat) {
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor) {
				AndroidVibrator.Call ("vibrate", pattern, repeat);
			} else {
				Handheld.Vibrate ();
			}
		}

		/// <summary>
		/// Vibrate the phone in a specific pattern. The file format is platform specific (AHAP for iOS, custom VibrationEffect.Composition format for Android)
		/// </summary>
		public static void Vibrate (string pattern) {
			if (!Application.isEditor) {
				OnPlayVibrationEffect?.Invoke (pattern);
			}
		}

		/// <summary>
		/// Stop vibrating phone. Only implemented on Android, iOS will continue vibrating the default time.
		/// </summary>
		public static void VibrateStop () {
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor) {
				AndroidVibrator.Call ("cancel");
			}
		}

		/// <summary>
		/// From https://android.googlesource.com/platform/frameworks/base/+/master/core/java/android/view/HapticFeedbackConstants.java
		/// </summary>
		public enum HapticFeedbackConstants {
			LONG_PRESS = 0,
			VIRTUAL_KEY = 1,
			KEYBOARD_TAP = 3,
			CLOCK_TICK = 4,
			CALENDAR_DATE = 5,
			CONTEXT_CLICK = 6,

			/// <summary>
			/// Available on Android 8 (2017)
			/// </summary>
			KEYBOARD_PRESS = KEYBOARD_TAP,
			/// <summary>
			/// Available on Android 8 (2017)
			/// </summary>
			KEYBOARD_RELEASE = 7,
			/// <summary>
			/// Available on Android 8 (2017)
			/// </summary>
			VIRTUAL_KEY_RELEASE = 8,
			/// <summary>
			/// Available on Android 8 (2017)
			/// </summary>
			TEXT_HANDLE_MOVE = 9,

			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			ENTRY_BUMP = 10,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			DRAG_CROSSING = 11,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			GESTURE_START = 12,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			GESTURE_END = 13,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			EDGE_SQUEEZE = 14,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			EDGE_RELEASE = 15,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			CONFIRM = 16,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			REJECT = 17,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			ROTARY_SCROLL_TICK = 18,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			ROTARY_SCROLL_ITEM_FOCUS = 19,
			/// <summary>
			/// Available on Android 11 (2020)
			/// </summary>
			ROTARY_SCROLL_LIMIT = 20,
		}

		/// <summary>
		/// Play a haptic feedback effect
		/// </summary>
		public static void PerformHapticFeedback (HapticFeedbackConstants feedbackConstant) {
			OnPlayHapticFeedback?.Invoke (feedbackConstant);
		}

		/// <summary>
		/// From https://android.googlesource.com/platform/frameworks/base.git/+/master/core/java/android/os/VibrationEffect.java#838
		/// </summary>
		public enum AndroidHapticPrimitive {

			/// <summary>
			/// This effect should produce a sharp, crisp click sensation. (API level 30)
			/// </summary>
			PRIMITIVE_CLICK = 1,

			/// <summary>
			/// A haptic effect that simulates downwards movement with gravity. Often followed by extra energy of hitting and reverberation to augment physicality. (API 31)
			/// </summary>
			PRIMITIVE_THUD = 2,

			/// <summary>
			/// A haptic effect that simulates spinning momentum. (API 31)
			/// </summary>
			PRIMITIVE_SPIN = 3,

			/// <summary>
			/// A haptic effect that simulates quick upward movement against gravity. (API 30)
			/// </summary>
			PRIMITIVE_QUICK_RISE = 4,

			/// <summary>
			/// A haptic effect that simulates slow upward movement against gravity. (API 30)
			/// </summary>
			PRIMITIVE_SLOW_RISE = 5,

			/// <summary>
			/// A haptic effect that simulates quick downwards movement with gravity. (API 30)
			/// </summary>
			PRIMITIVE_QUICK_FALL = 6,

			/// <summary>
			/// This very short effect should produce a light crisp sensation intended to be used repetitively for dynamic feedback. (API 30)
			/// </summary>
			PRIMITIVE_TICK = 7,

			/// <summary>
			/// This very short low frequency effect should produce a light crisp sensation intended to be used repetitively for dynamic feedback. (API 31)
			/// </summary>
			PRIMITIVE_LOW_TICK = 8,
		}

		static readonly string FALLBACK_HEADER = "#fallback:";
		static Dictionary<int, AndroidJavaObject> AndroidHapticCompositions = new Dictionary<int, AndroidJavaObject> ();

		public static AndroidJavaObject CreateAndroidHapticEffect (string pattern, string filename = "") {
			var hash = pattern.GetHashCode ();
			if (AndroidHapticCompositions.TryGetValue (hash, out AndroidJavaObject vibrationEffect)) {
				return vibrationEffect;
			}

			using var sr = new StringReader (pattern);
			string line = string.Empty;
			var lineNum = 0;

			if (Application.isEditor || AndroidAPILevel >= 30) {
				// Android 11 (API 30) haptic primitives
				// If no primitives are defined or there's an error parsing the data, use fallback waveform
				var composition = Application.isEditor ? null : AndroidVibrationEffect.CallStatic<AndroidJavaObject> ("startComposition");
				try {
					var numPrimitives = 0;
					while ((line = sr.ReadLine ()) != null && !line.Trim ().Equals (FALLBACK_HEADER, StringComparison.InvariantCultureIgnoreCase)) {
						var columns = line.Split (',');
						if (columns.Length == 3) {
							var primId = columns [0].Trim ();
							if (!primId.StartsWith ("PRIMITIVE_", StringComparison.InvariantCultureIgnoreCase)) {
								primId = "PRIMITIVE_" + primId;
							}
							var primitiveId = Enum.Parse<AndroidHapticPrimitive> (primId);
							if ((primitiveId == AndroidHapticPrimitive.PRIMITIVE_THUD || primitiveId == AndroidHapticPrimitive.PRIMITIVE_LOW_TICK || primitiveId == AndroidHapticPrimitive.PRIMITIVE_SPIN)
								&& (Application.isEditor || AndroidAPILevel == 30)) {
								Debug.LogWarning ($"{primitiveId} not supported on API 30, fallback on PRIMITIVE_TICK");
								primitiveId = AndroidHapticPrimitive.PRIMITIVE_TICK;
							}
							composition?.Call ("addPrimitive", (int)primitiveId, float.Parse (columns [1].Trim ()), int.Parse (columns [2].Trim ()));
							numPrimitives++;
						}
						lineNum++;
					}
					if (numPrimitives > 0) {
						vibrationEffect = composition?.Call<AndroidJavaObject> ("compose");
						if (vibrationEffect != null) {
							AndroidHapticCompositions [hash] = vibrationEffect;
						}
						composition?.Dispose ();
						return vibrationEffect;
					}
				} catch (Exception ex) {
					Debug.LogError ($"Exception while parsing Android haptic pattern {filename} line {lineNum}: {line} => {ex}");
				}
				composition?.Dispose ();
			}

			// Fallback: Android 8 (API 26) vibration waveforms
			try {
				List<long> timings = new List<long> (16);
				List<int> amplitudes = new List<int> (16);
				while ((line = sr.ReadLine ()) != null) {
					if (line.Trim ().Equals (FALLBACK_HEADER, StringComparison.InvariantCultureIgnoreCase))
						break;
					lineNum++;
				}
				while ((line = sr.ReadLine ()) != null) {
					var columns = line.Split (',');
					if (columns.Length == 2) {
						amplitudes.Add ((int)(float.Parse (columns [0].Trim ()) * 255f));
						timings.Add (long.Parse (columns [1].Trim ()));
					}
					lineNum++;
				}
				if (Application.isEditor)
					return null;
				vibrationEffect = AndroidVibrationEffect.CallStatic<AndroidJavaObject> ("createWaveform", timings.ToArray (), amplitudes.ToArray (), -1);
				AndroidHapticCompositions [hash] = vibrationEffect;
				return vibrationEffect;
			} catch (Exception ex) {
				Debug.LogError ($"Exception while parsing Android vibration waveform line {lineNum}: {line} => {ex}");
				return null;
			}
		}

		/// <summary>
		/// Determine if Android phone uses Google Play services or Huawei Mobile Services
		/// </summary>
		public static bool IsHuaweiHcm () {
			if (Application.platform != RuntimePlatform.Android) {
				return false;
			}
			var isHuaweiHcm = SystemInfo.deviceModel.StartsWith ("HUAWEI", StringComparison.InvariantCultureIgnoreCase);
			try {
				using (var apiAvailability = new AndroidJavaClass ("com.google.android.gms.common.GoogleApiAvailability"))
				using (var instance = apiAvailability.CallStatic<AndroidJavaObject> ("getInstance"))
				using (var player = new AndroidJavaClass ("com.unity3d.player.UnityPlayer"))
				using (var activity = player.GetStatic<AndroidJavaObject> ("currentActivity")) {
					var value = instance.Call<int> ("isGooglePlayServicesAvailable", activity);
					// result codes from https://developers.google.com/android/reference/com/google/android/gms/common/ConnectionResult
					// 0 == success
					// 1 == service_missing
					// 2 == update service required
					// 3 == service disabled
					// 18 == service updating
					// 9 == service invalid
					isHuaweiHcm = isHuaweiHcm && !(value == 0 || value == 2 || value == 18);
					Debug.Log ($"isGooglePlayServicesAvailable: {value} - IsHuaweiHcm: {isHuaweiHcm}");
				}
			} catch (Exception ex) {
				Debug.Log ($"{ex.GetType ()} while trying to determine if Google Play available -- IsHuaweiHcm: {isHuaweiHcm}");
			}
			return isHuaweiHcm;
		}

		public static void GetSdkVersionAndBuildTime (out Version version, out DateTime buildTime) {
			//Get build time from auto-generated assembly version
			version = Assembly.GetAssembly (typeof (Util)).GetName ().Version;
			buildTime = new DateTime (2000, 1, 1, 0, 0, 0 /*, DateTimeKind.Utc*/).Add (new TimeSpan (version.Build, 0, 0, version.Revision * 2));
		}

		/// <summary>
		/// Returns true if Developer Mode is activated
		/// </summary>
		public static bool IsDevModeActive {
			get {
				return PlayerPrefs.GetInt ("Developer", 0) == 1;
			}
		}

		static MethodInfo miFindObjectInstanceFromID;

		/// <summary>
		/// Find Object based on InstanceID
		/// </summary>
		public static UnityEngine.Object FindObjectFromInstanceID (int iid) {
			if (miFindObjectInstanceFromID == null) {
				miFindObjectInstanceFromID = typeof (UnityEngine.Object).GetMethod ("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static);
			}
			return (UnityEngine.Object)miFindObjectInstanceFromID.Invoke (null, new object [] { iid });
		}
	}

	public static class ComponentHelpers {
		/// <summary>
		/// Returns true if Component's gameobject or any of its parents are tagged as EditorOnly (not included in build)
		/// </summary>
		public static bool IsEditorOnly (this Component c) {
			if (c.CompareTag ("EditorOnly"))
				return true;
			var t = c.transform;
			while (t != null) {
				if (t.CompareTag ("EditorOnly"))
					return true;
				t = t.parent;
			}
			return false;
		}

		public static void EnsureActive (this Component c) {
			c.gameObject.EnsureActive ();
		}
	}

	public static class GameObjectHelpers {
		/// <summary>
		/// Ensure that the GameObject is active in the hierarchy by traversing upwards until root
		/// </summary>
		public static void EnsureActive (this GameObject g) {
			var t = g.transform;
			while (t != null) {
				t.gameObject.SetActive (true);
				t = t.parent;
			}
		}

		public static string GetScenePath (this GameObject obj, char separator = '/') {
			return Util.GetObjectScenePath (obj, separator);
		}
	}

	public static class StringHelpers {
		/// <summary>
		/// Cap string length and if capped, add a postfix (defaults to ellipsis character)
		/// </summary>
		public static string Truncate (this string s, int maxLength, string postfix = "…") {
			if (s.Length <= maxLength) return s;
			return s.Substring (0, maxLength) + postfix;
		}

		/// <summary>
		/// Checks if string is a valid email address
		/// </summary>
		public static bool IsEmailAddress (this string s) {
			if (s == null || string.IsNullOrWhiteSpace (s))
				return false;
			var v = s.Trim ().Split ('@');
			return v.Length == 2 && v [0].Length > 0 && v [1].IndexOf ('.') > 0 && v [1].LastIndexOf ('.') < v [1].Length - 1;
		}
	}

	[AttributeUsage (AttributeTargets.Class)]
	public class ComponentReferenceAttribute : Attribute {
		public string Text { get; set; }
		public ComponentReferenceAttribute (string text) {
			Text = text;
		}
	}

	[AttributeUsage (AttributeTargets.Field)]
	public class TagSelectorAttribute : PropertyAttribute {
		//public bool UseDefaultTagFieldDrawer = false;
	}

}
