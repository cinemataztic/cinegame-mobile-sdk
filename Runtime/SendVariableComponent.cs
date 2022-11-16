﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace CineGame.MobileComponents {

	public class SendVariableComponent : ReplicatedComponent {
		[Header ("Send variables or messages to host. Invoke the 'Action' methods with variables or constants.")]

		[Header("Replication")]
		[Tooltip("Message to prefix. Leavy empty if no private message should be sent.")]
		public string Message = string.Empty;
		[Tooltip("Variable to send. Leave empty if no object message should be sent.")]
		public string Key = "fire";
		[Tooltip("How often should this be allowed to replicate.")]
		public float CooldownTime = .1f;

		float lastUpdateTime = 0f;

		bool Cooldown () {
			if (lastUpdateTime + CooldownTime <= Time.time) {
				lastUpdateTime = Time.time;
				return true;
			}
			return false;
		}

		new void SendHostMessage (string action) {
			if (!string.IsNullOrEmpty (action)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} SendVariableComponent: Sending host message '{1}'", Util.GetObjectScenePath (this.gameObject), action);
				}
				base.SendHostMessage (action);
			}
		}

		void SendMessageWithArgument (object arg) {
			base.SendHostMessage (string.Format ("{0} {1}", Message, arg));
		}

		//-----------------------------------

		public void Action () {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					//Send dummy bool as a sort of flag
					Send (Key, true);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendHostMessage (Message);
				}
			}
		}

		public void Action (string value) {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					Send (Key, value);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendMessageWithArgument (value);
				}
			}
		}

		public void Action (float value) {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					Send (Key, value);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendMessageWithArgument (value);
				}
			}
		}

		public void Action (int value) {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					Send (Key, value);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendMessageWithArgument (value);
				}
			}
		}

		public void Action (char value) {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					Send (Key, value);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendMessageWithArgument (value);
				}
			}
		}

		public void Action (bool value) {
			if (Cooldown ()) {
				if (!string.IsNullOrEmpty (Key)) {
					Send (Key, value);
				}
				if (!string.IsNullOrEmpty (Message)) {
					SendMessageWithArgument (value ? "1" : "0");
				}
			}
		}
	}

}
