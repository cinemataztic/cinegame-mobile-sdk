using System.Collections.Generic;
using UnityEngine;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	public abstract class ReplicatedComponent : BaseComponent {
		public delegate void ObjectMessageDelegate (ISFSObject dataObj, int senderId);
		public static event ObjectMessageDelegate onObjectMessage;

		public delegate void PrivateMessageDelegate (string message, int senderId);
		public static event PrivateMessageDelegate onPrivateMessage;

		public static ISFSObject objectMessageToHost = new SFSObject ();
		public static List<string> messagesToHost = new List<string> ();

		public static void InvokePrivateMessage (string message, int senderId) {
			onPrivateMessage?.Invoke (message, senderId);
		}

		public static void InvokeObjectMessage (ISFSObject obj, int senderId) {
			onObjectMessage?.Invoke (obj, senderId);
		}

		/// <summary>
		/// Initialize replication (and filtering) of data from host to client
		/// </summary>
		public virtual void InitReplication () {
			onObjectMessage += OnObjectMessage;
			onPrivateMessage += OnPrivateMessage;
			if (Util.IsDevModeActive) {
				Debug.Log ($"{GetType ()} InitReplication {Util.GetObjectScenePath (gameObject)}", this);
			}
		}

		void OnDestroy () {
			onObjectMessage -= OnObjectMessage;
			onPrivateMessage -= OnPrivateMessage;
			if (Util.IsDevModeActive) {
				Debug.Log ($"{GetType ()} DeinitReplication {Util.GetObjectScenePath (gameObject)}");
			}
		}

		internal virtual void OnObjectMessage (ISFSObject dataObj, int senderId) { }
		internal virtual void OnPrivateMessage (string message, int senderId) { }

		protected void Send (string varName, bool value) {
			objectMessageToHost.PutBool (varName, value);
		}

		protected void Send (string varName, int value) {
			objectMessageToHost.PutInt (varName, value);
		}

		protected void Send (string varName, long value) {
			objectMessageToHost.PutLong (varName, value);
		}

		protected void Send (string varName, int [] value) {
			objectMessageToHost.PutIntArray (varName, value);
		}

		protected void Send (string varName, float value) {
			objectMessageToHost.PutFloat (varName, value);
		}

		protected void Send (string varName, float [] value) {
			objectMessageToHost.PutFloatArray (varName, value);
		}

		protected void Send (string varName, string value) {
			objectMessageToHost.PutUtfString (varName, value);
		}

		protected void SendHostMessage (string message) {
			messagesToHost.Add (message);
		}
	}
}
