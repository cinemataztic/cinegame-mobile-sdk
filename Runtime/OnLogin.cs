using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CineGame.MobileComponents {
	/// <summary>
	/// GameComponent which fires events based on what type of login is currently used. Also if the user is minor (below MinorAge, defaults to 17)
	/// </summary>
	public class OnLogin : MonoBehaviour, IGameComponentIcon {
		[Header ("Trigger events based on login type and if user is a minor")]

		[SerializeField]	private int MinorAge = 18;
		[SerializeField]	private UnityEvent OnAnonymous;
		[SerializeField]	private UnityEvent OnVerified;
		[SerializeField]	private UnityEvent OnIsMinor;
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
				if (Util.IsDevModeActive) {
					Debug.LogFormat ("{0} OnAnonymous:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnAnonymous));
				}
				OnAnonymous.Invoke ();
				break;
			case LoginType.Verified:
				if (Util.IsDevModeActive) {
					Debug.LogFormat("{0} OnVerified:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnVerified));
				}
				OnVerified.Invoke();
				break;
			case LoginType.NotLoggedIn:
				if (Util.IsDevModeActive) {
					Debug.LogFormat ("{0} OnLoggedOut:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnLoggedOut));
				}
				OnLoggedOut.Invoke ();
				break;
			}
			bool isMinor = EstimatedAge < MinorAge;
			if (isMinor) {
				OnIsMinor.Invoke ();
				if (Util.IsDevModeActive) {
					Debug.LogFormat ("{0} OnIsMinor:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnIsMinor));
				}
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
