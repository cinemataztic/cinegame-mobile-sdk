using UnityEngine;

namespace CineGame.MobileComponents {
    [ComponentReference ("Cast a blob shadow or highlight effect down to the nearest collider beneath PositionSource")]
    public class BlobShadowComponent : BaseComponent {
        public Transform PositionSource;
        public Transform Shadow;
        public float MaxDistance = 10f;
        [Tooltip ("Layers to project unto. Eg floor")]
        public LayerMask LayerMask = -1;
        [Tooltip ("Interval between ray casts. 0 = every frame")]
        public float Interval = 0f;

        float nextRayTime;
        float latestY, currentY;
        Quaternion latestRotation;
        Quaternion currentRotation;

        private void Start () {
            currentY = latestY = Shadow.position.y;
            currentRotation = latestRotation = Shadow.rotation;
        }

        void Update () {
            var sourcePos = PositionSource.position;
            if (Time.time >= nextRayTime) {
                nextRayTime += Interval;
                if (Physics.Raycast (sourcePos, Vector3.down, out RaycastHit hitInfo, MaxDistance, LayerMask)) {
                    latestY = hitInfo.point.y;
                    latestRotation = Quaternion.FromToRotation (Vector3.up, hitInfo.normal);
                } else {
                    latestY = sourcePos.y - MaxDistance;
                    latestRotation = Quaternion.identity;
                }
            }
            currentY = currentY * .8f + latestY * .2f;
            currentRotation = Quaternion.Slerp (currentRotation, latestRotation, .2f);
            Shadow.SetPositionAndRotation (new Vector3 (sourcePos.x, currentY, sourcePos.z), currentRotation);
        }
    }

}