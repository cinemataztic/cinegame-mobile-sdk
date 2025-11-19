using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Dynamically invoke an event on a target. The Template property is the prefab or root scene object on which the event is modelled.
	/// </summary>
	[ComponentReference ("Dynamically invoke an event on a target. The Template property is the prefab or root scene object on which the event is modelled.")]
	public class DynamicInvoke : BaseComponent {

		[Tooltip ("The root of the prefab or scene object which the event is modeled on")]
		public GameObject Template;

		[Tooltip ("The event to invoke on a dynamic object")]
		public UnityEvent Event;

		FieldInfo fiTarget;
		object [] Listeners;
		int [] [] childIndex;
		Type [] TargetTypes;

		void Start () {
			var fiPersistentCalls = typeof (UnityEventBase).GetField ("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
			var miGetListener = fiPersistentCalls.FieldType.GetMethod ("GetListener");
			var engineAssembly = typeof(UnityEventBase).Assembly;
			var tPersistentCall = engineAssembly.GetType ("UnityEngine.Events.PersistentCall");
			fiTarget = tPersistentCall.GetField ("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);

			var calls = fiPersistentCalls.GetValue (Event);

			var c = Event.GetPersistentEventCount ();
			var objInt = new object [1];
			Listeners = new object [c];
			TargetTypes = new Type [c];
			childIndex = new int [c] [];
			var l = new List<int> ();
			var t = Template.transform;
			Transform ot;
			for (int i = 0; i < c; i++) {
				objInt [0] = i;
				Listeners [i] = miGetListener.Invoke (calls, objInt);
				var o = Event.GetPersistentTarget (i);
				TargetTypes [i] = o.GetType ();
				if (o is GameObject go) {
					ot = go.transform;
				} else {
					ot = ((Component)o).transform;
				}
				l.Clear ();
				if (ot.gameObject == Template) {
					childIndex [i] = new int [0];
				} else if (BuildChildIndex (t, ot, ref l)) {
					var cc = l.Count;
					childIndex [i] = new int [cc];
					for (int j = 0; j < cc; j++) {
						childIndex [i] [j] = l [cc - 1 - j];
					}
				} else {
					LogError ($"Listener target {i} not found in Root hierarchy!");
				}
			}
		}

		/// <summary>
        /// Builds the child index array for the listener target recursively
        /// </summary>
		bool BuildChildIndex (Transform parent, Transform child, ref List<int> l) {
			for (int i = 0; i < parent.childCount; i++) {
				var t = parent.GetChild (i);
				if (t == child || BuildChildIndex (t, child, ref l)) {
					l.Add (i);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Invoke event on a new instance
		/// </summary>
		public void InvokeOn (Transform v) {
			InvokeOn (v.gameObject);
		}

		/// <summary>
        /// Invoke event on a new instance
        /// </summary>
        public void InvokeOn (GameObject v) {
			if (v == null)
				return;
			var c = Event.GetPersistentEventCount ();
			if (c == 0)
				return;
			if (VerboseDebug) {
				Log ($"InvokeOn {v.GetScenePath ()}:\n{Util.GetEventPersistentListenersInfo (Event)}");
			}
			var root = v.transform;
			object listenerTarget;
			for (int i = 0; i < c; i++) {
				var ci = childIndex [i];
				var t = root;
				for (int j = 0; j < ci.Length; j++) {
					t = t.GetChild (ci [j]);
				}
				var typ = TargetTypes [i];
				if (typ != typeof(GameObject)) {
					listenerTarget = t.GetComponent (typ);
				} else {
					listenerTarget = t.gameObject;
				}
				fiTarget.SetValue (Listeners [i], listenerTarget);
			}
			//Dirty persistent calls so they will be rebuilt with new targets
			Event.SetPersistentListenerState (0, UnityEventCallState.RuntimeOnly);
			Event.Invoke ();
		}

		/// <summary>
        /// Invoke again on the same target
        /// </summary>
		public void Invoke () {
			Event.Invoke ();
		}
    }

}