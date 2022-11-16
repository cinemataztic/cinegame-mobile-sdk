using UnityEngine;
using UnityEngine.EventSystems;

namespace CineGame.MobileComponents {

	public class GravityComponent : MonoBehaviour, IGameComponentIcon {
		[Header ("Apply gravity to the entire scene according to mobile device gyro.")]

		public float GravityScale = 20f;

		private Vector3 smoothedAcceleration = Vector3.zero;

		void FixedUpdate () {
			var force = Input.acceleration * GravityScale;
			force.z = -force.z;

			smoothedAcceleration = Vector3.Lerp (smoothedAcceleration, force, 0.5f);

			var dif = force - smoothedAcceleration;
			force *= Mathf.Max (1f, dif.magnitude);

			Physics.gravity = force;
		}
	}

}