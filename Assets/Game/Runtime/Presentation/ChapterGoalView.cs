using UnityEngine;

namespace HOne.Game
{
    public sealed class ChapterGoalView : MonoBehaviour
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private ProgressCondition condition;
        private GameplayFeedbackMesh mesh;
        [SerializeField] private bool roundMechanism = true;
        public void SetMechanismShape(bool round) => roundMechanism = round;
        public void Configure(LevelSession level, ProgressCondition requirements) { session = level; condition = requirements; }
        private void Start() => mesh = new GameplayFeedbackMesh(transform, "ConstellationNodes", null, 30);
        private void LateUpdate()
        {
            if (mesh == null || session == null || condition == null) return;
            mesh.Begin();
            var center = (Vector2)transform.position;
            var ids = condition.Fragments;
            for (var i = 0; i < ids.Length; i++)
            {
                var point = center + new Vector2((i - (ids.Length - 1) * 0.5f) * 0.65f, 0.6f);
                var tint = session.HasFragment(ids[i]) ? Color.yellow : new Color(.65f,.72f,.82f);
                mesh.Diamond(point, .21f, .035f, tint);
                if (session.HasFragment(ids[i])) mesh.Star(point, .14f, tint);
                if (i > 0) mesh.Line(point + Vector2.left * .44f, point + Vector2.left * .21f, .025f, tint);
            }
            for (var i = 0; i < condition.Mechanisms.Length; i++)
            {
                var point = center + new Vector2((i - (condition.Mechanisms.Length - 1) * .5f) * .7f, 1.25f);
                var ready = session.HasMechanism(condition.Mechanisms[i]);
                var round = condition.Mechanisms.Length > 1 ? i > 0 : roundMechanism;
                if (round) mesh.Arc(point, .2f, 0, 360, .035f, ready ? Color.yellow : Color.gray);
                else mesh.Diamond(point, .2f, .035f, ready ? Color.yellow : Color.gray);
                if (ready) mesh.Star(point, .13f, Color.yellow);
            }
            if (condition.Value) mesh.Arrow(center + Vector2.down * .5f, Vector2.right, .4f, .06f, Color.yellow);
            else mesh.Lock(center + Vector2.down * .4f, .22f, .04f, Color.white);
            mesh.Upload();
        }
        private void OnDestroy() => mesh?.Dispose();
    }
}
