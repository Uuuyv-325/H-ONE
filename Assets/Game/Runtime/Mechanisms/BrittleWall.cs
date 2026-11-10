using UnityEngine;

namespace HOne.Game
{
    // Session lifetime: ordinary failure and current-puzzle reset preserve the broken wall.
    public sealed class BrittleWall : MonoBehaviour
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private Collider2D collision;
        [SerializeField] private GameObject visual;
        [SerializeField] private string stableId;
        public string StableId => stableId;
        public bool Broken { get; private set; }
        public void Configure(LevelSession level, Collider2D shape, GameObject drawing, string id)
        {
            if (session != null) session.ProgressReset -= Restore;
            session = level; collision = shape; visual = drawing; stableId = id;
            if (session != null && isActiveAndEnabled) session.ProgressReset += Restore;
            Broken = session != null && session.HasMechanism(id); Apply();
        }
        private void OnEnable() { if (session != null) session.ProgressReset += Restore; }
        private void OnDisable() { if (session != null) session.ProgressReset -= Restore; }
        public void Break()
        {
            if (Broken) return;
            session?.TryTriggerMechanism(stableId);
            Broken = true;
            Apply();
            session?.NotifyWallBroken(this);
        }
        private void Restore() { Broken = false; Apply(); }
        private void Apply() { collision.enabled = !Broken; visual.SetActive(!Broken); }
    }
}
