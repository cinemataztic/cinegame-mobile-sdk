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
	[ComponentReference ("Replicated text. The text can be a string constant or formatted with a set of keys and values. You can apply U and L formatters (upper-case and lower-case)")]
	public class RemoteTextComponent : ReplicatedComponent {

        [Tooltip("If true, the data from host will be formatted in the default value of the text component")]
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

		public UnityEvent<string> OnReceive;

		string formattedString = null;
		CustomStringFormat customStringFormat;

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
					if (c == null) {
#if UNITY_2021_1_OR_NEWER
						c = GetComponent<TMPro.TMP_Text> ();
#endif
					}
				}
			}
			if (c is Text textComponent) {
				formattedString = textComponent.text;
				textComponent.enabled = false;
				if (addListener) {
					OnReceive.AddListener ((value) => {
						textComponent.text = value;
						textComponent.enabled = true;
					});
				}
			} else if (c is TextMesh textMesh) {
				formattedString = textMesh.text;
				var renderer = textMesh.GetComponent<Renderer> ();
				renderer.enabled = false;
				if (addListener) {
					OnReceive.AddListener ((value) => {
						textMesh.text = value;
						renderer.enabled = true;
					});
				}
#if UNITY_2021_1_OR_NEWER
			} else if (c is TMPro.TMP_Text tmp) {
				formattedString = tmp.text;
				tmp.enabled = false;
				if (addListener) {
					OnReceive.AddListener ((value) => {
						tmp.text = value;
						tmp.enabled = true;
					});
				}
			}
#endif
			customStringFormat = new CustomStringFormat ();
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
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
				if (formattedString != null && args.Count == Keys.Length) {
					s = string.Format (customStringFormat, formattedString, args.ToArray ());
				}
			} else if (dataObj.ContainsKey (Key)) {
				s = dataObj.GetUtfString (Key);
			}
			if (s != null) {
				OnReceive?.Invoke (s);
				if (Util.IsDevModeActive) {
					Debug.Log ($"RemoteTextComponent: {Util.GetObjectScenePath (gameObject)} = \"{s}\"");
				}
			}
        }
    }

	/// <summary>
	/// Custom string formatting. U is uppercase, L is lowercase, Txx trims to max xx characters with ellipse character if trimmed
	/// </summary>
	public class CustomStringFormat : IFormatProvider, ICustomFormatter {
		public object GetFormat (Type formatType) {
			if (formatType == typeof (ICustomFormatter))
				return this;
			else
				return null;

		}

		public string Format (string format, object arg, IFormatProvider formatProvider) {
			switch (format) {
			case "U":
				return arg.ToString ().ToUpper ();
			case "L":
				return arg.ToString ().ToLower ();
			default:
				if (format.Length > 1 && format [0] == 'T') {
					var trimLen = int.Parse (format.Substring (1));
					var str = arg.ToString ();
					if (str.Length > trimLen) {
						str = arg.ToString ().Substring (0, trimLen) + "…";
					}
					return str;
				}
				return HandleOtherFormats (format, arg, formatProvider);
			}
		}

		private string HandleOtherFormats (string format, object arg, IFormatProvider formatProvider) {
			if (arg is IFormattable)
				return ((IFormattable)arg).ToString (format, formatProvider);
			else if (arg != null)
				return arg.ToString ();
			else
				return string.Empty;
		}
	}
}
