using System;
using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	public class TextInputComponent : ReplicatedComponent {
        [Header ("Opens a keyboard for text input and replicates text to host")]

        [Tooltip("Placeholder text")]
        public string PlaceholderString = "...";
        [Tooltip ("Default text")]
        public string DefaultString = "";

        [Header ("Allow GIF search?")]
        public bool AllowGifSearch = true;

        [Header ("Replication")]
        [Tooltip("Message to host when user texted")]
        public string TextMessage = "/m";
        [Tooltip ("Message to host when user selected a gif animation from Giphy")]
        public string GiphyMessage = "/giphy";
        [Tooltip ("Message to host when user selected a gif animation from Tenor")]
        public string TenorMessage = "/tenor";
        [Tooltip ("Uservariable (bool) whether user is typing (has mobile input open)")]
        public string VariableNameTyping = "";

        /// <summary>
		/// Callback when textinput is opened/activated
		/// </summary>
        public static Action<TextInputComponent> OnOpen;

        /// <summary>
		/// Callback to validate text input. Eg moderation/filtering
		/// </summary>
        public static Action<string, Action<bool>> ValidateText;

        public static float ValidationCooldownSecs = 1f;
        string ValidationText;

        public void Open () {
            if (!string.IsNullOrEmpty (VariableNameTyping)) {
                Send (VariableNameTyping, true);
            }
            OnOpen?.Invoke (this);
        }

        public void SetDefaultString (Text source) {
            DefaultString = source.text;
        }

        public void OnClose () {
            if (!string.IsNullOrEmpty (VariableNameTyping)) {
                Send (VariableNameTyping, false);
            }
        }

        public void OnTextEntered (string text) {
            if (ValidateText == null) {
                Log ("WARNING: ValidateText callback not set, sending unfiltered message: " + text);
                SendHostMessage ($"{TextMessage} text");
            } else {
                ValidationText = text;
                CancelInvoke (nameof (RunValidation));
                Invoke (nameof (RunValidation), ValidationCooldownSecs);
            }
        }

        void RunValidation () {
            var text = ValidationText;
            ValidateText.Invoke (text, (isValid) => {
                if (!isValid) {
                    Log ("Input text did not validate, sending empty message");
                    text = string.Empty;
                }
                SendHostMessage ($"{TextMessage} {text}");
            });
        }

        public void OnGiphySelected (string giphyId) {
            SendHostMessage (string.Format ("{0} {1}", GiphyMessage, giphyId));
        }

        public void OnTenorSelected (string tenorId) {
            SendHostMessage (string.Format ("{0} {1}", TenorMessage, tenorId));
        }
    }
}