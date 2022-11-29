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

        public static Action<TextInputComponent> OnOpen;

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
            SendHostMessage (string.Format ("{0} {1}", TextMessage, text));
        }

        public void OnGiphySelected (string giphyId) {
            SendHostMessage (string.Format ("{0} {1}", GiphyMessage, giphyId));
        }

        public void OnTenorSelected (string tenorId) {
            SendHostMessage (string.Format ("{0} {1}", TenorMessage, tenorId));
        }
    }
}