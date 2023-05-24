using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	[ComponentReference ("Set or crossfade a graphic's color property from HTML string\n\nStrings that begin with '#' will be parsed as hexadecimal in the following way:\n#RGB (becomes RRGGBB)\n#RRGGBB\n#RGBA (becomes RRGGBBAA)\n#RRGGBBAA\n\nWhen not specified alpha will default to FF.\nStrings that do not begin with '#' will be parsed as literal colors, with the following supported:\nred, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta.\n")]
	public class SetGraphicColor : BaseComponent {

		public Graphic Receiver;
		public float CrossfadeDuration = .2f;

		public void SetColor (string html) {
			if (ColorUtility.TryParseHtmlString (html, out Color color)) {
				Log ($"SetColor \"{html}\"");
				Receiver.color = color;
			} else {
				LogError ($"Unable to parse string as color: \"{html}\"");
			}
		}

		public void CrossfadeColor (string html) {
			if (ColorUtility.TryParseHtmlString (html, out Color color)) {
				Log ($"CrossfadeColor \"{html}\"");
				Receiver.CrossFadeColor (color, CrossfadeDuration, false, false);
			} else {
				LogError ($"Unable to parse string as color: \"{html}\"");
			}
		}
	}

}
