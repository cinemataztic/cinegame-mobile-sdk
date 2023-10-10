using UnityEngine;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Remotely replace sprites (or invoke methods) with a pre-defined sprite array.
	/// </summary>
	[ComponentReference ("Replace a sprite with one from the Sprites array, according to received index")]
	public class RemoteSpriteComponent : ReplicatedComponent {
		[Tooltip("Key in the ObjectMessage from host - int index in Sprites array")]
		public string Key;

        [Tooltip("")]
        public Sprite[] Sprites;

		[Serializable] public class RemoteSpriteEvent : UnityEvent<Sprite> { }

		[Tooltip ("Invoke with a Sprite from the array to replace in an UI Image or Sprite component")]
		public RemoteSpriteEvent onReceive;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				var idx = dataObj.GetInt (Key);
				var sprite = Sprites [idx];
				Log ($"RemoteSpriteComponent index={idx} Sprite={((sprite != null) ? sprite.name : string.Empty)}");
				onReceive.Invoke (sprite);
			}
        }
    }

}
