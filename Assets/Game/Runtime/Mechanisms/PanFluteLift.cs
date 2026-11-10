using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PanFluteLift : MonoBehaviour, IPuzzleResettable
    {
        [SerializeField] private Vector2 lower, upper;
        [SerializeField, Min(0.1f)] private float speed = 1.2f;
        private Rigidbody2D body;
        public bool Raised { get; private set; }
        public Vector2 Lower => lower;
        public Vector2 Upper => upper;
        public void Configure(Vector2 bottom, Vector2 top) { lower = bottom; upper = top; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        public void SetRaised(bool value) => Raised = value;
        private void FixedUpdate() => body.MovePosition(Vector2.MoveTowards(body.position, Raised ? upper : lower, speed * Time.fixedDeltaTime));
        public void ResetPuzzle()
        {
            Raised = false;
            if (body == null) body = GetComponent<Rigidbody2D>();
            body.position = lower; body.linearVelocity = Vector2.zero;
        }
    }
}
