using System.Collections.Generic;
using UnityEngine;

namespace CineGame.MobileComponents {
    /// <summary>
    /// High performance controller for spawning finite effects in world space locations. The effect can be eg a particle system, a decal projector or a one-shot animation.
    /// </summary>
    [ComponentReference ("High performance controller for spawning finite effects in world space locations. The effect can be eg a particle system, a decal projector or a one-shot animation.")]
    public class EffectController : BaseComponent {
        public GameObject Prefab;

        [Tooltip ("Duration of effect. If 0 then duration will be taken from ParticleSystem or Animator inside Prefab")]
        public float Duration;

        [Tooltip ("Number of instances which will be pre-initialized for performance")]
        public int NumInstances = 20;

        float [] timeToKill;
        Transform [] transforms;
        GameObject [] gameObjects;

        /// <summary>
        /// Dictionary of all instances, indexed by prefab name
        /// </summary>
        static readonly Dictionary<string, EffectController> EffectControllers = new ();

        void Awake () {
            transform.SetPositionAndRotation (Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            timeToKill = new float [NumInstances];

            if (Prefab == null)
                return;
            EffectControllers.Add (Prefab.name, this);

            Log ($"EffectController Setting up a stack of {NumInstances} {Prefab.name} instances at {gameObject.GetScenePath ()}");

            var effectDuration = SetupEffect (Prefab, NumInstances);
            Duration = (Duration > float.Epsilon) ? Duration : effectDuration;
        }

        /// <summary>
        /// Sets up a number of deactivated instance of the prefab, ready to be moved and activated when needed. Returns duration from ParticleSystem or Animator, defaults to 1 sec if none found
        /// </summary>
        float SetupEffect (GameObject prefab, int numInstances) {
            transforms = new Transform [numInstances];
            gameObjects = new GameObject [numInstances];
            var parent = transform;
            for (int i = 0; i < numInstances; i++) {
                var go = Instantiate (prefab, parent);
                go.SetActive (false);
                gameObjects [i] = go;
                transforms [i] = go.transform;
            }
            var ps = prefab.GetComponentInChildren<ParticleSystem> ();
            if (ps != null) {
                Log ($"EffectController ParticleSystem main duration of {prefab.name}: {ps.main.duration}");
                return ps.main.duration;
            }
            var animator = prefab.GetComponentInChildren<Animator> ();
            if (animator != null) {
                var animStateInfo = animator.GetNextAnimatorStateInfo (0);
                Log ($"EffectController Animator duration of {prefab.name}: {animStateInfo.length}");
                return animStateInfo.length;
            }
            Debug.LogWarning ("EffectController Returning default duration of 1 for effect " + prefab.name);
            return 1f;
        }

        void Update () {
            var _t = Time.time;
            for (int i = 0; i < NumInstances; i++) {
                if (timeToKill [i] < _t) {
                    timeToKill [i] = float.MaxValue;
                    gameObjects [i].SetActive (false);
                    //Log ($"Deactivated instance of {Prefab.name} at {transforms [i].position}");
                }
            }
        }

        /// <summary>
        /// Spawn an effect at the specified transform's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Transform location) {
            Spawn (location.position, location.rotation);
        }

        /// <summary>
        /// Spawn an effect at the specified gameobject's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (GameObject location) {
            Spawn (location.transform.position, location.transform.rotation);
        }

        /// <summary>
        /// Spawn an effect at the specified world position. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Vector3 worldPosition) {
            Spawn (worldPosition, Quaternion.identity);
        }

        /// <summary>
        /// Find first inactive instance of prefab and move it to given world position and rotation. If no inactive instance available, warn in the log and hijack the oldest active.
        /// </summary>
        public void Spawn (Vector3 position, Quaternion rotation) {
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
                Debug.LogWarning ($"{name} EffectController exhausted. You should increase NumInstance next time round!");
                //Debug.Break ();
            }

            timeToKill [min_i] = Time.time + Duration;
            transforms [min_i].SetLocalPositionAndRotation (position, rotation);
            gameObjects [min_i].SetActive (true);
            //Log ($"Spawned instance of {Prefab.name} at {position}");
        }

        /// <summary>
        /// Static interface to find an active EffectController for the specified prefab
        /// </summary>
        public static EffectController GetController (string prefabName) {
            if (!EffectControllers.TryGetValue (prefabName, out EffectController ec)) {
                Debug.LogError ("No active EffectController for " + prefabName);
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