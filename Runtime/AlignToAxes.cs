using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	/// <summary>
	/// This script will align this gameobject to the closest axis of the transform specified in AlignObject, either snapping or interpolating.
	/// This can be used to eg zoom in on a die that has been cast.
	/// If Reposition is true, the position of the object will also be moved.
	/// </summary>
	[ComponentReference ("Align this transform with the AlignObject transform. Optionally reposition to AlignObject position. You can chose to align the closes axis, or you can set an interpolation time (if you set it to 0 then the transform will snap into position)")]
	public class AlignToAxes : BaseComponent {

		[Tooltip ("The transform to align object to")]
		public Transform AlignObject;

		[Tooltip ("If true then the closest axes will be chosen, eg Z->X rather than aligning Z->Z")]
		public bool ClosestAxes = true;

		[Tooltip ("If true then a call to Snap or Interpolate will also align position")]
		public bool Reposition = false;

		public Interpolation.Type InterpType = Interpolation.Type.EaseInOutCubic;

		[Tooltip ("This can be overridden by calling the specific Interpolate (float)")]
		public float InterpTime = 0.3f;

		[Tooltip ("Event fired when interpolation is finished and object is aligned")]
		public UnityEvent OnAligned;

		/// <summary>
		/// Find closest axis for minimal rotation
		/// </summary>
		static Vector3 GetAxis (Vector3 inDir) {
			Vector3 outAxis;
			float absx = Mathf.Abs (inDir.x);
			float absy = Mathf.Abs (inDir.y);
			float absz = Mathf.Abs (inDir.z);
			if (absx > absy && absx > absz) {
				if (inDir.x < 0f) {
					outAxis = Vector3.left;
				} else {
					outAxis = Vector3.right;
				}
			} else if (absy > absx && absy > absz) {
				if (inDir.y < 0f) {
					outAxis = Vector3.down;
				} else {
					outAxis = Vector3.up;
				}
			} else {
				if (inDir.z < 0f) {
					outAxis = Vector3.back;
				} else {
					outAxis = Vector3.forward;
				}
			}
			/*		if (inDir.z < -.6f) {
						outAxis = Vector3.back;
					} else if (inDir.z > .6f) {
						outAxis = Vector3.forward;
					} else if (inDir.y < -.6f) {
						outAxis = Vector3.down;
					} else if (inDir.y > .6f) {
						outAxis = Vector3.up;
					} else if (inDir.x < 0f) {
						outAxis = Vector3.left;
					} else {
						outAxis = Vector3.right;
					}
			*/
			return outAxis;
		}


		/// <summary>
		/// Find the quaternion that rotates from current orientation to the desired orientation according to which axis is closest.
		/// </summary>
		Quaternion GetLookRotation (Quaternion destRotation) {
			var invDestRot = Quaternion.Inverse (destRotation);
			var oneDir = invDestRot * -transform.forward;//.TransformDirection (0f,0f,-1f);
			var threeDir = invDestRot * transform.up;
			var lookDir = destRotation * (ClosestAxes ? GetAxis (oneDir) : Vector3.back);
			var upDir = destRotation * (ClosestAxes ? GetAxis (threeDir) : Vector3.up);
			var qRot = Quaternion.LookRotation (-lookDir, upDir);
			return qRot;
		}

		void DisablePhysicsIfPresent () {
			var rb = GetComponent<Rigidbody> ();
			if (rb != null) {
				Log ("AlignToAxes Disable physics");
				rb.isKinematic = true;
				rb.angularVelocity =
#if UNITY_6000_0_OR_NEWER
					rb.linearVelocity =
#else
					rb.velocity =
#endif
					Vector3.zero;
			}
		}

		/// <summary>
		/// Set 'AlignObject' property
		/// </summary>
		public void SetAlignObject (Transform t) {
			AlignObject = t;
		}

		/// <summary>
		/// Set 'AlignObject' property
		/// </summary>
		public void SetAlignObject (GameObject v) {
			AlignObject = v.transform;
		}

		/// <summary>
		/// Align with 'AlignObject' instantly-- and move to it if specified by 'Reposition'
		/// </summary>
		public void Snap () {
			if (!enabled)
				return;
			Log ("AlignToAxes.Snap");
			DisablePhysicsIfPresent ();
			transform.rotation = GetLookRotation (AlignObject.rotation);
			if (Reposition) {
				transform.position = AlignObject.position;
			}
		}

		/// <summary>
		/// Set 'AlignObject' property and align with it instantly
		/// </summary>
		public void Snap (GameObject alignObject) {
			AlignObject = alignObject.transform;
			Snap ();
		}

		/// <summary>
		/// Smoothly interpolates to align with the 'AlignObject' over the time constant specified
		/// </summary>
		public void Interpolate (float time) {
			if (!enabled)
				return;
			StartCoroutine (E_Interp (time));
		}

		/// <summary>
		/// Smoothly interpolates to align with the 'AlignObject' over the time given by 'InterpTime' seconds
		/// </summary>
		public void Interpolate () {
			Interpolate (InterpTime);
		}

		/// <summary>
		/// Smoothly interpolates to align with the specified object over the time given by 'InterpTime' seconds
		/// </summary>
		public void Interpolate (GameObject alignObject) {
			AlignObject = alignObject.transform;
			Interpolate (InterpTime);
		}

		IEnumerator E_Interp (float time) {
			Log ("AlignToAxes.Interpolate time={time}");
			if (time > float.Epsilon) {
				var startRotation = transform.rotation;
				var startPosition = transform.position;
				float t = 0f;
				float dt = 1f / time;
				while (t < 1f) {
					DisablePhysicsIfPresent ();
					float tCubic = Interpolation.Interp (t, InterpType);
					transform.rotation = Quaternion.Slerp (startRotation, GetLookRotation (AlignObject.rotation), tCubic);
					if (Reposition) {
						transform.position = Vector3.Lerp (startPosition, AlignObject.position, tCubic);
					}
					t += Time.deltaTime * dt;
					yield return null;
				}
			}
			transform.rotation = GetLookRotation (AlignObject.rotation);
			if (Reposition) {
				transform.position = AlignObject.position;
			}
			Log ("AlignToAxes.OnAligned\n{Util.GetEventPersistentListenersInfo (OnAligned)}");
			OnAligned.Invoke ();
		}
	}
}