using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Play haptic feedback and vibration effects when enabled or when invoked via the Start or Play methods.")]
	public class Vibrate: BaseComponent {

		[Tooltip ("Text file in the format {PRIMITIVE_ID},{intensity},{delay in msecs}")]
		public TextAsset AndroidHapticFile;

		[Tooltip ("Standard AHAP file")]
		public TextAsset iOSHapticFile;

		[Tooltip ("Autoplay when gameobject is enabled")]
		public bool PlayOnEnable = true;

		/// <summary>
		/// Start vibrating for 500 ms (default on iOS)
		/// </summary>
		private void OnEnable()	{
			if (Application.platform == RuntimePlatform.Android && AndroidHapticFile != null) {
				Log ("Vibrate.OnEnable " + AndroidHapticFile.name);
				Util.Vibrate (AndroidHapticFile);
			} else if (Application.platform == RuntimePlatform.IPhonePlayer && iOSHapticFile != null) {
				Log ("Vibrate.OnEnable " + iOSHapticFile.name);
				Util.Vibrate (iOSHapticFile);
			} else {
				Log ("Vibrate.OnEnable default vibration");
				Util.Vibrate (500);
			}
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void Start (int milliseconds) {
			Log ("Vibrate.Start " + milliseconds);
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void Start (float milliseconds) {
			Log ("Vibrate.Start " + milliseconds);
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Stop vibrating feedback.
		/// </summary>
		public void Stop () {
			Log ("Vibrate.Stop");
			Util.VibrateStop ();
		}

		/// <summary>
		/// Play a haptic transient feedback effect
		/// </summary>
		public void PlayHapticTransient (Util.HapticFeedbackConstants feedbackConstant) {
			Log ("Vibrate.PlayHapticTransient " + feedbackConstant);
			Util.PerformHapticFeedback (feedbackConstant);
		}

		/// <summary>
		/// Play a haptic pattern effect. The file format depends on the platform (iOS = AHAP file, Android = custom file in the format {PRIMITIVE_ID},{intensity},{delay in msec}
		/// </summary>
		public void PlayHapticPattern (TextAsset textAsset) {
			Log ("Vibrate.PlayHapticPattern " + textAsset.name);
			Util.Vibrate (textAsset);
		}

		public void Play () {
			OnEnable ();
		}
	}
}