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
        public bool ScanOnEnable = true;

        [Tooltip ("Choose QR or barcode format")]
        public BarcodeFormat ValidFormats = BarcodeFormat.QR_CODE;

        [Tooltip ("Optional regex validating the scanned code (eg url format, or four digit code)")]
        public string ValidRegex;

        [Tooltip ("Will request full screen brightness when a code is generated. The brightness will be reset when the component is disabled")]
        public bool FullBrightnessOnWrite = true;

        [Tooltip ("If true, a call to Generate() will result in a transparent texture with white foreground")]
        public bool Transparent;

        [Tooltip ("This overlay will be activated, positioned and sized when a code is detected")]
        public RectTransform CodeOverlay;

        [Tooltip ("Fired when a QR Code is scanned and validated")]
        public UnityEvent<string> OnRead;

        [Tooltip ("Fired when a QR Code is scanned but doesn't validate")]
        public UnityEvent<string> OnInvalidCode;

        [Tooltip ("Fired when a QR Code is generated")]
        public UnityEvent<Texture> OnWrite;

        [Tooltip ("Fired when user has denied access to camera")]
        public UnityEvent OnDenied;

        WebCamTexture webcamTexture;

        float originalScreenBrightness;

        Quaternion baseRotation = Quaternion.identity;
        Vector3 baseScale = Vector3.one;
        Vector3 flipScale = new (1f, -1f, 1f);

        Vector2 minCodeSize = new (128f, 128f);

        bool hasUserPermission, hasUserDenied;

		void OnEnable () {
            if (FullBrightnessOnWrite) {
                originalScreenBrightness = Screen.brightness;
            }
            if (ScanOnEnable) {
                Scan ();
            }
            baseRotation = transform.rotation;
            baseScale = transform.localScale;
            flipScale = new Vector3 (baseScale.x, -baseScale.y, baseScale.z);

            if (CodeOverlay != null) {
                minCodeSize = CodeOverlay.rect.size;
                CodeOverlay.gameObject.SetActive (false);
                CodeOverlay.anchorMin = CodeOverlay.anchorMax = Vector2.zero;
                CodeOverlay.localScale = Vector3.one;
            }
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
#else
            if (!Application.HasUserAuthorization (UserAuthorization.WebCam)) {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
                if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) {
#endif
                    Log ($"CodeScanner.OnDenied\n{Util.GetEventPersistentListenersInfo (OnDenied)}");
                    OnDenied.Invoke ();
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
            var offset = Vector3.zero;
            var scale = GetComponent<RectTransform> ().rect.size;
            if (webcamTexture.width > webcamTexture.height) {
                var s = (float)webcamTexture.height / webcamTexture.width;
                r = new (.5f - .5f * s, 0f, s, 1f);
                offset.x = webcamTexture.width * r.min.x;
                scale.x /= webcamTexture.width * r.size.x;
                scale.y /= webcamTexture.height;
            } else {
                var s = (float)webcamTexture.width / webcamTexture.height;
                r = new (0f, .5f - .5f * s, 1f, s);
                offset.y = webcamTexture.height * r.min.y;
                scale.x /= webcamTexture.width;
                scale.y /= webcamTexture.height * r.size.y;
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
                        if (CodeOverlay != null) {
                            var rp = result.ResultPoints;
                            var b = new Bounds (new Vector3 (rp [0].X, rp [0].Y), Vector3.zero);
                            for (int i = 1; i < rp.Length; i++)
                                b.Encapsulate (new Vector3 (rp [i].X, rp [i].Y, 0f));
                            b.extents *= 1.4f;
                            CodeOverlay.anchoredPosition = (b.min - offset) * scale;
                            CodeOverlay.sizeDelta = Vector2.Max (b.size * scale, minCodeSize);
                            CodeOverlay.gameObject.EnsureActive ();
                        }
                        var newQrCode = result.Text;
                        if (!string.IsNullOrEmpty (newQrCode) && (validRegex == null || validRegex.IsMatch (newQrCode))) {
                            Log ($"Validated text from scanned {result.BarcodeFormat}: '{newQrCode}' OnRead\n{Util.GetEventPersistentListenersInfo (OnRead)}");
                            OnRead.Invoke (newQrCode);
                            break;
                        } else if (qrCode != newQrCode) {
                            Log ($"Text from scanned {result.BarcodeFormat} did not validate: '{newQrCode}' regex: {validRegex} OnInvalidCode\n{Util.GetEventPersistentListenersInfo (OnInvalidCode)}");
                            OnInvalidCode.Invoke (newQrCode);
                        }
                        qrCode = newQrCode;
                    } else {
                        if (CodeOverlay != null) {
                            CodeOverlay.gameObject.SetActive (false);
                        }
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
                transform.localScale = webcamTexture.videoVerticallyMirrored ? flipScale : baseScale;
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
            var foundFormat = false;
            for (int i = 0; i < 32; i++) {
                if (((int)ValidFormats & (1 << i)) != 0) {
                    if (foundFormat) {
                        LogError ("CodeScanner could not generate code, more than one code format selected!");
                        return;
                    }
                    foundFormat = true;
                }
            }

            Log ($"CodeScanner Generate {ValidFormats} {text}");

            var barcodeWriter = new BarcodeWriterPixelData {
                Format = ValidFormats,
                Options = new ZXing.Common.EncodingOptions {
                    Width = (ValidFormats & BarcodeFormat.QR_CODE) != 0 ? 128 : 512,
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

            Log ($"CodeScanner.OnWrite\n{ Util.GetEventPersistentListenersInfo (OnWrite)}");
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