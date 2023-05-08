using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {
    public abstract class BaseComponent : MonoBehaviour, IGameComponentIcon {
		[Tooltip ("Log events verbosely in editor and debug builds")]
		[SerializeField]
		private bool VerboseDebug;

		private void Awake () {
			VerboseDebug &= Debug.isDebugBuild || Util.IsDevModeActive;
			if (VerboseDebug) {
				Debug.Log ($"Enabled verbose debug for {gameObject.GetScenePath ()}.{GetType ().Name}", this);
			}
		}

		protected void Log (string message) {
			if (VerboseDebug) {
				Debug.Log ($"{gameObject.GetScenePath ()} " + message, this);
			}
		}

		protected void LogError (string message) {
			Debug.LogError ($"{gameObject.GetScenePath ()} " + message, this);
		}

		protected void Log (string format, params object[] args) {
			if (VerboseDebug) {
				Debug.LogFormat (this, $"{gameObject.GetScenePath ()} " + format, args);
			}
		}
	}
}