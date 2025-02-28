using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
    [ComponentReference ("Listen to Animation events. You can define a list of named events to trigger by creating Animation Events named 'NewEvent' and supplying the name of the event in this component as a string.\nThe component also has a list of utility methods you can invoke, such as AddForceUp, AddForceLocalForward etc.")]
    public class AnimationEventListener : BaseComponent {

        [System.Serializable]
        public class EventTrigger {
            [Tooltip ("Name of event in Animation")]
            public string EventName;
            [Tooltip ("Event to trigger")]
            public UnityEvent<Transform> EventToTrigger;
        }

        [System.Serializable]
        public class SoundTrigger {
            [Tooltip ("String parameter passed to Sound event in Animation")]
            public string SoundName;
            [Tooltip ("AudioClips to play (chooses a random index when event triggered)")]
            public AudioClip [] SoundClips;
        }

        public EventTrigger [] Triggers;
        public SoundTrigger [] Sounds;

        [Tooltip ("AudioSource to play sounds on. If none defined, then GetComponentInParent is used and finally AddComponent")]
        public AudioSource AudioSource;

        Dictionary<string, UnityEvent<Transform>> events;
        Dictionary<string, AudioClip []> audioClips;

        Rigidbody rigidBody;
        Rigidbody2D rigidBody2d;

        void Start () {
            events = Triggers
                .Where (t => !string.IsNullOrWhiteSpace (t.EventName) && t.EventToTrigger != null)
                .ToDictionary (t => t.EventName, t => t.EventToTrigger);

            audioClips = Sounds
                .Where (t => !string.IsNullOrWhiteSpace (t.SoundName) && t.SoundClips.Length != 0)
                .ToDictionary (t => t.SoundName, t => t.SoundClips);

            if (audioClips.Count != 0 && AudioSource == null) {
                AudioSource = GetComponentInParent<AudioSource> ();
                if (AudioSource == null) {
                    AudioSource = gameObject.AddComponent<AudioSource> ();
                }
            }

            FindRigidBody ();
        }

        public void NewEvent (string eventName) {
            if (events.TryGetValue (eventName, out UnityEvent<Transform> e)) {
                Log ($"AnimationEventListener.NewEvent (\"{eventName}\")");
                e.Invoke (transform);
            } else {
                Debug.LogWarning ($"{gameObject.GetScenePath ()} AnimationEventListener.NewEvent (\"{eventName}\") not defined!", this);
            }
        }

        public void Sound (string soundName) {
            if (audioClips.TryGetValue (soundName, out AudioClip [] clips)) {
                Log ($"AnimationEventListener.Sound (\"{soundName}\")");
                AudioSource.PlayOneShot (clips [Random.Range (0, clips.Length)]);
            } else {
                Debug.LogWarning ($"{gameObject.GetScenePath ()} AnimationEventListener.Sound (\"{soundName}\") not defined!", this);
            }
        }

        /// <summary>
        /// Legacy method for invoking an event from an animation
        /// </summary>
        [System.Obsolete ("Use NewEvent (default name in UnityEditor)")]
        public void TriggerEvent (string eventName) {
            NewEvent (eventName);
		}

        void FindRigidBody () {
            rigidBody = GetComponentInParent<Rigidbody> ();
            if (rigidBody == null) {
                rigidBody = GetComponentInChildren<Rigidbody> ();
                if (rigidBody == null) {
                    rigidBody2d = GetComponentInParent<Rigidbody2D> ();
                    if (rigidBody2d == null) {
                        rigidBody2d = GetComponentInChildren<Rigidbody2D> ();
                    }
                }
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of world Y axis
		/// </summary>
        public void AddForceUp (float size) {
            Log ($"AnimationEventListener.AddForceUp ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (Vector3.up * size);
            } else {
                rigidBody2d.AddForce (Vector2.up * size);
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of world Z axis
		/// </summary>
        public void AddForceForward (float size) {
            Log ($"AnimationEventListener.AddForceForward ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (Vector3.forward * size);
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of world X axis
		/// </summary>
        public void AddForceRight (float size) {
            Log ($"AnimationEventListener.AddForceRight ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (Vector3.right * size);
            } else {
                rigidBody2d.AddForce (Vector2.right * size);
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of its local Y axis
		/// </summary>
        public void AddForceLocalUp (float size) {
            Log ($"AnimationEventListener.AddForceLocalUp ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (rigidBody.transform.up * size);
            } else {
                rigidBody2d.AddForce (rigidBody2d.transform.up * size);
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of its local Z axis
		/// </summary>
        public void AddForceLocalForward (float size) {
            Log ($"AnimationEventListener.AddForceLocalForward ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (rigidBody.transform.forward * size);
            }
        }

        /// <summary>
		/// Apply a force at the center of the rigid body in direction of its local X axis
		/// </summary>
        public void AddForceLocalRight (float size) {
            Log ($"AnimationEventListener.AddForceLocalRight ({size})");
            if (rigidBody != null) {
                rigidBody.AddForce (rigidBody.transform.right * size);
            } else {
                rigidBody2d.AddForce (rigidBody2d.transform.right * size);
            }
        }
    }
}
