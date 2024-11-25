using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
    /// <summary>
    /// Adjust the attached RectTransform AnchorMin and AnchorMax to the screen's safeArea (eg outside notch area on phones)
    /// </summary>
    [RequireComponent (typeof (RectTransform))]
    [ComponentReference ("Adjust the attached RectTransform AnchorMin and AnchorMax to the screen's safeArea (eg outside notch area on phones). If the corner Area properties are defined they will also be adjusted (these should have same parent as this transform for the calculations to work)\nIn the editor, if the user chooses a iPhone 12 Pro display (1170x2532 or 2532x1170), we simulate a safe area within notch and OS navigation bar.")]
    public class SafeAreaMargin : BaseComponent {
        [Tooltip ("Area to the left of the top notch in portrait")]
        public RectTransform TopLeftArea;
        [Tooltip ("Area to the right of the top notch in portrait")]
        public RectTransform TopRightArea;
        [Tooltip ("Area to the left of the bottom notch or navigation bar in portrait")]
        public RectTransform BottomLeftArea;
        [Tooltip ("Area to the right of the bottom notch or navigation bar in portrait")]
        public RectTransform BottomRightArea;

        [Tooltip ("If true then the margin applied to screens without notches will not apply to smaller safe areas, and the graphics will go all the way to the edge of the notch(es)")]
        public bool UseTightFit = true;

        [Tooltip ("Invoked if the screen has a safe-area smaller than fullscreen (eg phone display with notches and/or navigation bar)")]
        public UnityEvent OnAdjusted;

        void Awake () {
            var safeArea = Screen.safeArea;
#if UNITY_EDITOR
            //Simulate iPhone 12 Pro safeArea (portrait values taken from actual device)
            if (Screen.width == 1170 && Screen.height == 2532) {
                safeArea = new Rect (0, 102, Screen.width, Screen.height - 241);
            } else if (Screen.width == 2532 && Screen.height == 1170) {
                if (Screen.orientation == ScreenOrientation.LandscapeRight)
                    safeArea = new Rect (102, 0, Screen.width - 241, Screen.height);
                else
                    safeArea = new Rect (241, 0, Screen.width - 241, Screen.height);
            }
#endif
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (safeArea.width == screenWidth && safeArea.height == screenHeight) {
                Log ("SafeAreaMargin no adjustments necessary for this display");
                return;
            }
            var _rt = GetComponent<RectTransform> ();
            var safeAnchorMin = safeArea.position;
            var safeAnchorMax = safeAnchorMin + safeArea.size;
            safeAnchorMin.x /= screenWidth;
            safeAnchorMin.y /= screenHeight;
            safeAnchorMax.x /= screenWidth;
            safeAnchorMax.y /= screenHeight;
            if (UseTightFit) {
                safeAnchorMin = Vector2.Max (_rt.anchorMin, safeAnchorMin - _rt.anchorMin);
                safeAnchorMax = Vector2.Min (_rt.anchorMax, safeAnchorMax + Vector2.one - _rt.anchorMax);
            }
            _rt.anchorMin = safeAnchorMin;
            _rt.anchorMax = safeAnchorMax;
            Log ($"SafeAreaMargin safeAreaMin={safeArea.min} safeAreaMax={safeArea.max} safeAnchorMin={safeAnchorMin} safeAnchorMax={safeAnchorMax}");

            if (screenWidth <= screenHeight) {
                if (TopLeftArea != null)     TopLeftArea.anchorMin = new Vector2 (TopLeftArea.anchorMin.x, safeAnchorMax.y);
                if (TopRightArea != null)    TopRightArea.anchorMin = new Vector2 (TopRightArea.anchorMin.x, safeAnchorMax.y);
                if (BottomLeftArea != null)  BottomLeftArea.anchorMax = new Vector2 (BottomLeftArea.anchorMax.x, safeAnchorMin.y);
                if (BottomRightArea != null) BottomRightArea.anchorMax = new Vector2 (BottomRightArea.anchorMax.x, safeAnchorMin.y);
            } else {
                if (TopLeftArea != null)     TopLeftArea.anchorMax = new Vector2 (safeAnchorMin.x, TopLeftArea.anchorMax.x);
                if (TopRightArea != null)    TopRightArea.anchorMin = new Vector2 (safeAnchorMax.x, TopRightArea.anchorMin.y);
                if (BottomLeftArea != null)  BottomLeftArea.anchorMax = new Vector2 (safeAnchorMin.x, BottomLeftArea.anchorMax.y);
                if (BottomRightArea != null) BottomRightArea.anchorMax = new Vector2 (BottomRightArea.anchorMax.x, BottomRightArea.anchorMax.y);
            }

            var cutouts = Screen.cutouts;
#if UNITY_EDITOR
            //Simulate iPhone 12 Pro notch cutout (portrait values taken from actual device)
            if (screenWidth == 1170 && screenHeight == 2532) {
                cutouts = new Rect [1];
                cutouts [0] = new Rect (269, 2436, 630, 95);
            } else if (screenWidth == 2532 && screenHeight == 1170) {
                cutouts = new Rect [1];
                if (Screen.orientation == ScreenOrientation.LandscapeRight)
                    cutouts [0] = new Rect (2436, 269, 95, 630);
                else
                    cutouts [0] = new Rect (0, 269, 95, 630);
            }
#endif
            if (cutouts != null) {
                for (int i = 0; i < cutouts.Length; i++) {
                    var r = cutouts [i];
                    Log ($"SafeAreaMargin cutout {i} min={r.min} max={r.max}");
                    if (screenWidth <= screenHeight) {
                        if (r.yMin < safeArea.yMin) {
                            if (BottomLeftArea != null)  BottomLeftArea.anchorMax = Vector2.Min (BottomLeftArea.anchorMax, new Vector2 (r.xMin / screenWidth, safeAnchorMin.y));
                            if (BottomRightArea != null) BottomRightArea.anchorMin = Vector2.Max (BottomRightArea.anchorMin, new Vector2 (r.xMax / screenWidth, 0f));
                        } else if (r.yMax > safeArea.yMax) {
                            if (TopLeftArea != null)     TopLeftArea.anchorMax = Vector2.Min (TopLeftArea.anchorMax, new Vector2 (r.xMin / screenWidth, 1f));
                            if (TopRightArea != null)    TopRightArea.anchorMin = Vector2.Max (TopRightArea.anchorMin, new Vector2 (r.xMax / screenWidth, safeAnchorMax.y));
                        }
                    } else {
                        if (r.xMin < safeArea.xMin) {
                            if (TopLeftArea != null)     TopLeftArea.anchorMin = Vector2.Max (TopLeftArea.anchorMin, new Vector2 (0f, r.yMax / screenHeight));
                            if (BottomLeftArea != null)  BottomLeftArea.anchorMax = Vector2.Min (BottomLeftArea.anchorMax, new Vector2 (safeAnchorMin.x, r.yMin / screenHeight));
                        } else if (r.xMax > safeArea.xMax) {
                            if (TopRightArea != null)    TopRightArea.anchorMin = Vector2.Max (TopRightArea.anchorMin, new Vector2 (safeAnchorMax.x, r.yMax / screenHeight));
                            if (BottomRightArea != null) BottomRightArea.anchorMax = Vector2.Min (BottomRightArea.anchorMax, new Vector2 (1f, r.yMin / screenHeight));
                        }
                    }
                }
            }

            OnAdjusted.Invoke ();
        }
    }
}
