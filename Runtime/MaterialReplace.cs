using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System.Collections.Generic;
using System.Linq;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Replace materials (or invoke methods) from host or locally with a pre-defined array of materials.
	/// </summary>
	[ComponentReference ("Invoke an event with a material from the Materials array. Can be invoked remotely with Key=MaterialName")]
	public class MaterialCycleComponent : ReplicatedComponent {
		[Tooltip ("Key in the ObjectMessage from host, string specifying a name of a Material in the array below")]
		public string Key;

		[Tooltip ("Array of materials, one of which will be passed to the listeners")]
		public Material [] Materials;

		//This will invoke with a material from the array to assign to a Model or UI Image
		public UnityEvent<Material> OnChange;

		//This will invoke with a material from the array to assign to a Model or UI Image
		public UnityEvent<Material> OnSelect;

		Dictionary<string, Material> Dict;
		string Name;

		void Start () {
			Dict = Materials.ToDictionary (m => m.name, m => m);
		}

		/// <summary>
		/// Tries to locate the named texture in the array and invoke the OnChange event with it
		/// </summary>
		public void Set (string name) {
			if (Dict.ContainsKey (name)) {
				Name = name;
				var material = Dict [name];
				Log ($"MaterialCycleComponent Set={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
				OnChange.Invoke (material);
			} else {
				LogError ($"MaterialCycleComponent: Material with name={name} not found!");
			}
		}

		/// <summary>
		/// Invokes the OnSelect event with the last replacement texture and, if Key is defined, sends Key={texture.name} to host
		/// </summary>
		public void Select () {
			var material = Dict [Name];
			Log ($"MaterialCycleComponent Select={material.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (material);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Log ($"MaterialCycleComponent Select={material.name} - Send {Key}={material.name}");
				Send (Key, Name);
			}
		}

		public void Previous () {
			var material = Materials [0];
			for (int i = 0; i < Materials.Length; i++) {
				if (Materials [i].name == Name) {
					var j = (i == 0) ? Materials.Length - 1 : i - 1;
					material = Materials [j];
					break;
				}
			}
			Name = material.name;
			Log ($"MaterialCycleComponent Previous={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		public void Next () {
			var material = Materials [0];
			for (int i = 0; i < Materials.Length; i++) {
				if (Materials [i].name == Name) {
					var j = (i == Materials.Length - 1) ? 0 : i + 1;
					material = Materials [j];
					break;
				}
			}
			Name = material.name;
			Log ($"MaterialCycleComponent Next={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				Name = dataObj.GetUtfString (Key);
				var material = Dict [Name];
				Log ($"MaterialCycleComponent Remote Set={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
				OnChange.Invoke (material);
			}
		}
	}

}
