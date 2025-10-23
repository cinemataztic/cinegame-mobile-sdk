using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	[ComponentReference ("Component for setting up a multiple choice array or grid. For example a memory card game where the player has to remember the position of a card.")]
	public class ChoicesComponent : ReplicatedComponent {

        [Header("Choice prefab (eg quiz button)")]
        [Tooltip("The choice prefab, eg a button. If left empty, existing instances will be used")]
        public ChoiceComponent ChoicePrefab = null;

		[Header("Correct Answer Indices. Leave empty if no validation required. Can be replicated from host.")]
		public int[] CorrectIndices;

		[Header("Sprite options. If host sends index options and sprites are applicable, these will be applied to each choice.")]
		public Sprite[] Sprites;

		[Header("Texture options. If host sends index options and textures are applicable, these will be applied to each choice.")]
		public Texture2D[] Textures;

		[Tooltip("If set, all other options than the one chosen will be deactivated when player makes a choice.")]
		public bool DeactivateOthersOnChoice = false;

		[Tooltip("If false then each choice can invoke multiple times without being reset")]
		public bool ChoicesAreSingleShot = true;

		[Tooltip("If true then user must make choices in the correct order")]
		public bool OrderMatters = false;

		[Tooltip("If true then user is allowed to make a choice")]
		public bool PlayerCanMakeChoice = false;

		[Tooltip("Delay between each card reset if all cards are reset at once")]
		public float ResetDelay = 0.1f;

		[Header("Replication")]
        [Tooltip("Key in the ObjectMessage from host with texts or sprite indices")]
        public string ArrayKey = "options";
		[Tooltip("Key in the ObjectMessage from host with the correct choices")]
		public string CorrectIndicesKey = "correctIndices";
		[Tooltip("Key in the ObjectMessage from host - int index to choose automatically")]
		public string ChooseKey = "choose";
		[Tooltip("Key in the ObjectMessage from host - int index to reset, or -1 to reset all")]
		public string ResetKey = "reset";
        [Tooltip("Key in ObjectMessage TO host when choosing an option - int index. Leave empty if not required")]
        public string ChoiceKey = "answer";
		[Tooltip("Key in ObjectMessage TO host - bool result true=success, false=fail")]
		public string SuccessFailKey = "success";

		public UnityEvent onFail;
        public UnityEvent onSuccess;

		private List<int> CorrectIndicesStack = new List<int> (10);

		private ChoiceComponent[] staticChoices;
		private ChoiceComponent[] StaticChoices {
			get {
				if (staticChoices == null) {
					staticChoices = GetComponentsInChildren<ChoiceComponent> (true);
				}
				return staticChoices;
			}
		}

        void Start () {
            InitializeChoices ();
			InitializeCorrectIndicesStack ();
        }

		ChoiceComponent GetOrCreateChoice (int index) {
			if (ChoicePrefab != null) {
				var choice = Instantiate <ChoiceComponent> (ChoicePrefab, transform);
				choice.name = string.Format ("Choice_{0}", index);
				choice.transform.localScale = Vector3.one;
				return choice;
			}
			//Reuse static choice object
			return StaticChoices [index];
		}

		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
            if (dataObj.ContainsKey (ArrayKey)) {
				if (ChoicePrefab != null) {
					//Nuke old choices before instantiating new prefabs
					foreach (Transform t in this.transform) {
						Destroy (t.gameObject);
					}
				}

				int i = 0;
				if (Sprites.Length == 0 && Textures.Length == 0) {
					//We have no sprite array, so we expect the options to be text strings
					var texts = dataObj.GetUtfStringArray (ArrayKey);
					foreach (var text in texts) {
						GetOrCreateChoice (i).SetChoiceText (text);
						i++;
					}
				} else if (Sprites.Length > 0) {
					//We have a sprite array, so we expect the options to be sprite indices
					var indices = dataObj.GetIntArray (ArrayKey);
					foreach (var spriteIndex in indices) {
						GetOrCreateChoice (i).SetChoiceSprite (Sprites [spriteIndex]);
						i++;
					}
				} else {
					//We have a textures array, so we expect the options to be texture indices
					var indices = dataObj.GetIntArray (ArrayKey);
					foreach (var textureIndex in indices) {
						GetOrCreateChoice (i).SetChoiceTexture (Textures [textureIndex]);
						i++;
					}
				}

				Invoke ("InitializeChoices", 0f);
            }

			if (dataObj.ContainsKey (CorrectIndicesKey)) {
				CorrectIndices = dataObj.GetIntArray (CorrectIndicesKey);
				Log (string.Format ("Received Correct Indices: {0}", string.Join (",", new List<int> (CorrectIndices).ConvertAll (i => i.ToString ()).ToArray ())));
				InitializeCorrectIndicesStack ();
				if (DeactivateOthersOnChoice) {
					//Reactivate remaining choices, if previously deactivated
					BroadcastMessage ("ReactivateNotChosen", SendMessageOptions.DontRequireReceiver);
				}
			}

			if (dataObj.ContainsKey (ChooseKey)) {
				var chosenIndex = dataObj.GetInt (ChooseKey);
				int i=0;
				foreach (Transform t in transform) {
					var choices = t.GetComponentsInChildren<ChoiceComponent> (true);
					foreach (var choice in choices) {
						if (chosenIndex == -1 || chosenIndex == i) {
							choice.ChooseThis ();
						}
						i++;
					}
				}
			}

			if (dataObj.ContainsKey (ResetKey)) {
				var chosenIndex = dataObj.GetInt (ResetKey);
				int i=0;
				float invokeTime = 0f;
				foreach (Transform t in transform) {
					var choices = t.GetComponentsInChildren<ChoiceComponent> (true);
					foreach (var choice in choices) {
						if (chosenIndex == -1 || chosenIndex == i) {
							choice.Invoke ("ResetChoice", invokeTime);
						}
						i++;
						invokeTime += ResetDelay;
					}
				}
			}
		}

        void InitializeChoices () {
            int i=0;
            foreach (Transform t in transform) {
                var choices = t.GetComponentsInChildren<ChoiceComponent> (true);
				foreach (var choice in choices) {
                    int choiceIndex = i;
					choice.ResetChoice ();
					choice.SetSingleShot (ChoicesAreSingleShot);
					choice.AddButtonListener (delegate {
						Log ($"Player chooses {gameObject.name} index {choiceIndex} ({choice.gameObject.name})");
						if (ChooseIndex (choiceIndex)) {
							choice.Success ();
						} else {
							choice.Fail ();
						}
                    });
                    i++;
                }
            }
        }

		void InitializeCorrectIndicesStack () {
			CorrectIndicesStack.Clear ();
			CorrectIndicesStack.AddRange (CorrectIndices);
		}

		bool ChooseIndex (int choiceIndex) {
			Util.PerformHapticFeedback (Util.HapticFeedbackConstants.VIRTUAL_KEY);
			bool success = false;
			//If host wants a variable for each choice player makes, send it
			if (!string.IsNullOrEmpty (ChoiceKey)) {
				Send (ChoiceKey, choiceIndex);
			}
			//If we have a stack of correct indices, check if this choice is valid and if so, remove it from stack
			if (CorrectIndicesStack.Count != 0) {
				var idx = CorrectIndicesStack.IndexOf (choiceIndex);
				if ((!OrderMatters && idx >= 0) || (OrderMatters && idx == 0)) {
					success = true;
					CorrectIndicesStack.Remove (choiceIndex);
					if (CorrectIndicesStack.Count == 0) {
						onSuccess?.Invoke ();
						//if host wants a success/fail variable, send it
						if (!string.IsNullOrEmpty (SuccessFailKey)) {
							Send (SuccessFailKey, true);
						}
					}
				} else {
					CorrectIndicesStack.Clear ();
					onFail?.Invoke ();
					//if host wants a success/fail variable, send it
					if (!string.IsNullOrEmpty (SuccessFailKey)) {
						Send (SuccessFailKey, false);
					}
				}
			}

			//If no choices left (or none to begin with) and DeactivateOthersOnChoice=true, deactivate them now.
			if (DeactivateOthersOnChoice && CorrectIndicesStack.Count == 0) {
				BroadcastMessage ("DeactivateNotChosen", SendMessageOptions.DontRequireReceiver);
			}

			return success;
		}
    }
        
}