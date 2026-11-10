using UnityEngine;

namespace HOne.Game
{
    public sealed class PressurePlateArtView : MonoBehaviour
    {
        [SerializeField] private PressurePlate plate;
        [SerializeField] private SpriteRenderer view;
        [SerializeField] private DeliveredArtSet art;
        public void Configure(PressurePlate owner, SpriteRenderer target, DeliveredArtSet delivery)
        { plate = owner; view = target; art = delivery; }
        private void LateUpdate()
        {
            if (view == null || plate == null || art == null) return;
            view.sprite = plate.Value ? art.PlateDown : art.PlateUp;
            view.color = Color.white;
        }
    }
}
