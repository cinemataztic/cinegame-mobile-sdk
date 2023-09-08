using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System.Collections.Generic;
using System.Linq;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Replace textures (or invoke events) from host or locally with a pre-defined array of materials.
	/// </summary>
	[ComponentReference ("Invoke an event with a texture from the Textures array. Can be invoked remotely with texture name")]
	public class TextureCycleComponent : ReplicatedComponent {
		[Tooltip("Key in the ObjectMessage from host, string specifying a name of a Texture in the array below")]
		public string Key;

        [Tooltip("Array of textures which will be passed to the listeners")]
        public Texture2D[] Textures;

		//This will invoke with a texture from the array to replace in a Material or Sprite
		public UnityEvent<Texture2D> OnChange;

		public UnityEvent<Texture2D> OnSelect;

		Dictionary<string, Texture2D> Dict;
		string Name;

		void Start () {
			Dict = Textures.ToDictionary (m => m.name, m => m);
		}

		/// <summary>
		/// Tries to locate the named texture in the array and invoke the OnChange event with it
		/// </summary>
		public void Set (string name) {
			if (Dict.TryGetValue (name, out Texture2D texture)) {
				Name = name;
				Log ($"TextureCycleComponent Set={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
				OnChange.Invoke (texture);
			} else {
				LogError ($"TextureCycleComponent: Texture.name={name} not found!");
			}
		}

		/// <summary>
		/// Invokes the OnSelect event with the last replacement texture and, if Key is defined, sends Key={texture.name} to host
		/// </summary>
		public void Select () {
			var texture = Dict [Name];
			Log ($"TextureCycleComponent Select={texture.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (texture);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Send (Key, Name);
			}
		}

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
			Log ($"TextureCycleComponent Previous={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
		}

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
			Log ($"TextureCycleComponent Next={Name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
		}

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				var name = dataObj.GetUtfString (Key);
				if (Dict.TryGetValue (name, out Texture2D texture)) {
					Log ($"TextureCycleComponent Remote Set={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
					OnChange.Invoke (texture);
				} else {
					LogError ($"TextureCycleComponent Texture.name={name} from host not found!");
				}
			}
        }
    }

}
