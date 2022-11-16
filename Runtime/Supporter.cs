using UnityEngine;
using UnityEngine.UI;

public class Supporter : MonoBehaviour {
	public Text UserName;
	public Image UserProfileImage;

	public void DestroyMe () {
		Debug.LogFormat ("Supporter.DestroyMe {0}", CineGame.MobileComponents.Util.GetObjectScenePath (gameObject));
		Destroy (gameObject);
	}
}
