using UnityEngine;

namespace HOne.Platformer
{
    public sealed class LevelBounds2D : MonoBehaviour
    {
        [SerializeField] private Vector2 minimum = Vector2.zero;
        [SerializeField] private Vector2 maximum = new Vector2(88f, 30f);

        public Vector2 Minimum => minimum;
        public Vector2 Maximum => maximum;

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minimum = min;
            maximum = max;
        }

        public Vector3 ClampCameraCenter(Vector3 center, float halfWidth, float halfHeight)
        {
            var minX = minimum.x + halfWidth;
            var maxX = maximum.x - halfWidth;
            var minY = minimum.y + halfHeight;
            var maxY = maximum.y - halfHeight;

            center.x = minX > maxX
                ? (minimum.x + maximum.x) * 0.5f
                : Mathf.Clamp(center.x, minX, maxX);
            center.y = minY > maxY
                ? (minimum.y + maximum.y) * 0.5f
                : Mathf.Clamp(center.y, minY, maxY);
            return center;
        }
    }
}
