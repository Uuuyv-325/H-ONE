using System;
using HOne.Platformer;
using UnityEngine;

namespace HOne.Game
{
    public enum PlayerState { Human, Charging, StarFlight, Reforming }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInputReader))]
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        [SerializeField] private PlayerTuning tuning;
        [SerializeField] private LevelSession session;
        [SerializeField] private LevelBounds2D bounds;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask worldMask = 1;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private HumanoidMotor2D human;
        private StarFlightMotor2D star;
        private float chargeAge, reformAge, previousTimeScale = 1f;

        public event Action<PlayerState> StateChanged;
        public event Action<StarContact> StarImpact;
        public event Action Jumped;
        public event Action Reformed;
        public PlayerState State { get; private set; }
        public PlayerInputReader Input { get; private set; }
        public LevelSession Session => session;
        public PlayerTuning Tuning => tuning;
        public int WorldMask => worldMask;
        public bool IsGrounded => human != null && human.Grounded;
        public bool Paused { get; private set; }
        public float Charge => Mathf.Clamp01(chargeAge / tuning.ChargeSeconds);
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public Vector2 Position => body.position;
        public StarFlightState Flight => star.Path;
        public Vector2 HumanSize => shape.size;

        public void Configure(PlayerTuning config, LevelSession level, LevelBounds2D limits, Camera camera, int mask = 1)
        {
            tuning = config; session = level; bounds = limits; aimCamera = camera; worldMask = mask;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            Input = GetComponent<PlayerInputReader>();
            if (tuning == null || session == null || session.CurrentCheckpoint == null || aimCamera == null)
            {
                Debug.LogError("Player needs tuning, session with entry checkpoint, and aim camera.", this);
                enabled = false;
                return;
            }
            human = new HumanoidMotor2D(body, shape, tuning, worldMask);
            star = new StarFlightMotor2D(body, tuning, worldMask, transform);
            star.Impact += contact => StarImpact?.Invoke(contact);
            SetHumanBody();
        }

        private void Update()
        {
            if (Input.ConsumePause()) SetPaused(!Paused);
            if (Paused || aimCamera == null) return;
            var pointer = aimCamera.ScreenToWorldPoint(new Vector3(Input.PointerPosition.x, Input.PointerPosition.y,
                -aimCamera.transform.position.z));
            var direction = (Vector2)pointer - body.position;
            if (direction.sqrMagnitude > 0.001f) AimDirection = direction.normalized;
        }

        private void FixedUpdate()
        {
            if (Paused) return;
            var input = Input.Consume();
            if (bounds != null && (body.position.x < bounds.Minimum.x || body.position.x > bounds.Maximum.x ||
                body.position.y < bounds.Minimum.y || body.position.y > bounds.Maximum.y)) Fail();
            switch (State)
            {
                case PlayerState.Human:
                    human.Probe(Time.time);
                    // Jump wins when jump and charge arrive in the same physics step.
                    var jumped = human.Tick(input.Move, input.Jump, false, Time.time);
                    if (jumped) Jumped?.Invoke();
                    if (!jumped && input.ChargePressed && IsGrounded)
                    {
                        chargeAge = 0f;
                        human.Tick(0f, false, true, Time.time);
                        ChangeState(PlayerState.Charging);
                        if (input.Cancel) CancelCharge();
                        else if (input.ChargeReleased) Launch();
                    }
                    break;
                case PlayerState.Charging:
                    human.Probe(Time.time);
                    human.Tick(0f, false, true, Time.time);
                    if (!IsGrounded || input.Cancel) CancelCharge();
                    else
                    {
                        chargeAge += Time.fixedDeltaTime;
                        if (input.ChargeReleased) Launch();
                    }
                    break;
                case PlayerState.StarFlight:
                    if (star.Tick(Time.fixedDeltaTime)) Fail();
                    break;
                case PlayerState.Reforming:
                    reformAge += Time.fixedDeltaTime;
                    if (reformAge >= tuning.ReformSeconds &&
                        session.CurrentCheckpoint.TryGetSafePosition(shape.size, worldMask, out var safe))
                    {
                        body.position = safe;
                        human.Reset();
                        SetHumanBody();
                        Input.DiscardUntilRelease();
                        ChangeState(PlayerState.Human);
                        Reformed?.Invoke();
                    }
                    break;
            }
        }

        private void SetHumanBody()
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = tuning.GravityScale;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            shape.enabled = true;
        }

        private void Launch()
        {
            var amount = Charge;
            shape.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            human.Reset();
            star.Launch(AimDirection, amount);
            ChangeState(PlayerState.StarFlight);
        }

        public void CancelCharge()
        {
            Input.DiscardUntilRelease();
            if (State != PlayerState.Charging) return;
            chargeAge = 0f;
            human.Reset();
            ChangeState(PlayerState.Human);
        }

        public void Fail()
        {
            if (State == PlayerState.Reforming) return;
            Input.DiscardUntilRelease();
            shape.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            human.Reset();
            reformAge = chargeAge = 0f;
            ChangeState(PlayerState.Reforming);
        }

        private void ChangeState(PlayerState value)
        {
            State = value;
            StateChanged?.Invoke(value);
        }

        public void SetPaused(bool value)
        {
            if (Paused == value) return;
            if (value)
            {
                CancelCharge();
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else Time.timeScale = previousTimeScale;
            Paused = value;
            Input.SetGameplayEnabled(!value);
        }

        private void OnApplicationFocus(bool focused) { if (!focused && Input != null) CancelCharge(); }
        private void OnDisable() { if (Paused) { Time.timeScale = previousTimeScale; Paused = false; } }
    }
}
