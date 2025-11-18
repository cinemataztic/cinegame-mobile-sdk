using UnityEngine;

namespace CineGame.MobileComponents {
    public abstract class BaseComponent : MonoBehaviour, IGameComponentIcon {
		[Tooltip ("Log events verbosely in editor and debug builds")]
		[SerializeField]
		private bool VerboseDebug;

		private void Awake () {
			VerboseDebug &= Debug.isDebugBuild || Util.IsDevModeActive;
			//if (VerboseDebug) {
			//	Debug.Log ($"Enabled verbose debug for {gameObject.GetScenePath ()}.{GetType ().Name}", this);
			//}
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

		protected void DrawLine (Vector3 start, Vector3 end, Color color) {
			if (VerboseDebug) {
				Debug.DrawLine (start, end, color);
			}
		}

        protected void DrawListenersLines (UnityEngine.Events.UnityEventBase e, Color color) {
            if (VerboseDebug) {
                Util.DrawLinesToPersistentEventListeners (e, transform.position, color);
            }
        }
    }

    public abstract class BaseEventComponent : BaseComponent {
        [HideInInspector]
        [SerializeField]
#pragma warning disable 0414
		private int eventMask = 0;
#pragma warning restore
	}
}