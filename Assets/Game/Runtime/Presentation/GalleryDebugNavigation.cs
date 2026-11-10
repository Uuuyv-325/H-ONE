using UnityEngine;

namespace HOne.Game
{
    // Authoring convenience, never displayed in a non-development Player.
    public sealed class GalleryDebugNavigation : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private Checkpoint[] stations;
        [SerializeField] private UnityEngine.UI.Button[] buttons;
        public void Configure(PlayerStateMachine owner, Checkpoint[] targets, UnityEngine.UI.Button[] controls)
        { player = owner; stations = targets; buttons = controls; }
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#else
            for (var i = 0; i < buttons.Length; i++)
            {
                var station = stations[i];
                buttons[i].onClick.AddListener(() => Visit(station));
            }
#endif
        }
        private void Visit(Checkpoint checkpoint)
        {
            if (player.Paused || !player.BeginPuzzleReset(checkpoint)) return;
            player.Session.TryActivate(checkpoint);
        }
    }
}
