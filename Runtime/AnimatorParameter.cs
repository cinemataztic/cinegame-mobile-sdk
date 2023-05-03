using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Component to proxy parameters or IK values into a specific Animator
	/// </summary>
	[ComponentReference ("Feed Parameter or IK values into an Animator")]
	public class AnimatorParameter : BaseComponent {

		[Tooltip ("If no Animator is referenced, searches for Animator on this GameObject")]
		public Animator Animator;
		[Tooltip ("Parameter which receives float, integer or bool")]
		[SerializeField]
		private string ParameterName;
		[Tooltip ("IK Goal which receives position or rotation")]
		public AvatarIKGoal AvatarIkGoal;
		[Tooltip ("IK Hint which receives position")]
		public AvatarIKHint AvatarIkHint;

		void Start () {
			if (Animator == null) {
				Animator = GetComponent<Animator> ();
			}
		}

		public void SetFloat (float value) {
			Log ("{0} AnimatorComponent.SetFloat {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetFloat (ParameterName, value);
		}

		public void SetBool (bool value) {
			Log ("{0} AnimatorComponent.SetBool {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetBool (ParameterName, value);
		}

		public void SetInteger (int value) {
			Log ("{0} AnimatorComponent.SetInteger {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetInteger (ParameterName, value);
		}

		//////////////
		/// IK support

		public void SetIKPosition (Vector3 value) {
			Log ("{0} AnimatorComponent.SetIKPosition {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetIKPosition (AvatarIkGoal, value);
		}

		public void SetIKRotation (Quaternion value) {
			Log ("{0} AnimatorComponent.SetIKRotation {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetIKRotation (AvatarIkGoal, value);
		}

		public void SetIKPositionWeight (float value) {
			Log ("{0} AnimatorComponent.SetIKPositionWeight {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetIKPositionWeight (AvatarIkGoal, value);
		}

		public void SetIKRotationWeight (float value) {
			Log ("{0} AnimatorComponent.SetIKRotationWeight {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetIKRotationWeight (AvatarIkGoal, value);
		}

		public void SetIKHintPosition (Vector3 value) {
			Log ("{0} AnimatorComponent.SetIKHintPosition {1}={2}", Animator.gameObject.GetScenePath (), ParameterName, value);
			Animator.SetIKHintPosition (AvatarIkHint, value);
		}
	}
}