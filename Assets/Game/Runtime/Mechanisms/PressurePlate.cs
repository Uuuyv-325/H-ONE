using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    [DefaultExecutionOrder(-90), RequireComponent(typeof(BoxCollider2D))]
    public sealed class PressurePlate : BoolSignal, IPuzzleResettable
    {
        [SerializeField] private BoxCollider2D sensor;
        [SerializeField] private SpriteRenderer visual;
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private readonly HashSet<string> occupants = new HashSet<string>();
        public int OccupantCount => occupants.Count;
        public void Configure(BoxCollider2D area, SpriteRenderer view) { sensor = area; visual = view; }
        private void FixedUpdate() => Recompute();

        public void Recompute()
        {
            occupants.Clear();
            if (sensor != null && sensor.enabled)
            {
                overlaps.Clear();
                sensor.Overlap(new ContactFilter2D { useTriggers = true }, overlaps);
                foreach (var hit in overlaps)
                {
                    var weight = hit.GetComponentInParent<PressureWeight>();
                    if (weight != null && weight.isActiveAndEnabled && !string.IsNullOrWhiteSpace(weight.StableId))
                        occupants.Add(weight.StableId);
                }
            }
            SetValue(occupants.Count > 0);
            if (visual != null) visual.color = Value ? new Color(1f, 0.82f, 0.3f) : new Color(0.4f, 0.55f, 0.7f);
        }
        public void ResetPuzzle() => Recompute();
        private void OnDisable() { occupants.Clear(); SetValue(false); }
    }
}
