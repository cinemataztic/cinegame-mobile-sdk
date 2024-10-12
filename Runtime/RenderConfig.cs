using UnityEngine;
using UnityEngine.Rendering;

namespace CineGame.MobileComponents {

	[ComponentReference ("Render and Quality settings applied in OnEnable")]
	public class RenderConfig : BaseComponent {

		[System.Serializable]
		public enum AALevel {
			Disabled = 0,
			SamplingX2 = 2,
			SamplingX4 = 4,
			SamplingX8 = 8,
		}
		public AALevel AntiAliasing = AALevel.Disabled;

		public ShadowResolution ShadowResolution = ShadowResolution.High;

		[Tooltip("Shadow distance from camera. Smaller distance = finer shadows")]
		public float ShadowDistance = 50;
		
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
			QualitySettings.shadowDistance = ShadowDistance;
			QualitySettings.shadowResolution = ShadowResolution;
			QualitySettings.antiAliasing = (int)AntiAliasing;
			RenderSettings.defaultReflectionMode = (CustomReflection != null)? DefaultReflectionMode.Custom : DefaultReflectionMode.Skybox;
#if UNITY_2022_3_OR_NEWER
			RenderSettings.customReflectionTexture = CustomReflection;
#else
			RenderSettings.customReflection = CustomReflection;
#endif
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

		void OnValidate() {
			OnEnable ();
		}
	}

}