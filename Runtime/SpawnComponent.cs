using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	[ComponentReference ("Utility for spawning a prefab as a child of this Transform, with same position and orientation. An optional impulse can be applied by triggering the Impulse methods. A maximum amount of spawns can be set.")]
	public class SpawnComponent : BaseComponent {

		public GameObject Prefab;
		public float RespawnDelay = 0.3f;
		[Tooltip("How many spawns (0=infinite)")]
		public int Capacity = 0;

		[Tooltip("Aligns impulses to this transform if set")]
		public Transform ImpulseAlign;

		[Tooltip("World units to randomize impulse position")]
		public float RandomImpulsePosition = 0f;

		public UnityEvent OnSpawn;
		public UnityEvent OnEmpty;

		GameObject Current;
		int numSpawns=0;

		void Respawn () {
			Invoke ("Spawn", RespawnDelay);
		}

		public void Spawn () {
			if (Capacity != 0 && numSpawns >= Capacity) {
				OnEmpty.Invoke ();
			} else {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.LogFormat ("{0} Spawn", Util.GetObjectScenePath (gameObject));
#endif
				Current = Instantiate (Prefab, transform);
				Current.transform.localPosition = Vector3.zero;
				Current.transform.localRotation = Quaternion.identity;
				OnSpawn.Invoke ();
				numSpawns++;
			}
		}

		public void Reload () {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.LogFormat ("{0} Reload", Util.GetObjectScenePath (gameObject));
#endif
			numSpawns = 0;
		}

		public void SetCapacity (int newCapacity) {
			Capacity = newCapacity;
		}

		public void Impulse (Vector2 force) {
			var t = (ImpulseAlign != null)? ImpulseAlign : this.transform;
			var rb2d = Current.GetComponent<Rigidbody2D> ();
			if (rb2d != null) {
				rb2d.AddForce (force, ForceMode2D.Impulse);
				Respawn ();
			} else {
				Impulse (force.x * t.right + force.y * t.up);
			}
		}

		public void Impulse (Vector3 force) {
			var t = (ImpulseAlign != null)? ImpulseAlign : this.transform;
			Current.GetComponent<Rigidbody> ().AddForceAtPosition (force, t.position + Random.insideUnitSphere * RandomImpulsePosition, ForceMode.Impulse);
			Respawn ();
		}
	}
}
