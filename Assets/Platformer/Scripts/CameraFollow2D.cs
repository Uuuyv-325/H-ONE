using UnityEngine;

namespace HOne.Platformer
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private LevelBounds2D worldBounds;
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField, Range(0.1f, 0.9f)] private float rightwardAnchor = 0.29f;
        [SerializeField, Range(0.1f, 0.9f)] private float leftwardAnchor = 0.68f;
        [SerializeField, Range(0.05f, 0.45f)] private float verticalAnchor = 0.25f;
        [SerializeField, Min(0.01f)] private Vector2 deadZone = new Vector2(0.42f, 0.34f);
        [SerializeField, Min(0.1f)] private float followStrength = 12f;
        [SerializeField, Min(0.1f)] private float anchorShiftSpeed = 1.5f;

        private Camera followCamera;
        private Vector3 previousTargetPosition;
        private Vector2 previousCameraPosition;
        private float currentHorizontalAnchor;
        private bool tracking;

        public void Configure(Transform followTarget, LevelBounds2D bounds)
        {
            target = followTarget;
            worldBounds = bounds;
            targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        }

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
            if (target != null && targetBody == null)
                targetBody = target.GetComponent<Rigidbody2D>();
            previousTargetPosition = target != null ? target.position : Vector3.zero;
            previousCameraPosition = transform.position;
            currentHorizontalAnchor = rightwardAnchor;
        }

        private void LateUpdate()
        {
            if (target == null || followCamera == null || Time.deltaTime <= 0f)
                return;

            var deltaTime = Time.deltaTime;
            var targetDelta = (Vector2)(target.position - previousTargetPosition);
            previousTargetPosition = target.position;

            var horizontalSpeed = targetBody != null ? targetBody.linearVelocity.x : targetDelta.x / deltaTime;
            var chosenAnchor = horizontalSpeed < -0.05f ? leftwardAnchor :
                horizontalSpeed > 0.05f ? rightwardAnchor : currentHorizontalAnchor;
            currentHorizontalAnchor = Mathf.MoveTowards(currentHorizontalAnchor, chosenAnchor,
                anchorShiftSpeed * deltaTime);

            var halfHeight = followCamera.orthographicSize;
            var halfWidth = halfHeight * followCamera.aspect;
            var desiredCenter = target.position + new Vector3(
                halfWidth * 2f * (0.5f - currentHorizontalAnchor),
                halfHeight * 2f * (0.5f - verticalAnchor), 0f);
            if (worldBounds != null)
                desiredCenter = worldBounds.ClampCameraCenter(desiredCenter, halfWidth, halfHeight);

            var current = (Vector2)transform.position;
            var error = (Vector2)desiredCenter - current;
            var insideDeadZone = Mathf.Abs(error.x) <= deadZone.x && Mathf.Abs(error.y) <= deadZone.y;
            if (!tracking && !insideDeadZone)
                tracking = true;
            if (tracking && insideDeadZone && targetDelta.sqrMagnitude < 0.000001f &&
                (current - previousCameraPosition).sqrMagnitude < 0.000001f)
                tracking = false;

            previousCameraPosition = current;
            if (!tracking)
                return;

            // Translate with the target first, then correct the remaining error without crossing
            // the current focus point. Constant target velocity therefore has no steady lag.
            var translated = current + targetDelta;
            var blend = 1f - Mathf.Exp(-followStrength * deltaTime);
            var candidate = Vector2.Lerp(translated, desiredCenter, blend);
            var next = new Vector3(
                Mathf.Clamp(candidate.x, Mathf.Min(current.x, desiredCenter.x), Mathf.Max(current.x, desiredCenter.x)),
                Mathf.Clamp(candidate.y, Mathf.Min(current.y, desiredCenter.y), Mathf.Max(current.y, desiredCenter.y)),
                transform.position.z);
            if (worldBounds != null)
                next = worldBounds.ClampCameraCenter(next, halfWidth, halfHeight);
            transform.position = next;
        }
    }
}
