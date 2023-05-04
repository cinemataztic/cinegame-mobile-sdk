﻿using UnityEngine;
﻿using Sfs2X.Entities.Data;
using System;
using System.IO;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace CineGame.MobileComponents {

	[ComponentReference ("Play haptic feedback and vibration effects when enabled or when invoked via the Play methods. You can trigger the effect from host or send haptic patterns")]
	public class Vibrate : ReplicatedComponent {

		[Tooltip ("Text file in the format {PRIMITIVE_ID},{intensity 0;1},{delay in msecs}")]
		public TextAsset AndroidHapticFile;

		[Tooltip ("Standard AHAP file")]
		public TextAsset iOSHapticFile;

		[Tooltip ("Autoplay when gameobject is enabled")]
		public bool PlayOnEnable = true;

		[Tooltip ("If non-0, repeat at this interval in seconds until Stop is called")]
		public float RepeatInterval;

		[Tooltip ("If above 1 repeat this many times at the above interval or until Stop is called")]
		public int RepeatCount = 1;

		[Space]
		[Header ("Replication")]

		[Tooltip ("If this property is received from host, play as an iOS AHAP file")]
		public string iOSHapticKey = "iOSHaptic";

		[Tooltip ("If this property is received from host, play as an Android VibrationEffect.Composition file")]
		public string AndroidHapticKey = "AndroidHaptic";

		[Tooltip ("If this property is received from host, set repeat interval in seconds (float)")]
		public string RepeatIntervalKey = "HapticRepeatInterval";

		[Tooltip ("If this property is received from host, set repeat count")]
		public string RepeatCountKey = "HapticRepeatCount";

		/// <summary>
		/// Start vibrating for 500 ms (default on iOS)
		/// </summary>
		private void OnEnable () {
			if (PlayOnEnable) {
				Play ();
			}
		}

		private void OnDisable() {
			Stop ();
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void PlayVibration (int milliseconds) {
			Log ("Vibrate.PlayVibration " + milliseconds);
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Start vibrating feedback.
		/// </summary>
		/// <param name="milliseconds">Milliseconds (only valid on Android).</param>
		public void PlayVibration (float milliseconds) {
			Log ("Vibrate.PlayVibration " + milliseconds);
			Util.Vibrate ((long)milliseconds);
		}

		/// <summary>
		/// Stop vibration/haptic
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
			StopAllCoroutines ();
			StartCoroutine (E_Play (textAsset.text));
		}

		IEnumerator E_Play (string pattern = null) {
			for (int i = 0; i < RepeatCount; i++) {
				if (pattern != null)
					Util.Vibrate (pattern);
				else
					Util.Vibrate ();
				if (RepeatInterval <= float.Epsilon)
					break;
				yield return new WaitForSeconds (RepeatInterval);
			}
		}

		public void Play () {
			if (Application.platform == RuntimePlatform.Android && AndroidHapticFile != null) {
				PlayHapticPattern (AndroidHapticFile);
			} else if (Application.platform == RuntimePlatform.IPhonePlayer && iOSHapticFile != null) {
				PlayHapticPattern (iOSHapticFile);
			} else {
				StopAllCoroutines ();
				StartCoroutine (E_Play ());
			}
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (RepeatIntervalKey)) {
				RepeatInterval = dataObj.GetFloat (RepeatIntervalKey);
			}
			if (dataObj.ContainsKey (RepeatCountKey)) {
				RepeatCount = dataObj.GetInt (RepeatCountKey);
			}
			OnValidate ();
			if (Application.platform == RuntimePlatform.IPhonePlayer && dataObj.ContainsKey (iOSHapticKey)) {
				StopAllCoroutines ();
				StartCoroutine (E_Play (dataObj.GetUtfString (iOSHapticKey)));
			} if (Application.platform == RuntimePlatform.Android && dataObj.ContainsKey (AndroidHapticKey)) {
				StopAllCoroutines ();
				StartCoroutine (E_Play (dataObj.GetUtfString (AndroidHapticKey)));
			}
		}

		private void OnValidate() {
			RepeatCount = Mathf.Max (RepeatCount, 1);
			RepeatInterval = Mathf.Max (RepeatInterval, 0f);
#if UNITY_EDITOR
			if (iOSHapticFile != null) {
				JObject.Parse (iOSHapticFile.text);
			}

			if (AndroidHapticFile != null) {
				Util.CreateAndroidHapticEffect (AndroidHapticFile.text, AndroidHapticFile.name);
			}
#endif
		}
	}
}