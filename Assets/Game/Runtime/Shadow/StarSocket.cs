using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class StarSocket : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private ShadowService shadows;
        [SerializeField] private GameObject shadow;
        public string StableId => stableId;
        public ShadowService Shadows => shadows;
        public GameObject Shadow => shadow;
        public bool Available => isActiveAndEnabled && shadows != null && !shadows.HasShadow;

        public void Configure(string id, ShadowService service, GameObject platform)
        {
            stableId = id; shadows = service; shadow = platform;
            SetShadow(false);
        }

        private void Awake() => SetShadow(false);
        public bool TryEmbed() => Available && shadows.TryPlace(this);
        public void SetShadow(bool value) { if (shadow != null) shadow.SetActive(value); }
        private void OnDisable() { if (shadows != null) shadows.Release(this); }
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
