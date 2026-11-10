using UnityEngine;

namespace HOne.Game
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraBackdrop : MonoBehaviour
    {
        [SerializeField] private Camera target;
        [SerializeField] private SpriteRenderer background, foreground;
        public void Configure(Camera camera, SpriteRenderer back, SpriteRenderer front)
        { target = camera; background = back; foreground = front; }
        private void LateUpdate()
        {
            if (target == null || !target.orthographic) return;
            Fit(background); Fit(foreground);
        }
        private void Fit(SpriteRenderer view)
        {
            if (view == null || view.sprite == null) return;
            view.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, 0f);
            var bounds = view.sprite.bounds.size;
            var size = target.orthographicSize * 2f;
            var scale = Mathf.Max(size / bounds.y, size * target.aspect / bounds.x);
            view.transform.localScale = Vector3.one * scale;
        }
    }
}
