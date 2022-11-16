using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Choice component, to reside inside a ChoicesComponent container.
	/// </summary>
	public class ChoiceComponent : MonoBehaviour, IGameComponentIcon {
		[Header ("Replicated choice (eg multiple choice test). Must have a ChoicesComponent as parent.")]

		public Text ChoiceText;
		public Image ChoiceImage;
		public Texture2D ChoiceTexture;
        public Button ChoiceButton;
		[Tooltip("If true then entire gameobject will be deactivated rather than just the button interaction")]
		public bool DeactivateGameobject = false;
		[Tooltip("If choice has an animator component, you can specify a deactivate state name")]
		public string DeactivateAnimStateName = "deactivate";
		[Tooltip("If choice has an animator component, you can specify a choosing state name")]
		public string ChoosingAnimStateName = "choose";
		[Tooltip("If choice has an animator component, you can specify a reset state name")]
		public string ResetAnimStateName = "reset";
		[Tooltip("If choice has an animator component, you can specify a success state name")]
		public string SuccessAnimStateName = "success";
		[Tooltip("If choice has an animator component, you can specify a fail state name")]
		public string FailAnimStateName = "fail";

		bool Chosen = false;
		bool SingleShot = true;
		bool ChoiceImageActive;

		void Start () {
			ChoiceImageActive = (ChoiceImage != null && ChoiceImage.enabled && ChoiceImage.gameObject.activeSelf);
		}

		/// <summary>
		/// Plays the specific animation if possible.
		/// </summary>
		/// <returns><c>true</c>, if animation was played, <c>false</c> otherwise.</returns>
		bool PlayAnimIfPossible (string stateName) {
			var anim = GetComponentInChildren<Animator> ();
			if (anim != null && !string.IsNullOrEmpty (stateName)) {
				anim.Play (stateName);
				return true;
			}
			return false;
		}

		/// <summary>
		/// If this choice was not made, deactivate it either using an animation, disabling the button or deactivating the gameobject
		/// </summary>
		void DeactivateNotChosen () {
			//We only want to deactivate the ones which were not chosen
			if (!Chosen && !PlayAnimIfPossible (DeactivateAnimStateName)) {
				if (DeactivateGameobject) {
					gameObject.SetActive (false);
				} else {
					ChoiceButton.interactable = false;
				}
			}
		}

		/// <summary>
		/// Reactivate the choice after a game round or a certain amount of time.
		/// </summary>
		void ReactivateNotChosen () {
			if (!Chosen) {
				if (DeactivateGameobject) {
					gameObject.SetActive (true);
				}
				ChoiceButton.interactable = true;
				ChoiceButton.enabled = true;
			}
		}

		public void SetChoiceText (string text) {
			if (ChoiceText != null) {
				ChoiceText.text = text;
			} else {
				Debug.LogError ("SetChoiceText called on ChoiceComponent with no Text property!");
			}
        }

		public void SetChoiceSprite (Sprite sprite) {
			if (ChoiceImage != null) {
				ChoiceImage.sprite = sprite;
			} else {
				Debug.LogError ("SetChoiceSprite called on ChoiceComponent with no Sprite property!");
			}
		}

		public void SetChoiceTexture (Texture2D texture) {
			if (ChoiceTexture != null) {
				ChoiceTexture = texture;
			} else {
				Debug.LogError ("SetChoiceTexture called on ChoiceComponent with no Texture property!");
			}
		}

		public void ResetChoice () {
			Chosen = false;
			ReactivateNotChosen ();

			//reset visibility state of ChoiceImage, if present
			if (!PlayAnimIfPossible (ResetAnimStateName) && ChoiceImage != null && !ChoiceImageActive) {
				ChoiceImage.enabled = false;
				ChoiceImage.gameObject.SetActive (false);
			}
		}

		public void SetSingleShot (bool enableSingleShot) {
			SingleShot = enableSingleShot;
		}

		public void AddButtonListener (UnityAction action, bool exclusive=true) {
			if (exclusive) {
				//Remove "old" listeners to avoid double-firing
				ChoiceButton.onClick.RemoveAllListeners ();
			}
			ChoiceButton.onClick.AddListener (delegate {
				ChooseThis ();
				action.Invoke ();
			});
        }

		public void ChooseThis () {
			if (!Chosen || !SingleShot) {
				Chosen = true;

				//Disable button component completely
				ChoiceButton.enabled = false;

				//Show choice image (eg other side of card)
				if (!PlayAnimIfPossible (ChoosingAnimStateName) && ChoiceImage != null) {
					//If no anim then just enable gameobject and image component
					ChoiceImage.enabled = true;
					ChoiceImage.gameObject.SetActive (true);
				}
			}
		}

		public void Success () {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} Choice.Success", gameObject.name);
			}
			//Play local animation if choice was correct
			PlayAnimIfPossible (SuccessAnimStateName);
		}

		public void Fail () {
			if (Debug.isDebugBuild) {
				Debug.LogFormat ("{0} Choice.Fail", gameObject.name);
			}
			//Play local animation if choice was wrong
			PlayAnimIfPossible (FailAnimStateName);
		}
    }

}