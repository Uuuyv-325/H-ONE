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
        [SerializeField, Range(0f, 1f)] private float impactStrength = 0.65f;
        private Camera view;
        private Vector2 previousTarget;
        private Vector3 trackedPosition;
        private float horizontalAnchor = 0.4f;
        private float verticalAnchor = 0.30f;
        private Vector2 aimOffset;
        private Vector2 impulse;
        private float impulseAge = 1f;
        private bool trackingInitialized;
        private PlayerState previousState;
        [SerializeField] private ChapterCameraLayout layout;
        public void ConfigureRooms(ChapterCameraLayout frames) => layout = frames;
        public float ImpactStrength { get => impactStrength; set => impactStrength = Mathf.Clamp01(value); }
        public void Configure(PlayerStateMachine owner, LevelBounds2D world)
        {
            Unsubscribe(); player = owner; bounds = world;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            if (player == null) return;
            player.Reformed += ResetTracking;
            player.StarImpact += OnImpact;
        }
        private void Unsubscribe()
        {
            if (player == null) return;
            player.Reformed -= ResetTracking;
            player.StarImpact -= OnImpact;
        }

        private void OnEnable()
        {
            view = GetComponent<Camera>();
            Subscribe();
            if (trackingInitialized) ResetTracking();
        }
        private void OnDisable()
        {
            Unsubscribe();
            if (Application.isPlaying && trackingInitialized) transform.position = trackedPosition;
            impulse = Vector2.zero;
            impulseAge = 1f;
        }
        private void Start() => ResetTracking();

        private void OnImpact(StarContact contact)
        {
            if (contact.Kind == StarImpactKind.Hazard || contact.Kind == StarImpactKind.Socket) return;
            var weight = contact.Kind == StarImpactKind.Drum ? 1f : contact.Kind == StarImpactKind.Ceramic ? 0.8f : 0.6f;
            impulse = -contact.Normal * (0.12f * weight * Mathf.Lerp(0.6f, 1f, contact.Charge));
            impulseAge = 0f;
        }

        private Vector3 Focus()
        {
            if (layout != null)
            {
                var room = layout.RoomAt(player.Position.x);
                return new Vector3(room.Center.x, room.Center.y, transform.position.z);
            }
            var halfHeight = view.orthographicSize;
            var halfWidth = halfHeight * view.aspect;
            var state = player.State;
            var forward = state == PlayerState.StarFlight ? player.Flight.Direction : player.AimDirection;
            var velocity = player.GetComponent<Rigidbody2D>().linearVelocity.x;
            var desiredAnchor = velocity < -0.05f ? 0.6f : velocity > 0.05f ? 0.4f : horizontalAnchor;
            if (state == PlayerState.Charging || state == PlayerState.StarFlight)
                desiredAnchor = forward.x < 0f ? 0.58f : 0.42f;
            horizontalAnchor = Mathf.MoveTowards(horizontalAnchor, desiredAnchor, Time.deltaTime * 1.5f);
            var focus = player.transform.position + new Vector3(halfWidth * 2f * (0.5f - horizontalAnchor),
                halfHeight * 2f * (0.5f - verticalAnchor), 0f);
            focus += (Vector3)aimOffset;
            if (bounds != null) focus = bounds.ClampCameraCenter(focus, halfWidth, halfHeight);
            focus.z = transform.position.z;
            return focus;
        }

        public void ResetTracking()
        {
            if (player == null || view == null) return;
            previousTarget = player.transform.position;
            if (layout != null) view.orthographicSize = layout.RoomAt(player.Position.x).HalfHeight;
            horizontalAnchor = 0.4f;
            verticalAnchor = 0.30f;
            aimOffset = impulse = Vector2.zero;
            impulseAge = 1f;
            transform.position = trackedPosition = Focus();
            trackingInitialized = true;
            previousState = player.State;
        }

        private void LateUpdate()
        {
            if (player == null || Time.deltaTime <= 0f) return;
            if (layout != null) view.orthographicSize = Mathf.Lerp(view.orthographicSize,
                layout.RoomAt(player.Position.x).HalfHeight, 1f - Mathf.Exp(-6f * Time.deltaTime));
            var aiming = player.State == PlayerState.Charging || player.State == PlayerState.StarFlight;
            verticalAnchor = Mathf.MoveTowards(verticalAnchor, aiming ? 0.42f : 0.30f, Time.deltaTime * 0.35f);
            var direction = player.State == PlayerState.StarFlight ? player.Flight.Direction : player.AimDirection;
            aimOffset = Vector2.Lerp(aimOffset, aiming ? direction * 0.8f : Vector2.zero, 1f - Mathf.Exp(-8f * Time.deltaTime));
            var focus = Focus();
            var current = (Vector2)trackedPosition;
            var delta = (Vector2)player.transform.position - previousTarget;
            previousTarget = player.transform.position;
            var error = (Vector2)focus - current;
            var withinDeadZone = previousState == player.State && Mathf.Abs(error.x) <= deadZone.x && Mathf.Abs(error.y) <= deadZone.y;
            previousState = player.State;
            var candidate = Vector2.Lerp(current + delta, focus, 1f - Mathf.Exp(-followStrength * Time.deltaTime));
            var next = new Vector3(
                Mathf.Clamp(candidate.x, Mathf.Min(current.x, focus.x), Mathf.Max(current.x, focus.x)),
                Mathf.Clamp(candidate.y, Mathf.Min(current.y, focus.y), Mathf.Max(current.y, focus.y)), transform.position.z);
            if (bounds != null) next = bounds.ClampCameraCenter(next, view.orthographicSize * view.aspect, view.orthographicSize);
            if (!withinDeadZone && player.State != PlayerState.Reforming) trackedPosition = next;
            impulseAge += Time.deltaTime;
            var fade = Mathf.Clamp01(1f - impulseAge / 0.18f);
            var offset = impulse * (impactStrength * fade * fade * Mathf.Cos(impulseAge * 48f));
            var presented = trackedPosition + (Vector3)offset;
            if (bounds != null) presented = bounds.ClampCameraCenter(presented, view.orthographicSize * view.aspect, view.orthographicSize);
            transform.position = presented;
        }
    }
}
