using UnityEngine;

namespace HOne.Game
{
    public sealed class SessionView : MonoBehaviour
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private SpriteRenderer fragmentNode;
        [SerializeField] private Sprite empty;
        [SerializeField] private Sprite filled;
        public void Configure(LevelSession level, SpriteRenderer node, Sprite emptyNode, Sprite filledNode)
        { session = level; fragmentNode = node; empty = emptyNode; filled = filledNode; }
        private void OnEnable() { if (session != null) { session.FragmentCollected += Refresh; Refresh(null); } }
        private void OnDisable() { if (session != null) session.FragmentCollected -= Refresh; }
        private void Refresh(string id) => fragmentNode.sprite = session.FragmentCount > 0 ? filled : empty;
    }
}
