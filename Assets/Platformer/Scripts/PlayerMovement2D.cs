using UnityEngine;
using UnityEngine.InputSystem;

namespace HOne.Platformer
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maxSpeed = 4.4f;

        private Rigidbody2D body;
        private float horizontalInput;

        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public float MaxSpeed => maxSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                horizontalInput = 0f;
                return;
            }

            horizontalInput = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        }

        private void FixedUpdate()
        {
            var velocity = body.linearVelocity;
            velocity.x = horizontalInput * maxSpeed;
            body.linearVelocity = velocity;
        }
    }
}
