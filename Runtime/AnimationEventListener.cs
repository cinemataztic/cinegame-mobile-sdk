using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
    [ComponentReference ("Listen to Animation events. You can define a list of named events and trigger actions based on these by creating Animation Events like 'TriggerEvent(name)'")]
    public class AnimationEventListener : BaseComponent {

        [System.Serializable]
        public class EventTrigger {
            [Tooltip ("Name of event in Animation")]
            public string EventName;
            [Tooltip ("Event to trigger")]
            public UnityEvent<Transform> EventToTrigger;
        }

        [SerializeField] EventTrigger [] Triggers;

        Dictionary<string, UnityEvent<Transform>> events;

        void Start () {
            events = Triggers
                .Where (t => !string.IsNullOrWhiteSpace (t.EventName) && t.EventToTrigger != null)
                .ToDictionary (t => t.EventName, t => t.EventToTrigger);
        }

        public void NewEvent (string eventName) {
            if (events.TryGetValue (eventName, out UnityEvent<Transform> e)) {
                Log ($"AnimationEventListener.NewEvent (\"{eventName}\")");
                e.Invoke (transform);
            } else {
                Debug.LogWarning ($"{gameObject.GetScenePath ()} AnimationEventListener.NewEvent (\"{eventName}\") not defined!", this);
            }
        }

        /// <summary>
        /// Legacy method for invoking an event from an animation
        /// </summary>
        [System.Obsolete ("Use NewEvent (default name in UnityEditor)")]
        public void TriggerEvent (string eventName) {
            NewEvent (eventName);
		}
    }
}
