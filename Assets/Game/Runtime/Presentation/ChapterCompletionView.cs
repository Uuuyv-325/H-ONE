using UnityEngine;

namespace HOne.Game
{
    public sealed class ChapterCompletionView : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private UnityEngine.UI.Button replay, menu;
        public void Configure(PlayerStateMachine owner, UnityEngine.UI.Button restart, UnityEngine.UI.Button title)
        { player = owner; replay = restart; menu = title; }
        private void OnEnable() { replay.onClick.AddListener(Replay); menu.onClick.AddListener(Menu); }
        private void OnDisable() { replay.onClick.RemoveListener(Replay); menu.onClick.RemoveListener(Menu); }
        private void Replay() { player.SetPaused(false); GameFlow.StartChapter(); }
        private void Menu() => GameFlow.ReturnToMenu(player);
    }
}
