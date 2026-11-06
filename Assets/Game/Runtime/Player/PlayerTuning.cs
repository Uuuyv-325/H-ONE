using UnityEngine;

namespace HOne.Game
{
    [CreateAssetMenu(menuName = "H-ONE/Player Tuning")]
    public sealed class PlayerTuning : ScriptableObject
    {
        [Min(0.1f)] public float WalkSpeed = 4.4f;
        [Min(0.1f)] public float JumpHeight = 2.2f;
        [Min(0.1f)] public float GravityScale = 3f;
        [Min(0f)] public float CoyoteSeconds = 0.1f;
        [Min(0f)] public float JumpBufferSeconds = 0.1f;
        [Min(0.01f)] public float ChargeSeconds = 0.8f;
        public Vector2 StarSpeed = new Vector2(10f, 18f);
        [Min(0.01f)] public float StarRadius = 0.22f;
        [Min(0.01f)] public float FlightSeconds = 1f;
        [Min(0.01f)] public float FlightDistance = 18f;
        [Min(0.01f)] public float ReformSeconds = 0.5f;

        public float SpeedAt(float charge) => Mathf.Lerp(StarSpeed.x, StarSpeed.y, Mathf.Clamp01(charge));
        public float RangeAt(float charge) => Mathf.Min(FlightDistance, SpeedAt(charge) * FlightSeconds);
    }
}
