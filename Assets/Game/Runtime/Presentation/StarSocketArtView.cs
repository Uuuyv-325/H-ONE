using UnityEngine;

namespace HOne.Game
{
    // Reads the real occupancy; artwork never places, recalls or removes a shadow.
    public sealed class StarSocketArtView : MonoBehaviour
    {
        [SerializeField] private StarSocket socket;
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private DeliveredArtSet art;

        public void Configure(StarSocket owner, SpriteRenderer target, DeliveredArtSet delivery)
        { socket = owner; view = target; art = delivery; Refresh(); }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (socket == null || view == null || art == null) return;
            var occupied = socket.Shadows != null && socket.Shadows.ActiveSocket == socket;
            view.sprite = occupied ? art.RockLight : art.RockDark;
            view.color = !Application.isPlaying || occupied || socket.Available
                ? Color.white : new Color(0.55f, 0.58f, 0.65f);
        }
    }
}
