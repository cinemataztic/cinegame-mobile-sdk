using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CineGame.MobileComponents {
	/// <summary>
	/// The controller proxy for each AR Game. Loads and saves gamestate from/to PlayMaker FSMs and updates global var "DevicePoseStatus" with value from Vuforia
	/// </summary>
	public class ARGameProxy : BaseEventComponent {

		static ARGameProxy instance;

		public bool AutoSaveOnPause = true;

		[Tooltip ("Invoked when tracking starts. Tracking may still be limited")]
        public UnityEvent onTrackingStarted;
		[Tooltip ("Invoked when tracking is fully functional")]
        public UnityEvent onTrackingNormal;
		[Tooltip ("Invoked when saved game data has loaded (it may be empty though)")]
        public UnityEvent onGameDataLoaded;

		[Tooltip ("Invoked when tracking is limited for some reason")]
		public UnityEvent onRelocalizing;
		[Tooltip ("Invoked when player is moving too much for tracking to work")]
        public UnityEvent onExcessiveMotion;
		[Tooltip ("Invoked if environment is too blurry")]
		public UnityEvent onInsufficientFeatures;
		[Tooltip ("Invoked if lighting is insufficient")]
		public UnityEvent onInsufficientLight;

		bool gameDataLoaded;
		bool trackingStarted;

        public delegate void SimpleEvent ();

        public static event SimpleEvent SaveGameDataEvent;
        public static event SimpleEvent OpenWalletEvent;
        public static event SimpleEvent CloseGameEvent;
		public static event SimpleEvent GameOverEvent;
        public static event SimpleEvent GameStartEvent;

        void Awake () {
			if (instance != null) {
				Debug.LogError ("More than one ARGameProxy currently loaded!");
			}
			instance = this;
		}

		void Start () {
			SceneManager.SetActiveScene (gameObject.scene);
		}

		public static void OnRelocalizing () {
			instance.onRelocalizing.Invoke ();
		}

		public static void OnExcessiveMotion () {
			instance.onExcessiveMotion.Invoke ();
		}

        public static void OnInsufficientFeatures () {
            instance.onInsufficientFeatures.Invoke ();
        }

        public static void OnInsufficientLight () {
            instance.onInsufficientLight.Invoke ();
        }

        public static void OnTrackingStarted () {
            instance.trackingStarted = true;
            instance.onTrackingStarted.Invoke ();
        }

        public static void OnTrackingNormal () {
            instance.onTrackingNormal.Invoke ();
        }

        public static void OnGameLoaded () {
            instance.gameDataLoaded = true;
        }

#if UNITY_IOS && !UNITY_EDITOR
	void OnApplicationFocus (bool hasFocus) {
		bool pauseStatus = !hasFocus;
#else
        void OnApplicationPause (bool pauseStatus) {
#endif
			if (pauseStatus && AutoSaveOnPause) {
				Log ("ARGameProxy: Application paused, auto-saving gamestate ...");
				SaveGameDataEvent.Invoke ();
			}
		}

		void Update () {
#if UNITY_EDITOR
			trackingStarted = true;
#endif
			if (trackingStarted && gameDataLoaded) {
				gameDataLoaded = false;
				onGameDataLoaded.Invoke ();
				Log ("ARGameProxy: Game data loaded");
			}

#if UNITY_ANDROID || UNITY_EDITOR
			if (Input.GetKeyDown (KeyCode.Escape)) {
				CloseGame ();
			}
#endif
		}

		public void SaveGame () {
			SaveGameDataEvent.Invoke ();
		}

		/// <summary>
		/// Called when actual gameplay starts
		/// </summary>
		public void StartGame () {
			GameStartEvent.Invoke ();
		}

		/// <summary>
		/// Called when user clicks Close Game button or Back nav
		/// </summary>
		public void CloseGame () {
			CloseGameEvent.Invoke ();
		}

		/// <summary>
		/// Called when game is over and we return to app
		/// </summary>
		public void GameOver () {
			GameOverEvent.Invoke ();
		}

		/// <summary>
		/// Called if player has won a voucher and wants to see it in his wallet
		/// </summary>
		public void OpenWallet () {
			OpenWalletEvent.Invoke ();
		}

		public static void RegisterOnGameLoaded (UnityAction a) {
			instance.onGameDataLoaded.AddListener (a);
		}
	}
}
