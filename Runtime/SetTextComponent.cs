using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	public class SetTextComponent : MonoBehaviour, IGameComponentIcon {
		[Header ("Change text in a Text Component based on an index")]

		public string[] Texts;

		public void SetText (string text) {
			var textComponent = GetComponent<Text> ();
			textComponent.text = text;
		}

		public void SetTextIndex (int i) {
			var textComponent = GetComponent<Text> ();
			textComponent.text = Texts [Mathf.Clamp (i, 0, Texts.Length-1)];
		}
	}

}
