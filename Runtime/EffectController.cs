using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CineGame.MobileComponents {
    /// <summary>
    /// High performance controller for spawning finite effects in world space locations. The effect can either be a particle system or an animation.
    /// </summary>
    [ComponentReference ("High performance controller for spawning finite effects in world space locations. The effect can either be a particle system or an animation.")]
    public class EffectController : BaseComponent {
        public GameObject [] Prefabs;

        [Tooltip ("Number of instances of each effect which will be pre-initialized for performance")]
        public int NumInstances = 20;

        Stack<GameObject> [] stacks;
        float [] durations;

        static EffectController Instance;

        void Awake () {
            Instance = this;
            transform.SetPositionAndRotation (Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;

            durations = new float [Prefabs.Length];
            stacks = new Stack<GameObject> [Prefabs.Length];
            for (int i = 0; i < Prefabs.Length; i++) {
                Log ($"EffectController Setting up a stack of {NumInstances} {Prefabs [i].name} instances");
                var stack = new Stack<GameObject> (NumInstances);
                durations [i] = SetupEffect (Prefabs [i], stack);
                stacks [i] = stack;
            }
        }

        /// <summary>
        /// Sets up a stack of deactivated instances of a specific prefab, ready to be moved and activated when needed.
        /// </summary>
        float SetupEffect (GameObject prefab, Stack<GameObject> stack) {
            GameObject go = new (prefab.name);
            var parent = go.transform;
            parent.parent = transform;
            parent.SetLocalPositionAndRotation (Vector3.zero, Quaternion.identity);
            parent.localScale = Vector3.one;
            for (int i = 0; i < NumInstances; i++) {
                go = Instantiate (prefab, parent);
                stack.Push (go);
                go.SetActive (false);
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
            //Default to 1 sec
            Debug.LogWarning ("Returning default duration on " + prefab.name, this);
            return 1f;
        }

        /// <summary>
        /// Spawn an effect at the specified transform's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Transform location) {
            Spawn (location.position, location.rotation, Random.Range (0, Prefabs.Length));
        }

        /// <summary>
        /// Spawn an effect at the specified gameobject's position and with same rotation. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (GameObject location) {
            Spawn (location.transform.position, location.transform.rotation, Random.Range (0, Prefabs.Length));
        }

        /// <summary>
        /// Spawn an effect at the specified world position. If multiple prefabs are defined, chose a random
        /// </summary>
        public void Spawn (Vector3 worldPosition) {
            Spawn (worldPosition, Quaternion.identity, Random.Range (0, Prefabs.Length));
        }

        /// <summary>
        /// Get ID (index) of the named prefab for future use
        /// </summary>
        public static int GetPrefabID (string prefabName) {
            for (int i = 0; i < Instance.Prefabs.Length; i++) {
                if (Instance.Prefabs [i].name == prefabName) {
                    return i;
                }
            }
            Instance.LogError ($"Prefab {prefabName} not registered!");
            return -1;
        }

        public static void Spawn (Vector3 position, Quaternion rotation, int prefabID) {
            if (prefabID < 0 || prefabID >= Instance.stacks.Length)
                return;
            if (!Instance.stacks [prefabID].TryPop (out GameObject instance)) {
                var prefab = Instance.Prefabs [prefabID];
                Instance.LogError ($"{prefab.name} stack exhausted! You should increase the NumInstances property on next build. Adding another instance (expensive)");
                //Debug.Break ();
                var parent = Instance.transform.Find (prefab.name);
                instance = Instantiate (prefab, parent);
            }
            Instance.StartCoroutine (Instance.E_Effect (instance, position, rotation, Instance.stacks [prefabID], Instance.durations [prefabID]));
        }

        /// <summary>
        /// Activates an instance in the given world space location, waits for [duration] seconds and then deactivates the instance and puts it back on the stack.
        /// </summary>
        IEnumerator E_Effect (GameObject instance, Vector3 position, Quaternion rotation, Stack<GameObject> queue, float duration) {
            instance.transform.SetLocalPositionAndRotation (position, rotation);
            instance.SetActive (true);
            //Loop rather than create new WaitForSeconds YieldInstruction-- creates no garbage
            var t = 0f;
            while (t < duration) {
                yield return null;
                t += Time.deltaTime;
            }
            instance.SetActive (false);
            queue.Push (instance);
        }
    }
}