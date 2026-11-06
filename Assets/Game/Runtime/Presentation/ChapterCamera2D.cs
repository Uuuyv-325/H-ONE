using HOne.Platformer;
using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Camera))]
    public sealed class ChapterCamera2D : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private LevelBounds2D bounds;
        [SerializeField] private Vector2 deadZone = new Vector2(0.42f, 0.34f);
        [SerializeField] private float followStrength = 12f;
        private Camera view;
        private Vector2 previousTarget;
        private float horizontalAnchor = 0.4f;
        private PlayerState previousState;
        public void Configure(PlayerStateMachine owner, LevelBounds2D world) { player = owner; bounds = world; }

        private void OnEnable()
        {
            view = GetComponent<Camera>();
            if (player != null) player.Reformed += ResetTracking;
        }
        private void OnDisable() { if (player != null) player.Reformed -= ResetTracking; }
        private void Start() => ResetTracking();

        private Vector3 Focus()
        {
            var halfHeight = view.orthographicSize;
            var halfWidth = halfHeight * view.aspect;
            var state = player.State;
            var forward = state == PlayerState.StarFlight ? player.Flight.Direction : player.AimDirection;
            var velocity = player.GetComponent<Rigidbody2D>().linearVelocity.x;
            var desiredAnchor = velocity < -0.05f ? 0.6f : velocity > 0.05f ? 0.4f : horizontalAnchor;
            if (state == PlayerState.Charging || state == PlayerState.StarFlight)
                desiredAnchor = forward.x < 0f ? 0.58f : 0.42f;
            horizontalAnchor = Mathf.MoveTowards(horizontalAnchor, desiredAnchor, Time.deltaTime * 1.5f);
            var verticalAnchor = state == PlayerState.Human ? 0.30f : 0.46f;
            var focus = player.transform.position + new Vector3(halfWidth * 2f * (0.5f - horizontalAnchor),
                halfHeight * 2f * (0.5f - verticalAnchor), 0f);
            if (state == PlayerState.Charging || state == PlayerState.StarFlight)
                focus += (Vector3)(forward * 0.8f);
            if (bounds != null) focus = bounds.ClampCameraCenter(focus, halfWidth, halfHeight);
            focus.z = transform.position.z;
            return focus;
        }

        public void ResetTracking()
        {
            previousTarget = player.transform.position;
            horizontalAnchor = 0.4f;
            transform.position = Focus();
            previousState = player.State;
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0f || player.State == PlayerState.Reforming) return;
            var focus = Focus();
            var current = (Vector2)transform.position;
            var delta = (Vector2)player.transform.position - previousTarget;
            previousTarget = player.transform.position;
            var error = (Vector2)focus - current;
            if (previousState == player.State && Mathf.Abs(error.x) <= deadZone.x && Mathf.Abs(error.y) <= deadZone.y)
                return;
            previousState = player.State;
            var candidate = Vector2.Lerp(current + delta, focus, 1f - Mathf.Exp(-followStrength * Time.deltaTime));
            var next = new Vector3(
                Mathf.Clamp(candidate.x, Mathf.Min(current.x, focus.x), Mathf.Max(current.x, focus.x)),
                Mathf.Clamp(candidate.y, Mathf.Min(current.y, focus.y), Mathf.Max(current.y, focus.y)), transform.position.z);
            if (bounds != null) next = bounds.ClampCameraCenter(next, view.orthographicSize * view.aspect, view.orthographicSize);
            transform.position = next;
        }
    }
}
