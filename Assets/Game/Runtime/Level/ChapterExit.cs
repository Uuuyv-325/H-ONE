using System.Collections;
using UnityEngine;

namespace HOne.Game
{
    public sealed class ChapterExit : MonoBehaviour
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private ProgressCondition condition;
        [SerializeField] private GameObject completionPanel;
        private bool leaving;
        public bool Completed => leaving;
        public ProgressCondition Condition => condition;
        public void Configure(LevelSession level, ProgressCondition requirements, GameObject completion)
        { session = level; condition = requirements; completionPanel = completion; }
        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerStateMachine>();
            if (leaving || condition == null || !condition.Value || player == null ||
                player.Session != session || player.Paused || player.State != PlayerState.Human || !player.IsGrounded) return;
            leaving = true;
            session.Complete();
            StartCoroutine(Leave(player));
        }
        private IEnumerator Leave(PlayerStateMachine player)
        {
            player.SetPaused(true);
            yield return new WaitForSecondsRealtime(0.35f);
            if (!string.IsNullOrEmpty(session.Definition.NextScene))
            {
                player.SetPaused(false);
                GameFlow.LoadScene(session.Definition.NextScene);
            }
            else if (completionPanel != null) completionPanel.SetActive(true);
        }
    }
}
