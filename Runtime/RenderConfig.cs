using UnityEngine;
using UnityEngine.Rendering;

namespace CineGame.MobileComponents {

	public class RenderConfig : MonoBehaviour, IGameComponentIcon {
		[Header ("Global config for render settings")]
		
		public AmbientMode AmbientMode = AmbientMode.Flat;
		[Tooltip ("Flat ambient color, or sky color if mode is TriLight")]
		public Color AmbientSkyColor;
		[Tooltip ("Only used in TriLight mode")]
		public Color AmbientGroundColor;
		[Tooltip ("Only used in TriLight mode")]
		public Color AmbientEquatorColor;
		[Tooltip("If mode is skybox, use this material")]
		public Material SkyboxMaterial;

		public Cubemap CustomReflection;

		void OnEnable () {
			RenderSettings.defaultReflectionMode = (CustomReflection != null)? DefaultReflectionMode.Custom : DefaultReflectionMode.Skybox;
			RenderSettings.customReflection = CustomReflection;

			RenderSettings.ambientMode = AmbientMode;
			switch (AmbientMode) {
			case AmbientMode.Skybox:
				RenderSettings.skybox = SkyboxMaterial;
				break;
			case AmbientMode.Flat:
				RenderSettings.ambientLight = AmbientSkyColor;
				break;
			case AmbientMode.Trilight:
				RenderSettings.ambientSkyColor = AmbientSkyColor;
				RenderSettings.ambientGroundColor = AmbientGroundColor;
				RenderSettings.ambientEquatorColor = AmbientEquatorColor;
				break;
			}
		}
	}

}