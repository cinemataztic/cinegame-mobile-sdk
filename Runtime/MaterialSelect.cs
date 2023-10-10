using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System.Collections.Generic;
using System.Linq;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Select materials (or invoke methods) from host or locally with a pre-defined array of materials.
	/// </summary>
	[ComponentReference ("Invoke an event with a material from the Materials array. Can be invoked remotely with Key=Material.name")]
	public class MaterialSelect : ReplicatedComponent {
		[Tooltip ("Key in the ObjectMessage from host, string specifying the precise name of a Material in the array below")]
		public string Key;

		[Tooltip ("Array of materials, one of which will be passed to the listeners")]
		public Material [] Materials;

		[Tooltip ("Invoked with a material from the array when Change, Next or Previous is called")]
		public UnityEvent<Material> OnChange;

		[Tooltip ("Invoked with a material from the array when Select is called")]
		public UnityEvent<Material> OnSelect;

		Dictionary<string, Material> Dict;
		string Name;

		void Start () {
			Dict = Materials.ToDictionary (m => m.name, m => m);
		}

		/// <summary>
		/// Tries to locate the named material in the array, set as current and invoke the OnChange event with it
		/// </summary>
		public void Change (string name) {
			if (Dict.ContainsKey (name)) {
				Name = name;
				var material = Dict [name];
				Log ($"MaterialSelect Set={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
				OnChange.Invoke (material);
			} else {
				LogError ($"MaterialSelect: Material with name={name} not found!");
			}
		}

		/// <summary>
		/// Invokes the OnSelect event with the current material and, if Key is defined, sends Key={material.name} to host
		/// </summary>
		public void Select () {
			var material = Dict [Name];
			Log ($"MaterialSelect Select={material.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (material);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Log ($"MaterialSelect Select={material.name} - Send {Key}={material.name}");
				Send (Key, Name);
			}
		}

		/// <summary>
		/// Find current material in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
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
			Log ($"MaterialSelect Previous={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		/// <summary>
		/// Find current material in array, set the next one as current and invoke the OnChange event with it
		/// </summary>
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
			Log ($"MaterialSelect Next={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		/// <summary>
		/// If 'Key' property present in payload, try to locate the material, set as current and invoke the OnChange event with it
		/// </summary>
		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				var value = dataObj.GetUtfString (Key);
				if (Dict.TryGetValue (value, out Material material)) {
					Name = value;
					Log ($"MaterialSelect Remote Change={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
					OnChange.Invoke (material);
				} else {
					LogError ($"MaterialSelect Remote Change={value} not found!");
				}
			}
		}
	}

}
