using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Utility to invoke GameObject.BroadcastMessage with the \"DontRequireReceiver\" option, ie without logging an error if children do not have a receiving method.\n\nYou can also supply a dynamic Object or a string as parameter.")]
	public class BroadcastMessage : BaseComponent {

		[Tooltip ("Predefined Message. You can override this by invoking the Broadcast (string) method")]
		public string Message;

		/// <summary>
		/// Broadcast a dynamic message, ignoring the predefined Message
		/// </summary>
		public void Broadcast (string Message) {
			BroadcastMessage (Message, SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
		/// Broadcast the predefined Message with a dynamic parameter
		/// </summary>
		public void Broadcast (Object parameter) {
			BroadcastMessage (Message, parameter, SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
		/// Broadcast the predefined Message with a dynamic string
		/// </summary>
		public void BroadcastString (string parameter) {
			BroadcastMessage (Message, parameter, SendMessageOptions.DontRequireReceiver);
		}

	}
}