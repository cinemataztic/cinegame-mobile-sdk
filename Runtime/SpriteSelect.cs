using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Replace sprites (or invoke methods) with a pre-defined sprite array.
	/// </summary>
	[ComponentReference ("Replace sprites (or invoke methods) with a Sprite from a pre-defined array.")]
	public class SpriteSelect : ReplicatedComponent {
		[Tooltip("Key in the ObjectMessage from host - int index in Sprites array property")]
		public string Key;

        public Sprite[] Sprites;

		[Tooltip ("Invoked with a Sprite from the array to eg replace the sprite property of a UI Image or SpriteRenderer component")]
		[SerializeField]
		private UnityEvent<Sprite> onReceive;

		/// <summary>
        /// Invoke the onReceive method with the Sprite at the specified index in the array
        /// </summary>
		public void SetIndex (int idx) {
			if (idx < 0 || idx > Sprites.Length) {
				LogError ($"RemoteSprite.SetIndex: {idx} out of range [0..{Sprites.Length}]");
				return;
			}
			var sprite = Sprites [idx];
			Log ($"RemoteSprite.SetIndex index={idx} Sprite={((sprite != null) ? sprite.name : string.Empty)}");
			onReceive.Invoke (sprite);
		}

		/// <summary>
        /// Find the Sprite with the specified name in the array and invoke the onReceive method
        /// </summary>
		public void Change (string name) {
			for (int i = 0; i < Sprites.Length; i++) {
				if (Sprites [i].name == name) {
					var sprite = Sprites [i];
					Log ($"RemoteSprite.SetIndex index={i} Sprite={((sprite != null) ? sprite.name : string.Empty)}");
					onReceive.Invoke (sprite);
				}
			}
		}

		internal override void OnObjectMessage (ISFSObject dataObj, Sfs2X.Entities.User sender) {
			if (dataObj.ContainsKey (Key)) {
				var idx = dataObj.GetInt (Key);
				SetIndex (idx);
			}
        }
    }

}
