using UnityEngine;

namespace CineGame.MobileComponents {
	/// <summary>
	/// Projects a world-space coordinate onto a canvas as a 2D AnchoredPosition coordinate.
	/// Useful for creating HUD overlays, speech bubbles etc.
	/// </summary>
	[ComponentReference ("Projects a world 3D position onto a screen-aligned canvas. This can be used for eg HUD overlays or speech bubbles.")]
	[ExecuteAlways]
	[DefaultExecutionOrder (1000)]
	[RequireComponent (typeof (CanvasGroup))]
	public class AnchorPositionFrom3D : BaseComponent {
		[Tooltip ("Transform to project")]
		public Transform PositionSource;

		[Tooltip ("Offset in local source transform space")]
		public Vector3 OffsetInSourceSpace;

		[Tooltip ("Offset on the destination canvas after projection")]
		public Vector2 OffsetInRectTransform;

		[Tooltip ("If true then the entire rect will always stay within screen boundaries")]
		public bool StayOnScreen;

		[Tooltip ("Align with world Y axis")]
		public bool AlignWithWorldAxis;

		[Tooltip ("When using the Interpolate method to gently move towards the projected position")]
		public float InterpTime = .75f;

		Vector2 localPoint;

		RectTransform myRectTransform;
		RectTransform parentRectTransform;
		Camera canvasCamera;
		CanvasGroup canvasGroup;
		readonly Vector3 [] worldCorners = new Vector3 [4];
		float InterpStartTime;
		Vector2 InterpStartPos;
		bool bInterp;

		void Start () {
			myRectTransform = GetComponent<RectTransform> ();
			parentRectTransform = transform.parent.GetComponent<RectTransform> ();
			canvasCamera = GetComponentInParent<Canvas> ().worldCamera;
			canvasGroup = GetComponent<CanvasGroup> ();
		}

		void LateUpdate () {
			if (PositionSource == null)
				return;
			var screenPoint3 = Camera.main.WorldToScreenPoint (PositionSource.TransformPoint (OffsetInSourceSpace));
			var dz = screenPoint3.z / Camera.main.nearClipPlane;
			if (dz < 1f)
				canvasGroup.alpha = Mathf.Clamp01 (dz);
			if (dz < 0f)
				return;
			RectTransformUtility.ScreenPointToLocalPointInRectangle (parentRectTransform, new Vector2 (screenPoint3.x, screenPoint3.y), canvasCamera, out localPoint);
			localPoint += OffsetInRectTransform;
			if (bInterp) {
				var t = (Time.time - InterpStartTime) / InterpTime;
				myRectTransform.anchoredPosition = Vector2.Lerp (InterpStartPos, localPoint, Interpolation.EaseOutQuad (t));
				bInterp = t < 1f;
			} else {
				myRectTransform.anchoredPosition = localPoint;
			}

			if (StayOnScreen) {
				myRectTransform.GetWorldCorners (worldCorners);
				var min = canvasCamera == null ? worldCorners [0] : canvasCamera.WorldToScreenPoint (worldCorners [0]);
				var max = canvasCamera == null ? worldCorners [2] : canvasCamera.WorldToScreenPoint (worldCorners [2]);
				myRectTransform.anchoredPosition += new Vector2 (
					Mathf.Max (0f, -min.x) - Mathf.Max (0f, max.x - Screen.width),
					Mathf.Max (0f, -min.y) - Mathf.Max (0f, max.y - Screen.height)
				);
			}

			if (AlignWithWorldAxis) {
				var rotCam = (canvasCamera != null) ? canvasCamera : Camera.main;
				var myRot = myRectTransform.rotation;
				myRectTransform.localRotation = Quaternion.Euler (myRot.eulerAngles.x, myRot.eulerAngles.y, -rotCam.transform.eulerAngles.z);
			}

		}

		public void Interpolate () {
			Interpolate (InterpTime);
		}

		public void Interpolate (float time) {
			bInterp = true;
			InterpStartTime = Time.time;
			InterpStartPos = myRectTransform.anchoredPosition;
			InterpTime = time;
		}

		public void SetStayOnScreen (bool enable) {
			StayOnScreen = enable;
		}
	}
}
