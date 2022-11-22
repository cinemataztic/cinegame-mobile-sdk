using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;
using System.Linq;

namespace CineGame.MobileComponents.GC {

    public class GC_GameManager : MonoBehaviour {

        public static GC_GameManager instance;

        /// <summary>
		/// Event to CoinController when GC Game wants to reward player with coins
		/// </summary>
        public static UnityEvent<int> OnRewardCoins;

        public Util.APIRegion Market;

        [Header ("Version Info")]
        public Text VersionText;
        public float Version;

        [Header ("UI GameObject")]
        // Header
        public GameObject Header;

        // Start Screen
        public GameObject StartScreen;

        // End Screen
        public GameObject GameOverScreen;
        public GameObject UploadingInfo;
        public GameObject NetworkErrorObject;
        public GameObject PlayAgainButton;
        public GameObject PlayerChallengeButton;

        // Prize screen
        public Image PrizeImage;

        // Prize Overlay
        public GameObject PrizeOverlay;

        [Header ("UI Text")]
        public Text NetworkErrorText;
        public Text UploadingText;

        public Text YouGotText;
        public Text ScoreText;
        public Text PointsText;
        public Text ScreenText;
        public Text PlayAgainText;
        public Text ChallangeText;

        public Text CongratulationsText;
        public Text PrizeInformationText;
        public Text MoreInfoText;


        [Header ("Static Strings")]
        // System
        public string NetworkError;
        public string Uploading;

        // Prize Overlay
        public string Congratulations;
        public string PrizeInformation;
        public string MoreInfo;

        // End Screen
        public string YouGot;
        public string Points;
        public string PlayAgain;
        public string Challenge;

        [Header ("Dynamic Strings")]
        public string HowManyPointsFromPrize;
        public string YouWonPrize;
        public string HowFarFromHighscore;
        public string YouHaveBeatenHighscore;

        private Dictionary<string, string> TextContainer;

        [Serializable]
        public class OnStartGame : UnityEvent { }
        [Space (10)]
        public OnStartGame onStartGame;

        [Serializable]
        public class OnEndGame : UnityEvent { }

        public OnEndGame onEndGame;

        void Awake () {
            instance = this;
        }

        void Start () {
            try {
                string sceneName = this.gameObject.scene.name;

                //Set prize info text with string from server if any was sent.
                if (PrizeInformationText != null) {
                    string newPrizeInfo = GC_Bridge.instance.GetPrizeInfoText (sceneName);
                    if (!string.IsNullOrEmpty (newPrizeInfo)) {
                        PrizeInformation = newPrizeInfo;
                    }
                }

                //Download and set prize image if url sent from server.
                if (PrizeImage != null) {
                    string imageUrl = GC_Bridge.instance.GetPrizeImageUrl (sceneName);
                    if (!string.IsNullOrEmpty (imageUrl)) {
                        GC_Bridge.instance.DownloadImage (PrizeImage, imageUrl);
                    }
                }
            } catch (Exception ex) {
                Debug.LogWarning (ex);
            }

            if (gameObject != null && gameObject.scene.isLoaded) {
                SceneManager.SetActiveScene (gameObject.scene);
            } else {
                return;
            }

            SetTextInScene ();

            GC_Bridge.instance.onGameOverResponse.AddListener (PlayerInformation);
            GC_Bridge.instance.onServerError.AddListener (ServerError);

            Header.SetActive (true);
            StartScreen.SetActive (true);
            GameOverScreen.SetActive (false);
            PrizeOverlay.SetActive (false);

            if (Util.IsDevModeActive) {
                VersionText.text = Util.GetRegion ().ToString () + ": A." + Application.version + " - V." + Version + " - U." + Application.unityVersion;
            }
        }

        public void SetTextInScene () {
            TextContainer = new Dictionary<string, string> { {"NetworkError", NetworkError }, {"Uploading", Uploading }, {"YouGot", YouGot }, {"Points", Points }, { "HowManyPointsFromPrize", HowManyPointsFromPrize },
            { "YouWonPrize", YouWonPrize }, { "HowFarFromHighscore", HowFarFromHighscore }, { "YouHaveBeatenHighscore", YouHaveBeatenHighscore },
            { "PlayAgain", PlayAgain }, { "Challenge", Challenge }, { "Congratulations", Congratulations },
            { "PrizeInformation", PrizeInformation }, { "MoreInfo",MoreInfo } };

            var en = TextContainer.Keys.ToList ();

            foreach (var key in en) {
                TextContainer [key] = InsertLinebreaks (TextContainer [key]);
            }

            NetworkErrorText.text = TextContainer ["NetworkError"];
            UploadingText.text = TextContainer ["Uploading"];
            YouGotText.text = TextContainer ["YouGot"];
            PointsText.text = TextContainer ["Points"];
            PlayAgainText.text = TextContainer ["PlayAgain"];
            ChallangeText.text = TextContainer ["Challenge"];
            CongratulationsText.text = TextContainer ["Congratulations"];
            PrizeInformationText.text = TextContainer ["PrizeInformation"];
            MoreInfoText.text = TextContainer ["MoreInfo"];

            ScoreText.text = "";
            ScreenText.text = "";
        }

        public static OnStartGame GetOnStartGame () {
            return instance.onStartGame;
        }

        public static OnEndGame GetOnEndGame () {
            return instance.onEndGame;
        }

        private string InsertLinebreaks (string text) {
            return text.Replace ("<br>", "\n");
        }

        public void StartGame () {
            Header.SetActive (false);
            StartScreen.SetActive (false);

            GC_Bridge.OnGameSessionStart ();

            if (onStartGame != null) {
                onStartGame.Invoke ();
            }
        }

        public static void EndGame (int score) {
            instance.ScoreText.text = score.ToString ();

            instance.UploadingInfo.SetActive (true);
            instance.Header.SetActive (true);
            instance.GameOverScreen.SetActive (true);

            GC_Bridge.OnGameSessionEnd (score);

            if (instance.onEndGame != null) {
                instance.onEndGame.Invoke ();
            }

        }

        void PlayerInformation (GC_Bridge.GameSessionResponse data, long bestScore) {
            UploadingInfo.SetActive (false);

            if (data.pointsFromGoal > 0) {
                ScreenText.text = string.Format (TextContainer ["HowManyPointsFromPrize"], data.pointsFromGoal);
            } else if (data.pointsFromGoal == 0) {
                ScreenText.text = string.Format (TextContainer ["YouWonPrize"]);
            } else if (data.pointsScored == bestScore) {
                ScreenText.text = string.Format (TextContainer ["YouHaveBeatenHighscore"]);
            } else {
                ScreenText.text = string.Format (TextContainer ["HowFarFromHighscore"], bestScore - data.pointsScored);
            }

            if (data.prizeProgress == 100) {
                PrizeOverlay.SetActive (true);
            }

            if (data.wonCoinsAmount > 0) {
                OnRewardCoins.Invoke ((int)data.wonCoinsAmount);
            }


            PlayAgainButton.SetActive (true);
            ScreenText.gameObject.SetActive (true);

        }

        public void ClosePrizeOverlay () {
            PrizeOverlay.SetActive (false);
        }

        public void Highscore () {
            GC_Bridge.instance.GoToHighscore ();
        }

        void ServerError () {
            NetworkErrorObject.SetActive (true);
            PlayAgainButton.SetActive (true);
            UploadingInfo.SetActive (false);
        }
    }
}