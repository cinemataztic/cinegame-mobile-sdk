using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Simple component for invoking actions and settings based on platform
	/// </summary>
	[ComponentReference ("Trigger events when enabled based on App and OS specifics")]
	public class OnApplicationPlatform : BaseEventComponent {

		[Tooltip ("Invoked when device is an iOS")]
		[SerializeField] private UnityEvent OnIphone;
		[Tooltip ("Invoked when device is an Android")]
		[SerializeField] private UnityEvent OnAndroid;
		[Tooltip ("Invoked when device is a newer Huawei Android (Post-Google break up)")]
		[SerializeField] private UnityEvent OnHuaweiHcm;
		[SerializeField] private int MinimumAndroidSdk = 30;
		[Tooltip ("Invoked when the device's Android SDK >= Minimum Android Sdk")]
		[SerializeField] private UnityEvent OnMinimumAndroidSdk;
		[SerializeField] private int MinimumIosSdk = 14;
		[Tooltip ("Invoked when the device's iOS SDK >= Minimum Ios Sdk")]
		[SerializeField] private UnityEvent OnMinimumIosSdk;
		[SerializeField] private string MinimumAppVersion;
		[Tooltip ("Invoked when app version >= Minimum App Version")]
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
