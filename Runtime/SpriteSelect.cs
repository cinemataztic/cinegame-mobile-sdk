using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Select sprites (or invoke methods) with a pre-defined sprite array.
	/// </summary>
	[ComponentReference ("Select sprites (or invoke methods) with a Sprite from a pre-defined array, and optionally replicate to host. Sprite can also be changed from host with Key={sprite.name}")]
	public class SpriteSelect : ReplicatedComponent {
		[Tooltip ("Key from host and to host - name of selected sprite")]
		public string Key;

		public Sprite [] Sprites;

		[Tooltip ("Invoked with a Sprite from the array to eg replace the sprite property of a UI Image or SpriteRenderer component")]
		[FormerlySerializedAs ("onReceive")]
		public UnityEvent<Sprite> OnChange;

		[Tooltip ("Invoked when the Select method is called")]
		public UnityEvent<Sprite> OnSelect;

		int Index;

        void Start () {
			SetIndex (0);
        }

        /// <summary>
        /// Invoke the OnChange method with the Sprite at the specified index in the array
        /// </summary>
        public void SetIndex (int idx) {
			if (idx >= 0 && idx < Sprites.Length) {
				Index = idx;
				var sprite = Sprites [idx];
				Log ($"SpriteSelect.SetIndex index={idx} Sprite={((sprite != null) ? sprite.name : string.Empty)}");
				OnChange.Invoke (sprite);
				return;
			}
			LogError ($"SpriteSelect.SetIndex: {idx} out of range [0..{Sprites.Length}]");
		}

		/// <summary>
		/// Find the Sprite with the specified name in the array and invoke the OnChange method.
		/// If the specified name is a wellformed URI, attempt to download a texture, create a new sprite and add it to the array and invoke the event with that.
		/// </summary>
		public void Change (string name) {
			if (System.Uri.IsWellFormedUriString (name, System.UriKind.Absolute)) {
				StartCoroutine (Util.E_LoadTexture (name, (texture) => {
					if (texture != null) {
						var sprite = Sprite.Create (texture, new Rect (0, 0, texture.width, texture.height), new Vector2 (.5f, .5f));
						sprite.name = System.IO.Path.GetFileNameWithoutExtension (new System.Uri (name).LocalPath);
						Index = Sprites.Length;
						var newSprites = new Sprite [Index + 1];
						System.Array.Copy (Sprites, newSprites, Index);
						Sprites = newSprites;
						Sprites [Index] = sprite;
						OnChange.Invoke (sprite);
					}
				}));
			}
			for (int i = 0; i < Sprites.Length; i++) {
				if (Sprites [i].name == name) {
					var sprite = Sprites [i];
					Index = i;
					Log ($"SpriteSelect.Change index={i} Sprite={((sprite != null) ? sprite.name : string.Empty)}");
					OnChange.Invoke (sprite);
					return;
				}
			}
			LogError ($"SpriteSelect.Change sprite with name '{name}' not found");
		}

		/// <summary>
		/// Invokes the OnSelect event with the current texture and, if Key is defined, sends Key={sprite.name} to host
		/// </summary>
		public void Select () {
			var sprite = Sprites [Index];
			Log ($"SpriteSelect Select={sprite.name}\n{Util.GetEventPersistentListenersInfo (OnSelect)}");
			OnSelect.Invoke (sprite);
			if (!string.IsNullOrWhiteSpace (Key)) {
				Send (Key, sprite.name);
			}
		}

		/// <summary>
		/// Set the previous one as current and invoke the OnChange event with it
		/// </summary>
		public void Previous () {
			Index--;
			if (Index < 0)
				Index = Sprites.Length - 1;
			var sprite = Sprites [Index];
			Log ($"SpriteSelect Previous={sprite.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (sprite);
		}

		/// <summary>
		/// Set the next one as current and invoke the OnChange event with it
		/// </summary>
		public void Next () {
			Index++;
			if (Index >= Sprites.Length)
				Index = 0;
			var sprite = Sprites [Index];
			Log ($"SpriteSelect Next={sprite.name}\n{Util.GetEventPersistentListenersInfo (OnChange)}");
			OnChange.Invoke (sprite);
		}

		/// <summary>
        /// Handle message from host with sprite name (NB: was index before sdk 1.6.1, changed to string/name for uniformity with TextureSelect and MaterialSelect)
        /// </summary>
		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
			if (dataObj.ContainsKey (Key)) {
				Change (dataObj.GetUtfString (Key));
			}
		}
	}

}
