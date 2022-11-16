using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CineGame.MobileComponents {

	public class PlaySound : MonoBehaviour, IGameComponentIcon {

		[Tooltip("Path of each soundfile. Relative to StreamingAssets/Resources/Sounds")]
		public string[] Sounds;

		private int[] SoundIds;

		public delegate int LoadSoundDelegate (string path);
		public static event LoadSoundDelegate LoadSoundEvent;

		public delegate void UnloadSoundDelegate (int id);
		public static event UnloadSoundDelegate UnloadSoundEvent;

		public delegate void PlaySoundDelegate (int id);
		public static event PlaySoundDelegate PlaySoundEvent;

		void Start () {
			if (Sounds.Length > 0) {
				SoundIds = new int[Sounds.Length];
				var i = 0;
				foreach (var sound in Sounds) {
					Debug.LogFormat ("Loading sound {0} ...", sound);
					SoundIds [i++] = LoadSoundEvent.Invoke (sound);
				}
			}
		}

		void OnDestroy () {
			foreach (var id in SoundIds) {
				UnloadSoundEvent.Invoke (id);
			}
		}

		public void Play (string name) {
			var i = 0;
			foreach (var sound in Sounds) {
				if (sound == name) {
#if UNITY_EDITOR
					Debug.LogFormat ("Playing sound {0} ...", name);
#endif
					PlaySoundEvent.Invoke (SoundIds [i]);
					return;
				}
				i++;
			}
			Debug.LogErrorFormat ("{0}:PlaySound ({1}) Unable to find sound", Util.GetObjectScenePath (gameObject), name);
		}

		public void Play (int index) {
			if (index >= 0 && index < Sounds.Length) {
#if UNITY_EDITOR
				Debug.LogFormat ("Playing sound {0} ...", Sounds[index]);
#endif
				PlaySoundEvent.Invoke (SoundIds [index]);
				return;
			}
			Debug.LogErrorFormat ("{0}:PlaySound ({1}) index out of range!", Util.GetObjectScenePath (gameObject), index);
		}
	}
}