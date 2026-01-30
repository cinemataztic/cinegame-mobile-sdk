using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
    [ComponentReference ("Pinch zoom, rotate and pan a child UI element inside a parent rect (crop area). On non-multitouch platforms the mouse wheel is used for zooming.\n\nWhen Crop method is invoked, it will invoke the OnCropUV event with the UV Rect of the crop area. This can be used for setting the uvRect on a RawImage.\n\nIf the OnCropTexture event has any listeners, and the content is a UI Image or RawImage, a Texture2D cropped to the same area will be created and sent to these listeners.")]
    public class PinchZoomAndPan : BaseComponent {
        [HideInInspector][SerializeField] Shader CropShader;
        [SerializeField] RectTransform content;
        [Tooltip ("Maximum zoom factor")]
        [SerializeField] float _maxZoom = 4f;
        [Tooltip ("On platforms with mouse input we support wheel zoom")]
        [SerializeField] float _mouseWheelSensitivity = .5f;
        [Tooltip ("Allow user to rotate the image?")]
        [SerializeField] bool _allowRotation = true;

        [Tooltip ("Check boundaries against inner circle. Only relevant if Allow Rotation is checked")]
        [SerializeField] bool _circular = false;

        [Tooltip ("If allow rotation is unchecked, the UV Rect of the crop area")]
        [SerializeField] UnityEvent<Rect> OnCropUV;

        [Tooltip ("Invoked with a new Texture2D of just the cropped area")]
        [SerializeField] UnityEvent<Texture2D> OnCropTexture;

        RectTransform parentRT;
        Camera cam;

        Material blitMaterial;
        readonly float minZoom = .1f;
        Vector3 maxZoomVector;

        /// <summary>
        /// ReadPixels can only write to uncompressed formats like RGBA32, RGB24, RGB565 and RGBA4444
        /// </summary>
        readonly TextureFormat textureFormat = TextureFormat.RGB24;

#if UNITY_EDITOR
        void OnValidate () {
            CropShader = Shader.Find ("Hidden/UnlitCrop");
        }
#endif

        void Start () {
            maxZoomVector = new Vector3 (_maxZoom, _maxZoom, 1f);
            blitMaterial = new Material (CropShader) {
                color = Color.white
            };
        }

        void OnEnable () {
            Input.multiTouchEnabled = true;
            if (content == null) {
                content = GetComponent<RectTransform> ();
            }
            parentRT = content.parent.GetComponent<RectTransform> ();
            var canvas = content.GetComponentInParent<Canvas> (true);
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) {
                cam = canvas.worldCamera;
            }
        }

        Vector2 origPos1, origPos2;
        Quaternion origRot;
        float origM;
        Vector3 origScale;

        int lastTc;

        void Update () {
#if UNITY_STANDALONE || UNITY_EDITOR
            float scrollWheelInput = Input.GetAxis ("Mouse ScrollWheel");
            if (Mathf.Abs (scrollWheelInput) > float.Epsilon) {
                var contentSize = content.rect.size;
                var _startPinchScreenPosition = Input.mousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle (content, _startPinchScreenPosition, cam, out Vector2 _startPinchCenterPosition);
                var pivot = new Vector2 (_startPinchCenterPosition.x / content.rect.width, _startPinchCenterPosition.y / content.rect.height);
                var deltaPosition = new Vector3 (pivot.x * contentSize.x, pivot.y * contentSize.y) * content.localScale.x;
                content.pivot += pivot;
                content.localPosition += deltaPosition;

                var s = Mathf.Clamp (content.localScale.x * Mathf.Max (.000001f, 1 + scrollWheelInput * _mouseWheelSensitivity), minZoom, _maxZoom);
                content.localScale = new Vector3 (s, s);

                CheckBounds ();
            }

            var pos1 = Input.mousePosition;
            var pos2 = pos1;
            var tc = 0;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            MacTrackpad.Poll ();
            if (MacTrackpad.deviceCount != 0) {
                var dev = MacTrackpad.devices [0];
                tc = dev.touchCount;
                if (tc != 0) {
                    pos1 = dev.touches [0].position;
                    pos1.x *= Screen.width;
                    pos1.y *= Screen.height;
                    pos2 = pos1;
                    if (tc > 1) {
                        pos2 = dev.touches [1].position;
                        pos2.x *= Screen.width;
                        pos2.y *= Screen.height;
                    }
                }
            }
#endif
            if (tc == 0) {
                if (!Input.GetMouseButton (0)) {
                    lastTc = 0;
                    return;
                }
                tc = 1;
            }
#else
            var tc = Input.touchCount;
            if (tc == 0) {
                lastTc = 0;
                return;
            }
            var pos1 = Input.touches [0].position;
            var pos2 = tc == 2 ? Input.touches [1].position : pos1;
#endif
            RectTransformUtility.ScreenPointToLocalPointInRectangle (content, (pos1 + pos2) / 2, cam, out Vector2 _lpir1);
            var offset = _lpir1;
            content.anchoredPosition += (Vector2)(content.localRotation * offset) * content.localScale.x;
            if (tc == 1) {
                if (tc != lastTc) {
                    content.pivot += new Vector2 (offset.x / content.rect.width, offset.y / content.rect.height);
                }
            } else {
                var m = (pos2 - pos1).magnitude;
                if (tc != lastTc) {
                    content.pivot += new Vector2 (offset.x / content.rect.width, offset.y / content.rect.height);
                    origPos1 = pos1;
                    origPos2 = pos2;
                    origM = m;
                    origScale = content.localScale;
                    origRot = content.localRotation;
                } else {
                    if (_allowRotation) {
                        var angle = Vector2.SignedAngle (origPos2 - origPos1, pos2 - pos1);
                        content.localRotation = origRot * Quaternion.Euler (0f, 0f, angle);
                    }
                    var scale = origScale * m / origM;
                    content.localScale = Vector3.Min (maxZoomVector, scale);
                }
            }
            lastTc = tc;

            CheckBounds ();
        }

        /// <summary>
        /// Check if parent bounds are within the content rect
        /// </summary>
        public void CheckBounds () {
            var contentRect = content.rect;
            var bounds = CalculateRelativeRectTransformBounds (content, parentRT);

            var _r = parentRT.rect.width / (2f * content.localScale.x);

            if (_circular) {
                bounds.size = new Vector3 (_r * 2f, _r * 2f);
            }

            if (bounds.size.x > contentRect.width || bounds.size.y > contentRect.height) {
                var s = Mathf.Max (bounds.size.x / contentRect.width, bounds.size.y / contentRect.height);
                content.localScale = new Vector3 (content.localScale.x * s, content.localScale.y * s);
                bounds = CalculateRelativeRectTransformBounds (content, parentRT);
                _r = parentRT.rect.width / (2f * content.localScale.x);
                contentRect = content.rect;
            }

            var xVec = content.localRotation * Vector3.right * content.localScale.x;
            var yVec = content.localRotation * Vector3.up * content.localScale.y;

            if (_circular) {
                bounds.SetMinMax (
                    new Vector3 (bounds.center.x - _r, bounds.center.y - _r),
                    new Vector3 (bounds.center.x + _r, bounds.center.y + _r)
                );
            }

            if (bounds.min.x < contentRect.xMin) {
                content.localPosition += xVec * (bounds.min.x - contentRect.xMin);
            } else if (bounds.max.x > contentRect.xMax) {
                content.localPosition += xVec * (bounds.max.x - contentRect.xMax);
            }

            if (bounds.min.y < contentRect.yMin) {
                content.localPosition += yVec * (bounds.min.y - contentRect.yMin);
            } else if (bounds.max.y > contentRect.yMax) {
                content.localPosition += yVec * (bounds.max.y - contentRect.yMax);
            }
        }

        static readonly Vector3 [] s_Corners = new Vector3 [4];

        /// <summary>
        /// Similar to the one in RectTransformUtility, but only considers the one RectTransform supplied rather than all children
        /// </summary>
        static Bounds CalculateRelativeRectTransformBounds (RectTransform root, RectTransform child) {
            var vMin = new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3 (float.MinValue, float.MinValue, float.MinValue);
            var toLocal = root.worldToLocalMatrix;
            child.GetWorldCorners (s_Corners);
            for (int i = 0; i < 4; i++) {
                Vector3 v = toLocal.MultiplyPoint3x4 (s_Corners [i]);
                vMin = Vector3.Min (v, vMin);
                vMax = Vector3.Max (v, vMax);
            }
            var b = new Bounds (vMin, Vector3.zero);
            b.Encapsulate (vMax);
            return b;
        }

        /// <summary>
		/// Crop the content to the parent rect
		/// </summary>
        public void Crop () {
            Rect uvRect;
            if (!_allowRotation) {
                var child = RectTransformUtility.CalculateRelativeRectTransformBounds (parentRT, content);
                var parent = parentRT.rect;
                var x = (parent.min.x - child.min.x) / child.size.x;
                var y = (parent.min.y - child.min.y) / child.size.y;
                var w = (parent.max.x - parent.min.x) / child.size.x;
                var h = (parent.max.y - parent.min.y) / child.size.y;
                uvRect = new Rect (x, y, w, h);

                Log ($"OnCropUV x={x:0.##} y={y:0.##} w={w:0.##} h={h:0.##}");
                OnCropUV.Invoke (uvRect);
            }

            if (OnCropTexture.GetPersistentEventCount () == 0)
                return;

            Texture2D texture = null;
            var ri = content.GetComponentInChildren<RawImage> ();
            if (ri != null) {
                texture = ri.texture as Texture2D;
            } else {
                var i = content.GetComponentInChildren<Image> ();
                if (i != null) {
                    texture = i.sprite.texture;
                }
            }
            if (texture == null) {
                LogError ("No texture to crop!");
                return;
            }
            blitMaterial.mainTexture = texture;

            var parentSize = parentRT.rect.size;
            var mipmap = texture.mipmapCount > 1;
            var _w = (int)(texture.width  * (parentSize.x / content.rect.size.x) / content.localScale.x);
            var _h = (int)(texture.height * (parentSize.y / content.rect.size.y) / content.localScale.y);
            var _sz = TextureUtility.GetAdjustedSize (TextureFormat.ASTC_4x4, _w, _h, mipmap);
            _w = _sz.x;
            _h = _sz.y;
            var croppedTexture = new Texture2D (_w, _h, textureFormat, mipmap) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = texture.filterMode,
                anisoLevel = texture.anisoLevel,
            };
            if (croppedTexture == null) {
                LogError ($"Unable to create new texture width={_w} height={_h}");
                return;
            }

            var tmpRenderTexture = RenderTexture.GetTemporary (_w, _h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            if (tmpRenderTexture == null) {
                LogError ($"Unable to get temp rendertexture width={_w} height={_h}");
                return;
            }

            Vector3 [] corners = new Vector3 [4];
            parentRT.GetWorldCorners (corners);
            // corners[0] = Bottom Left
            // corners[1] = Top Left
            // corners[2] = Top Right
            // corners[3] = Bottom Right

            // Convert World corners to Content's Local Space
            Vector3 bl_Local = content.InverseTransformPoint (corners [0]);
            Vector3 tl_Local = content.InverseTransformPoint (corners [1]);
            Vector3 br_Local = content.InverseTransformPoint (corners [3]);

            // Convert Local Space pixels to Normalized UV coords (0 to 1)
            Vector2 bl_UV = LocalPointToUV (bl_Local, content.rect);
            Vector2 tl_UV = LocalPointToUV (tl_Local, content.rect);
            Vector2 br_UV = LocalPointToUV (br_Local, content.rect);

            // Build the Basis Vectors
            // The shader expects a matrix that transforms the Quad's (0,0) to BL_UV
            // and (1,0) to BR_UV, etc.
            Vector2 origin = bl_UV;
            Vector2 xAxis = br_UV - bl_UV;
            Vector2 yAxis = tl_UV - bl_UV;

            // Construct the Matrix
            // Col 0: X Axis, Col 1: Y Axis, Col 3: Origin translation
            var uvMatrix = Matrix4x4.identity;
            uvMatrix.m00 = xAxis.x; uvMatrix.m01 = yAxis.x; uvMatrix.m03 = origin.x;
            uvMatrix.m10 = xAxis.y; uvMatrix.m11 = yAxis.y; uvMatrix.m13 = origin.y;

            blitMaterial.SetMatrix ("_UVMatrix",uvMatrix);

            var prevRenderTexture = RenderTexture.active;
            RenderTexture.active = tmpRenderTexture;

            GL.Clear (true, true, Color.clear);

            blitMaterial.SetPass (0);
            Graphics.Blit (texture, tmpRenderTexture, blitMaterial);

            // Copy the pixels to the cropped texture
            croppedTexture.ReadPixels (new Rect (0, 0, _w, _h), 0, 0);
            croppedTexture.Apply (mipmap, makeNoLongerReadable: false);

            //release RenderTexture
            RenderTexture.active = prevRenderTexture;
            RenderTexture.ReleaseTemporary (tmpRenderTexture);

            Log ($"Created cropped Texture2D width={croppedTexture.width} height={croppedTexture.height}");
            OnCropTexture.Invoke (croppedTexture);
        }

        Vector2 LocalPointToUV (Vector3 localPoint, Rect contentRect) {
            float u = (localPoint.x - contentRect.x) / contentRect.width;
            float v = (localPoint.y - contentRect.y) / contentRect.height;
            return new Vector2 (u, v);
        }
    }
}
