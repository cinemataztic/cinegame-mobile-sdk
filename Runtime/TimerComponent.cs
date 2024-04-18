using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	[ComponentReference("TimerComponent can be used as a stopclock. You can use it for countdowns and countups. When the time has passed, OnTimeout event will be invoked.\n\nOnUpdate will output the normalized time eg for Image.fillAmount or Slider.normalizedValue.\n\nOnUpdateString will format the TimeSpan value for eg a Text, TextMesh or TMPro.Text component.\n\nYou can invoke methods StartClock, StopClock and ResumeClock as you please.")]
	public class TimerComponent : BaseComponent {

		[Tooltip ("If true, the clock will start when the component or parent GameObject gets enabled")]
		public bool StartOnEnable = true;

		[Tooltip ("If CountDown=true, this is the starting time. If CountDown=false, the clock will time out at this time")]
		public float TimeInSeconds = 10;

		[Tooltip ("Direction of clock")]
		public bool CountDown = true;

		[Tooltip ("Format string for the OnUpdateString event. Remember to \\-escape characters like : and .")]
		public string StringFormat = "{0:mm\\:ss\\.fff}";

		[Tooltip("Invoked every frame with a string formatted time span value (see StringFormat property above)")]
		public UnityEvent<string> OnUpdateString;

		[Tooltip("Invoked every frame with the normalized time, eg for Image.fillAmount or Slider.normalizedValue")]
		public UnityEvent<float> OnUpdate;

		[Tooltip("Invoked when timer finishes")]
		public UnityEvent OnTimeout;

		float t0;
		bool stopped = true;

		void OnEnable() {
			stopped = true;
			UpdateString (CountDown ? TimeInSeconds : 0f);
			if (StartOnEnable) {
				StartClock ();
			}
		}

		/// <summary>
		/// Start the clock, resetting the counter
		/// </summary>
		public void StartClock () {
			Log ($"StartClock CountDown={CountDown} TimeInSecond={TimeInSeconds}");
			t0 = CountDown ? Time.time + TimeInSeconds : Time.time;
			stopped = false;
		}

		/// <summary>
		/// Stop the clock
		/// </summary>
		public void StopClock () {
			Log ($"StopClock");
			stopped = true;
		}

		/// <summary>
		/// Resume the clock, continuing from the current counter
		/// </summary>
		public void ResumeClock () {
			if ((CountDown && t0 > Time.time)
			|| (!CountDown && Time.time - t0 < TimeInSeconds)) {
				Log ($"ResumeClock: Clock already expired");
				stopped = false;
			}
			Log ($"ResumeClock");
		}

		void UpdateString (float time) {
			var timeSpan = new TimeSpan ((long)(time * TimeSpan.TicksPerSecond));
			OnUpdateString?.Invoke (string.Format (StringFormat, timeSpan));
		}

		void Update() {
			if (!stopped && TimeInSeconds > float.Epsilon) {
				var time = CountDown ? t0 - Time.time : Time.time - t0;
				if (time < float.Epsilon) {
					time = 0f;
				}
				OnUpdate?.Invoke (time / TimeInSeconds);
				if (!string.IsNullOrWhiteSpace (StringFormat)) {
					UpdateString (time);
				}

				if ((CountDown && time < float.Epsilon)
				|| (!CountDown && time >= TimeInSeconds)) {
					Log ($"OnTimeout\n{Util.GetEventPersistentListenersInfo (OnTimeout)}");
					stopped = true;
					OnTimeout?.Invoke ();
				}
			}
		}

		void OnValidate () {
			TimeInSeconds = Math.Max (0f, TimeInSeconds);

			//Log exception immediately if StringFormat property is not valid
			if (!string.IsNullOrWhiteSpace (StringFormat)) {
				_ = string.Format (StringFormat, TimeSpan.Zero);
			}
		}
	}

}
