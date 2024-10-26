using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;

using Sfs2X.Entities;
using Sfs2X.Entities.Data;

using Smartfox;

namespace CineGame.MobileComponents {

	public abstract class ReplicatedComponent : BaseComponent {

		[RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		static void Init () {
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		internal static void OnSceneLoaded (Scene scene, LoadSceneMode mode) {
			if (SmartfoxClient.Instance == null)
				return;
			var replicatedComponents = Resources.FindObjectsOfTypeAll<ReplicatedComponent> ();
			var sb = new StringBuilder ();
			foreach (var rc in replicatedComponents) {
				var go = rc.gameObject;
				if (go.scene == scene) {
					sb.AppendLine ($"{Util.GetObjectScenePath (go)}:{rc.GetType ()}");
					rc.InitReplication ();
				}
			}
			if (Util.IsDevModeActive) {
				Debug.Log ("ReplicatedComponent.OnSceneLoaded - InitReplication\n" + sb.ToString ());
			}
		}

		/// <summary>
		/// Initialize replication (and filtering) of data from host to client
		/// </summary>
		public virtual void InitReplication () {
			SmartfoxClient.Instance.OnObjectMessage.AddListener (OnObjectMessage);
			SmartfoxClient.Instance.OnPrivateMessage.AddListener (OnPrivateMessage);
		}

		void OnDestroy () {
			if (SmartfoxClient.Instance != null) {
				SmartfoxClient.Instance.OnObjectMessage.RemoveListener (OnObjectMessage);
				SmartfoxClient.Instance.OnPrivateMessage.RemoveListener (OnPrivateMessage);
				if (Util.IsDevModeActive) {
					Debug.Log ($"DeinitReplication {Util.GetObjectScenePath (gameObject)}:{GetType ()}");
				}
			}
		}

		internal virtual void OnObjectMessage (ISFSObject dataObj, User sender) { }
		internal virtual void OnPrivateMessage (string message, User sender) { }

		protected void Send (string varName, bool value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, int value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, long value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, int [] value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, float value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, float [] value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void Send (string varName, string value) {
			SmartfoxClient.Send (varName, value);
		}

		protected void SendHostMessage (string message) {
			SmartfoxClient.Send (message);
		}
	}
}
