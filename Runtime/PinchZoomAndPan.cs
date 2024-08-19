using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
    [ComponentReference ("Pinch zoom and pan a child UI element inside a parent rect (crop area). On non-multitouch platforms the mouse wheel is used for zooming.\n\nWhen Crop method is invoked, it will invoke the OnCropUV event with the UV Rect of the crop area. This can be used for setting the uvRect on a RawImage. If the OnCropTexture event has any listeners, and the content is a UI Image or RawImage, a Texture2D cropped to the same area will be created and sent to these listeners")]
    public class PinchZoomAndPan : BaseComponent {
        [SerializeField] float _minZoom = .1f;
        [SerializeField] float _maxZoom = 10;
        [SerializeField] RectTransform content;

        [Tooltip ("Invoked with the UV Rect of the crop area")]
        [SerializeField] UnityEvent<Rect> OnCropUV;

        [Tooltip ("Invoked with a new Texture2D of the cropped area")]
        [SerializeField] UnityEvent<Texture2D> OnCropTexture;

        bool _isPinching;
        bool _isPanning;
        float _lastPinchDist;
		readonly float _mouseWheelSensitivity = 1f;
        RectTransform parentRT;
        Camera cam;
        Vector2 _pivotOffset;

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

        void Update () {
            var size = content.rect.size;
            var tc = Input.touchCount;
            if (tc != 0 || Input.GetMouseButton (0)) {
                var pos1 = (Vector2)Input.mousePosition;
                var pos2 = tc > 1 ? Input.touches [1].position : pos1;

                var _newPivotScreenPosition = (pos1 + pos2) / 2;
                RectTransformUtility.ScreenPointToLocalPointInRectangle (content, _newPivotScreenPosition, cam, out Vector2 _newPivotRectPosition);
                var deltaPivot = new Vector2 (_newPivotRectPosition.x / content.rect.width, _newPivotRectPosition.y / content.rect.height);
                if (!_isPanning || (!_isPinching && tc > 1) || (_isPinching && tc == 1)) {
                    _pivotOffset = deltaPivot;
                }

                deltaPivot -= _pivotOffset;
                var deltaPosition = new Vector3 (deltaPivot.x * size.x, deltaPivot.y * size.y) * content.localScale.x;
                content.localPosition += deltaPosition;

                if (tc > 1) {
                    var _pinchDist = Distance (pos1, pos2) * content.localScale.x;
                    if (_lastPinchDist > float.Epsilon) {
                        var s = Mathf.Clamp (content.localScale.x * _pinchDist / _lastPinchDist, _minZoom, _maxZoom);
                        content.localScale = new Vector3 (s, s);
                    }
                    _lastPinchDist = _pinchDist;
                    _isPinching = true;
                } else {
                    _lastPinchDist = 0f;
                    _isPinching = false;
                }

                _isPanning = true;
            } else {
                _isPanning = false;
                _isPinching = false;
                _lastPinchDist = 0f;
            }

#if UNITY_EDITOR
            float scrollWheelInput = Input.GetAxis ("Mouse ScrollWheel");
            if (Mathf.Abs (scrollWheelInput) > float.Epsilon) {
                var _startPinchScreenPosition = Input.mousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle (content, _startPinchScreenPosition, cam, out Vector2 _startPinchCenterPosition);
                var pivot = new Vector2 (_startPinchCenterPosition.x / content.rect.width, _startPinchCenterPosition.y / content.rect.height);
                var deltaPosition = new Vector3 (pivot.x * size.x, pivot.y * size.y) * content.localScale.x;
                content.pivot += pivot;
                content.localPosition += deltaPosition;

                var s = Mathf.Clamp (content.localScale.x * Mathf.Max (.000001f, 1 + scrollWheelInput * _mouseWheelSensitivity), _minZoom, _maxZoom);
                content.localScale = new Vector3 (s, s);
            }
#endif
            //Check bounds, limit zoom and pan to encapsulate parent

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds (parentRT, content);
            var parentRect = parentRT.rect;

            if (bounds.size.x < parentRect.width || bounds.size.y < parentRect.height) {
                var s = Mathf.Max (parentRect.width / bounds.size.x, parentRect.height / bounds.size.y);
                content.localScale = new Vector3 (content.localScale.x * s, content.localScale.y * s);
                bounds = RectTransformUtility.CalculateRelativeRectTransformBounds (parentRT, content);
                parentRect = parentRT.rect;
            }

            if (bounds.min.x > parentRect.xMin) {
                content.localPosition -= new Vector3 (bounds.min.x - parentRect.xMin, 0f);
            } else if (bounds.max.x < parentRect.xMax) {
                content.localPosition -= new Vector3 (bounds.max.x - parentRect.xMax, 0f);
            }

            if (bounds.min.y > parentRect.yMin) {
                content.localPosition -= new Vector3 (0f, bounds.min.y - parentRect.yMin);
            } else if (bounds.max.y < parentRect.yMax) {
                content.localPosition -= new Vector3 (0f, bounds.max.y - parentRect.yMax);
            }
        }

        float Distance (Vector2 pos1, Vector2 pos2) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle (content, pos1, cam, out pos1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle (content, pos2, cam, out pos2);
            return Vector2.Distance (pos1, pos2);
        }

        /// <summary>
		/// Invokes the OnCrop event with the UV Rect of the parent (crop area) inside the child (content). This can be used to call Texture.GetPixels and create a cropped texture, or set RawImage UV coordinates
		/// </summary>
        public void Crop () {
            var child = RectTransformUtility.CalculateRelativeRectTransformBounds (parentRT, content);
            var parent = parentRT.rect;
            var x = (parent.min.x - child.min.x) / child.size.x;
            var y = (parent.min.y - child.min.y) / child.size.y;
            var w = (parent.max.x - parent.min.x) / child.size.x;
            var h = (parent.max.y - parent.min.y) / child.size.y;
            Log ($"Cropping coordinates: {x:0.##},{y:0.##},{w:0.##},{h:0.##}");
            var uvRect = new Rect (x, y, w, h);

            OnCropUV.Invoke (uvRect);

            if (OnCropTexture.GetPersistentEventCount () != 0) {
                Texture2D texture = null;
                var ri = content.GetComponentInChildren<RawImage> ();
                if (ri != null) {
                    texture = ri.texture as Texture2D;
                } else {
                    var i = content.GetComponentInChildren<Image> ();
                    if (i != null) {
                        texture = i.mainTexture as Texture2D;
                    }
                }
                if (texture != null) {
                    Util.CropTexture (texture, uvRect, mipChain: true, readable: true, out Texture2D croppedTexture);
                    Log ($"Created cropped Texture2D with dimensions: {croppedTexture.width}x{croppedTexture.height}");
                    OnCropTexture.Invoke (croppedTexture);
                }
            }
        }
    }
}