using UnityEngine;
using UnityEngine.UI;

namespace CineGame.MobileComponents {
	[ComponentReference ("Updates aspect ratio of the RectTransform or LayoutElement to the image sprite's aspect ratio")]
	[ExecuteAlways]
	[RequireComponent (typeof (Image))]
	public class DynamicAspectRatioFitter : BaseComponent {
		public enum DARFitterMode {
			UpdateWidth,
			UpdateHeight,
		}
		[Tooltip ("Whether to update width or height to the aspect ratio")]
		public DARFitterMode Mode;
		[Tooltip ("Whether the aspect ratio should be applied to the parent instead (eg in case of a masked image)")]
		public bool ApplyToParent;

		RectTransform myRectTransform;
		LayoutElement myLayoutElement;
		Image myImage;

		Sprite currentSprite;
		Rect currentRect;

		void Start () {
			var c = (Component)(ApplyToParent ? transform.parent : this);
            myRectTransform = c.GetComponent<RectTransform> ();
            myLayoutElement = c.GetComponent<LayoutElement> ();
            myImage = GetComponent<Image>();
        }

        void OnEnable() {
            currentSprite = null;
        }

        void UpdateAspectRatio() {
			if (currentSprite == null)
				return;

            // Calculate aspect ratio from sprite dimensions
            var aspectRatio = (float)currentSprite.rect.width / currentSprite.rect.height;

            // Set the height based on the width and aspect ratio
			if (Mode == DARFitterMode.UpdateHeight) {
                var width = myRectTransform.rect.width;
                var height = width / aspectRatio;
				if (myLayoutElement != null) {
					myLayoutElement.minHeight =
						myLayoutElement.preferredHeight = height;
				}
				myRectTransform.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, height);
			} else {
                var height = myRectTransform.rect.height;
                var width = height * aspectRatio;
                if (myLayoutElement != null) {
                    myLayoutElement.minWidth =
                        myLayoutElement.preferredWidth = width;
                }
                myRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            Log ($"Changed aspect ratio to {aspectRatio}");
        }

		void Update () {
			if (myImage.sprite != currentSprite || !myRectTransform.rect.Equals (currentRect)) {
				currentSprite = myImage.sprite;
				currentRect = myRectTransform.rect;
				UpdateAspectRatio ();
			}
		}

        void OnValidate() {
			Start ();
        }
    }
}
