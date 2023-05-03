using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;
using System.Reflection;

namespace CineGame.MobileComponents {

	public static class Util {
		public const string CinematazticIconGUID = "5ea2613536f3d4926b87aa5246734341";

		[Serializable]
		public enum APIRegion {
			DK = 0,                                                 //BioSpil
			NO = 1,                                                 //KinoSpill
			EN = 2,                                                 //CineGame (International)
			FI = 3,                                                 //Leffapeli
			AU = 4,                                                 //CineGame AU (Australia)
			AE = 5,                                                 //CineGame AE (Arab Emirates)
			DE = 6,                                                 //RedyPlay (Germany)
			EE = 7,                                                 //ForumFun (Estonia)
			ES = 8,                                                 //CinesaFun (Spain)
			IE = 9,                                                 //CineGame IE (Ireland)
			IN = 10,                                                //CineGame IN (India)
			NZ = 11,                                                //CineGame NZ (New Zealand)
		};

		private static Uri [] regionBaseUris = {
			new Uri ("https://biospil.cinegamecore.drf-1.cinemataztic.com/api/"),		//DK
			new Uri ("https://kinospill.cinegamecore.drf-1.cinemataztic.com/api/"),		//NO
			new Uri ("https://cinegame.cinegamecore.eu-1.cinemataztic.com/api/"),		//EN
			new Uri ("https://leffapeli.cinegamecore.eu-1.cinemataztic.com/api/"),		//FI
			new Uri ("https://cinegame-au.cinegamecore.au-1.cinemataztic.com/api/"),	//AU
			new Uri ("https://cinegame-ae.cinegamecore.au-1.cinemataztic.com/api/"),	//AE
			new Uri ("https://redyplay.cinegamecore.eu-2.cinemataztic.com/api/"),		//DE
			new Uri ("https://forumfun.cinegamecore.eu-1.cinemataztic.com/api/"),		//EE
			new Uri ("https://cinesafun.cinegamecore.eu-2.cinemataztic.com/api/"),		//ES
			new Uri ("https://cinegame-ie.cinegamecore.eu-2.cinemataztic.com/api/"),	//IE
			new Uri ("https://cinegame-in.cinegamecore.asia-1.cinemataztic.com/api/"),	//IN
			new Uri ("https://cinegame-nz.cinegamecore.au-1.cinemataztic.com/api/"),	//NZ
			new Uri ("https://biospil.cinegamecore.drf-1.cinemataztic.com/api/"),		//Baltoppen
			new Uri ("https://cinegame.cinegamecore.eu-1.cinemataztic.com/api/"),		//DEMO CineGame
			new Uri ("https://cinegame.cinegamecore.eu-1.cinemataztic.com/api/"),		//cinemataztic-dev
		};

		public static string [] MarketIds = {
			"57ff5b54359bc3000f1e1303", //BioSpil
			"57e79e40bb29b2000f22c704", //KinoSpill
			"57e79e61bb29b2000f22c705", //CineGame (International)
			"5829676efd5ab2000f4eb252", //Leffapeli
			"5ba2a95eb81b02b3d8198f89", //CineGame AU
			"5c12f1c58c2a1a5509cad589", //CineGame AE
			"5c44f3ba8c2a1a5509df3f6b", //REDyPLAY
			"5ced2b5a8c2a1a5509b0116b", //FORUMFUN
			"5df786218c2a1a550974e19d", //CinesaFun
			"618301a5be9b8d3befa0b589", //CineGame IE
			"627049112c827460088db3fd", //CineGame IN
			"62a741d8709ea7ac02336c29", //CineGame NZ
			"58750bffb2928c000f2ff481", //Baltoppen
			"5b841697b81b02b3d8381244", //DEMO CineGame
			"594be135e9678d3bb75fe7aa", //cinemataztic-dev
		};

		private static readonly string [] regionDefaultLanguages = {
			"da", 													//DK
			"no", 													//NO
			"en",													//EN
			"fi",													//FI
			"en-AU",												//AU
			"en-US",                                                //AE
			"de",                                                 	//DE
			"et",                                                 	//EE
			"es",                                                 	//ES
			"en-IE",												//IE
			"en-IN",												//IN
			"en-NZ",                                                //NZ
		};

