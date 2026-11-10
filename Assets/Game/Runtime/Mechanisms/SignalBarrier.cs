using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    // Bridge: solidWhenTrue. Gate: solidWhenTrue=false. Source stays independent of blocked closing.
    [DefaultExecutionOrder(-80)]
    public sealed class SignalBarrier : MonoBehaviour, IPuzzleResettable
    {
        [SerializeField] private BoolSignal source;
        [SerializeField] private bool solidWhenTrue = true;
        [SerializeField] private BoxCollider2D solid;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0.01f)] private float transitionSeconds = 0.2f;
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private float amount;
        public BoolSignal Source => source;
        public bool SolidWhenTrue => solidWhenTrue;
        public bool IsSolid => solid != null && solid.enabled;
        public bool WaitingForClearance { get; private set; }
        public void Configure(BoolSignal signal, bool bridge, BoxCollider2D collision, SpriteRenderer view)
        { source = signal; solidWhenTrue = bridge; solid = collision; visual = view; }
        private bool Wanted => source != null && source.Value == solidWhenTrue;
        private void Start() => ResetPuzzle();

        private bool Occupied()
        {
            overlaps.Clear();
            var scale = transform.lossyScale;
            var size = Vector2.Scale(solid.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Physics2D.OverlapBox(transform.TransformPoint(solid.offset), size * 0.98f,
                transform.eulerAngles.z, new ContactFilter2D { useTriggers = false }, overlaps);
            foreach (var hit in overlaps)
                if (hit != solid && (hit.GetComponentInParent<PlayerStateMachine>() != null ||
                    hit.GetComponentInParent<PressureWeight>() != null)) return true;
            return false;
        }

        private void FixedUpdate()
        {
            if (solid == null) return;
            var wanted = Wanted;
            WaitingForClearance = wanted && !solid.enabled && Occupied();
            if (WaitingForClearance) return;
            amount = Mathf.MoveTowards(amount, wanted ? 1f : 0f, Time.fixedDeltaTime / transitionSeconds);
            // The support changes at the same midpoint as the visible transition.
            if (amount >= 0.5f && !solid.enabled && Occupied())
            { amount = 0.49f; WaitingForClearance = true; }
            solid.enabled = amount >= 0.5f;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (visual == null) return;
            var color = visual.color; color.a = Mathf.Lerp(0.12f, 1f, amount); visual.color = color;
        }
        public void ResetPuzzle()
        {
            if (solid == null) return;
            WaitingForClearance = Wanted && Occupied();
            amount = Wanted && !WaitingForClearance ? 1f : 0f;
            solid.enabled = amount >= 0.5f;
            RefreshVisual();
        }
    }
}
