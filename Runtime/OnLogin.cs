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
		[SerializeField]	private UnityEvent OnFacebook;
		[SerializeField]	private UnityEvent OnSignInWithApple;
		[SerializeField]	private UnityEvent OnGooglePlay;
		[SerializeField]	private UnityEvent OnAnonymous;
		[SerializeField]	private UnityEvent OnIsMinor;
		[SerializeField]	private UnityEvent OnLoggedOut;

		public enum LoginType {
			NotLoggedIn,
			Anonymous,
			Facebook,
			Apple,
			GooglePlay
		}

		public static LoginType CurrentLoginType;
		public static int EstimatedAge;

		void OnEnable () {
			UpdateLoginType ();
		}

		void UpdateLoginType () {
			switch (CurrentLoginType) {
			case LoginType.Facebook:
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnFacebook:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnFacebook));
				}
				OnFacebook.Invoke ();
				break;
			case LoginType.Apple:
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnSignInWithApple:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnSignInWithApple));
				}
				OnSignInWithApple.Invoke ();
				break;
			case LoginType.GooglePlay:
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnGooglePlay:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnGooglePlay));
				}
				OnGooglePlay.Invoke ();
				break;
			case LoginType.Anonymous:
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnAnonymous:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnAnonymous));
				}
				OnAnonymous.Invoke ();
				break;
			case LoginType.NotLoggedIn:
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnLoggedOut:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnAnonymous));
				}
				OnLoggedOut.Invoke ();
				break;
			}
			bool isMinor = EstimatedAge < MinorAge;//(UserInfoSurvey.GetUserAge () < MinorAge) || (FacebookController.LoggedIn && FacebookController.IsMinor);
			if (isMinor) {
				OnIsMinor.Invoke ();
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnIsMinor:\n{1}", Util.GetObjectScenePath (gameObject), Util.GetEventPersistentListenersInfo (OnIsMinor));
				}
			}
		}

		/// <summary>
		/// login status changed-- either logged out or logged in.
		/// </summary>
		public static void LoginChanged () {
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				var gos = SceneManager.GetSceneAt (i).GetRootGameObjects ();
				foreach (var go in gos) {
					var comps = go.GetComponentsInChildren<OnLogin> ();
					foreach (var comp in comps) {
						comp.UpdateLoginType ();
					}
				}
			}
		}
	}
}
