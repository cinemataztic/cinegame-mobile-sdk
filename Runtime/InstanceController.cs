using System.Collections.Generic;
using UnityEngine;

namespace CineGame.MobileComponents {
    /// <summary>
    /// High performance controller for spawning finite instances in world space locations. If the prefab is a particle system or a one-shot animation, the duration of the instance (time to deactivate) is taken from the duration set in that component.
    /// </summary>
    [ComponentReference ("High performance controller for spawning finite instances in world space locations. If the prefab is a particle system, or a one-shot animation, the duration of the instance (time to deactivate) is taken from the duration set in that component.")]
    public class InstanceController : MonoBehaviour {
        public bool Verbose;

        public GameObject Prefab;

        [Tooltip ("Default duration of instances. If 0 then duration will be taken from ParticleSystem or Animator inside Prefab")]
        public float Duration;

        [Tooltip ("Number of instances which will be pre-initialized for performance")]
        public int NumInstances = 20;

        float [] timeToKill;
        Transform [] transforms;
        GameObject [] gameObjects;

        /// <summary>
        /// Dictionary of all instances, indexed by prefab name
        /// </summary>
        static readonly Dictionary<string, InstanceController> InstanceControllers = new ();

        void Log (string message) {
            if (Verbose) {
                Debug.Log (message, this.gameObject);
            }
        }

        void Awake () {
            transform.SetPositionAndRotation (Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            timeToKill = new float [NumInstances];

            if (Prefab == null)
                return;
            InstanceControllers.Add (Prefab.name, this);

            Log ($"InstanceController Setting up a stack of {NumInstances} {Prefab.name} instances at {gameObject.GetScenePath ()}");

            var effectDuration = Setup (Prefab, NumInstances);
            Duration = (Duration > float.Epsilon) ? Duration : effectDuration;
        }

        void OnDestroy () {
            InstanceControllers.Remove (Prefab.name);
        }

        /// <summary>
        /// Sets up a number of deactivated instances of the prefab, ready to be moved and activated when needed. Returns duration from ParticleSystem or Animator, defaults to 1 sec if none found
        /// </summary>
        float Setup (GameObject prefab, int numInstances) {
            transforms = new Transform [numInstances];
            gameObjects = new GameObject [numInstances];
            var parent = transform;
            for (int i = 0; i < numInstances; i++) {
                var go = Instantiate (prefab, parent);
                go.SetActive (false);
                gameObjects [i] = go;
                transforms [i] = go.transform;
            }
            if (Duration < float.Epsilon) {
                var ps = prefab.GetComponentInChildren<ParticleSystem> ();
                if (ps != null) {
                    Log ($"InstanceController ParticleSystem main duration of {prefab.name}: {ps.main.duration}");
                    return ps.main.duration;
                }
                var animator = prefab.GetComponentInChildren<Animator> ();
                if (animator != null) {
                    var animStateInfo = animator.GetNextAnimatorStateInfo (0);
                    Log ($"InstanceController Animator duration of {prefab.name}: {animStateInfo.length}");
                    return animStateInfo.length;
                }
                Debug.LogWarning ($"InstanceController No default duration for prefab " + prefab.name, gameObject);
            }
            return Duration;
        }

        void Update () {
            var _t = Time.time;
            for (int i = 0; i < NumInstances; i++) {
                if (timeToKill [i] <= _t) {
                    timeToKill [i] = float.MaxValue;
                    gameObjects [i].SetActive (false);
                    //Log ($"Deactivated instance of {Prefab.name} at {transforms [i].position}");
                }
            }
        }

        /// <summary>
        /// Activate an instance at the specified transform's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Transform location) {
            Spawn (location.position, location.rotation);
        }

        /// <summary>
        /// Activate an instance at the specified gameobject's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (GameObject location) {
            Spawn (location.transform.position, location.transform.rotation);
        }

        /// <summary>
        /// Activate an instance at the specified world position. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Vector3 worldPosition) {
            Spawn (worldPosition, Quaternion.identity);
        }

        /// <summary>
        /// Find first inactive instance of prefab and move it to given world position and rotation. If no inactive instance available, warn in the log and hijack the oldest active.
        /// </summary>
        public void Spawn (Vector3 position, Quaternion rotation) {
            var index = GetInactiveIndex ();
            timeToKill [index] = Time.time + Duration;
            transforms[index].SetLocalPositionAndRotation (position, rotation);
            //Log ($"Spawned instance of {Prefab.name} at {position}");
        }

        public GameObject GetInstance () {
            var index = GetInactiveIndex ();
            timeToKill [index] = float.MaxValue;
            return gameObjects [index];
        }

        public void FreeInstance (GameObject instance) {
            var l = gameObjects.Length;
            for (int i = 0; i < l; i++) {
                if (gameObjects [i] == instance) {
                    timeToKill [i] = float.MaxValue;
                    gameObjects [i].SetActive (false);
                    break;
                }
            }
        }

        int GetInactiveIndex () {
            var min_i = 0;
            var min_ttk = float.MaxValue;
            var i = 0;
            for (; i < NumInstances; i++) {
                var ttk = timeToKill [i];
                if (ttk == float.MaxValue) {
                    min_i = i;
                    break;
                }
                if (min_ttk > ttk) {
                    min_ttk = ttk;
                    min_i = i;
                }
            }
            if (i == NumInstances) {
                Debug.LogWarning ($"{name} InstanceController exhausted. You should increase NumInstance next time round!");
                //Debug.Break ();
            }
            gameObjects[min_i].SetActive(true);
            return min_i;
        }

        /// <summary>
        /// Static interface to find an active InstanceController for the specified prefab
        /// </summary>
        public static InstanceController GetController (string prefabName) {
            if (!InstanceControllers.TryGetValue (prefabName, out InstanceController ec)) {
                Debug.LogError ("No active InstanceController for " + prefabName);
                return null;
            }
            return ec;
        }

        /// <summary>
        /// Static interface to activate an instance of a named prefab at the given world position and rotation
        /// </summary>
        public static void Spawn (string prefabName, Vector3 position, Quaternion rotation) {
            var ec = GetController (prefabName);
            if (ec != null) {
                ec.Spawn (position, rotation);
            }
        }
    }
}
