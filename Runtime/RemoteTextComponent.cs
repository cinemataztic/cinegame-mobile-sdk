using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Remotely replace text, or supply params for a formatted string
	/// </summary>
	[ComponentReference ("Replicated text. The text can be a string constant or formatted with a set of keys and values. You can apply custom U and L formatters (upper-case and lower-case).\nIf the StringFormat property is left empty, the format will be determined from the receiving Text, TextMesh or TMP serialized value.")]
	public class RemoteTextComponent : ReplicatedComponent {

        [Tooltip("If true, the data from host will be formatted in the StringFormat string or the default value of the listener")]
        public bool IsFormattedString = false;

		[Tooltip("Key in the ObjectMessage from host. Eg 'question'")]
		public string Key;

        [Tooltip("Keys in the ObjectMessage from host to format into the text. Eg 'rank' and 'score'")]
        public string[] Keys;

		public enum ComponentType {
			String,
			Integer,
			Float
		}
		public ComponentType[] Types;

		[Tooltip ("The format string to invoke OnReceive with. If empty the format will be determined runtime from the listener")]
		public string StringFormat;

		[Tooltip ("Invoked with the formatted string, or the raw string value if no formatter is specified")]
		public UnityEvent<string> OnReceive;

		public override void InitReplication () {
			base.InitReplication ();

			Component c;
			bool addListener = false;
			if (OnReceive.GetPersistentEventCount () != 0) {
				c = OnReceive.GetPersistentTarget (0) as Component;
			} else {
				addListener = true;
				c = GetComponent<Text> ();
				if (c == null) {
					c = GetComponent<TextMesh> ();
#if UNITY_2021_1_OR_NEWER
					if (c == null) {
						c = GetComponent<TMPro.TMP_Text> ();
					}
#endif
				}
			}
			if (c is Text textComponent) {
				if (IsFormattedString && string.IsNullOrWhiteSpace (StringFormat)) {
					StringFormat = textComponent.text;
					textComponent.enabled = false;
				}
				if (addListener) {
					OnReceive.AddListener ((value) => {
						textComponent.text = value;
						textComponent.enabled = true;
					});
				}
			} else if (c is TextMesh textMesh) {
				var renderer = textMesh.GetComponent<Renderer> ();
				if (IsFormattedString && string.IsNullOrWhiteSpace (StringFormat)) {
					StringFormat = textMesh.text;
					renderer.enabled = false;
				}
				if (addListener) {
					OnReceive.AddListener ((value) => {
						textMesh.text = value;
						renderer.enabled = true;
					});
				}
#if UNITY_2021_1_OR_NEWER
			} else if (c is TMPro.TMP_Text tmp) {
				if (IsFormattedString && string.IsNullOrWhiteSpace (StringFormat)) {
					StringFormat = tmp.text;
					tmp.enabled = false;
				}
				if (addListener) {
					OnReceive.AddListener ((value) => {
						tmp.text = value;
						tmp.enabled = true;
					});
				}
			}
#endif
		}

		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
			string s = null;
			if (IsFormattedString) {
				var args = new List<object> (Keys.Length);
				int i = 0;
				foreach (var key in Keys) {
					if (dataObj.ContainsKey (key)) {
						switch (Types [i]) {
						case ComponentType.String:
							args.Add (dataObj.GetUtfString (key));
							break;
						case ComponentType.Integer:
							args.Add (dataObj.GetInt (key));
							break;
						case ComponentType.Float:
							args.Add (dataObj.GetFloat (key));
							break;
						default:
							Debug.LogError ($"RemoteTextComponent Unknown Type: {Types [i]}");
							break;
						}
					}
					i++;
				}
				if (!string.IsNullOrWhiteSpace (StringFormat) && args.Count == Keys.Length) {
					s = string.Format (Util.CustomStringFormat, StringFormat, args.ToArray ());
				}
			} else if (dataObj.ContainsKey (Key)) {
				s = dataObj.GetUtfString (Key);
			}
			if (s != null) {
				OnReceive?.Invoke (s);
				Log ($"RemoteTextComponent: \"{s}\"\n{Util.GetEventPersistentListenersInfo (OnReceive)}");
			}
        }
	}
}
