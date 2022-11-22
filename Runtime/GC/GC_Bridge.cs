using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;

namespace CineGame.MobileComponents.GC {

	public class GC_Bridge : MonoBehaviour, IGameComponentIcon {

		[Serializable]
		public struct GameSessionResponse {
			public long gameSessionId;
			public long gameDuration;
			public long pointsScored;
			public long prizeProgress;
			public long appGameId;
			public bool hasUserData;
			public long pointsFromGoal;
			public long wonCoinsAmount;
		}

		public enum GoToHighscoreEnum {
			none,
			Top,
			ThisWeek,
			Friends
		}

		public static GC_Bridge instance;

		/// <summary>
		/// Event to EnterGameCenter that user wants to quit game
		/// </summary>
		public static UnityEvent OnQuitGame;

		/// <summary>
		/// Event to GameCenterController that user wants to save game session
		/// </summary>
		public static UnityEvent<string, int, int> OnSaveGameSession;

		/// <summary>
		/// Event to GameCenterController that user has pressed "retry"
		/// </summary>
		public static UnityEvent OnRestartGame;

		// Events from GameCenterController
		public static Action<GameSessionResponse, long> GameOver;
		public static Action ServerPostFail;

		public GameObject ConnectionErrorGO;

		private int GameStartTimeStamp;
		bool hasSavedSessionData = false;

		//private long currentSessionId = -1;

		public GoToHighscoreEnum HighscoreScreen = GoToHighscoreEnum.Top;

		[Serializable]
		public class OnGameOverResponse : UnityEvent<GameSessionResponse, long> { }
		public OnGameOverResponse onGameOverResponse;

		[Serializable]
		public class OnServerError : UnityEvent { }
		public OnServerError onServerError;

		private void Awake () {
			instance = this;

			GameOver += OnGameOver;
			ServerPostFail += OnServerConnectionFail;
		}

		public static void OnGameSessionStart () {
			Debug.Log ("GC_Bridge OnGameSessionStart");
			instance.GameStartTimeStamp = (int)Time.time;

			instance.hasSavedSessionData = false;
		}

		public static void OnGameSessionEnd (int score) {
			if (instance.hasSavedSessionData)
				return;

			var playDuration = 0;
			if (instance.GameStartTimeStamp != 0) {
				playDuration = Mathf.Abs ((int)Time.time - instance.GameStartTimeStamp);
				Debug.Log ($"GC_Bridge OnGameSessionEnd playDuration = {playDuration} seconds");
			} else {
				Debug.LogError ("GC_Bridge OnGameSessionStart never called! Setting playDuration=0");
			}

			//Get the score for playsession.
			int gamesessionScore = score;

			//Get name of the active game
			Scene scene = SceneManager.GetActiveScene ();
			string sceneName = scene.name;

			OnSaveGameSession.Invoke (sceneName, playDuration, gamesessionScore);

			instance.hasSavedSessionData = true;
		}

		private void OnGameOver (GameSessionResponse gameOverData, long localBestScore) {
			onGameOverResponse.Invoke (gameOverData, localBestScore);
		}

		public void OnServerConnectionFail () {
			onServerError.Invoke ();
		}

		public void RestartGame () {
			OnRestartGame.Invoke ();
		}

		public void QuitGame () {
			OnQuitGame.Invoke ();
		}

		public static UnityEvent<GoToHighscoreEnum> OnViewHighscore;
		public static UnityEvent<Image, string> OnDownloadImage;
		public delegate string GetStringEvent (string gameName);
		public static GetStringEvent OnGetPrizeImageUrl;
		public static GetStringEvent OnGetPrizeInfoText;

		public void GoToHighscore () {
			OnViewHighscore.Invoke (HighscoreScreen);
		}

		public string GetPrizeImageUrl (string gameName) {
			return OnGetPrizeImageUrl.Invoke (gameName);
		}

		public string GetPrizeInfoText (string gameName) {
			return OnGetPrizeInfoText.Invoke (gameName);
		}

		public void DownloadImage (Image image, string url) {
			OnDownloadImage.Invoke (image, url);
		}

		/*
		bool hasPressedSendChallengeBefore = false;

		public void SendChallengeToFriends()
		{
			if (hasPressedSendChallengeBefore || FacebookController.LoggedIn)
			{
				return;
			}

			hasPressedSendChallengeBefore = true;

			Scene scene = SceneManager.GetActiveScene();
			gameCenterController.SendChallengeToFriend(currentSessionId, scene.name);
		}

		*/

	}
}