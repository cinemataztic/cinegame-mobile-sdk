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
using System.Collections;
using System.Net;

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
			PT = 13,//adline-pt
		};

		public class Market {
			public APIRegion Region;
			public string MarketID;
			public string Network;
			public string Country;
			public string Cluster;
			public string DefaultLocale;
			public string AppleTeamID;
			public string GoogleProjectID;
			public string iOSBundleID;
			public string AndroidBundleID;
		}

		public static readonly Dictionary<APIRegion, Market> Markets = new () {
			{
				APIRegion.DK,
				new Market {
					Region = APIRegion.DK,
					MarketID = "57ff5b54359bc3000f1e1303",
					Network = "drf", Country = "dk",
					Cluster = "eu-1",
					DefaultLocale = "da",
					GoogleProjectID = "610570399119",
					AppleTeamID = "U48582W842",
					iOSBundleID = "com.oxmond.biospil",
					AndroidBundleID = "air.com.oxmond.biospil",
				}
			},
			{
				APIRegion.NO,
				new Market {
					Region = APIRegion.NO,
					MarketID = "57e79e40bb29b2000f22c704",
					Network = "mdn", Country = "no",
					Cluster = "eu-1",
					DefaultLocale = "no",
					GoogleProjectID = "643744015712",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.cinemataztic.kinospill",
					AndroidBundleID = "com.cinemataztic.kinospill",
				}
			},
			{
				APIRegion.EN,
				new Market {
					Region = APIRegion.EN,
					MarketID = "57e79e61bb29b2000f22c705",
					Network = "cinemataztic", Country = "en",
					Cluster = "eu-1",
					DefaultLocale = "en",
					GoogleProjectID = "94261933586",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.cinemataztic.cinemagame",
					AndroidBundleID = "com.cinemataztic.cinemagame",
				}
			},
			{
				APIRegion.FI,
				new Market {
					Region = APIRegion.FI,
					MarketID = "5829676efd5ab2000f4eb252",
					Network = "finnkino", Country = "fi",
					Cluster = "eu-1",
					DefaultLocale = "fi",
					GoogleProjectID = "964262829088",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.cinemataztic.leffapeli",
					AndroidBundleID = "com.cinemataztic.leffapeli",
				}
			},
			{
				APIRegion.AU,
				new Market {
					Region = APIRegion.AU,
					MarketID = "5ba2a95eb81b02b3d8198f89",
					Network = "valmorgan", Country = "au",
					Cluster = "au-1",
					DefaultLocale = "en-AU",
					GoogleProjectID = "675637400113",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.cinemataztic.cinegameau",
					AndroidBundleID = "com.cinegameau.cinegameau",
				}
			},
			{
				APIRegion.DE,
				new Market {
					Region = APIRegion.DE,
					MarketID = "5c44f3ba8c2a1a5509df3f6b",
					Network = "weischer", Country = "de",
					Cluster = "eu-2",
					DefaultLocale = "de",
					GoogleProjectID = "582638132572",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.cinemataztic.cinegameger",
					AndroidBundleID = "com.redyplay.redyplay",
				}
			},
			{
				APIRegion.IE,
				new Market {
					Region = APIRegion.IE,
					MarketID = "618301a5be9b8d3befa0b589",
					Network = "wideeyemedia", Country = "ie",
					Cluster = "eu-2",
					DefaultLocale = "en-IE",
					GoogleProjectID = "87709878890",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.wideeyemedia.cinegameie",
					AndroidBundleID = "com.wideeyemedia.cinegameie",
				}
			},
			{
				APIRegion.NZ,
				new Market {
					Region = APIRegion.NZ,
					MarketID = "62a741d8709ea7ac02336c29",
					Network = "valmorgan", Country = "nz",
					Cluster = "au-1",
					DefaultLocale = "en-NZ",
					GoogleProjectID = "96803814888",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.valmorgan.cinegamenz",
					AndroidBundleID = "com.valmorgan.cinegamenz",
				}
			},
			{
				APIRegion.SE,
				new Market {
					Region = APIRegion.SE,
					MarketID = "653676850c50fc8ecda86b43",
					Network = "filmstaden", Country = "se",
					Cluster = "eu-1",
					DefaultLocale = "sv",
					GoogleProjectID = "717367223763",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.se.filmstaden.cinegame",
					AndroidBundleID = "com.se.filmstaden.cinegamese",
				}
			},
			{
				APIRegion.PT,
				new Market {
					Region = APIRegion.PT,
					MarketID = "6642106e9f745c39d99a95e7",
					Network = "adline", Country = "pt",
					Cluster = "eu-2",
					DefaultLocale = "pt",
					GoogleProjectID = "150212816952",
					AppleTeamID = "6TCLK4NZ92",
					iOSBundleID = "com.pt.adline.cinegame",
					AndroidBundleID = "com.pt.adline.cinegame",
				}
			},
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

		public static readonly CustomStringFormatter CustomStringFormat = new ();

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
			case "com.pt.adline.cinegame":
				return APIRegion.PT;
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
		/// Crops texture to specified UV rect. If the texture is non-readable, blit to temp RenderTexture and copy from there
		/// </summary>
		public static void CropTexture (Texture2D texture, Rect uvRect, bool mipChain, bool readable, out Texture2D croppedTexture) {
			var x = (int)(texture.width * uvRect.xMin);
			var y = (int)(texture.height * uvRect.yMin);
			var w = (int)(texture.width * uvRect.size.x);
			var h = (int)(texture.height * uvRect.size.y);
			croppedTexture = new Texture2D (w, h, texture.format, mipChain);
			if (texture.isReadable) {
				var pixels = texture.GetPixels (x, y, w, h, 0);
				croppedTexture.SetPixels (pixels);
				croppedTexture.Apply (mipChain, !readable);
			} else {
				// Create a temporary RenderTexture of the same size as the texture
				var tmpRenderTexture = RenderTexture.GetTemporary (texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
				Graphics.Blit (texture, tmpRenderTexture);
				var prevRenderTexture = RenderTexture.active;
				RenderTexture.active = tmpRenderTexture;

				// Copy the pixels to the cropped texture
				croppedTexture.ReadPixels (new Rect (x, y, w, h), 0, 0);
				croppedTexture.Apply (mipChain, !readable);

				//release RenderTexture
				RenderTexture.active = prevRenderTexture;
				RenderTexture.ReleaseTemporary (tmpRenderTexture);
			}
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
		/// Play a haptic feedback effect (parse string)
		/// </summary>
		public static void PerformHapticFeedback (string feedbackConstant) {
			if (!Enum.TryParse (feedbackConstant, out HapticFeedbackConstants fc)) {
				fc = HapticFeedbackConstants.VIRTUAL_KEY;
			}
			OnPlayHapticFeedback?.Invoke (fc);
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

		static readonly char [] AndroidHapticTrimChars = new char [] { ' ', ',' };
		static readonly Dictionary<int, AndroidJavaObject> AndroidHapticCompositions = new Dictionary<int, AndroidJavaObject> ();

		/// <summary>
		/// Parse a JSON file of Android timings/amplitudes generated by HapticSync app
		/// https://apps.apple.com/dk/app/hapticsync-vibration-studio/id6743813963
		/// </summary>
		public static AndroidJavaObject CreateAndroidHapticEffect (string pattern, string filename = "") {
			var hash = pattern.GetHashCode ();
			if (AndroidHapticCompositions.TryGetValue (hash, out AndroidJavaObject vibrationEffect)) {
				return vibrationEffect;
			}

			using var sr = new StringReader (pattern);
			string line = string.Empty;
			var lineNum = 0;

			try {
				List<long> timings = new List<long> (16);
				List<int> amplitudes = new List<int> (16);
				while ((line = sr.ReadLine ()) != null) {
					lineNum++;
					if (line.Trim () == "\"timings\" : [")
						break;
				}
				while ((line = sr.ReadLine ()) != null) {
					lineNum++;
					line = line.Trim (AndroidHapticTrimChars);
					if (line == "]")
						break;
					timings.Add (long.Parse (line));
				}
				while ((line = sr.ReadLine ()) != null) {
					lineNum++;
					if (line.Trim () == "\"amplitudes\" : [")
						break;
				}
				while ((line = sr.ReadLine ()) != null) {
					lineNum++;
					line = line.Trim (AndroidHapticTrimChars);
					if (line == "]")
						break;
					amplitudes.Add (int.Parse (line));
				}
				if (Application.isEditor)
					return null;
				vibrationEffect = AndroidVibrationEffect.CallStatic<AndroidJavaObject> ("createWaveform", timings.ToArray (), amplitudes.ToArray (), -1);
				AndroidHapticCompositions [hash] = vibrationEffect;
				return vibrationEffect;
			} catch (Exception ex) {
				Debug.LogError ($"Exception while parsing Android haptic JSON file {filename} line {lineNum}: {line} => {ex}");
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

		public static IEnumerator E_LoadTexture (string url, Action<Texture2D> callback) {
			var statusCode = HttpStatusCode.OK;
			using var request = LoadTextureFromCacheOrUrl (url);
			yield return request.SendWebRequest ();
			Texture2D response = null;
			if (request.result == UnityWebRequest.Result.ConnectionError) {
				statusCode = HttpStatusCode.ServiceUnavailable;
				Debug.LogWarningFormat ("Network error while loading texture from {0} : {1}", request.url, request.error);
			} else if (request.result != UnityWebRequest.Result.Success) {
				statusCode = (HttpStatusCode)request.responseCode;
				Debug.LogErrorFormat ("Error happened while loading texture from {0} : {1}", request.url, request.error);
			} else {
				//cache image if not already cached
				StoreTextureInCache (request);
				response = DownloadHandlerTexture.GetContent (request);
			}
			callback?.Invoke (response);
		}

		/// <summary>
		/// Generate a unique temp filename based on the original download URL
		/// </summary>
		public static string GetCacheFileName (string url) {
			return string.Format ("{0}/{1}", Application.temporaryCachePath, Util.ComputeMD5Hash (url));
		}

		/// <summary>
		/// Get a UnityWebRequest to load a texture either from cache or URL
		/// </summary>
		public static UnityWebRequest LoadTextureFromCacheOrUrl (string url) {
			var filename = GetCacheFileName (url);
			if (File.Exists (filename)) {
				//We can expire the cached version based on timestamp
				//var lastWriteTimeUtc = System.IO.File.GetLastWriteTimeUtc (filename);
				//var nowUtc = DateTime.UtcNow;
				//var totalHours = (int)nowUtc.Subtract (lastWriteTimeUtc).TotalHours;
				//if (totalHours < 7*24) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				//Debug.LogFormat ("CACHE LOAD {0} => {1}", url, filename);
#endif
				return UnityWebRequestTexture.GetTexture ("file://" + filename, true);
			}
			var webRequest = UnityWebRequestTexture.GetTexture (url, true);
			webRequest.timeout = 10;
			return webRequest;
		}

		/// <summary>
		/// Store a downloaded binary as a temp file (name based on download url)
		/// </summary>
		public static void StoreTextureInCache (UnityWebRequest w) {
			try {
				if (!w.url.StartsWith ("file://") && w.result == UnityWebRequest.Result.Success) {
					var filename = GetCacheFileName (w.url);
					System.IO.File.WriteAllBytes (filename, w.downloadHandler.data);
				}
			} catch (Exception ex) {
				Debug.LogWarning ($"{ex.GetType ()} happened while storing texture in cache: {ex}");
			}
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

	/// <summary>
	/// Custom string formatting. U is uppercase, L is lowercase, Txx trims to max xx characters with ellipse character if trimmed
	/// </summary>
	public class CustomStringFormatter : IFormatProvider, ICustomFormatter {
		public object GetFormat (Type formatType) {
			if (formatType == typeof (ICustomFormatter))
				return this;
			else
				return null;
		}

		public string Format (string format, object arg, IFormatProvider formatProvider) {
			switch (format) {
			case "U":
				return arg.ToString ().ToUpper ();
			case "L":
				return arg.ToString ().ToLower ();
			default:
				if (format != null && format.Length > 1 && format [0] == 'T') {
					var trimLen = int.Parse (format [1..]);
					var str = arg.ToString ();
					if (str.Length > trimLen) {
						str = arg.ToString ().Substring (0, trimLen) + "…";
					}
					return str;
				}
				return HandleOtherFormats (format, arg, formatProvider);
			}
		}

		private string HandleOtherFormats (string format, object arg, IFormatProvider formatProvider) {
			if (format != null && arg is IFormattable formattable)
				return formattable.ToString (format, formatProvider);
			else if (arg != null)
				return arg.ToString ();

			return string.Empty;
		}
	}
}
