using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
	/// <summary>
	/// Proxy for wiring up buttons and events in canvases with static methods in singletons in the main level.
	/// Should be added to each canvas of each scene.
	/// </summary>
	public class GameProxy : MonoBehaviour {
		[Header ("Proxy for games to contact the app functions, eg Exit Game, Facebook Share, Show Prizes etc")]

		[Tooltip ("Mark if this canvas blocks the background completely. This way we can disable background effects and save battery.")]
		public bool BlocksBackground = true;

		[HideInInspector]
		public string GameType;

		[Header ("Support-A-Friend")]
		[Tooltip ("A prefab containing at least name (Text) and profile image (Image)")]
		public Supporter SupporterPrefab;

		[Tooltip ("The container for spawned supporters")]
		public Transform SupporterContainer;

		[Header ("Normal profile images")]
		[Tooltip ("All images which should receive the facebook profile image OR user-chosen avatar")]
		public Image [] ProfileImages;

		public static Sprite ProfileImageSprite = null;
		private Sprite CurrentImageSprite = null;

		public delegate void SimpleEvent ();
		public delegate void GotoScreenEvent (string screenName);

		public static event SimpleEvent OpenMenuEvent;
		public static event SimpleEvent OpenWalletEvent;
		public static event SimpleEvent GoBackEvent;
		public static event SimpleEvent FacebookShareEvent;
		public static event GotoScreenEvent GotoEvent;
		public static event GotoScreenEvent ConfirmExitEvent;

		void OnDestroy () {
			Debug.LogFormat ("{0} GameProxy.OnDestroy()", Util.GetObjectScenePath (gameObject));
			Screen.orientation = ScreenOrientation.Portrait;
		}

		void Update () {
			UpdateProfileImages ();
		}

		void OnApplicationFocus (bool focus) {
			OnApplicationPause (!focus);
		}

		void OnApplicationPause (bool pauseStatus) {
			if (!pauseStatus) {
				var animators = GetComponentsInChildren<Animator> (false);
				if (animators != null && animators.Length > 0) {
					foreach (var animator in animators) {
						//restart animation
						animator.gameObject.SetActive (false);
						animator.gameObject.SetActive (true);
					}
				}
			}
		}

		public void OpenMenu () {
			OpenMenuEvent.Invoke ();
		}

		public void GoBack () {
			GoBackEvent.Invoke ();
		}

		public void Goto (string name) {
			GotoEvent.Invoke (name);
		}

		public void OpenWallet () {
			OpenWalletEvent.Invoke ();
		}

		public void ExitGameWithPopup (string message) {
			ConfirmExitEvent.Invoke (message);
		}

		/// <summary>
		/// Invokable method which opens a facebook share dialog with the preset parameters in FacebookController (usually set up from the host via SmartfoxClient)
		/// </summary>
		public void FacebookShare () {
			FacebookShareEvent.Invoke ();
			//FacebookController.Share ();
		}

		public void DestroyGameObject (GameObject go) {
			if (go != null) {
				if (Debug.isDebugBuild || Util.IsDevModeActive) {
					Debug.LogFormat ("GameProxy.DestroyGameObject {0}", Util.GetObjectScenePath (go));
				}
				Destroy (go);
			}
		}

		/// <summary>
		/// If ProfileImageSprite property has changed, propagate the new sprite in all due image references (property ProfileImages)
		/// </summary>
		void UpdateProfileImages () {
			var sprite = ProfileImageSprite;
			if (sprite != CurrentImageSprite) {
				CurrentImageSprite = sprite;
				foreach (var img in ProfileImages) {
					if (img != null) {
						Debug.Log ($"GameProxy: Update profile image sprite {Util.GetObjectScenePath (img.gameObject)} ...");
						img.overrideSprite = sprite;
					} else {
						Debug.LogWarning ($"Image reference in GameProxy is null: {Util.GetObjectScenePath (gameObject)}");
					}
				}
			}
		}

		/// <summary>
		/// Invokable method to force LandscapeLeft screen orientation
		/// </summary>
		public void SetLandscapeLeftOrientation () {
			Screen.orientation = ScreenOrientation.LandscapeLeft;
		}

		/// <summary>
		/// Invokable method to force LandscapeRight screen orientation
		/// </summary>
		public void SetLandscapeRightOrientation () {
			Screen.orientation = ScreenOrientation.LandscapeRight;
		}

		/// <summary>
		/// Invokable method to force Portrait screen orientation (reset)
		/// </summary>
		public void SetPortraitOrientation () {
			Screen.orientation = ScreenOrientation.Portrait;
		}
	}
}