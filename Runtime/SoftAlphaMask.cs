using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CineGame.MobileComponents {
    [ComponentReference ("Alpha-blended masking of UI elements. When the script is added to a GameObject, all the MaskableGraphic children will have their materials replaced with one using the SoftMaskShader.")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class SoftAlphaMask : BaseComponent {
        [HideInInspector][SerializeField] Shader SoftMaskShader;

        [Tooltip ("Mask works better in some cases using LateUpdate instead of Update.")]
        public bool UseLateUpdate = false;

        private readonly Vector3 [] worldCorners = new Vector3 [4];
        private readonly List<MaskableGraphic> graphicsToUpdate = new ();
        private Material mat;

        private Image _maskImage;
        private Image MaskImage {
            get {
                if (_maskImage == null) {
                    _maskImage = GetComponent<Image> ();
                }
                return _maskImage;
            }
        }
        private RectTransform _rt;
        private RectTransform RectTransform {
            get {
                if (_rt == null) {
                    _rt = GetComponent<RectTransform> ();
                }
                return _rt;
            }
        }

#if UNITY_EDITOR
        void OnValidate () {
            SoftMaskShader = Shader.Find ("UI Extensions/SoftMaskShader");
        }
#endif

        void OnEnable () {
#if UNITY_EDITOR
            OnValidate ();
#endif
            OnTransformChildrenChanged ();
        }

        void OnTransformChildrenChanged () {
            Log ("SoftAlphaMask reapply effect on all MaskableGraphic children ...");

            if (mat == null) {
                mat = new Material (SoftMaskShader);
            }

            var maskImage = MaskImage;
            graphicsToUpdate.Clear ();
            CollectChildGraphics (transform, graphicsToUpdate, maskImage);

            UpdateMaskParameters ();
        }

        /// <summary>
        /// Recursively gathers child graphics, but stops at sub-trees that have their own SoftAlphaMask.
        /// </summary>
        private void CollectChildGraphics (Transform current, List<MaskableGraphic> results, Image maskImage) {
            var defaultUiMaterial = Graphic.defaultGraphicMaterial;
            var l = current.childCount;
            for (int i = 0; i < l; i++) {
                Transform child = current.GetChild (i);

                // Stop traversing down this branch if a nested Mask or SoftAlphaMask is found
                if (child.GetComponent<SoftAlphaMask> () != null || child.GetComponent<Mask> () != null) {
                    continue;
                }

                var graphic = child.GetComponent<MaskableGraphic> ();
                if (graphic != null && graphic != maskImage) {
                    if (graphic.material != defaultUiMaterial) {
                        Log ($"WARNING: Not default UI material on {graphic.gameObject.GetScenePath ()}. The graphic will most likely not render correctly after applying SoftMaskShader.");
                    }
                    graphic.material = mat;
                    var isEnabled = graphic.enabled;
                    graphic.enabled = false;
                    graphic.SetAllDirty ();

                    var text = graphic.GetComponent<Text> ();
                    if (text != null && transform.parent != null) {
                        if (transform.parent.GetComponent<Mask> () == null) {
                            transform.parent.gameObject.AddComponent<Mask> ();
                        }
                        transform.parent.GetComponent<Mask> ().enabled = false;
                    }

                    if (isEnabled) {
                        graphic.enabled = true;
                    }

                    results.Add (graphic);
                }

                // Recurse into deeper children
                CollectChildGraphics (child, results, maskImage);
            }
        }

        void Update () {
            if (UseLateUpdate == false) {
                UpdateMaskParameters ();
            }
        }

        void LateUpdate () {
            if (UseLateUpdate == true) {
                UpdateMaskParameters ();
            }
        }

        void UpdateMaskParameters () {
            var maskImage = MaskImage;
            if (mat == null || maskImage == null || maskImage.sprite == null || maskImage.canvas == null)
                return;
            var sprite = maskImage.sprite;

            var scaledBorder = MaskImage.type == Image.Type.Simple ? Vector4.zero : sprite.border * maskImage.pixelsPerUnitMultiplier;

            // Get absolute rect boundaries inside texture or atlas
            Rect spriteRectPacked = sprite.packed ? sprite.textureRect : sprite.rect;

            // Calculate anchor positioning corrections for packed Sprite Atlases
            float baseAtlasX = spriteRectPacked.xMin - sprite.textureRectOffset.x;
            float baseAtlasY = spriteRectPacked.yMin - sprite.textureRectOffset.y;

            var maskRt = RectTransform;
            var maskRect = maskRt.rect;
            var worldToLocal = maskRt.worldToLocalMatrix;
            var rectMin = new Vector4 (maskRect.xMin, maskRect.yMin, 0, 0);
            var rectSize = new Vector4 (maskRect.width, maskRect.height, 0, 0);

            var spriteRect = new Vector4 (sprite.rect.width, sprite.rect.height, baseAtlasX, baseAtlasY);
            var textureSize = new Vector4 (sprite.texture.width, sprite.texture.height, 0, 0);
            var texture = sprite.texture;

            var l = graphicsToUpdate.Count;
            for (int i = 0; i < l; i++) {
                var graphic = graphicsToUpdate [i];
                if (graphic == maskImage)
                    continue;
                if (graphic == null) {
                    OnTransformChildrenChanged ();
                    return;
                }
                var matInst = graphic.materialForRendering;

                // Assign texture tracking variables
                matInst.SetTexture ("_AlphaMask", texture);
                matInst.SetMatrix ("_MaskWorldToLocal", worldToLocal);
                matInst.SetVector ("_MaskRectMin", rectMin);
                matInst.SetVector ("_MaskRectSize", rectSize);
                matInst.SetVector ("_SpriteRect", spriteRect);
                matInst.SetVector ("_SpriteBorder", scaledBorder);
                matInst.SetVector ("_TextureSize", textureSize);
            }
        }
    }
}
