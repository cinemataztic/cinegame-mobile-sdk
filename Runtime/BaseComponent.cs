using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CineGame.MobileComponents {
    public abstract class BaseComponent : MonoBehaviour, IGameComponentIcon {
		[Tooltip ("Log events verbosely in editor and debug builds")]
		public bool VerboseDebug;

		private void Start () {
			VerboseDebug &= Debug.isDebugBuild || Util.IsDevModeActive;
		}
	}
}