using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CineGame.MobileComponents {

	public class PhysicsConfig : MonoBehaviour, IGameComponentIcon {
		[Header ("Global config for physics settings")]

		public Vector3 Gravity = new Vector3 (0f, -9.81f, 0f);

		void Update () {
			Physics.gravity = Gravity;
		}
	}

}