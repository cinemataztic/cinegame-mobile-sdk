using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
    [ComponentReference ("Pinch zoom, rotate and pan a child UI element inside a parent rect (crop area). On non-multitouch platforms the mouse wheel is used for zooming.\n\nWhen Crop method is invoked, it will invoke the OnCropUV event with the UV Rect of the crop area. This can be used for setting the uvRect on a RawImage.\n\nIf the OnCropTexture event has any listeners, and the content is a UI Image or RawImage, a Texture2D cropped to the same area will be created and sent to these listeners.\n\nNote that the Unlit/Texture built-in shader must be added to \"Always Included Shaders\" in Graphics Settings")]
    public class PinchZoomAndPan : BaseComponent {
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

        Mesh quadMesh;
        Material blitMaterial;
        readonly float minZoom = .1f;
        Vector3 maxZoomVector;

        /// <summary>
        /// ReadPixels can only write to uncompressed formats like RGBA32, RGB24, RGB565 and RGBA4444
        /// </summary>
        readonly TextureFormat textureFormat = TextureFormat.RGB24;

        void Start () {
            maxZoomVector = new Vector3 (_maxZoom, _maxZoom, 1f);
            var unlitShader = Shader.Find ("Unlit/Texture");
            if (unlitShader == null) {
                LogError ("Unlit/Texture shader not found. Cropping to texture will not work.");
            }
            blitMaterial = new Material (unlitShader);
            quadMesh = new Mesh {
                vertices = new Vector3 [] {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3( 0.5f, -0.5f, 0),
                    new Vector3( 0.5f,  0.5f, 0),
                    new Vector3(-0.5f,  0.5f, 0),
                },

                uv = new Vector2 [] {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                },

                triangles = new int [] {
                    0, 2, 1,
                    0, 3, 2,
                }
            };
            quadMesh.RecalculateNormals ();
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
        void CheckBounds () {
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

            uvRect = new Rect (0f, 0f, 1f, 1f);

            Texture2D texture = null;
            var ri = content.GetComponentInChildren<RawImage> ();
            if (ri != null) {
                texture = ri.texture as Texture2D;
            } else {
                var i = content.GetComponentInChildren<Image> ();
                if (i != null) {
                    texture = i.sprite.texture;
                    if (texture != null) {
                        uvRect = i.sprite.textureRect;
                        uvRect.min += i.sprite.textureRectOffset;
                        //Log ("sprite rect " + uvRect);
                        uvRect = new Rect (uvRect.x / texture.width, uvRect.y / texture.height, uvRect.width / texture.width, uvRect.height / texture.height);
                    }
                }
            }
            if (texture == null) {
                LogError ("No texture to crop!");
                return;
            }
            //Log ("uvRect " + uvRect);
            blitMaterial.mainTexture = texture;
            //blitMaterial.mainTextureScale = uvRect.size;
            //blitMaterial.mainTextureOffset = uvRect.min;

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

            var prevRenderTexture = RenderTexture.active;
            RenderTexture.active = tmpRenderTexture;

            GL.PushMatrix ();
            var proj = Matrix4x4.Ortho (0, 1, 0, 1, -1, 1);

            // apply scaling around center (0.5, 0.5)
            var scaleM = Matrix4x4.TRS (
                new Vector3 (0.5f, 0.5f, 0),
                Quaternion.identity,
                new Vector3 (1f / parentSize.x, 1f / parentSize.y, 1)
            );
            GL.LoadProjectionMatrix (proj * scaleM);

            var scale = new Vector3 (content.rect.size.x * content.localScale.x, content.rect.size.y * content.localScale.y);

            var toLocal = parentRT.worldToLocalMatrix;
            content.GetWorldCorners (s_Corners);
            var v1 = toLocal.MultiplyPoint3x4 (s_Corners [0]);
            var v2 = toLocal.MultiplyPoint3x4 (s_Corners [2]);
            var pos = new Vector3 ((v1.x + v2.x) * .5f, (v1.y + v2.y) * .5f);

            var matrix = Matrix4x4.TRS (pos, content.localRotation, scale);
            blitMaterial.SetPass (0);
            Graphics.DrawMeshNow (quadMesh, matrix);

            GL.PopMatrix ();

            // Copy the pixels to the cropped texture
            croppedTexture.ReadPixels (new Rect (0, 0, _w, _h), 0, 0);
            croppedTexture.Apply (mipmap, makeNoLongerReadable: false);

            //release RenderTexture
            RenderTexture.active = prevRenderTexture;
            RenderTexture.ReleaseTemporary (tmpRenderTexture);

            Log ($"Created cropped Texture2D width={croppedTexture.width} height={croppedTexture.height}");
            OnCropTexture.Invoke (croppedTexture);
        }
    }
}