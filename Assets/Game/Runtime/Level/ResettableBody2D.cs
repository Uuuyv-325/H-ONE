using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ResettableBody2D : MonoBehaviour, IPuzzleResettable
    {
        private Rigidbody2D body;
        private Vector2 initialPosition;
        private float initialRotation;
        private void Awake() => CaptureInitialPose();
        public void CaptureInitialPose()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            initialPosition = transform.position; initialRotation = transform.eulerAngles.z;
        }
        public void ResetPuzzle()
        {
            body.position = initialPosition; body.rotation = initialRotation;
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0f;
        }
    }
}
