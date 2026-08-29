using UnityEngine;

namespace CineGame.MobileComponents {
	/// <summary>
	/// Automatically position mid air content dependant on Camera position and rotation (projected along look vector if DistanceFromCamera != 0)
	/// </summary>
	public class ARAutoMidAirPositioner : MonoBehaviour {
		public float DistanceFromCamera = 0f;

		void Start () {
			ARGameProxy.RegisterOnGameLoaded (Reposition);
		}

		public void Reposition () {
			Debug.Log ($"ARAutoMidAirPositioner Reposition camera at distance={DistanceFromCamera:##.00}");
			var rot = Camera.main.transform.rotation.eulerAngles;
			rot.x = rot.z = 0f;
			this.transform.SetPositionAndRotation (
				Camera.main.transform.position + Camera.main.transform.forward * DistanceFromCamera,
				Quaternion.Euler (rot)
			);
		}
	}
}
