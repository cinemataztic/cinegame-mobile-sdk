using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.Linq;
using Smartfox;
#endif

namespace CineGame.MobileComponents {
	/// <summary>
	/// Proxy for wiring up buttons and events in canvases with static methods in singletons in the main level.
	/// Should be added to each canvas of each scene.
	/// </summary>
	public class GameProxy : MonoBehaviour {
		[Tooltip ("Mark if this game scene blocks the background completely. This way we can disable background effects and save battery.")]
		public bool BlocksBackground = true;

		[HideInInspector]
		public string GameType;

		[Tooltip ("Supporter prefab: containing at least name (Text) and profile image (Image)")]
		public Supporter SupporterPrefab;

		[Tooltip ("The container for spawned supporters")]
		public Transform SupporterContainer;

		[Tooltip ("All images which should receive the user avatar or profile image")]
		public Image [] ProfileImages;

		[Header ("TESTING IN EDITOR")]
		[Tooltip ("Only for testing in editor!")]
		public string GameServer = "sfs-fin-1.cinemataztic.com";
		[Tooltip ("Only for testing in editor!")]
		public string GameCode = "1234";
		[Tooltip ("Only for testing in editor!")]
		public string GameZone;

		public static Sprite ProfileImageSprite = null;
		private Sprite CurrentImageSprite = null;

		public delegate void SimpleEvent ();
		public delegate void GotoScreenEvent (string screenName);

		public static event SimpleEvent OpenMenuEvent;
		public static event SimpleEvent OpenWalletEvent;
		public static event SimpleEvent GoBackEvent;
		public static event GotoScreenEvent GotoEvent;
		public static event GotoScreenEvent ConfirmExitEvent;

#if UNITY_EDITOR
		CanvasGroup [] canvasGroups;
		readonly System.Collections.Generic.Dictionary<Util.APIRegion, string> marketSfsZones = new () {
			{ Util.APIRegion.DK, "Denmark" },
			{ Util.APIRegion.EN, "International" },
			{ Util.APIRegion.FI, "Finland" },
			{ Util.APIRegion.DE, "Germany" },
			{ Util.APIRegion.PT, "Portugal" },
			{ Util.APIRegion.SE, "Sweden" },
		};
		private void Start () {
			if (string.IsNullOrWhiteSpace (GameCode)) {
				InitScreens ();
			} else {
				if (SmartfoxClient.Instance == null) {
					Debug.Log ("GameProxy: Instantiating SmartfoxClient for testing");
					var go = new GameObject {
						name = "SmartfoxClient"
					};
					var sfc = SmartfoxClient.Instance = go.AddComponent<SmartfoxClient> ();
					sfc.InitEvents ();
				}

				var zone = GameZone;
				if (string.IsNullOrWhiteSpace (zone)) {
					var _regions = Util.Markets.Select (m => m.Key).ToArray ();
					var marketIndex = UnityEditor.EditorPrefs.GetInt ("AssetMarketIndex", 0);
					zone = marketSfsZones [_regions [marketIndex]];
				}

				SmartfoxClient.Connect (GameServer, GameZone, (success) => {
					if (success) {
						SmartfoxClient.Login (string.Empty, (error) => {
							if (string.IsNullOrEmpty (error)) {
								SmartfoxClient.JoinRoom (GameCode, false, (room, isRoomFull) => {
									if (room != null) {
										var roomGameType = room.GetVariable ("GameType").GetStringValue ();
										if (roomGameType != GameType) {
											Debug.LogError ($"{roomGameType} does not match GameProxy.GameType={GameType}");
										} else {
											Debug.Log ($"GameProxy: Game room {room.Name} joined succesfully");
										}
										SmartfoxClient.Send ("bkid", 0);
										SmartfoxClient.Send ("name", "UnityEditor");
										SmartfoxClient.Send ("age", 21);
										SmartfoxClient.Send ("gender", "N/A");
										SmartfoxClient.Send ("avatar", "ct_ghost");

										InitScreens ();
									} else {
										Debug.LogError ($"Could not join room {GameCode} - isRoomFull={isRoomFull}");
									}
								});
							} else {
								Debug.LogError ("Error while logging in to smartfox server: " + error);
							}
						});
					} else {
						Debug.LogError ("Could not connect to smartfox server");
					}
				});
			}
		}


		void InitScreens () {
			ReplicatedComponent.OnSceneLoaded (gameObject.scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

			canvasGroups = GetComponentsInChildren<CanvasGroup> (includeInactive: true);
			GotoEvent += (screenName) => {
				foreach (var cg in canvasGroups) {
					if (cg.transform.parent == transform) {
						cg.gameObject.SetActive (cg.name == screenName);
					}
				}
			};

			Goto ("Lobby");
		}
#endif

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
						img.sprite = sprite;
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