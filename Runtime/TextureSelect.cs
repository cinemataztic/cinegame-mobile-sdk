using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System.Collections.Generic;
using System.Linq;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Replace textures (or invoke events) from host or locally with a pre-defined array of materials.
	/// </summary>
	[ComponentReference ("Invoke an event with an entry from the Textures array. Can be invoked remotely with Key=Texture.name")]
	public class TextureSelect : ReplicatedComponent {
		[Tooltip("Key in the ObjectMessage from host, string specifying a name of a Texture in the array below")]
		public string Key;

        [Tooltip("Array of textures which will be passed to the listeners")]
        public Texture2D[] Textures;

		[Tooltip ("Invoked with a texture from the array when Change, Next or Previous is called")]
		public UnityEvent<Texture2D> OnChange;

		[Tooltip ("Invoked with a texture from the array when Select is called")]
		public UnityEvent<Texture2D> OnSelect;

		Dictionary<string, Texture2D> Dict;
		string Name;

		void Start () {
			Dict = Textures.ToDictionary (m => m.name, m => m);
		}

		/// <summary>
		/// Tries to locate the named texture in the array,set as current and invoke the OnChange event with it
		/// </summary>
		public void Change (string name) {
			if (Dict.TryGetValue (name, out Texture2D texture)) {
				Name = name;
				Log ($"TextureSelect Set={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
				OnChange.Invoke (texture);
			} else {
				LogError ($"TextureSelect: Texture.name={name} not found!");
			}
		}

		/// <summary>
		/// Invokes the OnSelect event with the current texture and, if Key is defined, sends Key={texture.name} to host
		/// </summary>
		public void Select () {
			var texture = Dict [Name];
			Log ($"TextureSelect Select={texture.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (texture);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Send (Key, Name);
			}
		}

		/// <summary>
		/// Find current texture in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Previous () {
			var texture = Textures [0];
			for (int i = 0; i < Textures.Length; i++) {
				if (Textures [i].name == Name) {
					var j = (i == 0) ? Textures.Length - 1 : i - 1;
					texture = Textures [j];
					break;
				}
			}
			Name = texture.name;
			Log ($"TextureSelect Previous={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
		}

		/// <summary>
		/// Find current texture in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Next () {
			var texture = Textures [0];
			for (int i = 0; i < Textures.Length; i++) {
				if (Textures [i].name == Name) {
					var j = (i == Textures.Length - 1) ? 0 : i + 1;
					texture = Textures [j];
					break;
				}
			}
			Name = texture.name;
			Log ($"TextureSelect Next={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
		}

		/// <summary>
		/// If 'Key' property present in payload, try to locate the material, set as current and invoke the OnChange event with it
		/// </summary>
		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				var value = dataObj.GetUtfString (Key);
				if (Dict.TryGetValue (value, out Texture2D texture)) {
					Name = value;
					Log ($"TextureSelect Remote Change={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
					OnChange.Invoke (texture);
				} else {
					LogError ($"TextureSelect Remote Change={value} not found!");
				}
			}
        }
    }

}
