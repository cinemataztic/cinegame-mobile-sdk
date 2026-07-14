using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace CineGame.MobileComponents {

    [ComponentReference ("Render and Quality settings accessible through UnityEvents")]
    public class RenderConfig : BaseComponent {

        /// <summary>
        /// Invoked when this Component or GameObject is enabled
        /// </summary>
        public UnityEvent m_OnEnable;

        /// <summary>
        /// Level of MSAA: 0=None
        /// </summary>
		public int AntiAliasing { get { return QualitySettings.antiAliasing; } set { QualitySettings.antiAliasing = value; } }

        /// <summary>
        /// 0=Low, 1=Medium, 2=High, 3=Very high
        /// </summary>
        public int ShadowResolution { get { return (int)QualitySettings.shadowResolution; } set { QualitySettings.shadowResolution = (ShadowResolution)value; } }

        /// <summary>
        /// 0=None, 1=Hard Only, or 2=Hard+Soft
        /// </summary>
		public int Shadows { get { return (int)QualitySettings.shadows; } set { QualitySettings.shadows = (ShadowQuality)value; } }

        /// <summary>
        /// Shadow distance from camera. Smaller = more detail
        /// </summary>
		public float ShadowDistance { get { return QualitySettings.shadowDistance; } set { QualitySettings.shadowDistance = value; } }

        /// <summary>
        /// 0=Skybox, 1=Trilight, 2=Flat, 3=Custom
        /// </summary>
        public int AmbientMode { get { return (int)RenderSettings.ambientMode; } set { RenderSettings.ambientMode = (AmbientMode)value; } }

        public float AmbientIntensity { get { return RenderSettings.ambientIntensity; } set { RenderSettings.ambientIntensity = value; } }

        public string AmbientSkyColor {
            get { return ColorUtility.ToHtmlStringRGB (RenderSettings.ambientSkyColor); }
            set { if (ColorUtility.TryParseHtmlString (value, out Color c)) RenderSettings.ambientSkyColor = c; }
        }

        public string AmbientGroundColor {
            get { return ColorUtility.ToHtmlStringRGB (RenderSettings.ambientGroundColor); }
            set { if (ColorUtility.TryParseHtmlString (value, out Color c)) RenderSettings.ambientGroundColor = c; }
        }

        public string AmbientEquatorColor {
            get { return ColorUtility.ToHtmlStringRGB (RenderSettings.ambientEquatorColor); }
            set { if (ColorUtility.TryParseHtmlString (value, out Color c)) RenderSettings.ambientEquatorColor = c; }
        }

        public Material Skybox { get { return RenderSettings.skybox; } set { RenderSettings.skybox = value; } }

        public Texture CustomReflection
#if UNITY_2022_3_OR_NEWER
            { get { return RenderSettings.customReflectionTexture; } set { RenderSettings.customReflectionTexture = value; } }
#else
            { get { return RenderSettings.customReflection; } set { RenderSettings.customReflection = value; } }
#endif

        public bool Fog { get { return RenderSettings.fog; } set { RenderSettings.fog = value; } }

        /// <summary>
        /// 0=Linear, 1=Exponential, 2=Exp.Squared
        /// </summary>
		public int FogMode { get { return (int)RenderSettings.fogMode; } set { RenderSettings.fogMode = (FogMode)value; } }

        public string FogColor {
            get { return ColorUtility.ToHtmlStringRGB (RenderSettings.fogColor); }
            set { if (ColorUtility.TryParseHtmlString (value, out Color c)) RenderSettings.fogColor = c; }
        }

        /// <summary>
        /// Sets density for exponential fog
        /// </summary>
        public float FogDensity { get { return RenderSettings.fogDensity; } set { RenderSettings.fogDensity = value; } }

        public float FogStartDistance { get { return RenderSettings.fogStartDistance; } set { RenderSettings.fogStartDistance = value; } }

        public float FogEndDistance { get { return RenderSettings.fogEndDistance; } set { RenderSettings.fogEndDistance = value; } }

        void OnEnable () {
            m_OnEnable.Invoke ();
        }

        /// <summary>
        /// Set fog color from float[3] array
        /// </summary>
        public void SetFogColor (float [] rgb) {
            RenderSettings.ambientEquatorColor = new Color (rgb [0], rgb [1], rgb [2]);
        }

        /// <summary>
        /// Set sky color from float[3] array
        /// </summary>
        public void SetAmbientSkyColor (float [] rgb) {
            RenderSettings.ambientSkyColor = new Color (rgb [0], rgb [1], rgb [2]);
        }

        /// <summary>
        /// Set ground color from float[3] array
        /// </summary>
        public void SetAmbientGoundColor (float [] rgb) {
            RenderSettings.ambientGroundColor = new Color (rgb [0], rgb [1], rgb [2]);
        }

        /// <summary>
        /// Set equator color from float[3] array
        /// </summary>
        public void SetAmbientEquatorColor (float [] rgb) {
            RenderSettings.ambientEquatorColor = new Color (rgb [0], rgb [1], rgb [2]);
        }
    }

}
