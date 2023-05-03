using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CineGame.MobileComponents {
	[ComponentReference ("Fire events based on what type of login is currently used. Also if the user is minor (below MinorAge, defaults to 17)")]
	public class OnLogin : BaseComponent {

		[SerializeField]	private int MinorAge = 18;
		[SerializeField]	private UnityEvent OnAnonymous;
		[SerializeField]	private UnityEvent OnVerified;
		[SerializeField]	private UnityEvent<int> OnIsMinor;
		[SerializeField]	private UnityEvent OnLoggedOut;

		public enum LoginType {
			NotLoggedIn,
			Anonymous,
			Verified,
		}

		static LoginType CurrentLoginType;
		public static int EstimatedAge;

		void OnEnable () {
			PropagateLoginType ();
		}

		void PropagateLoginType () {
			switch (CurrentLoginType) {
			case LoginType.Anonymous:
				Log ($"OnAnonymous:\n{Util.GetEventPersistentListenersInfo (OnAnonymous)}");
				OnAnonymous.Invoke ();
				break;
			case LoginType.Verified:
				Log ($"OnVerified:\n{Util.GetEventPersistentListenersInfo (OnVerified)}");
				OnVerified.Invoke();
				break;
			case LoginType.NotLoggedIn:
				Log ($"OnLoggedOut:\n{Util.GetEventPersistentListenersInfo (OnLoggedOut)}");
				OnLoggedOut.Invoke ();
				break;
			}
			bool isMinor = EstimatedAge < MinorAge;
			if (isMinor) {
				Log ($"OnIsMinor:\n{Util.GetEventPersistentListenersInfo (OnIsMinor)}");
				OnIsMinor.Invoke (EstimatedAge);
			}
		}

		/// <summary>
		/// login status changed-- propagate events
		/// </summary>
		public static void LoginChanged (LoginType loginType) {
			CurrentLoginType = loginType;
			foreach (var onLogin in FindObjectsOfType<OnLogin> (includeInactive: true)) {
				onLogin.PropagateLoginType ();
			}
		}
	}
}
