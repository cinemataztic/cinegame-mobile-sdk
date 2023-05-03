using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Play haptic feedback and vibration effects.")]
	public class Vibrate: BaseComponent {

		/// <summary>
		/// Start vibrating for 500 ms (default on iOS)
		/// </summary>
		public void Start () {
			Util.Vibrate (500);
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void Start (int milliseconds) {
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void Start (float milliseconds) {
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Stop vibrating feedback.
		/// </summary>
		public void Stop () {
			Util.VibrateStop ();
		}

		/// <summary>
		/// Play a haptic transient feedback effect
		/// </summary>
		public void PlayHapticTransient (Util.HapticFeedbackConstants feedbackConstant) {
			Util.PerformHapticFeedback (feedbackConstant);
		}
	}
}