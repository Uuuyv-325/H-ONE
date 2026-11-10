using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private Transform endpoint;
        private LineRenderer line;
        private StarPathSolver solver;
        private GameplayFeedbackMesh drawing;
        private readonly List<Vector2> points = new List<Vector2>(8);
        private readonly List<StarContact> contacts = new List<StarContact>(16);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.48f, 0.8f);
        public void Configure(PlayerStateMachine owner, Transform endMarker) { player = owner; endpoint = endMarker; }

        private void Start()
        {
            line = GetComponent<LineRenderer>();
            line.enabled = false;
            // Keep the old marker as an authoring reference. The runtime mesh supplies semantic end shapes.
            if (endpoint != null) endpoint.gameObject.SetActive(false);
            drawing = new GameplayFeedbackMesh(transform, "StarPathShapes", line.sharedMaterial, line.sortingOrder);
            if (player == null || player.Tuning == null) { enabled = false; return; }
            solver = new StarPathSolver(player.Tuning.StarRadius, player.WorldMask, player.transform);
        }

        private void LateUpdate()
        {
            var visible = player != null && player.State == PlayerState.Charging && !player.Paused;
            drawing.SetVisible(visible);
            if (!visible) return;
            var path = new StarFlightState(player.Position, player.AimDirection);
            points.Clear();
            contacts.Clear();
            points.Add(path.Position);
            solver.Advance(ref path, player.Tuning.RangeAt(player.Charge), points, contacts);
            drawing.Begin();
            DrawPath(path.Travelled);
            foreach (var contact in contacts)
            {
                if (!contact.Reflected) continue;
                var center = contact.Point + contact.Normal * player.Tuning.StarRadius;
                drawing.Diamond(center, 0.17f, 0.027f, Gold);
                drawing.Star(center, 0.07f, Gold);
            }
            DrawEndpoint(path);
            drawing.Upload();
        }

        private void DrawPath(float totalDistance)
        {
            const float spacing = 0.43f;
            var next = 0.4f;
            var travelled = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                var delta = points[i] - points[i - 1];
                var distance = delta.magnitude;
                if (distance < 0.0001f) continue;
                var direction = delta / distance;
                while (next < distance - 0.16f)
                {
                    var alpha = Mathf.Lerp(0.78f, 0.3f, (travelled + next) / Mathf.Max(0.1f, totalDistance));
                    drawing.Star(points[i - 1] + direction * next, 0.055f,
                        new Color(Gold.r, Gold.g, Gold.b, alpha));
                    next += spacing;
                }
                if (distance > 1f)
                    drawing.Arrow(points[i - 1] + direction * Mathf.Min(distance * 0.5f, 1.5f),
                        direction, 0.15f, 0.026f, Gold);
                // Keep a common spacing after a bend without drawing a straight line across it.
                next = Mathf.Max(0.16f, next - distance);
                travelled += distance;
            }
        }

        private void DrawEndpoint(StarFlightState path)
        {
            var end = path.Position;
            if (!path.Ended)
            {
                // Open circle: range limit. Bar/cross: physical stop. Diamond/star: a capturable slot.
                drawing.Arc(end, 0.15f, 25f, 310f, 0.022f, Gold);
                drawing.Line(end - path.Direction * 0.07f, end + path.Direction * 0.07f, 0.024f, Gold);
                return;
            }
            if (contacts.Count > 0)
            {
                var last = contacts[contacts.Count - 1];
                if (last.Embedded)
                {
                    drawing.Diamond(end, 0.25f, 0.035f, Gold);
                    drawing.Star(end, 0.13f, Gold);
                    return;
                }
                if (last.Collider != null && last.Collider.GetComponent<StarSocket>() != null)
                {
                    drawing.Lock(end, 0.19f, 0.032f, Gold);
                    return;
                }
            }
            var side = new Vector2(-path.Direction.y, path.Direction.x);
            drawing.Line(end - side * 0.19f, end + side * 0.19f, 0.035f, Gold);
            drawing.Line(end + new Vector2(-0.08f, -0.08f), end + new Vector2(0.08f, 0.08f), 0.025f, Gold);
            drawing.Line(end + new Vector2(-0.08f, 0.08f), end + new Vector2(0.08f, -0.08f), 0.025f, Gold);
        }

        private void OnDisable()
        {
            if (line != null) line.enabled = false;
            drawing?.SetVisible(false);
            if (endpoint != null) endpoint.gameObject.SetActive(false);
        }

        private void OnDestroy() => drawing?.Dispose();
    }
}
