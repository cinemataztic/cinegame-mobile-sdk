using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CineGame.MobileComponents {
    [ComponentReference ("Alpha-blended masking of UI elements. When the script is added to a GameObject, all the MaskableGraphic children will have their materials replaced with one using the SoftMaskShader.")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class SoftAlphaMask : BaseComponent {
        [HideInInspector][SerializeField] Shader SoftMaskShader;

        [Tooltip ("At what point to apply the alpha min range 0-1")]
        [Range (0, 1)]
        public float CutOff = 0;

        [Tooltip ("Implement a hard blend based on the Cutoff")]
        public bool HardBlend = false;

        [Tooltip ("Flip the masks alpha value")]
        public bool FlipAlphaMask = false;

        [Tooltip ("If a different Mask Scaling Rect is given, and this value is true, the area around the mask will not be clipped")]
        public bool DontClipMaskScalingRect = false;

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
            OnTransformChildrenChanged ();
        }

        void OnTransformChildrenChanged () {
            Log ("SoftAlphaMask reapply effect on all MaskableGraphic children ...");

            if (mat == null) {
                mat = new Material (SoftMaskShader);
            }

            var maskImage = GetComponent<Image> ();
            graphicsToUpdate.Clear ();
            foreach (var graphic in GetComponentsInChildren<MaskableGraphic> (true)) {
                if (graphic == maskImage) {
                    continue;
                }
                graphic.material = mat;
                var isEnabled = graphic.enabled;
                graphic.enabled = false;
                graphic.SetAllDirty ();
                if (graphic.TryGetComponent(out Text _)) {
                    if (transform.parent.GetComponent<Mask> () == null)
                        transform.parent.gameObject.AddComponent<Mask> ();

                    transform.parent.GetComponent<Mask> ().enabled = false;
                }
                if (isEnabled) {
                    graphic.enabled = true;
                }
                graphicsToUpdate.Add (graphic);
            }

            UpdateMaskParameters ();
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
            if (mat != null && maskImage != null && maskImage.sprite != null && maskImage.canvas != null) {
                var worldRect = GetCanvasRect (maskImage);
                Sprite sprite = maskImage.sprite;

                // Get absolute rect boundaries inside texture or atlas
                Rect spriteRectPacked = sprite.packed ? sprite.textureRect : sprite.rect;

                // Calculate anchor positioning corrections for packed Sprite Atlases
                float baseAtlasX = spriteRectPacked.xMin - sprite.textureRectOffset.x;
                float baseAtlasY = spriteRectPacked.yMin - sprite.textureRectOffset.y;

                for (int i = 0; i < graphicsToUpdate.Count; i++) {
                    if (graphicsToUpdate [i] == null) {
                        OnTransformChildrenChanged ();
                        return;
                    }

                    var matInst = graphicsToUpdate [i].materialForRendering;

                    // Assign texture tracking variables
                    matInst.SetTexture ("_AlphaMask", sprite.texture);
                    matInst.SetVector ("_MaskRectMin", new Vector4 (worldRect.xMin, worldRect.yMin, 0, 0));
                    matInst.SetVector ("_MaskRectSize", new Vector4 (worldRect.width, worldRect.height, 0, 0));
                    matInst.SetVector ("_SpriteRect", new Vector4 (sprite.rect.width, sprite.rect.height, baseAtlasX, baseAtlasY));
                    matInst.SetVector ("_SpriteBorder", sprite.border);
                    matInst.SetVector ("_TextureSize", new Vector4 (sprite.texture.width, sprite.texture.height, 0, 0));

                    // Keep standard configuration values updated in real-time
                    matInst.SetInt ("_HardBlend", HardBlend ? 1 : 0);
                    matInst.SetInt ("_FlipAlphaMask", FlipAlphaMask ? 1 : 0);
                    matInst.SetInt ("_NoOuterClip", DontClipMaskScalingRect ? 1 : 0);
                    matInst.SetFloat ("_CutOff", CutOff);
                }
            }
        }

        public Rect GetCanvasRect (Image img) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                Canvas.ForceUpdateCanvases ();
            }
#endif
            RectTransform.GetWorldCorners (worldCorners);
            var canvasCorners0 = img.canvas.transform.InverseTransformPoint (worldCorners [0]);
            var canvasCorners2 = img.canvas.transform.InverseTransformPoint (worldCorners [2]);
            return new Rect (canvasCorners0.x, canvasCorners0.y, canvasCorners2.x - canvasCorners0.x, canvasCorners2.y - canvasCorners0.y);
        }
    }
}
