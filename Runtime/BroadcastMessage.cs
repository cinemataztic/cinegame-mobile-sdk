using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Utility to invoke GameObject.BroadcastMessage or GameObject.SendUpwards with the \"DontRequireReceiver\" option, ie without logging an error if children or parents do not have a receiving method.\n\nYou can also supply a dynamic Object, string, int, float or bool as parameter.")]
	public class BroadcastMessage : BaseComponent {

		[Tooltip ("Predefined Message. You can override this by invoking the Broadcast (string) method")]
		public string Message;

		/// <summary>
		/// Broadcast a dynamic message, ignoring the predefined Message
		/// </summary>
		public void Broadcast (string message) {
			BroadcastMessage (message, SendMessageOptions.DontRequireReceiver);
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

        /// <summary>
        /// Broadcast the predefined Message with a dynamic int
        /// </summary>
        public void BroadcastInt (int parameter) {
            BroadcastMessage (Message, parameter, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Broadcast the predefined Message with a dynamic float
        /// </summary>
        public void BroadcastFloat (float parameter) {
            BroadcastMessage (Message, parameter, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Broadcast the predefined Message with a dynamic bool
        /// </summary>
        public void BroadcastBool (bool parameter) {
            BroadcastMessage (Message, parameter, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Send a dynamic message upwards, ignoring the predefined Message
        /// </summary>
        public void SendUpwards (string message) {
			SendMessageUpwards (message, SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
		/// Send the predefined message upwards with a dynamic parameter
		/// </summary>
		public void SendUpwards (Object parameter) {
			SendMessageUpwards (Message, parameter, SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
        /// Send the predefined message upwards with a dynamic string
        /// </summary>
		public void SendStringUpwards (string parameter) {
			SendMessageUpwards (Message, parameter, SendMessageOptions.DontRequireReceiver);
		}
	}
}