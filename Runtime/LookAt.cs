using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	public class LookAt : MonoBehaviour, IGameComponentIcon {
		[Header ("Simple LookAt controller which will orientate the GameObject towards Target")]
		public Transform Target;
		[Tooltip ("If true, GameObject will 'lookAt' main camera instead of Target")]
		public bool TargetIsMainCamera;
		[Tooltip ("If true, GameObject will use Target's up vector. If false, GameObject will use world-space up")]
		public bool UseTargetUp;
		[Tooltip ("If true, up vector will be inverted")]
		public bool NegateUp;
		[Tooltip ("If Speed > 0 then OnReachedTarget will trigger when angle is smaller than threshold. OnInterpolating will trigger when angle is bigger.")]
		public float AngleThreshold = 2f;

		[Header("How fast to interpolate. Zero = snap")]
		public float Speed = 0f;

		public UnityEvent OnReachedTarget;
		public UnityEvent OnInterpolating;

		private bool IsInterpolating;

		void Update () {
			var target = TargetIsMainCamera ? Camera.main.transform : Target;
			var up = UseTargetUp ? target.up : Vector3.up;
			if (NegateUp) {
				up = -up;
			}
			if (Speed < float.Epsilon) {
				transform.LookAt (target, up);
			} else {
				var deltaRot = Quaternion.LookRotation (target.position - transform.position, up);
				if (Quaternion.Angle (transform.rotation, deltaRot) > AngleThreshold) {
					if (!IsInterpolating) {
						IsInterpolating = true;
						OnInterpolating.Invoke ();
					}
				} else if (IsInterpolating) {
					IsInterpolating = false;
					OnReachedTarget.Invoke ();
				}
				transform.rotation = Quaternion.Slerp (transform.rotation, deltaRot, Time.deltaTime * Speed);
			}
		}
	}
}
