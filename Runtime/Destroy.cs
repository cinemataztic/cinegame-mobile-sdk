using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {

    [ComponentReference ("Destroyer of Objects. If Delay/DelayRandomSpread is set to nonzero, this GameObject will be destroyed. Otherwise you can use the public methods to destroy other objects immediately\n\nYou can also use DestroyChildrenNow to destroy all children of the supplied transform")]
    public class Destroy : BaseComponent {
        [FormerlySerializedAs ("destroySelfDelay")]
        [Range (0, float.MaxValue)]
        public float Delay = 0f;
        [Tooltip ("If non-zero a random delay is added")]
		[Range (0, float.MaxValue)]
		public float DelayRandomSpread = 0f;

        /// <summary>
        /// If delay is 
        /// </summary>
        /// <returns></returns>
        private void Start () {
            if (Delay > float.Epsilon || DelayRandomSpread > float.Epsilon) {
                StartCoroutine (E_DestroyAfterDelay (gameObject));
            }
        }

        private IEnumerator E_DestroyAfterDelay (UnityEngine.Object objectToDestroy) {
            var delay = Delay + UnityEngine.Random.Range (0f, DelayRandomSpread);
			Log ("Destroy.DestroyAfterDelay={1:#.###} {2}", delay, objectToDestroy is GameObject gameObj ? gameObj.GetScenePath () : objectToDestroy.name);
			yield return new WaitForSeconds (delay);
			DestroyNow (objectToDestroy);
		}

		/// <summary>
		/// Destroy this gameobject now
		/// </summary>
		public void DestroyNow () {
            Log ("Destroy.DestroyNow");
            Destroy (gameObject);
        }

        /// <summary>
        /// Destroy the Object passed in as argument now
        /// </summary>
        public void DestroyNow (UnityEngine.Object objectToDestroy) {
			Log ("Destroy.DestroyNow {1}", objectToDestroy is GameObject gameObj ? gameObj.GetScenePath () : objectToDestroy.name);
			Destroy (objectToDestroy);
        }

		/// <summary>
		/// Destroy the child transforms now
		/// </summary>
		public void DestroyChildrenNow (Transform parent) {
            var i = 0;
            foreach (Transform child in parent) {
                Destroy (child.gameObject);
                i++;
            }
			Log ($"Destroy.DestroyChildrenNow ({parent.gameObject.GetScenePath ()}) destroyed {i} children");
		}
	}
}