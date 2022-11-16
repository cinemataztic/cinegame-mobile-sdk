using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	public class TextInputComponent : ReplicatedComponent {
        [Header ("Opens a mobile keyboard for text input and replicates it to host as a message with a prefix (TextMessage).")]

        [Tooltip("Placeholder text for mobile input")]
        private string PlaceholderString = "...";
        public string DefaultString = "";

        [Header("Replication")]
        [Tooltip("Message to host when user texted")]
        public string TextMessage = "/m";
        [Tooltip("Uservariable (bool) whether user is typing (has mobile input open)")]
        public string VariableNameTyping = "";

        TouchScreenKeyboard keyboard = null;
        bool savedHideInput = false;

        void Update () {
			if (keyboard != null) {
                if (keyboard.status == TouchScreenKeyboard.Status.Done && !string.IsNullOrEmpty (keyboard.text)) {
                    SendHostMessage (string.Format ("{0} {1}", TextMessage, keyboard.text));
                    Close ();
                } else if (keyboard.status == TouchScreenKeyboard.Status.Canceled || !TouchScreenKeyboard.visible) {
                    Close ();
                }
            }
        }

        public void Open () {
            if (keyboard == null) {
                savedHideInput = TouchScreenKeyboard.hideInput;
                TouchScreenKeyboard.hideInput = false;
                keyboard = TouchScreenKeyboard.Open (DefaultString, TouchScreenKeyboardType.Default, false, false, false, true, PlaceholderString);
                if (!string.IsNullOrEmpty (VariableNameTyping)) {
                    Send (VariableNameTyping, true);
                }
            }
        }

        public void SetDefaultString (string str) {
            DefaultString = str;
        }

        public void SetDefaultString (Text source) {
            DefaultString = source.text;
        }

        public void Close () {
            //Closes the touchscreen keyboard if open
            keyboard = null;
            TouchScreenKeyboard.hideInput = savedHideInput;
            if (!string.IsNullOrEmpty (VariableNameTyping)) {
                Send (VariableNameTyping, false);
            }
        }
    }
}