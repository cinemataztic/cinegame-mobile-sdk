using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	[ExecuteAlways]
	[ComponentReference ("Utility for async downloading and spawning a prefab as originally placed in the editor as a single child under this component.\nThe original position, rotation and scale will be used when spawning the prefab.\nThe prefab must manually be assigned to an assetbundle before building and uploading the scene.")]
	public class AssetLoader : BaseComponent {

		/// <summary>
		/// The relative path to the AssetBundle on the host website
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private string AssetBundleURL;

		/// <summary>
		/// The original asset path in the project
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private string AssetName;

		/// <summary>
		/// Original tag on prefab instance as recorded when building
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private string InstanceTag;

		/// <summary>
		/// Original localPosition of prefab instance
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private Vector3 InstanceLocalPosition;

		/// <summary>
		/// Original localScale of prefab instance
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private Vector3 InstanceLocalScale;

		/// <summary>
		/// Original localRotation of prefab instance
		/// </summary>
		[HideInInspector]
		[SerializeField]
		private Quaternion InstanceLocalRotation;

		[Tooltip ("Invoked when the asset is loaded and instantiated")]
		public UnityEvent<GameObject> OnSpawn;

		private static readonly Dictionary<string, AssetBundle> LoadedAssetBundles = new Dictionary<string, AssetBundle> ();
		private static readonly Dictionary<string, GameObject> LoadedAssets = new Dictionary<string, GameObject> ();

		private static readonly HashSet<string> LoadingAssetBundles = new HashSet<string> ();
		private static readonly HashSet<string> LoadingAssets = new HashSet<string> ();

		void Start () {
			if (!Application.isPlaying)
				return;
			StartCoroutine (E_LoadAndSpawnPrefab ());
		}

		IEnumerator E_LoadAndSpawnPrefab () {
			while (LoadingAssetBundles.Contains (AssetBundleURL) || LoadingAssets.Contains (AssetName))
				yield return null;
			if (!LoadedAssets.TryGetValue (AssetName, out GameObject prefab)) {
				if (!LoadedAssetBundles.TryGetValue (AssetBundleURL, out AssetBundle bundle)) {
					Log ($"AssetLoader downloading assetbundle {AssetBundleURL} for {AssetName} ...");
					LoadingAssetBundles.Add (AssetBundleURL);
					LoadingAssets.Add (AssetName);
					using var wwwRequest = Util.DownloadAssetBundle (AssetBundleURL);
					yield return wwwRequest;
					if (wwwRequest.result == UnityWebRequest.Result.Success) {
						bundle = DownloadHandlerAssetBundle.GetContent (wwwRequest);
						LoadingAssetBundles.Remove (AssetBundleURL);
						LoadedAssetBundles.Add (AssetBundleURL, bundle);
					} else {
						LogError ($"AssetLoader download error: " + wwwRequest.error);
						yield break;
					}
				}
				if (bundle != null) {
					Log ($"AssetLoader loading {AssetName} ...");
					var abRequest = bundle.LoadAssetAsync<GameObject> (AssetName);
					yield return abRequest;
					prefab = abRequest.asset as GameObject;
					if (prefab != null) {
						LoadingAssets.Remove (AssetName);
						LoadedAssets.Add (AssetName, prefab);
					} else {
						LogError ($"AssetLoader DownloaderHandler returned null");
					}
				}
			}
			if (prefab != null) {
				Log ($"AssetLoader spawning {AssetName}");
				var instance = Instantiate (prefab, transform);
				instance.tag = InstanceTag;
				var t = instance.transform;
				t.localPosition = InstanceLocalPosition;
				t.localRotation = InstanceLocalRotation;
				t.localScale = InstanceLocalScale;

				OnSpawn.Invoke (instance);
			}
		}

#if UNITY_EDITOR
		void OnTransformChildrenChanged () {
			if (transform.childCount == 1) {
				var prefabInstance = transform.GetChild (0).gameObject;
				if (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot (prefabInstance)) {
					return;
				}
			}
			LogError ($"AssetLoader must have one child which is a prefab");
		}
#endif
	}
}
