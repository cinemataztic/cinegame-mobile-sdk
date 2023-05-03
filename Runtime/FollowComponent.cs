using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	[ComponentReference ("Follow other object smoothly. The object will always stay inside the followzone, and can move inside the deadzone without the following object moving. You can optionally orientate the transform towards the target (LookAt)")]
	public class FollowComponent : BaseComponent {

		public Transform ObjectToFollow;
		public bool LookAt = true;
		public BoxCollider Deadzone;
		public BoxCollider Followzone;

		void LateUpdate () {
			if (LookAt) {
				transform.LookAt (ObjectToFollow);
			}

			var pos = transform.InverseTransformPoint (ObjectToFollow.position);
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

				transform.position += transform.TransformDirection (v);
			}
		}
	}
}