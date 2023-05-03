using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	[ComponentReference ("Gyro controller, replicates mobile device orientation. Host can also set a goal for orientation and GyroComponent will send an event whether succeeded within given timeframe")]
	public class GyroComponent : ReplicatedComponent {

        [Header("2D Graphics")]
        public RectTransform Roll2D;
        public RectTransform Pitch2D;
        [Header("3D Graphics")]
        public Transform Gyro3D;

        [Header("Gameplay Options")]
        [Tooltip("Offset for pitch which is fudged from avg phone playing position.")]
        public float PitchOffset = 25f;
		[Tooltip("If player is off course, how long before onFail event is fired (0=N/A)")]
        public float FailTimeThreshold = 1f;
		[Tooltip("If player is on course, how long before onSuccess event is fired (0=N/A)")]
		public float SuccessTimeThreshold = 3f;
        [Tooltip("How much can player sway from perfect pitch before failing. Can be set by host")]
        public float PitchTolerance = 130f;
        [Tooltip("How much can player sway from perfect roll before failing. Can be set by host")]
        public float RollTolerance = 40f;
        //[Tooltip("Fired if player fails to level the gyro")]
        public UnityEvent onFail;
		public UnityEvent onSuccess;

        [Header("Replication")]
        [Tooltip("Name of the pitch uservariable")]
        public string VariableNamePitch = "pitch";
        [Tooltip("Name of the roll uservariable")]
        public string VariableNameRoll = "roll";
        [Tooltip("How often pitch and roll are replicated")]
        public float UpdateInterval = .5f;
        [Tooltip("Message to send to host if player fails. Leave empty if not required")]
        public string FailedMessage = "gyro failed";
		[Tooltip("Message to send to host if player fails. Leave empty if not required")]
		public string SuccessMessage = "gyro success";
        [Tooltip("ObjectMessage key for pitch offset from host.")]
        public string PitchOffsetKey = "pitch";
        [Tooltip("ObjectMessage key for roll offset from host.")]
        public string RollOffsetKey = "roll";
        [Tooltip("ObjectMessage key for pitch tolerance before fail from host.")]
        public string PitchToleranceKey = "pitchtol";
        [Tooltip("ObjectMessage key for roll tolerance before fail from host.")]
        public string RollToleranceKey = "rolltol";

        float remotePitchOffset = 0f;
        float remoteRollOffset = 0f;
        float lastUpdateTime = 0f;
        float failTime = 0f;
		float successTime = 0f;
        bool paused = false;
        Vector3 dampedInputRotation = Vector3.zero;

        internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
            if (dataObj.ContainsKey (PitchOffsetKey)) {
                remotePitchOffset = dataObj.GetFloat (PitchOffsetKey);
            }
            if (dataObj.ContainsKey (RollOffsetKey)) {
                remoteRollOffset = dataObj.GetFloat (RollOffsetKey);
            }
            if (dataObj.ContainsKey (PitchToleranceKey)) {
                PitchTolerance = dataObj.GetFloat (PitchToleranceKey);
            }
            if (dataObj.ContainsKey (RollToleranceKey)) {
                RollTolerance = dataObj.GetFloat (RollToleranceKey);
            }
        }

        void Update () {
            var inputRotation = new Vector3 (
                (Mathf.Asin (Mathf.Clamp (Input.acceleration.y, -1f, 1f)) / Mathf.PI) * 180f + PitchOffset + remotePitchOffset, 
                0f, 
                (Mathf.Asin (Mathf.Clamp (Input.acceleration.x, -1f, 1f)) / Mathf.PI) * 180f + remoteRollOffset);

            dampedInputRotation = Vector3.LerpUnclamped (dampedInputRotation, inputRotation, .1f);

            if (Roll2D != null) {
                Roll2D.eulerAngles = new Vector3 (0f, 0f, dampedInputRotation.z);
            }
            if (Pitch2D != null) {
                Pitch2D.anchoredPosition = new Vector2 (0f, dampedInputRotation.x * 3.89f);
            }
            if (Gyro3D != null) {
                Gyro3D.eulerAngles = dampedInputRotation;
            }

            float t = Time.time;
			if (!string.IsNullOrEmpty (VariableNamePitch) && !string.IsNullOrEmpty (VariableNameRoll) && (lastUpdateTime + UpdateInterval) <= t) {
                Send (VariableNamePitch, dampedInputRotation.x);
                Send (VariableNameRoll, dampedInputRotation.z);
                lastUpdateTime = t;
            }

			if (!paused) {
                if (Mathf.Abs (dampedInputRotation.x) > PitchTolerance || Mathf.Abs (dampedInputRotation.z) > RollTolerance) {
					successTime = 0f;
                    failTime += Time.deltaTime;
                    if (FailTimeThreshold != 0f && failTime >= FailTimeThreshold) {
						failTime = 0f;
                        if (!string.IsNullOrEmpty (FailedMessage)) {
                            SendHostMessage (FailedMessage);
                        }
						Log ($"GyroComponent.OnFail\n{Util.GetEventPersistentListenersInfo (onSuccess)}");
						onFail.Invoke ();
						Pause ();
                    }
                } else {
                    failTime = 0f;
					successTime += Time.deltaTime;
					if (SuccessTimeThreshold != 0f && successTime >= SuccessTimeThreshold) {
						successTime = 0f;
						if (!string.IsNullOrEmpty (SuccessMessage)) {
							SendHostMessage (SuccessMessage);
						}
						Log ($"GyroComponent.OnSuccess\n{Util.GetEventPersistentListenersInfo (onSuccess)}");
						onSuccess.Invoke ();
						Pause ();
					}
                }
            }
        }

        public void Pause () {
            paused = true;
        }

        public void Resume () {
            paused = false;
        }
    }

}
