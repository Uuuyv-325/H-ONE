using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class StarFragment : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private LevelSession session;
        [SerializeField] private GameObject visual;
        public string StableId => stableId;

        public void Configure(string id, LevelSession level, GameObject view)
        {
            stableId = id;
            session = level;
            visual = view;
        }

        private void OnEnable()
        {
            if (session != null)
                Refresh();
        }

        public bool TryCollect()
        {
            if (session == null || !session.TryCollect(stableId))
                return false;
            Refresh();
            return true;
        }

        private void Refresh()
        {
            var available = !session.HasFragment(stableId);
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = available;
            if (visual != null)
                visual.SetActive(available);
        }
    }
}
