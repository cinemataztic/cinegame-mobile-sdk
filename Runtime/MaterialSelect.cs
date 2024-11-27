using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;

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

		int Index;

		void Start () {
			SetIndex (0);
		}

		/// <summary>
		/// Invoke the OnChange method with the one at the specified index in the array
		/// </summary>
		public void SetIndex (int idx) {
			if (idx >= 0 && idx < Materials.Length) {
				Index = idx;
				var material = Materials [idx];
				Log ($"MaterialSelect.SetIndex index={idx} Sprite={((material != null) ? material.name : string.Empty)}");
				OnChange.Invoke (material);
				return;
			}
			LogError ($"MaterialSelect.SetIndex: {idx} out of range [0..{Materials.Length}]");
		}

		/// <summary>
		/// Tries to locate the named material in the array, set as current and invoke the OnChange event with it
		/// </summary>
		public void Change (string name) {
			for (int i = 0; i < Materials.Length; i++) {
				if (Materials [i].name == name) {
					var texture = Materials [i];
					Index = i;
					Log ($"MaterialSelect Set={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
					OnChange.Invoke (texture);
					return;
				}
			}
			LogError ($"MaterialSelect: Texture.name={name} not found!");
		}

		/// <summary>
		/// Invokes the OnSelect event with the current material and, if Key is defined, sends Key={material.name} to host
		/// </summary>
		public void Select () {
			var material = Materials [Index];
			Log ($"MaterialSelect Select={material.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (material);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Log ($"MaterialSelect Select={material.name} - Send {Key}={material.name}");
				Send (Key, material.name);
			}
		}

		/// <summary>
		/// Find current material in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Previous () {
			Index--;
			if (Index < 0)
				Index = Materials.Length - 1;
			var material = Materials [Index];
			Log ($"MaterialSelect Previous={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		/// <summary>
		/// Find current material in array, set the next one as current and invoke the OnChange event with it
		/// </summary>
		public void Next () {
			Index++;
			if (Index >= Materials.Length)
				Index = 0;
			var material = Materials [Index];
			Log ($"MaterialSelect Next={material.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (material);
		}

		/// <summary>
		/// If 'Key' property present in payload, try to locate the material, set as current and invoke the OnChange event with it
		/// </summary>
		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
			if (dataObj.ContainsKey (Key)) {
				Change (dataObj.GetUtfString (Key));
			}
		}
	}

}
