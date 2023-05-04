using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Simple component for invoking actions and settings based on platform
	/// </summary>
	[ComponentReference ("Trigger events when enabled based on App and OS specifics")]
	public class OnApplicationPlatform : BaseComponent {

		[SerializeField] private UnityEvent OnIphone;
		[SerializeField] private UnityEvent OnAndroid;
		[Header ("If Phone is newer Huawei (Post-Google) then this event will fire after OnAndroid")]
		[SerializeField] private UnityEvent OnHuaweiHcm;
		[Header ("Event that fires if Android SDK/Api is at or above")]
		[SerializeField] private int MinimumAndroidSdk = 30;
		[SerializeField] private UnityEvent OnMinimumAndroidSdk;
		[Header ("Event that fires if iOS SDK is at or above")]
		[SerializeField] private int MinimumIosSdk = 14;
		[SerializeField] private UnityEvent OnMinimumIosSdk;
		[Header("Event that fires if app version is at or above")]
		[SerializeField] private string MinimumAppVersion;
		[SerializeField] private UnityEvent OnMinimumAppVersion;

		void OnEnable () {
			if (Application.platform == RuntimePlatform.IPhonePlayer) {
				Log ($"OnIphone:\n{Util.GetEventPersistentListenersInfo (OnIphone)}");
				OnIphone.Invoke ();
				if (!Application.isEditor) {
					var m = Regex.Matches (SystemInfo.operatingSystem, "(\\d+)(\\.(\\d+))");
					if (m.Count > 1 && m [1].Success && int.TryParse (m [1].Value, out int sdk_int)) {
						if (sdk_int >= MinimumIosSdk) {
							Log ($"OnMinimumIosSdk:\n{Util.GetEventPersistentListenersInfo (OnMinimumIosSdk)}");
							OnMinimumIosSdk.Invoke ();
						}
					}
				}
			}
			if (Application.platform == RuntimePlatform.Android) {
				Log ($"OnAndroid:\n{Util.GetEventPersistentListenersInfo (OnAndroid)}");
				OnAndroid.Invoke ();
				if (Util.IsHuaweiHcm ()) {
					Log ($"OnHuaweiHcm:\n{Util.GetEventPersistentListenersInfo (OnHuaweiHcm)}");
					OnHuaweiHcm.Invoke ();
				}
				if (!Application.isEditor && Util.AndroidAPILevel >= MinimumAndroidSdk) {
					Log ($"OnMinimumAndroidSdk:\n{Util.GetEventPersistentListenersInfo (OnMinimumAndroidSdk)}");
					OnMinimumAndroidSdk.Invoke ();
				}
			}
			if (Version.TryParse(MinimumAppVersion, out Version minAppVersion) && new Version (Application.version) >= minAppVersion)	{
				Log($"OnMinimumAppVersion:\n{Util.GetEventPersistentListenersInfo(OnMinimumAppVersion)}");
				OnMinimumAppVersion.Invoke();
			}
		}
	}
}
