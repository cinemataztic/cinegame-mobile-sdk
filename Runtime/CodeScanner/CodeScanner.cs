using System.Collections;
using System;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

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

        bool hasUserPermission, hasUserDenied;

        Quaternion baseRotation;

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
            if (Application.platform == RuntimePlatform.Android) {
#if UNITY_ANDROID
                hasUserPermission = Permission.HasUserAuthorizedPermission (Permission.Camera);
                if (!hasUserPermission) {
                    Log ("Requesting permission to use Camera");
                    var callbacks = new PermissionCallbacks ();
                    callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
                    callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
                    callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
                    hasUserDenied = false;
                    Permission.RequestUserPermission (Permission.Camera, callbacks);
                    while (!hasUserPermission && !hasUserDenied)
                        yield return null;
                    if (hasUserDenied) {
                        OnDenied.Invoke ();
                        yield break;
                    }
                }
#endif
            }
            var rawImage = GetComponent<RawImage> ();
            webcamTexture = new WebCamTexture (512, 512, 30);
            rawImage.texture = webcamTexture;
            rawImage.enabled = false;
            Log ("Starting default camera (WebCamTexture)");
            webcamTexture.Play ();
            while (webcamTexture.width <= 16)
                yield return null;
            Log ($"WebCamTexture dim = {webcamTexture.width} x {webcamTexture.height}, fps = {webcamTexture.requestedFPS}");
            var rt = GetComponent<RectTransform> ();
            rt.sizeDelta = new Vector2 (rt.sizeDelta.x, webcamTexture.height * rt.sizeDelta.x / webcamTexture.width);
            var snap = new Texture2D (webcamTexture.width, webcamTexture.height, TextureFormat.ARGB32, false);
            var colors = new Color32 [webcamTexture.width * webcamTexture.height];
            rawImage.enabled = true;

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
                transform.rotation = baseRotation * Quaternion.AngleAxis (webcamTexture.videoRotationAngle, Vector3.up);
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

#if UNITY_ANDROID
        void PermissionCallbacks_PermissionGranted (string permissionName) {
            hasUserPermission = true;
            Log ($"CodeScanner {permissionName} granted");
        }

        void PermissionCallbacks_PermissionDeniedAndDontAskAgain (string permissionName) {
            hasUserDenied = true;
            Log ($"CodeScanner {permissionName} denied and do not ask again");
        }

        void PermissionCallbacks_PermissionDenied (string permissionName) {
            hasUserDenied = true;
            Log ($"CodeScanner {permissionName} denied");
        }
#endif
    }
}