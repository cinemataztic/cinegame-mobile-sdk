using UnityEngine;

namespace CineGame.MobileComponents {

	[ComponentReference ("Since you cannot route RemoteControl colors into ParticleSystem's startColor property, here's a small proxy script. Interpolation can be done in the RemoteControl component.")]
	public class SetParticlesColor : BaseComponent {
		public void Set (Color c) {
			var psMain = GetComponent<ParticleSystem> ().main;
			psMain.startColor = c;
		}
	}
}
