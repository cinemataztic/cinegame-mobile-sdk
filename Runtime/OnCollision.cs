using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	[ComponentReference("Receives messages and invokes events from physics (OnTriggerEnter, OnCollisionEnter, OnJointBreak etc). Collision and trigger events can be filtered on specific objects or tags besides the standard physics layers.")]
	public class OnCollision : BaseEventComponent {

		[Tooltip ("Filter trigger/collision events on these gameobjects")]
		[SerializeField] private GameObject[] FilterObjects;

		[Tooltip ("Filter trigger/collision on specified tags")]
		[TagSelector]
		[SerializeField] private string [] FilterTags;

		public UnityEvent<GameObject> m_onCollisionEnter;
		public UnityEvent<GameObject> m_onCollisionExit;

		public UnityEvent<GameObject> m_onTriggerEnter;
		public UnityEvent<GameObject> m_onTriggerExit;

		public UnityEvent<GameObject> m_onJointBreak;

		/// <summary>
        /// This event is only intended for adding listeners in script, hidden the inspector
        /// </summary>
		[HideInInspector]
		public UnityEvent<Collision> m_onEnterCollision;

		/// <summary>
		/// This event is only intended for adding listeners in script, hidden the inspector
		/// </summary>
		[HideInInspector]
		public UnityEvent<Collision> m_onExitCollision;

		private HashSet<GameObject> _filterObjects;
		private HashSet<string> _filterTags;

		void Start () {
			_filterObjects = new HashSet<GameObject> (FilterObjects);
			_filterTags = new HashSet<string> (FilterTags);
		}

		/// <summary>
		/// Init fields default when adding component from script
		/// </summary>
		public void Init (GameObject [] filterObjects = null, string [] filterTags = null) {
			FilterObjects = filterObjects ?? new GameObject [0];
			FilterTags = filterTags ?? new string [0];
			m_onCollisionEnter = new UnityEvent<GameObject> ();
			m_onCollisionExit = new UnityEvent<GameObject> ();
			m_onEnterCollision = new UnityEvent<Collision> ();
			m_onExitCollision = new UnityEvent<Collision> ();
			m_onTriggerEnter = new UnityEvent<GameObject> ();
			m_onTriggerExit = new UnityEvent<GameObject> ();
			m_onJointBreak = new UnityEvent<GameObject> ();
		}

		/// <summary>
		/// Return true if filterObjects contains 'other' gameobject OR filterTags contains its tag
		/// </summary>
		bool ShouldFireEvent (GameObject other) {
			return enabled && ((_filterObjects.Count == 0 && _filterTags.Count == 0) || _filterObjects.Contains (other) || _filterTags.Contains (other.tag));
		}

        void OnTriggerEnter (Collider other) {
			if (ShouldFireEvent (other.gameObject)) {
				Log ($"OnTriggerEnter {other.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onTriggerEnter)}");
				m_onTriggerEnter.Invoke (other.gameObject);
			}
		}

		void OnTriggerEnter2D (Collider2D other) {
			if (ShouldFireEvent (other.gameObject)) {
				Log ($"OnTriggerEnter2D {other.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onTriggerEnter)}");
				m_onTriggerEnter.Invoke (other.gameObject);
            }
		}

		void OnTriggerExit (Collider other) {
			if (ShouldFireEvent (other.gameObject)) {
				Log ($"OnTriggerExit {other.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onTriggerExit)}");
				m_onTriggerExit.Invoke (other.gameObject);
			}
		}

		void OnTriggerExit2D (Collider2D other) {
			if (ShouldFireEvent (other.gameObject)) {
				Log ($"OnTriggerExit2D {other.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onTriggerExit)}");
				m_onTriggerExit.Invoke (other.gameObject);
			}
		}

		void OnCollisionEnter (Collision collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				Log ($"OnCollisionEnter {collision.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onCollisionEnter)}");
				m_onCollisionEnter.Invoke (collision.gameObject);
				m_onEnterCollision.Invoke (collision);
			}
		}

		void OnCollisionEnter2D (Collision2D collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				Log ($"OnCollisionEnter2D {collision.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onCollisionEnter)}");
				m_onCollisionEnter.Invoke (collision.gameObject);
			}
		}

		void OnCollisionExit (Collision collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				Log ($"OnCollisionExit {collision.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onCollisionExit)}");
				m_onCollisionExit.Invoke (collision.gameObject);
				m_onExitCollision.Invoke (collision);
			}
		}

		void OnCollisionExit2D (Collision2D collision) {
			if (ShouldFireEvent (collision.gameObject)) {
				Log ($"OnCollisionExit2D {collision.gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onCollisionExit)}");
				m_onCollisionExit.Invoke (collision.gameObject);
			}
		}

		void OnJointBreak (float breakForce) {
			Log ($"OnJointBreak {breakForce} {gameObject.GetScenePath ()}\n{Util.GetEventPersistentListenersInfo (m_onJointBreak)}");
			m_onJointBreak.Invoke (gameObject);
		}

	}

}
