using UnityEngine;

namespace HOne.Game
{
    public sealed class HumanoidMotor2D
    {
        private readonly Rigidbody2D body;
        private readonly BoxCollider2D shape;
        private readonly PlayerTuning tuning;
        private readonly ContactFilter2D groundFilter;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private float coyoteUntil = float.NegativeInfinity;
        private float jumpUntil = float.NegativeInfinity;
        private bool jumpConsumed;
        public bool Grounded { get; private set; }

        public HumanoidMotor2D(Rigidbody2D rigidbody, BoxCollider2D collider, PlayerTuning config, int mask)
        {
            body = rigidbody;
            shape = collider;
            tuning = config;
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = false };
        }

        public void Reset()
        {
            Grounded = false;
            coyoteUntil = jumpUntil = float.NegativeInfinity;
            jumpConsumed = false;
        }

        public void Probe(float now)
        {
            Grounded = false;
            if (!shape.enabled || body.linearVelocity.y > 0.05f) return;
            var count = shape.Cast(Vector2.down, groundFilter, hits, 0.06f);
            for (var i = 0; i < count; i++)
                Grounded |= hits[i].normal.y > 0.65f;
            if (Grounded)
            {
                coyoteUntil = now + tuning.CoyoteSeconds;
                jumpConsumed = false;
            }
        }

        public bool Tick(float move, bool jumpPressed, bool charging, float now)
        {
            if (jumpPressed && !charging) jumpUntil = now + tuning.JumpBufferSeconds;
            body.linearVelocity = new Vector2(charging ? 0f : move * tuning.WalkSpeed, body.linearVelocity.y);
            if (charging || jumpConsumed || now > jumpUntil || now > coyoteUntil) return false;
            var gravity = Mathf.Abs(Physics2D.gravity.y * tuning.GravityScale);
            body.linearVelocity = new Vector2(body.linearVelocity.x,
                Mathf.Sqrt(2f * gravity * tuning.JumpHeight) + gravity * Time.fixedDeltaTime * 0.5f);
            Grounded = false;
            jumpConsumed = true;
            coyoteUntil = jumpUntil = float.NegativeInfinity;
            return true;
        }
    }
}
