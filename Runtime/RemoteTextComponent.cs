using UnityEngine;
using UnityEngine.UI;
using Sfs2X.Entities.Data;
using System;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Remotely replace text, or supply params for a formatted string
	/// </summary>
	public class RemoteTextComponent : ReplicatedComponent {
		[Header ("Replicated text. The text can be a string constant or formatted with a set of keys and values. You can apply U and L formatters (upper-case and lower-case)")]

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

		string formattedString = null;
		CustomStringFormat customStringFormat;

		public override void InitReplication () {
			base.InitReplication ();
			if (IsFormattedString) {
				var textComponent = GetComponent<Text> ();
				formattedString = textComponent.text;
				customStringFormat = new CustomStringFormat ();
				//Disable text until we have valid data
				textComponent.enabled = false;
			}
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
				var textComponent = GetComponent<Text> ();
				textComponent.text = s;
				textComponent.enabled = true;
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
				return String.Empty;
		}
	}
}
