using System.Collections;
using System;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using ZXing;

namespace CineGame.MobileComponents {
    /// <summary>
    /// Scans QR or bar codes from the platform default camera, or generates QR or bar codes from an ASCII string.
	/// You can specify a regex to validate the code before firing the OnRead event.
	/// You can hook up a RawImage's texture property to the OnWrite event to show the generated code on screen.
    /// </summary>
    [ComponentReference ("Scans QR or bar codes from the platform default camera, or generates QR or bar codes from an ASCII string.\nYou can specify a regex to validate the code before firing the OnRead event.\nYou can hook up a RawImage's texture property to the OnWrite event to show the generated code on screen.")]
    [RequireComponent (typeof(RawImage))]
    public class CodeScanner : BaseComponent {

        [Tooltip ("Call Scan() from OnEnable")]
        public bool ScanOnEnable;

        [Tooltip ("Choose QR or barcode format")]
        public BarcodeFormat ValidFormats = BarcodeFormat.QR_CODE;

        [Tooltip ("Fired when a QR Code is scanned and validated")]
        public UnityEvent<string> OnRead;

        [Tooltip ("Optional regex validating the scanned code (eg url format, or four digit code)")]
        public string ValidRegex;

        [Tooltip ("Fired when a QR Code is generated")]
        public UnityEvent<Texture> OnWrite;

        [Tooltip ("Fired when user has denied access to camera")]
        public UnityEvent OnDenied;

        [Tooltip ("Will request full screen brightness when a code is generated. The brightness will be reset when the component is disabled")]
        public bool FullBrightnessOnWrite;

        [Tooltip ("If true, a call to Generate() will result in a transparent texture with white foreground")]
        public bool Transparent;

        WebCamTexture webcamTexture;

        float originalScreenBrightness;

        Quaternion baseRotation = Quaternion.identity;

		void OnEnable () {
            if (FullBrightnessOnWrite) {
                originalScreenBrightness = Screen.brightness;
            }
            if (ScanOnEnable) {
                Scan ();
            }
            baseRotation = transform.rotation;
		}

		void OnDisable () {
            if (FullBrightnessOnWrite) {
                Screen.brightness = originalScreenBrightness;
            }
            if (webcamTexture != null) {
                if (webcamTexture.isPlaying)
                    webcamTexture.Stop ();
                webcamTexture = null;
            }
            transform.rotation = baseRotation;
        }

        /// <summary>
        /// Start scanning for a code using the default camera
        /// </summary>
        public void Scan () {
            StartCoroutine (E_ScanQRCode ());
        }

        IEnumerator E_ScanQRCode () {
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) {
                Log("Requesting permission to use Camera");
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
                if (!Application.HasUserAuthorization(UserAuthorization.Microphone)) {
                    Log("User denied permission to use Camera");
                    OnDenied.Invoke();
                    yield break;
                }
            }
            var rawImage = GetComponent<RawImage> ();
            webcamTexture = new WebCamTexture (1024, 1024, 30);
            rawImage.enabled = false;
            Log ("Starting default camera (WebCamTexture)");
            webcamTexture.Play ();
            while (webcamTexture.width <= 16)
                yield return null;
            Log ($"WebCamTexture dim = {webcamTexture.width} x {webcamTexture.height}, fps = {webcamTexture.requestedFPS}");
            var snap = new Texture2D (webcamTexture.width, webcamTexture.height, TextureFormat.ARGB32, false);
            var colors = new Color32 [webcamTexture.width * webcamTexture.height];
            rawImage.texture = webcamTexture;
            rawImage.enabled = true;
            Rect r;
            if (webcamTexture.width > webcamTexture.height) {
                var s = .25f * webcamTexture.height / webcamTexture.width;
                r = new (.5f - s, 0f, .5f + s, 1f);
            } else {
                var s = .25f * webcamTexture.width / webcamTexture.height;
                r = new (0f, .5f - s, 1f, .5f + s);
            }
            rawImage.uvRect = r;

            IBarcodeReader barCodeReader = new BarcodeReader ();
            string qrCode = null;
            var validRegex = !string.IsNullOrWhiteSpace (ValidRegex) ? new Regex (ValidRegex, RegexOptions.Compiled) : null;
            for (; ; ) {
                try {
                    //snap.UpdateExternalTexture (webcamTexture.GetNativeTexturePtr ());
                    webcamTexture.GetPixels32 (colors);
                    snap.SetPixels32 (colors);
                    var result = barCodeReader.Decode (snap.GetRawTextureData (), snap.width, snap.height, RGBLuminanceSource.BitmapFormat.ARGB32);
                    if (result != null && (result.BarcodeFormat & ValidFormats) != 0) {
                        var newQrCode = result.Text;
                        if (!string.IsNullOrEmpty (newQrCode) && (validRegex == null || validRegex.IsMatch (newQrCode))) {
                            Log ($"Validated text from scanned code: '{newQrCode}'");
                            OnRead.Invoke (newQrCode);
                            break;
                        } else if (qrCode != newQrCode) {
                            Log ($"Text from scanned code not validated: '{newQrCode}'");
                        }
                        qrCode = newQrCode;
                    }
                } catch (Exception ex) {
                    LogError (ex.Message);
                    break;
                }
                yield return null;
            }
            webcamTexture.Stop ();
        }

        void Update () {
            if (webcamTexture != null && webcamTexture.isPlaying) {
                var angle = webcamTexture.videoRotationAngle;
                if (angle != 0) {
                    transform.rotation = baseRotation * Quaternion.AngleAxis(-webcamTexture.videoRotationAngle, Vector3.forward);
                }
            }
        }

        /// <summary>
        /// Generate a texture from the input using the ValidFormats code
        /// </summary>
        public void Generate (string text) {
            var barcodeWriter = new BarcodeWriterPixelData {
                Format = ValidFormats,
                Options = new ZXing.Common.EncodingOptions {
                    Width = 128,
                    Height = 128,
                    Margin = 2,
                },
                Renderer = new ZXing.Rendering.PixelDataRenderer {
                    Foreground = Transparent ? System.Drawing.Color.White : System.Drawing.Color.Black,
                    Background = Transparent ? System.Drawing.Color.Transparent : System.Drawing.Color.White,
                },
            };
            var pixelData = barcodeWriter.Write (text);
            var texture = new Texture2D (pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false);
            texture.SetPixelData (pixelData.Pixels, 0);
            texture.Apply ();
            OnWrite.Invoke (texture);

            if (FullBrightnessOnWrite) {
                Screen.brightness = 1f;
            }
        }
    }
}