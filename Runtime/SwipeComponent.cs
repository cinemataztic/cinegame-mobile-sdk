using UnityEngine;

namespace CineGame.MobileComponents {
    public class SwipeComponent : ReplicatedComponent {
        readonly int SHOW_HASH = Animator.StringToHash ("Show");

        [Header ("Settings")]
        [Tooltip ("Minimum velocity per second, normalized to the width of the screen, needed to activate a swipe")]
        [SerializeField] float minSwipeVelocity = 1f;
        [SerializeField] string swipeVariable = "swipe";
        [SerializeField] float maxHoldTime = 0.1f;
        [SerializeField] GameObject trailRendererPrefab;
        [SerializeField] float trailRendererDestroyDelay;
        [SerializeField] float zPos = -10f;
        [SerializeField] Camera mainCam;
        [SerializeField] GameObject arrow;
        [SerializeField] Animator arrowAnimator;
        [SerializeField] float expectedMaxSwipeLength = 10f;

        private Vector3 lastMousePos;
        private Vector2 swipeStartPosition;
        private float swipeStartTime;
        private float holdTimer = 0f;
        private GameObject trailInstance;
        private RectTransform rt;

        private void OnEnable () {
            if (mainCam == null) {
                mainCam = Camera.main;
            }
            rt = GetComponentInParent<RectTransform> ();
        }

        private void Update () {
#if UNITY_EDITOR
            //Handle mouse input, for testing in editor
            if (Input.GetMouseButtonDown (0)) {
                OnBeginDrag (Input.mousePosition);
            } else if (Input.GetMouseButton (0)) {
                if (Input.mousePosition != lastMousePos) {
                    OnDrag (Input.mousePosition, Input.mousePosition - lastMousePos);
                } else {
                    OnHold (Input.mousePosition);
                }
                lastMousePos = Input.mousePosition;
            } else if (Input.GetMouseButtonUp (0)) {
                OnEndDrag (Input.mousePosition);
            }
#else
            if (Input.touches.Length > 0) {
                var touch = Input.GetTouch (0);
                switch (touch.phase) {
                case TouchPhase.Began:
                    OnBeginDrag (touch.position);
                    break;
                case TouchPhase.Moved:
                    OnDrag (touch.position, touch.deltaPosition);
                    break;
                case TouchPhase.Stationary:
                    OnHold (touch.position);
                    break;
                case TouchPhase.Ended:
                    OnEndDrag (touch.position);
                    break;
                default:
                    break;
                }
            }
#endif
        }

        private void OnBeginDrag (Vector3 position) {
            holdTimer = 0f;
            BeginSwipe (position, Time.time);
        }

        private void OnDrag (Vector3 position, Vector3 delta) {
            if (trailInstance != null) {
                var worldPosition = new Vector3 (position.x, position.y, zPos);
                worldPosition = mainCam.ScreenToWorldPoint (worldPosition);
                worldPosition.z = zPos;
                trailInstance.transform.position = worldPosition;
            }
            holdTimer = 0f;
        }

        private void OnHold (Vector3 position) {
            holdTimer += Time.deltaTime;
            if (holdTimer > maxHoldTime) {
                EndSwipe (position, Time.time);
                BeginSwipe (position, Time.time);
                holdTimer = 0f;
            }
        }

        private void OnEndDrag (Vector3 position) {
            holdTimer = 0f;
            EndSwipe (position, Time.time);

            if (trailInstance != null) {
                Destroy (trailInstance, trailRendererDestroyDelay);
                trailInstance = null;
            }
        }

        private void BeginSwipe (Vector2 position, float time) {
            if (trailInstance == null && trailRendererPrefab != null) {
                var worldPosition = new Vector3 (position.x, position.y, zPos);
                worldPosition = mainCam.ScreenToWorldPoint (worldPosition);
                worldPosition.z = zPos;
                trailInstance = Instantiate (trailRendererPrefab, worldPosition, Quaternion.identity);
            }

            swipeStartPosition = position;
            swipeStartTime = time;
        }

        private bool EndSwipe (Vector2 position, float time) {
            if (position != swipeStartPosition) {
                Destroy (trailInstance, trailRendererDestroyDelay);
                trailInstance = null;
            }

            var w = (float)Screen.width;
            var h = (float)Screen.height;
            if (rt != null) {
                w = rt.rect.width;
                h = rt.rect.height;
            }
            var normalizedStart = new Vector2 (swipeStartPosition.x / w, swipeStartPosition.y / h);
            var normalizedEnd = new Vector2 (position.x / w, position.y / h);
            var swipeVel = (normalizedEnd - normalizedStart) / (time - swipeStartTime);
            if (swipeVel.sqrMagnitude > minSwipeVelocity * minSwipeVelocity) {
                DoSwipe (normalizedStart, swipeVel);
                return true;
            }
            return false;
        }

        private void DoSwipe (Vector2 swipeStart, Vector2 swipeVector) {
            Log ($"Swipe ({swipeVector.x:#.00}, {swipeVector.y:#.00})");
            if (arrow != null) {
                var rotation = Quaternion.FromToRotation (Vector3.right, swipeVector.normalized);
                arrow.transform.rotation = rotation;
                arrow.transform.localScale = Vector3.one * (swipeVector.magnitude / expectedMaxSwipeLength);
                if (arrowAnimator != null) {
                    arrowAnimator.SetTrigger (SHOW_HASH);
                }
            }
            if (!string.IsNullOrWhiteSpace (swipeVariable)) {
                Send (swipeVariable, new float [] { swipeVector.x, swipeVector.y });
            }
            //OnSwipe.Invoke (swipeVector);
        }
    }
}
