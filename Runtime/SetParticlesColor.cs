using UnityEngine;

namespace CineGame.MobileComponents {

	/// <summary>
	/// Since you cannot route RemoteControl colors into ParticleSystem's startColor property, here's a small proxy script.
	/// Interpolation can be done in the RemoteControl component.
	/// </summary>
	public class SetParticlesColor : MonoBehaviour, IGameComponentIcon {
		[Header ("Small utility for setting the startColor property on ParticleSystems")]
		public bool NA;

		public void Set (Color c) {
			var psMain = GetComponent<ParticleSystem> ().main;
			psMain.startColor = c;
		}
	}

}
