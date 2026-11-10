using UnityEngine;

namespace HOne.Game
{
    // Runtime installation also covers existing authored scenes and editor-created players.
    internal static class GameFeelInstaller
    {
        public static void Ensure(PlayerStateMachine player)
        {
            var root = player.transform.Find("ProceduralFeedback");
            if (root == null)
            {
                root = new GameObject("ProceduralFeedback").transform;
                root.SetParent(player.transform, false);
            }
            if (player.GetComponentInChildren<ImpactFeedbackView>(true) == null)
                root.gameObject.AddComponent<ImpactFeedbackView>().Configure(player);
            if (player.GetComponentInChildren<GameplayFeedbackView>(true) == null)
                root.gameObject.AddComponent<GameplayFeedbackView>().Configure(player);
        }
    }
}
