using UnityEngine;
using UnityEngine.Events;

namespace CineGame.MobileComponents {
	/// <summary>
	/// Creates a rotation on a UI element according to the projection of a world 3D position on the canvas. This can be used for eg arrows on HUD overlays.
	/// </summary>
	[ComponentReference ("Creates a rotation on a UI element according to the projection of a world 3D position on the canvas. This can be used for eg arrows on HUD overlays.")]
	[ExecuteAlways]
	public class AnchorRotationFrom3D : BaseComponent {
		[Tooltip ("Transform to project")]
		public Transform PositionSource;

		[Tooltip ("Offset in local source transform space")]
		public Vector3 OffsetInSourceSpace;

		[Tooltip ("Radius in canvas space of center")]
		public float CenterRadius = 50f;

		[Tooltip ("Check if you want the UI element projected onto the border of the parent rect. The parent must have pivot point in (0.5,0.5)")]
		public bool ProjectOnRect;

		[Tooltip ("Invoked when the projected world position enters the center or parent rect")]
		public UnityEvent OnCenter;

		[Tooltip ("Invoked when the projected world position leaves the center or parent rect")]
		public UnityEvent OnHorizon;

		Vector2 localPoint;
		bool isInCenter, isRun;

		RectTransform myRectTransform;
		RectTransform parentRectTransform;
		Camera canvasCamera;

		void OnEnable () {
			myRectTransform = GetComponent<RectTransform> ();
			parentRectTransform = transform.parent.GetComponent<RectTransform> ();
			canvasCamera = GetComponentInParent<Canvas> ().worldCamera;
			isRun = false;
		}

		void LateUpdate () {
			var screenPoint3 = Camera.main.WorldToScreenPoint (PositionSource.TransformPoint (OffsetInSourceSpace));
			/*var dz = screenPoint3.z / Camera.main.nearClipPlane;
			canvasGroup.alpha = Mathf.Clamp01 (dz);
			if (dz < 0f)
				return;*/
			RectTransformUtility.ScreenPointToLocalPointInRectangle (parentRectTransform, new Vector2 (screenPoint3.x, screenPoint3.y), canvasCamera, out localPoint);
			var delta = localPoint;
			bool iic;

			if (ProjectOnRect) {
				var r = parentRectTransform.rect;
				var center = new Vector2 ((r.min.x + r.max.x) / 2, (r.min.y + r.max.y) / 2);
				Vector2 pos = center;
				if (delta.y < -float.Epsilon) {
					pos.x = delta.x * (r.min.y - center.y) / delta.y + center.x;
					pos.y = r.min.y;
					if (pos.x < r.min.x) {
						pos.x = r.min.x;
						pos.y = delta.y * (r.min.x - center.x) / delta.x + center.y;
					} else if (pos.x > r.max.x) {
						pos.x = r.max.x;
						pos.y = delta.y * (r.max.x - center.x) / delta.x + center.y;
					}
				} else if (delta.y > float.Epsilon) {
					pos.x = delta.x * (r.max.y - center.y) / delta.y + center.x;
					pos.y = r.max.y;
					if (pos.x < r.min.x) {
						pos.x = r.min.x;
						pos.y = delta.y * (r.min.x - center.x) / delta.x + center.y;
					} else if (pos.x > r.max.x) {
						pos.x = r.max.x;
						pos.y = delta.y * (r.max.x - center.x) / delta.x + center.y;
					}
				} else if (delta.x < 0) {
					pos.x = r.min.x;
				} else {
					pos.x = r.max.x;
				}
				myRectTransform.localPosition = pos;

				iic = r.Contains (localPoint);
			} else {
				delta -= myRectTransform.anchoredPosition;
				iic = delta.sqrMagnitude < CenterRadius * CenterRadius;
			}

			if (iic) {
				if (!isRun || !isInCenter)
					OnCenter?.Invoke ();
			} else {
				if (!isRun || isInCenter)
					OnHorizon?.Invoke ();
			}
			isInCenter = iic;
			isRun = true;

			myRectTransform.up = delta;
		}
	}
}
