using System;
using UnityEngine;

namespace HOne.Platformer
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class DistanceFootstepEmitter2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float stepInterval = 0.38f;
        [SerializeField, Min(0.1f)] private float teleportThreshold = 2f;

        private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
        private Rigidbody2D body;
        private Collider2D footCollider;
        private float previousX;
        private float timeSinceStep;

        public event Action<Vector2> Step;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            footCollider = GetComponent<Collider2D>();
            previousX = body.position.x;
        }

        private void FixedUpdate()
        {
            var currentX = body.position.x;
            var traveled = Mathf.Abs(currentX - previousX);
            previousX = currentX;
            if (traveled > teleportThreshold || traveled < 0.0001f || !IsGrounded())
            {
                timeSinceStep = 0f;
                return;
            }

            timeSinceStep += Time.fixedDeltaTime;
            if (timeSinceStep >= stepInterval)
            {
                timeSinceStep -= stepInterval;
                Step?.Invoke(new Vector2(currentX, footCollider.bounds.min.y));
            }
        }

        private bool IsGrounded()
        {
            var count = footCollider.GetContacts(contacts);
            for (var i = 0; i < count; i++)
            {
                if (contacts[i].normal.y > 0.5f)
                    return true;
            }
            return false;
        }
    }
}
