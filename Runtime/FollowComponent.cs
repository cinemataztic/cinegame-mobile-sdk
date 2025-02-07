using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Follow other object smoothly. The ObjectToFollow will always stay inside the Followzone, and can move inside the Deadzone without the following object moving. You can optionally orientate the transform towards the target (LookAt)")]
	public class FollowComponent : BaseComponent {

		public Transform ObjectToFollow;
		public bool LookAt = true;
		[Tooltip ("The inner box where the follower will not move so long as the ObjectToFollow is inside this")]
		public BoxCollider Deadzone;
		[Tooltip ("The outer box where the follower will smoothly follow the ObjectToFollow until it is again inside the Deadzone")]
		public BoxCollider Followzone;
		[Tooltip ("Threshold to prevent creeping. Should be adjusted according to world scale")]
		public float MoveThreshold = .003f;

		void LateUpdate () {
			if (LookAt) {
				transform.LookAt (ObjectToFollow);
			}

			var pos = Followzone.transform.InverseTransformPoint (ObjectToFollow.position);
			var deadzoneBounds = new Bounds (Deadzone.center, Deadzone.size);
			var followzoneBounds = new Bounds (Followzone.center, Followzone.size);

			if (!deadzoneBounds.Contains (pos)) {
				var v = Vector3.zero;
				if (pos.x < deadzoneBounds.min.x) {
					v.x = -(deadzoneBounds.min.x - pos.x) / (deadzoneBounds.min.x - followzoneBounds.min.x);
				} else if (pos.x > deadzoneBounds.max.x) {
					v.x =  (deadzoneBounds.max.x - pos.x) / (deadzoneBounds.max.x - followzoneBounds.max.x);
				}
				if (pos.y < deadzoneBounds.min.y) {
					v.y = -(deadzoneBounds.min.y - pos.y) / (deadzoneBounds.min.y - followzoneBounds.min.y);
				} else if (pos.y > deadzoneBounds.max.y) {
					v.y =  (deadzoneBounds.max.y - pos.y) / (deadzoneBounds.max.y - followzoneBounds.max.y);
				}
				if (pos.z < deadzoneBounds.min.z) {
					v.z = -(deadzoneBounds.min.z - pos.z) / (deadzoneBounds.min.z - followzoneBounds.min.z);
				} else if (pos.z > deadzoneBounds.max.z) {
					v.z =  (deadzoneBounds.max.z - pos.z) / (deadzoneBounds.max.z - followzoneBounds.max.z);
				}
				v.x *= v.x * v.x;
				v.y *= v.y * v.y;
				v.z *= v.z * v.z;

				v.x = Mathf.Clamp (v.x, -1f, 1f);
				v.y = Mathf.Clamp (v.y, -1f, 1f);
				v.z = Mathf.Clamp (v.z, -1f, 1f);

				v = Followzone.transform.TransformDirection (v);
				if (Mathf.Abs (v.x) < MoveThreshold) v.x = 0f;
				if (Mathf.Abs (v.y) < MoveThreshold) v.y = 0f;
				if (Mathf.Abs (v.z) < MoveThreshold) v.z = 0f;

				transform.position += v;
			}
		}
	}
}