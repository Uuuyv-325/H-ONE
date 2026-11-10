using UnityEngine;

namespace HOne.Game
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private SpriteRenderer human;
        [SerializeField] private SpriteRenderer star;
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private DeliveredArtSet art;
        private float reformStart;
        private float stateStart;
        private Rigidbody2D body;
        private GameplayFeedbackView chargeFeedback;
        private PlayerStateMachine boundPlayer;
        private LevelSession boundSession;
        private Vector3 starRestScale;
        private Quaternion starRestRotation;
        private Color starRestColor;
        private bool baselineCaptured;
        private float impactTime = float.NegativeInfinity;
        private Vector2 impactNormal, outgoingDirection;
        public void ConfigureArt(DeliveredArtSet delivery) => art = delivery;
        public void Configure(PlayerStateMachine owner, SpriteRenderer humanSprite, SpriteRenderer starSprite,
            SpriteRenderer ringSprite)
        {
            Unbind();
            if (Application.isPlaying) ResetDeformation();
            player = owner; human = humanSprite; star = starSprite; ring = ringSprite;
            baselineCaptured = false;
            CaptureBaseline();
            if (isActiveAndEnabled) Bind();
        }

        private void CaptureBaseline()
        {
            if (baselineCaptured || star == null) return;
            starRestScale = star.transform.localScale;
            starRestRotation = star.transform.localRotation;
            starRestColor = star.color;
            baselineCaptured = true;
        }
        private void OnEnable() { baselineCaptured = false; CaptureBaseline(); Bind(); }
        private void Bind()
        {
            if (player == null || boundPlayer == player) return;
            boundPlayer = player;
            boundPlayer.StateChanged += OnState;
            boundPlayer.StarImpact += Impact;
            BindSession();
        }
        private void BindSession()
        {
            if (boundSession != null) { boundSession.ProgressReset -= ResetDeformation; boundSession.PuzzleReset -= ResetDeformation; }
            boundSession = player != null ? player.Session : null;
            if (boundSession != null) { boundSession.ProgressReset += ResetDeformation; boundSession.PuzzleReset += ResetDeformation; }
        }
        private void Unbind()
        {
            if (boundPlayer != null) { boundPlayer.StateChanged -= OnState; boundPlayer.StarImpact -= Impact; }
            if (boundSession != null) { boundSession.ProgressReset -= ResetDeformation; boundSession.PuzzleReset -= ResetDeformation; }
            boundPlayer = null; boundSession = null;
        }
        private void OnDisable() { Unbind(); if (Application.isPlaying) ResetDeformation(); }
        private void ResetDeformation()
        {
            impactTime = float.NegativeInfinity;
            if (star == null || !baselineCaptured) return;
            star.transform.localScale = art != null ? Vector3.one * .55f : starRestScale;
            star.transform.localRotation = starRestRotation;
            star.color = starRestColor;
        }
        private void Impact(StarContact contact)
        {
            if (contact.Collider != null && contact.Collider.GetComponentInParent<StarFragment>() != null) return;
            impactTime = Time.time;
            impactNormal = contact.Normal.sqrMagnitude > .001f ? contact.Normal : -contact.IncomingDirection;
            outgoingDirection = contact.OutgoingDirection.sqrMagnitude > .001f ? contact.OutgoingDirection : contact.IncomingDirection;
        }
        private void OnState(PlayerState state)
        {
            stateStart = Time.time;
            if (state == PlayerState.Reforming) reformStart = Time.time;
            if (state != PlayerState.StarFlight) ResetDeformation();
            else impactTime = float.NegativeInfinity;
        }

        private void LateUpdate()
        {
            if (player == null || human == null || star == null || ring == null) return;
            if (boundSession != player.Session) BindSession();
            var state = player.State;
            if (body == null) body = player.GetComponent<Rigidbody2D>();
            human.enabled = state == PlayerState.Human || state == PlayerState.Charging;
            star.enabled = state == PlayerState.StarFlight;
            if (chargeFeedback == null) chargeFeedback = player.GetComponentInChildren<GameplayFeedbackView>();
            var proceduralCharge = state == PlayerState.Charging && chargeFeedback != null && chargeFeedback.isActiveAndEnabled;
            ring.enabled = (state != PlayerState.Human || player.Paused) && !proceduralCharge;
            if (state == PlayerState.Charging)
                ring.transform.localScale = Vector3.one * (0.7f + player.Charge * 0.5f);
            else if (state == PlayerState.Reforming)
                ring.transform.localScale = Vector3.one * (0.4f + (Time.time - reformStart) * 3f);
            else ring.transform.localScale = Vector3.one * 0.7f;
            if (state == PlayerState.Human && Mathf.Abs(body.linearVelocity.x) > 0.01f)
                human.flipX = body.linearVelocity.x < 0f;
            if (art != null)
            {
                var moving = state == PlayerState.Human && player.IsGrounded && Mathf.Abs(body.linearVelocity.x) > 0.1f;
                var humanFrame = DeliveredArtSet.Frame(moving ? art.Walk : art.Idle, Time.time, moving ? 6f : 4f);
                if (state == PlayerState.Human && !player.IsGrounded && art.Jump != null) humanFrame = art.Jump;
                if (humanFrame != null) human.sprite = humanFrame;
                if (state == PlayerState.StarFlight)
                {
                    // Launch is a visual overlap with flight, never an input/motion lock.
                    var elapsed = Time.time - stateStart;
                    if (elapsed < 0.18f && impactTime < stateStart && art.Launch != null && art.Launch.Length > 0)
                    {
                        star.sprite = art.Launch[Mathf.Min(art.Launch.Length - 1, Mathf.FloorToInt(elapsed / 0.18f * art.Launch.Length))];
                        star.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, elapsed / 0.18f);
                    }
                    else
                    {
                        var frame = DeliveredArtSet.Frame(art.Star, elapsed, 6f);
                        if (frame != null) star.sprite = frame;
                        star.transform.localScale = Vector3.one * 0.55f;
                    }
                }
            }
            ApplyImpactDeformation(state);
        }

        private void ApplyImpactDeformation(PlayerState state)
        {
            if (state != PlayerState.StarFlight) return;
            var age = Time.time - impactTime;
            if (age < 0f || age >= .19f)
            {
                star.transform.localRotation = starRestRotation;
                star.color = starRestColor;
                if (art == null) star.transform.localScale = starRestScale;
                return;
            }
            // PlayerView owns these sprite properties; the root and collision shape are untouched.
            var t = Mathf.Clamp01(age / .19f);
            var normal = star.transform.parent != null ? (Vector2)star.transform.parent.InverseTransformDirection(impactNormal) : impactNormal;
            var outgoing = star.transform.parent != null ? (Vector2)star.transform.parent.InverseTransformDirection(outgoingDirection) : outgoingDirection;
            var normalAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            var outgoingAngle = Mathf.Atan2(outgoing.y, outgoing.x) * Mathf.Rad2Deg;
            var angle = t < .4f
                ? Mathf.LerpAngle(normalAngle, outgoingAngle, Mathf.SmoothStep(0f, 1f, t / .4f))
                : Mathf.LerpAngle(outgoingAngle, 0f, Mathf.SmoothStep(0f, 1f, (t - .4f) / .6f));
            var squeeze = t < .4f ? Mathf.Lerp(.67f, 1.12f, t / .4f) : Mathf.Lerp(1.12f, 1f, (t - .4f) / .6f);
            var cross = t < .4f ? Mathf.Lerp(1.24f, .91f, t / .4f) : Mathf.Lerp(.91f, 1f, (t - .4f) / .6f);
            var baseline = art != null ? Vector3.one * .55f : starRestScale;
            star.transform.localScale = Vector3.Scale(baseline, new Vector3(squeeze, cross, 1f));
            star.transform.localRotation = Quaternion.Euler(0f, 0f, angle) * starRestRotation;
            star.color = Color.Lerp(new Color(1f, 1f, .8f, starRestColor.a), starRestColor, t);
        }
    }
}
