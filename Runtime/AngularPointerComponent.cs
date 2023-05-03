using UnityEngine;
using UnityEngine.EventSystems;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Tracks a given PointerEvent position angularly, eg for an arrow or a gun to point towards the screen point where the user has clicked
	/// </summary>
	[ComponentReference ("Control an arrow/direction on a canvas and optionally replicate the angle. Eg a turret/cannon or steering wheel")]
	public class AngularPointerComponent : ReplicatedComponent {

        public RectTransform Pointer;
		public float MinValue = -360f;
		public float MaxValue = 360f;

		[Header("Replication")]
        public string VariableName = "angle";
        public float UpdateInterval = .1f;

        float prevAngle = 0f;
        float lastUpdateTime = 0f;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (VariableName)) {
				SetAngle (dataObj.GetFloat (VariableName));
			}
		}

		void Update () {
			//Replicate angle if it has changed and if time since last update >= UpdateInterval
            float t = Time.time;
			float currentAngle = Pointer.eulerAngles.z;
            if (currentAngle != prevAngle && (lastUpdateTime + UpdateInterval) <= t) {
				Log ($"AngularPointerComponent.Update {VariableName}={currentAngle}");
				Send (VariableName, currentAngle);
                lastUpdateTime = t;
                prevAngle = currentAngle;
            }
        }

        public void UpdateAngle (BaseEventData data) {
            PointerEventData d = (PointerEventData)data;
            Vector2 dir = d.position - new Vector2(Pointer.position.x, Pointer.position.y);
            Pointer.up = dir;
            var angles = Pointer.eulerAngles;
            if (angles.z > 180f) {
                angles.z -= 360f;
            }
            angles.z = Mathf.Clamp (angles.z, MinValue, MaxValue);
			Pointer.eulerAngles = angles;
        }

		public void SetAngle (float angle) {
			Log ($"AngularPointerComponent.SetAngle {angle}");
			var angles = Pointer.eulerAngles;
			angles.z = Mathf.Clamp (angle, MinValue, MaxValue);
			Pointer.eulerAngles = angles;
		}
    }

}
