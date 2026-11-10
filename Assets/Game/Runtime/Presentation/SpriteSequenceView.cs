using UnityEngine;

namespace HOne.Game
{
    public sealed class SpriteSequenceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 6f;
        public void Configure(SpriteRenderer target, Sprite[] sequence) { view = target; frames = sequence; }
        private void LateUpdate()
        {
            var frame = DeliveredArtSet.Frame(frames, Time.time, fps);
            if (view != null && frame != null) view.sprite = frame;
        }
    }
}
