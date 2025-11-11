using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

namespace CineGame.MobileComponents {

    [RequireComponent (typeof (CanvasGroup))]
    [ComponentReference ("Animates a layout group with smooth interpolation of each direct child element. When child elements are reparented or destroyed, the visual representation will smoothly fade or scale out (or both). If you want to add a child element smoothly (like drag-and-drop), invoke the AddElement method")]
    public class LayoutAnimator : BaseComponent {
        [Tooltip ("The speed of interpolation. Default is 10")]
        [Range (1f, 100f)]
        public float AnimationSpeed = 10f;

        public enum DestroyAnimType {
            ScaleDown,
            FadeOut,
            ScaleAndFade,
            DestroyImmediately,
        }
        [Tooltip ("Which animation to perform when destroying an element")]
        public DestroyAnimType DestroyAnim = DestroyAnimType.ScaleDown;

        Transform Container;
        bool bCheckAdded;
        RectTransform MyRectTransform;

        class Tuple {
            public RectTransform original;
            public RectTransform animated;
        }

        readonly HashSet<Transform> TrackedTransforms = new ();
        readonly List<Tuple> TrackedObjects = new ();

        void Start () {
            var containerGo = Instantiate (gameObject, transform.parent);
            containerGo.name = "LayoutAnimator_" + containerGo.name;
            Container = containerGo.transform;
            Destroy (containerGo.GetComponent<LayoutAnimator> ());
            Destroy (containerGo.GetComponent<LayoutGroup> ());
            var layoutElement = containerGo.AddComponent<LayoutElement> ();
            layoutElement.ignoreLayout = true;
            GetComponent<CanvasGroup> ().alpha = 0f;
            MyRectTransform = GetComponent<RectTransform> ();
            //var containerGo = new GameObject ("LayoutAnimator_Container");
            //containerGo.transform.SetParent (GetComponentInParent<Canvas> ().transform);
            //Container = containerGo.AddComponent<RectTransform> ();
        }

        void OnDestroy () {
            if (Container != null) {
                Destroy (Container.gameObject);
            }
        }

        void OnTransformChildrenChanged () {
            bCheckAdded = true;
        }

        void LateUpdate () {
            if (bCheckAdded) {
                bCheckAdded = false;
                var num = transform.childCount;
                for (int i = 0; i < num; i++) {
                    var t = transform.GetChild (i);
                    if (TrackedTransforms.Add (t)) {
                        //new element, lets clone it and track it
                        var newGameObject = Instantiate (t.gameObject, Container);
                        TrackedObjects.Add (new Tuple {
                            original = t.GetComponent<RectTransform> (),
                            animated = newGameObject.GetComponent<RectTransform> (),
                        });
                    }
                }
            }

            var e = TrackedObjects.Count;
            var speed = Time.deltaTime * AnimationSpeed;

            for (int j = 0; j < e; j++) {
                var t = TrackedObjects [j];
                if (t.original == null || t.original.parent != transform) {
                    //original destroyed, destroy animated and remove tuple
                    if (DestroyAnim == DestroyAnimType.DestroyImmediately)
                        Destroy (t.animated.gameObject);
                    else
                        StartCoroutine (E_Destroy (t.animated));
                    TrackedTransforms.Remove (t.original);
                    TrackedObjects.RemoveAt (j--);
                    e--;
                    continue;
                }
                var pos = t.animated.position;
                var dest = t.original.position;
                t.animated.position = Vector3.Lerp (pos, dest, speed);
                var size = t.animated.sizeDelta;
                var sdest = t.original.sizeDelta;
                t.animated.sizeDelta = Vector2.Lerp (size, sdest, speed);
            }
        }

        IEnumerator E_Destroy (RectTransform animatedElement) {
            var t = 0f;
            CanvasGroup cg = null;
            if (DestroyAnim == DestroyAnimType.FadeOut || DestroyAnim == DestroyAnimType.ScaleAndFade) {
                cg = animatedElement.GetComponent<CanvasGroup> ();
                if (cg == null) {
                    cg = animatedElement.gameObject.AddComponent<CanvasGroup> ();
                    cg.alpha = 1f;
                }
            }
            while (t < 1f) {
                var speed = AnimationSpeed * Time.deltaTime;
                if (DestroyAnim == DestroyAnimType.ScaleDown || DestroyAnim == DestroyAnimType.ScaleAndFade) {
                    animatedElement.localScale = Vector3.Lerp (animatedElement.localScale, Vector3.zero, speed);
                }
                if (cg != null) {
                    cg.alpha = Mathf.Lerp (cg.alpha, 0f, speed);
                }
                t += Time.deltaTime;
                yield return null;
            }
            Destroy (animatedElement.gameObject);
        }

        /// <summary>
        /// Add an existing element to the end of the layout group with smooth interpolation
        /// </summary>
        public void AddElement (Object element) {
            if (element is GameObject go) {
                AddElement (go);
            } else if (element is Component c) {
                AddElement (c.gameObject);
            }
        }

        /// <summary>
        /// Insert an existing element at the top of the layout group with smooth interpolation
        /// </summary>
        public void InsertElement (Object element) {
            if (element is GameObject go) {
                AddElement (go, siblingIndex: 0);
            } else if (element is Component c) {
                AddElement (c.gameObject, siblingIndex: 0);
            }
        }

        /// <summary>
        /// Add an existing element to the layout group with smooth interpolation. Optionally set siblingIndex directly. Returns the animated element's RectTransform
        /// </summary>
        public RectTransform AddElement (GameObject element, int siblingIndex = -1) {
            var rt = element.GetComponent<RectTransform> ();
            if (!TrackedTransforms.Add (rt))
                return null;
            var pos = rt.position;
            rt.SetParent (transform, worldPositionStays: false);
            if (siblingIndex >= 0) {
                rt.SetSiblingIndex (siblingIndex);
            }
            //new element, lets clone it and track it
            var newGameObject = Instantiate (rt.gameObject, Container);
            var tuple = new Tuple {
                original = rt,
                animated = newGameObject.GetComponent<RectTransform> (),
            };
            tuple.animated.position = pos;
            LayoutRebuilder.ForceRebuildLayoutImmediate (MyRectTransform);
            TrackedObjects.Add (tuple);
            return tuple.animated;
        }
    }

}