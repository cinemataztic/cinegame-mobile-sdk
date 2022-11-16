using UnityEngine;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Simple class for controlling haptic feedback (vibration) on handheld devices.
	/// </summary>
	public class Vibrate: MonoBehaviour, IGameComponentIcon {

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