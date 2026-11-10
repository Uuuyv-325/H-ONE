using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HOne.Game
{
    public enum ChapterLessonKind { Walk, Jump, Launch, Reflect, Shadow, Push, Bridge, Drum, Flute }

    [Serializable]
    public sealed class ChapterLesson
    {
        public ChapterLessonKind Kind;
        public float StartX, EndX;
        public string CompletedFragment, CompletedMechanism;
        public Vector2[] Path = Array.Empty<Vector2>();
        public Vector2[] FollowupPath = Array.Empty<Vector2>();
    }

    // Presentation unlocks never disable an Input Action or change puzzle authority.
    public sealed class ChapterTutorial : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private ChapterLesson[] lessons = Array.Empty<ChapterLesson>();
        [SerializeField] private bool familiarControls;
        [SerializeField] private float recallAfterX = float.MaxValue;
        [SerializeField] private UnityEngine.UI.Text glyphs;
        private GameplayFeedbackMesh world;
        private ChapterLesson active;
        private float enteredAt;
        private bool stoodOnShadow;
        private PanFluteLift lift;
        public bool ChargeVisible { get; private set; }
        public bool ShadowVisible { get; private set; }
        public bool RecallVisible => stoodOnShadow;
        public bool CollectionVisible => player != null && player.Session.FragmentCount > 0;
        public ChapterLessonKind? ActiveLesson => active != null ? active.Kind : (ChapterLessonKind?)null;
        public void Configure(PlayerStateMachine owner, UnityEngine.UI.Text controls, bool familiar, float recallX, params ChapterLesson[] steps)
        { player = owner; glyphs = controls; familiarControls = familiar; recallAfterX = recallX; lessons = steps; }
        private void Start()
        {
            world = new GameplayFeedbackMesh(transform, "MosaicAndDemonstration", null, 16);
            lift = player.Session.GetComponentInChildren<PanFluteLift>();
            player.Session.ProgressReset += ResetLessons;
            ResetLessons();
        }
        private void OnDestroy()
        { if (player != null) player.Session.ProgressReset -= ResetLessons; world?.Dispose(); }
        private void ResetLessons()
        { ChargeVisible = familiarControls; ShadowVisible = false; stoodOnShadow = false; active = null; }
        private bool Finished(ChapterLesson lesson) =>
            (!string.IsNullOrEmpty(lesson.CompletedFragment) && player.Session.HasFragment(lesson.CompletedFragment)) ||
            (!string.IsNullOrEmpty(lesson.CompletedMechanism) && player.Session.HasMechanism(lesson.CompletedMechanism));
        private string Key(string action)
        {
            var binding = player.Input.Actions.FindAction("Gameplay/" + action, false);
            return binding != null ? binding.GetBindingDisplayString() : string.Empty;
        }
        private void LateUpdate()
        {
            if (world == null || player == null) return;
            glyphs.transform.parent.gameObject.SetActive(!player.Paused);
            if (player.Paused) return;
            var next = (ChapterLesson)null;
            foreach (var lesson in lessons)
                if (player.Position.x >= lesson.StartX && player.Position.x < lesson.EndX && !Finished(lesson))
                { next = lesson; break; }
            if (next != active) { active = next; enteredAt = Time.time; }
            if (active != null)
            {
                ChargeVisible |= active.Kind != ChapterLessonKind.Walk && active.Kind != ChapterLessonKind.Jump && active.Kind != ChapterLessonKind.Push;
                ShadowVisible |= active.Kind == ChapterLessonKind.Shadow || active.Kind == ChapterLessonKind.Bridge;
            }
            var shadow = player.Session.Shadows?.ActiveSocket?.Shadow;
            if (shadow != null && player.IsGrounded && player.State == PlayerState.Human)
            {
                var support = shadow.GetComponent<Collider2D>();
                var foot = player.GetComponent<BoxCollider2D>().bounds.min.y;
                if (support != null && Mathf.Abs(foot - support.bounds.max.y) < .12f && support.bounds.Contains(new Vector3(player.Position.x, support.bounds.center.y, 0)))
                    stoodOnShadow = true;
            }
            stoodOnShadow |= player.Position.x >= recallAfterX;
            glyphs.text = active == null ? string.Empty : active.Kind == ChapterLessonKind.Walk || active.Kind == ChapterLessonKind.Push
                ? Key("Move") + "   ↔" : active.Kind == ChapterLessonKind.Jump ? Key("Jump") + "   ↑"
                : active.Kind == ChapterLessonKind.Shadow && player.Session.Shadows.HasShadow
                    ? Key("Jump") + "   ↑     ◇ → ★" : Key("Charge") + "   ◉ → ★     ◇     " + Key("Cancel") + "   ↶";
            world.Begin();
            if (active != null) DrawLesson(active);
            world.Upload();
        }
        private void DrawLesson(ChapterLesson lesson)
        {
            var followup = lesson.FollowupPath.Length > 1 &&
                ((lesson.Kind == ChapterLessonKind.Flute && lift != null && lift.Raised) ||
                 ((lesson.Kind == ChapterLessonKind.Shadow || lesson.Kind == ChapterLessonKind.Bridge) && player.Session.Shadows.HasShadow));
            var path = followup ? lesson.FollowupPath : lesson.Path;
            if (path.Length < 2) return;
            var gold = new Color(1f, .79f, .39f, .65f);
            var pale = new Color(.74f, .84f, .95f, .55f);
            // Inlaid broken lines describe the actual shot/movement. The ghost has no support or collider.
            for (var i = 1; i < path.Length; i++)
            {
                var length = Vector2.Distance(path[i - 1], path[i]);
                var count = Mathf.Max(1, Mathf.CeilToInt(length / .38f));
                for (var j = 0; j < count; j++)
                    world.Line(Vector2.Lerp(path[i - 1], path[i], j / (float)count),
                        Vector2.Lerp(path[i - 1], path[i], (j + .4f) / count), .025f, pale);
                world.Arrow(path[i], (path[i] - path[i - 1]).normalized, .2f, .035f, gold);
            }
            var phase = Mathf.Repeat(Time.time - enteredAt, 8f);
            if (phase < 3.5f)
            {
                var progress = Mathf.Clamp01((phase - .4f) / 2.4f) * (path.Length - 1);
                var segment = Mathf.Min(path.Length - 2, Mathf.FloorToInt(progress));
                var point = Vector2.Lerp(path[segment], path[segment + 1], progress - segment);
                if (followup && segment == 0)
                    Human(point + Vector2.up * (Mathf.Sin((progress-segment)*Mathf.PI)*(lesson.Kind==ChapterLessonKind.Bridge?0f:1.1f)), pale);
                else if (lesson.Kind == ChapterLessonKind.Walk || lesson.Kind == ChapterLessonKind.Jump || lesson.Kind == ChapterLessonKind.Push)
                    Human(point, pale);
                else world.Star(point, .18f, gold);
            }
            world.Arc(path[0] - Vector2.up * .55f, .35f, 0, 360, .035f, gold);
            world.Diamond(path[path.Length - 1], .33f, .025f, gold);
            if (lesson.Kind == ChapterLessonKind.Shadow && player.Session.Shadows.HasShadow)
            {
                var socket = (Vector2)player.Session.Shadows.ActiveSocket.transform.position;
                Human(path[0], pale);
                world.Arrow(path[0] + Vector2.up, (socket - path[0]).normalized, .3f, .04f, gold);
                world.Line(socket + new Vector2(-1,.3f), socket + new Vector2(1,.3f), .06f, gold);
                Human(socket + Vector2.up * 1.1f, pale);
            }
            if (lesson.Kind == ChapterLessonKind.Drum)
            {
                world.Arc(path[path.Length - 1], .6f, 0, 360, .03f, gold);
                world.Arc(path[path.Length - 1], 1f, 0, 360, .02f, pale);
            }
            if (lesson.Kind == ChapterLessonKind.Flute)
                world.Arrow(path[path.Length - 1] + Vector2.up, Vector2.up, .4f, .05f, gold);
        }
        private void Human(Vector2 point, Color tint)
        {
            world.Arc(point + Vector2.up * .28f, .12f, 0, 360, .035f, tint);
            world.Line(point + Vector2.up * .14f, point - Vector2.up * .2f, .05f, tint);
            world.Line(point - Vector2.up * .2f, point + new Vector2(-.15f,-.43f), .04f, tint);
            world.Line(point - Vector2.up * .2f, point + new Vector2(.15f,-.43f), .04f, tint);
            world.Line(point + new Vector2(-.2f,0), point + new Vector2(.2f,0), .04f, tint);
        }
    }
}
