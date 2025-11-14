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

		[Tooltip("Shadow render quality: None, Hard Only, or Hard+Soft")]
		public ShadowQuality ShadowQuality = ShadowQuality.All;

		[Tooltip("Shadow distance from camera. Smaller distance = finer shadows")]
		public float ShadowDistance = 50;
		
		public AmbientMode AmbientMode = AmbientMode.Flat;
		public float AmbientIntensity;
		public SphericalHarmonicsL2 AmbientProbe;
		[Tooltip ("Flat ambient color, or sky color if mode is TriLight")]
		public Color AmbientSkyColor;
		[Tooltip ("Only used in TriLight mode")]
		public Color AmbientGroundColor;
		[Tooltip ("Only used in TriLight mode")]
		public Color AmbientEquatorColor;
		[Tooltip("If mode is skybox, use this material")]
		public Material SkyboxMaterial;

		public Texture CustomReflection;

		public bool Fog;
		public FogMode FogMode;
		public Color FogColor;
		public float FogDensity;
		public float FogStartDistance;
		public float FogEndDistance;

		void OnEnable () {
			QualitySettings.shadows = ShadowQuality;
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
			RenderSettings.fog = Fog;
			RenderSettings.fogMode = FogMode;
			RenderSettings.fogColor = FogColor;
			RenderSettings.fogDensity = FogDensity;
			RenderSettings.fogStartDistance = FogStartDistance;
			RenderSettings.fogEndDistance = FogEndDistance;
			RenderSettings.ambientProbe = AmbientProbe;
			RenderSettings.ambientIntensity = AmbientIntensity;
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

		void Reset () {
			ShadowQuality = QualitySettings.shadows;
			ShadowDistance = QualitySettings.shadowDistance;
			ShadowResolution = QualitySettings.shadowResolution;

			AntiAliasing = (AALevel)QualitySettings.antiAliasing;

			AmbientMode = RenderSettings.ambientMode;
			AmbientIntensity = RenderSettings.ambientIntensity;
			AmbientSkyColor = (AmbientMode == AmbientMode.Flat) ? RenderSettings.ambientLight : RenderSettings.ambientSkyColor;
			AmbientGroundColor = RenderSettings.ambientGroundColor;
			AmbientEquatorColor = RenderSettings.ambientEquatorColor;
			AmbientProbe = RenderSettings.ambientProbe;

			SkyboxMaterial = RenderSettings.skybox;
#if UNITY_2022_3_OR_NEWER
			CustomReflection = RenderSettings.customReflectionTexture;
#else
			CustomReflection = RenderSettings.customReflection;
#endif
			Fog = RenderSettings.fog;
			FogMode = RenderSettings.fogMode;
			FogColor = RenderSettings.fogColor;
			FogDensity = RenderSettings.fogDensity;
			FogStartDistance = RenderSettings.fogStartDistance;
			FogEndDistance = RenderSettings.fogEndDistance;
		}

        void OnValidate() {
			OnEnable ();
		}
	}

}