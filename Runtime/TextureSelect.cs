using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Select textures (or invoke events) from host or locally with a pre-defined array of materials.
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

		int Index;

		void Start () {
			SetIndex (0);
		}

		/// <summary>
		/// Invoke the OnChange method with the one at the specified index in the array
		/// </summary>
		public void SetIndex (int idx) {
			if (idx >= 0 && idx < Textures.Length) {
				Index = idx;
				var texture = Textures [idx];
				Log ($"SpriteSelect.SetIndex index={idx} Sprite={((texture != null) ? texture.name : string.Empty)}");
				OnChange.Invoke (texture);
				return;
			}
			LogError ($"SpriteSelect.SetIndex: {idx} out of range [0..{Textures.Length}]");
		}

		/// <summary>
		/// Tries to locate the named texture in the array,set as current and invoke the OnChange event with it
		/// </summary>
		public void Change (string name) {
			for (int i = 0; i < Textures.Length; i++) {
				if (Textures [i].name == name) {
					var texture = Textures [i];
					Index = i;
					Log ($"TextureSelect Set={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
					OnChange.Invoke (texture);
					return;
				}
			}
			LogError ($"TextureSelect: Texture.name={name} not found!");
		}

		/// <summary>
		/// Invokes the OnSelect event with the current texture and, if Key is defined, sends Key={texture.name} to host
		/// </summary>
		public void Select () {
			var texture = Textures [Index];
			Log ($"TextureSelect Select={texture.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (texture);

			if (!string.IsNullOrWhiteSpace (Key)) {
				Send (Key, texture.name);
			}
		}

		/// <summary>
		/// Find current texture in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Previous () {
			Index--;
			if (Index < 0)
				Index = Textures.Length - 1;
			var texture = Textures [Index];
			Log ($"TextureSelect Previous={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
		}

		/// <summary>
		/// Find current texture in array, set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Next () {
			Index++;
			if (Index >= Textures.Length)
				Index = 0;
			var texture = Textures [Index];
			Log ($"TextureSelect Next={texture.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (texture);
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
