using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {
	[ComponentReference ("Utility for making dice games. Will apply a random force to this GameObject and when the die stops moving (below thresholds) the result is replicated to host.")]
	public class ThrowComponent : ReplicatedComponent {

	    [SerializeField] float ThrowForce = 200f;
		[SerializeField] float ThrowTorque = 200f;
		[Tooltip("Minimum time from throws starts to result is calculated.")]
		[SerializeField] float ThrowMinimumTime = 1f;
		[SerializeField] float ThrowStoppedLinearThreshold = 0.02f;
		[SerializeField] float ThrowStoppedAngularThreshold = 0.02f;
		[Serializable] public class ResultEvent : UnityEvent<int> { }
		public ResultEvent onStopped;

		[Header("Replication")]
		[Tooltip("Object Message Key (integer) which side was up when throw finished.")]
		[SerializeField] string ResultKey = "eyes";

		float lastThrowTime = 0f;

#if UNITY_EDITOR
		void OnGUI () {
			GUI.skin.label.fontSize = 20;
			GUI.skin.button.fontSize = 20;
			GUI.Label (new Rect (10, 10, 200, 40), GetCurrentDieEyes ().ToString ());
	/*		if (GUI.Button (new Rect (10, 50, 200, 40), "Throw")) {
				ThrowDie ();
			}
	*/	}
#endif

		void FixedUpdate () {
			var rb = GetComponent<Rigidbody> ();
			if (lastThrowTime != 0f 
				&& (lastThrowTime + ThrowMinimumTime) < Time.time
#if UNITY_6000_0_OR_NEWER
				&& rb.linearVelocity.sqrMagnitude < ThrowStoppedLinearThreshold
#else
				&& rb.velocity.sqrMagnitude < ThrowStoppedLinearThreshold
#endif
				&& rb.angularVelocity.sqrMagnitude < ThrowStoppedAngularThreshold) {
				//A minimum of n seconds has passed, and die is nearly still, let's count dots!
				lastThrowTime = 0f;
				var eyes = GetCurrentDieEyes ();
				Send (ResultKey, eyes);
				Log ($"ThrowComponent.OnStopped\n{Util.GetEventPersistentListenersInfo (onStopped)}");
				onStopped.Invoke (eyes);
			}
			//Something else has taken control (eg Interpolator)? Then cancel throw... Not sure exactly how to handle this
			if (rb.isKinematic) {
				lastThrowTime = 0f;
			}
		}

		int GetCurrentDieEyes () {
			var oneDir = transform.TransformDirection (0f,0f,-1f);
			var twoDir = transform.right;
			var threeDir = transform.up;
			if (oneDir.z < -.6f) {
				return 1;
			} else if (oneDir.z > .6f) {
				return 6;
			} else if (twoDir.z < -.6f) {
				return 2;
			} else if (twoDir.z > .6f) {
				return 5;
			} else if (threeDir.z < 0f) {
				return 3;
			}
			return 4;
		}

		public void Throw () {
			if (lastThrowTime == 0f) {
				lastThrowTime = Time.time;
				var rb = GetComponent<Rigidbody> ();
				rb.isKinematic = false;
				var randomForce2D = new Vector2 (UnityEngine.Random.Range (-1f, 1f), UnityEngine.Random.Range (-1f, 1f));
				randomForce2D = (randomForce2D.sqrMagnitude > .0001f) ? randomForce2D.normalized : Vector2.up;
				randomForce2D *= ThrowForce;
				rb.AddForce (new Vector3 (randomForce2D.x, randomForce2D.y, -.3f * ThrowForce));
				var randomTorque3D = new Vector3 (UnityEngine.Random.Range (-1f, 1f), UnityEngine.Random.Range (-1f, 1f), UnityEngine.Random.Range (-1f, 1f));
				rb.AddTorque (randomTorque3D * ThrowTorque);
			}
		}
	}
}