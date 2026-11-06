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
        private readonly List<Vector2> points = new List<Vector2>(8);
        public void Configure(PlayerStateMachine owner, Transform endMarker) { player = owner; endpoint = endMarker; }

        private void Start()
        {
            line = GetComponent<LineRenderer>();
            solver = new StarPathSolver(player.Tuning.StarRadius, player.WorldMask, player.transform);
        }

        private void LateUpdate()
        {
            var visible = player.State == PlayerState.Charging && !player.Paused;
            line.enabled = visible;
            if (endpoint != null) endpoint.gameObject.SetActive(visible);
            if (!visible) return;
            var path = new StarFlightState(player.Position, player.AimDirection);
            points.Clear();
            points.Add(path.Position);
            solver.Advance(ref path, player.Tuning.RangeAt(player.Charge), points, null);
            line.positionCount = points.Count;
            for (var i = 0; i < points.Count; i++)
                line.SetPosition(i, new Vector3(points[i].x, points[i].y, -0.5f));
            if (endpoint != null) endpoint.position = new Vector3(path.Position.x, path.Position.y, -0.6f);
        }
    }
}
