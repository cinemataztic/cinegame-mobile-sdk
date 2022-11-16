using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Sfs2X.Entities.Data;
using System;
using System.Collections.Generic;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Remotely replace sprites (or invoke methods) with a pre-defined sprite array.
	/// </summary>
	public class RemoteSpriteComponent : ReplicatedComponent {
		[Tooltip("Key in the ObjectMessage from host - int index in Sprites array")]
		public string Key;

        [Tooltip("")]
        public Sprite[] Sprites;

		//This will invoke with a Sprite from the array to replace in an UI Image or Sprite component
		[Serializable] public class RemoteSpriteEvent : UnityEvent<Sprite> { }
		public RemoteSpriteEvent onReceive;

		internal override void OnObjectMessage (ISFSObject dataObj, int senderId) {
			if (dataObj.ContainsKey (Key)) {
				onReceive.Invoke (Sprites [dataObj.GetInt (Key)]);
			}
        }
    }

}