		public delegate void HapticEvent (HapticFeedbackConstants feedbackConstant);
		public delegate void VibrationEvent (TextAsset textAsset);
		/// <summary>
		/// App hooks up here so SDK can play haptics via PerformHapticFeedback
		/// </summary>
		public static event HapticEvent OnPlayHapticFeedback;
		/// <summary>
		/// App hooks up here so SDK can play custom vibration patterns
		/// </summary>
		public static event VibrationEvent OnPlayVibrationEffect;

		/// <summary>
		/// Determine API Region based on Application.identifier
		/// </summary>
		public static APIRegion GetRegion () {
			var appId = Application.identifier.Substring (Application.identifier.LastIndexOf ('.') + 1);
			switch (appId) {
			case "biospil":
				return APIRegion.DK;
			case "cinemagame":
				return APIRegion.EN;
			case "cinegameau":
				return APIRegion.AU;
			case "cinegameger":
			case "redyplay":
				return APIRegion.DE;
			case "cinegameuae":
				return APIRegion.AE;
			case "cinegameest":
				return APIRegion.EE;
			case "cinesaplay":
				return APIRegion.ES;
			case "kinospill":
				return APIRegion.NO;
			case "leffapeli":
				return APIRegion.FI;
			case "cinegameie":
				return APIRegion.IE;
			case "cinegamenz":
				return APIRegion.NZ;
			case "cinegamein":
				return APIRegion.IN;
			default:
				Debug.LogErrorFormat ("Unexpected application identifier {0}. App will NOT work!", Application.identifier);
				Debug.Break ();
				return APIRegion.DK;
			}
		}


		public static string GetRegionProfanityUrl () {
			return $"https://profanity.cinemataztic.com/{MarketIds [(int)GetRegion ()]}/txt-file";
		}


		/// <summary>
		/// Get API Base URI based on applicationIdentifier and whether app is in staging mode
		/// </summary>
		public static Uri GetRegionBaseUri (bool isStaging = false) {
			//return new Uri ("http://localhost:5000/api/");
			if (isStaging) {
				switch (GetRegion ()) {
				case APIRegion.EN:
					return new Uri ("https://cinegame.cinegamecore.staging.cinemataztic.com/api/");
				case APIRegion.FI:
					return new Uri ("https://leffapeli.cinegamecore.staging.cinemataztic.com/api/");
				}
			}
			return regionBaseUris [(int)GetRegion ()];
		}


		/// <summary>
		/// Get API Base URI based on Cloud marketId
		/// </summary>
		public static Uri GetRegionBaseUri (string marketId) {
			var marketIndex = Array.IndexOf (MarketIds, marketId);
			return regionBaseUris [marketIndex];
		}


		/// <summary>
		/// Get default language for region/market
		/// </summary>
		public static string GetRegionDefaultLanguage () {
			return regionDefaultLanguages [(int)GetRegion ()];
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

		/// <summary>
		/// Vibrate the phone (default time)
		/// </summary>
		public static void Vibrate () {
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor) {
				AndroidVibrator.Call ("vibrate");
			} else {
				Handheld.Vibrate ();
			}
		}

		/// <summary>
		/// Vibrate the phone for the specified amount of ms (only implemented on Android, iOS will vibrate default time)
		/// </summary>
		public static void Vibrate (long milliseconds) {
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
		/// Vibrate the phone in a specific pattern (long array of ms, even are vibrate, odd are pause). Only implemented on Android, iOS will vibrate once default time
		/// </summary>
		public static void Vibrate (TextAsset textAsset) {
			if (!Application.isEditor) {
				OnPlayVibrationEffect?.Invoke (textAsset);
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
		public static string Truncate (this string s, int maxLength, string postfix = "…") {
			if (s.Length <= maxLength) return s;
			return s.Substring (0, maxLength) + postfix;
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
