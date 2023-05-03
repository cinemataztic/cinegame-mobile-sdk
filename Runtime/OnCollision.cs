using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	[ComponentReference("OnCollision can fire events/actions based on collisions and trigger volumes in both 2D and 3D. The events can be filtered on specific objects or tags besides the standard physics layers.")]
	public class OnCollision : BaseComponent {

		[Header ("Listen to trigger/collision with these other gameobjects. Leave empty to test with all other objects.")]
		[SerializeField] private GameObject[] FilterObjects;

		[Header("Listen to trigger/collision with these tags. Leave empty to test with all tags")]
		[TagSelector]
		[SerializeField] private string [] FilterTags;

		[HideInInspector]
		[SerializeField]
		private int eventMask = 0;

		[Serializable] public class OnCollisionEvent : UnityEvent<GameObject> { }

		public OnCollisionEvent m_onCollisionEnter;
		public OnCollisionEvent m_onCollisionExit;

		public OnCollisionEvent m_onTriggerEnter;
		public OnCollisionEvent m_onTriggerExit;

		private HashSet<GameObject> _filterObjects;
		private HashSet<string> _filterTags;

		void Start () {
			VerboseDebug &= Debug.isDebugBuild || Util.IsDevModeActive;

			_filterObjects = new HashSet<GameObject> (FilterObjects);
			_filterTags = new HashSet<string> (FilterTags);
		}

		/**
		 * Return true if filterObjects contains 'other' gameobject OR filterTags contains its tag
		 **/
		bool ShouldFireEvent (GameObject other) {
			return (_filterObjects.Count == 0 && _filterTags.Count == 0) || _filterObjects.Contains (other) || _filterTags.Contains (other.tag);
		}

		void OnTriggerEnter (Collider other) {
			if (ShouldFireEvent (other.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnTriggerEnter {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (other.gameObject), Util.GetEventPersistentListenersInfo (m_onTriggerEnter));
				}
				m_onTriggerEnter.Invoke (other.gameObject);
			}
		}

		void OnTriggerEnter2D (Collider2D other) {
			if (ShouldFireEvent (other.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnTriggerEnter2D {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (other.gameObject), Util.GetEventPersistentListenersInfo (m_onTriggerEnter));
				}
				m_onTriggerEnter.Invoke (other.gameObject);
            }
		}

		void OnTriggerExit (Collider other) {
			if (ShouldFireEvent (other.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnTriggerExit {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (other.gameObject), Util.GetEventPersistentListenersInfo (m_onTriggerExit));
				}
				m_onTriggerExit.Invoke (other.gameObject);
			}
		}

		void OnTriggerExit2D (Collider2D other) {
			if (ShouldFireEvent (other.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnTriggerExit2D {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (other.gameObject), Util.GetEventPersistentListenersInfo (m_onTriggerExit));
				}
				m_onTriggerExit.Invoke (other.gameObject);
			}
		}

		void OnCollisionEnter (Collision collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnCollisionEnter {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (collision.gameObject), Util.GetEventPersistentListenersInfo (m_onCollisionEnter));
				}
				m_onCollisionEnter.Invoke (collision.gameObject);
			}
		}

		void OnCollisionEnter2D (Collision2D collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnCollisionEnter2D {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (collision.gameObject), Util.GetEventPersistentListenersInfo (m_onCollisionEnter));
				}
				m_onCollisionEnter.Invoke (collision.gameObject);
			}
		}

		void OnCollisionExit (Collision collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnCollisionExit {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (collision.gameObject), Util.GetEventPersistentListenersInfo (m_onCollisionExit));
				}
				m_onCollisionExit.Invoke (collision.gameObject);
			}
		}

		void OnCollisionExit2D (Collision2D collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				if (Debug.isDebugBuild) {
					Debug.LogFormat ("{0} OnCollisionExit2D/b> {1}\n{2}", Util.GetObjectScenePath (gameObject), Util.GetObjectScenePath (collision.gameObject), Util.GetEventPersistentListenersInfo (m_onCollisionExit));
				}
				m_onCollisionExit.Invoke (collision.gameObject);
			}
		}
    }

}
