using UnityEngine;

namespace HOne.Game
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private SpriteRenderer human;
        [SerializeField] private SpriteRenderer star;
        [SerializeField] private SpriteRenderer ring;
        private float reformStart;
        public void Configure(PlayerStateMachine owner, SpriteRenderer humanSprite, SpriteRenderer starSprite,
            SpriteRenderer ringSprite)
        { player = owner; human = humanSprite; star = starSprite; ring = ringSprite; }

        private void OnEnable() { if (player != null) player.StateChanged += OnState; }
        private void OnDisable() { if (player != null) player.StateChanged -= OnState; }
        private void OnState(PlayerState state) { if (state == PlayerState.Reforming) reformStart = Time.time; }

        private void LateUpdate()
        {
            var state = player.State;
            human.enabled = state == PlayerState.Human || state == PlayerState.Charging;
            star.enabled = state == PlayerState.StarFlight;
            ring.enabled = state != PlayerState.Human || player.Paused;
            if (state == PlayerState.Charging)
                ring.transform.localScale = Vector3.one * (0.7f + player.Charge * 0.5f);
            else if (state == PlayerState.Reforming)
                ring.transform.localScale = Vector3.one * (0.4f + (Time.time - reformStart) * 3f);
            else ring.transform.localScale = Vector3.one * 0.7f;
            if (state == PlayerState.Human && Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.x) > 0.01f)
                human.flipX = player.GetComponent<Rigidbody2D>().linearVelocity.x < 0f;
        }
    }
}
