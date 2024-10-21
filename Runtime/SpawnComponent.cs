using System.Collections.Generic;
using Sfs2X.Entities.Data;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	[ComponentReference ("Spawn a prefab as a child of this Transform, with same position and orientation. An optional impulse can be applied by triggering the Impulse methods. A maximum amount of spawns can be set (and a reload method can be invoked).\n\nYou can spawn objects from the game host by sending Key='suffix', the suffix being some arbitrary string that will be appended to the GameObject's name (or if null or empty, no suffix will be appended).\nIf the suffix from host parses as an integer, the transform will be insert-sorted by suffix with its siblings (treated as an ordered list).\nYou can set properties on instances remotely by using special keys '_spawn' and '_instance' to route by SpawnComponent and instance name.")]
	public class SpawnComponent : ReplicatedComponent {

		public GameObject Prefab;

		[Tooltip("How many spawns (0=infinite)")]
		public int Capacity = 0;

		[Tooltip("Aligns impulses to this transform if set")]
		public Transform ImpulseAlign;

		[Tooltip("World units to randomize impulse position")]
		public float RandomImpulsePosition = 0f;

		[Tooltip("Key in object message to listen for. The string value contains a suffix ID which can be filtered on")]
		public string Key;

		[Tooltip("Invoked with the newly spawned GameObject")]
		public UnityEvent<GameObject> OnSpawn;

		[Tooltip("When the capacity is reached, this is invoked instead of spawning. Can eg activate a Reload button, or a Game Over text")]
		public UnityEvent OnEmpty;

		GameObject Current;
		int numSpawns=0;

		/// <summary>
		/// Remember to test for null in case an instance has already been destroyed!
		/// </summary>
		readonly List<GameObject> Instances = new ();

		/// <summary>
        /// Transient buffer for replicating object messages in children
        /// </summary>
		readonly List<ReplicatedComponent> replicatedComponents = new ();

		/// <summary>
        /// Spawn an instance at this position and rotation
        /// </summary>
		public void Spawn () {
			Log ($"Spawn {Prefab.name}");
			Spawn (transform.position, transform.rotation);
		}

		/// <summary>
        /// Spawn an instance at the position and rotation of the specified transform
        /// </summary>
		public void SpawnAt (Transform sourceTransform) {
			Log ($"SpawnAt {Prefab.name} {sourceTransform.position} {sourceTransform.eulerAngles}");
			Spawn (sourceTransform.position, sourceTransform.rotation);

			var rbSrc = sourceTransform.GetComponentInParent<Rigidbody> ();
			if (rbSrc != null) {
				var rbDst = Current.GetComponentInChildren<Rigidbody> ();
				if (rbDst != null) {
#if UNITY_6000_0_OR_NEWER
					rbDst.linearVelocity = rbSrc.linearVelocity;
#else
					rbDst.velocity = rbSrc.velocity;
#endif
				}
			}
		}

		/// <summary>
        /// Spawn an instance at a specific world position
        /// </summary>
		public void SpawnAt (Vector3 worldPosition) {
			Log ($"SpawnAt {Prefab.name} {worldPosition}");
			Spawn (worldPosition, transform.rotation);
		}

		private GameObject Spawn (Vector3 worldPosition, Quaternion worldRotation, bool invoke=true) {
			if (Capacity != 0 && numSpawns >= Capacity) {
				Log ($"Spawn.OnEmpty\n{Util.GetEventPersistentListenersInfo (OnEmpty)}");
				OnEmpty.Invoke ();
				return null;
			} else {
				Log ($"SpawnAt {Prefab.name} {worldPosition} OnSpawn\n{Util.GetEventPersistentListenersInfo (OnSpawn)}");
				Current = Instantiate (Prefab, transform);
				Current.transform.SetPositionAndRotation (worldPosition, worldRotation);
				if (invoke)
					OnSpawn.Invoke (Current);
				numSpawns++;
				return Current;
			}
		}

		/// <summary>
        /// Destroy all spawned instances
        /// </summary>
		public void DestroyInstances () {
			var i = 0;
			foreach (var go in Instances) {
				if (go != null) {
					Destroy (go);
					i++;
				}
			}
			Instances.Clear ();
			Log ($"DestroyInstances destroyed {i} instances");
		}

		/// <summary>
        /// Reload to full capacity
        /// </summary>
		public void Reload () {
			Log ("SpawnComponent.Reload");
			numSpawns = 0;
		}

		/// <summary>
        /// Set "magazine" capacity to specific number
        /// </summary>
		public void SetCapacity (int newCapacity) {
			Log ($"SpawnComponent.SetCapacity ({newCapacity})");
			Capacity = newCapacity;
		}

		/// <summary>
        /// Apply a 2D impulse to the physics object of the last spawned instance. If the object is not 2D, the impulse is aligned with the X and Y axis of this object.
        /// </summary>
		public void Impulse (Vector2 force) {
			if (Current.TryGetComponent<Rigidbody2D>(out var rb2d)) {
				Log ($"SpawnComponent.Impulse 2D ({force})");
				rb2d.AddForce (force, ForceMode2D.Impulse);
			} else {
				var t = (ImpulseAlign != null) ? ImpulseAlign : transform;
				Impulse (force.x * t.right + force.y * t.up);
			}
		}

		/// <summary>
		/// Apply a physics 3D impulse to last spawned instance
		/// </summary>
		public void Impulse (Vector3 force) {
			var t = (ImpulseAlign != null)? ImpulseAlign : this.transform;
			var pos = t.position + Random.insideUnitSphere * RandomImpulsePosition;
			Log ($"SpawnComponent.Impulse ({force}) at position ({pos})");
			Current.GetComponent<Rigidbody> ().AddForceAtPosition (force, pos, ForceMode.Impulse);
		}

		/// <summary>
		/// Support spawning objects remotely. If value is non-null, it is treated as a suffix which will be appended to the GameObject's name (eg '1' = 'GameObjectClone_1').
		/// If the suffix can be parsed as an integer, the transform is insert-sorted among the siblings, assuming that an ordered list is desired.
		/// </summary>
		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				var go = Spawn (transform.position, transform.rotation, invoke: false);
				if (go == null)
					return;

				var insertSorted = false;
				var goName = dataObj.GetUtfString (Key);
				if (!string.IsNullOrWhiteSpace (goName)) {
					go.name = goName;

					//if name is suffixed with a '_[int]', we insert-sort the transform (assuming an ordered list is desired)
					var lastIndexOf_ = goName.LastIndexOf ('_');
					if (lastIndexOf_ > 0 && int.TryParse (goName.Substring (lastIndexOf_ + 1), out int index)) {
						for (int i = 0; i < transform.childCount; i++) {
							var siblingName = transform.GetChild (i).name;
							lastIndexOf_ = siblingName.LastIndexOf ('_');
							if (lastIndexOf_ > 0 && int.TryParse (siblingName.Substring (lastIndexOf_ + 1), out int sibIndex) && sibIndex > index) {
								go.transform.SetSiblingIndex (i);
								Log ($"Spawned from host insert-sorted at {i}: {go.GetScenePath ()}");
								insertSorted = true;
								break;
							}
						}
					}
				}
				if (!insertSorted) {
					Log ($"Spawned from host: {go.GetScenePath ()}");
				}

				go.transform.GetComponentsInChildren (includeInactive: true, replicatedComponents);
				foreach (var rc in replicatedComponents) {
					//Init replication for ReplicatedComponent instance
					rc.InitReplication ();
				}

				Log ($"OnSpawn\n{Util.GetEventPersistentListenersInfo (OnSpawn)}");
				OnSpawn.Invoke (go);
			}

			if (dataObj.ContainsKey ("_instance")) {
				var instance = dataObj.GetSFSObject ("_instance");
				var instanceName = instance.GetUtfString ("_name");
				var t = transform.Find (instanceName);
				if (t == null) {
					LogError ($"SpawnComponent instance not found: " + instanceName);
				} else {
					instance.RemoveElement ("_name");
					if (Util.IsDevModeActive) {
						Log ($"Routing instance properties to {t.gameObject.GetScenePath ()} : {instance.GetDump ()}");
					}
					t.GetComponentsInChildren (includeInactive: true, replicatedComponents);
					foreach (var rc in replicatedComponents) {
						rc.OnObjectMessage (instance, senderId);
					}
				}
			}
		}
	}
}
