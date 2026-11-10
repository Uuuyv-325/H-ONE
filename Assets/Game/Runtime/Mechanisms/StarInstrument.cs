using System;
using UnityEngine;

namespace HOne.Game
{
    public enum StarInstrumentKind { Lyre, Drum }

    // Effects are dispatched only by the real flight motor. Prediction merely sees the solid plane.
    public sealed class StarInstrument : BoolSignal
    {
        [SerializeField] private StarInstrumentKind kind;
        [SerializeField] private LevelSession session;
        [SerializeField] private string stableId;
        [SerializeField] private BrittleWall[] waveTargets = Array.Empty<BrittleWall>();
        [SerializeField, Min(0.1f)] private float waveRadius = 3f;
        public StarInstrumentKind Kind => kind;
        public float WaveRadius => waveRadius;
        public BrittleWall[] WaveTargets => waveTargets;
        public string StableId => stableId;
        public LevelSession Session => session;
        public event Action Struck;
        public void Configure(StarInstrumentKind value, LevelSession level, string id, params BrittleWall[] targets)
        {
            if (session != null) session.ProgressReset -= ResetProgress;
            kind = value; session = level; stableId = id; waveTargets = targets ?? Array.Empty<BrittleWall>();
            if (session != null && isActiveAndEnabled) session.ProgressReset += ResetProgress;
            SetValue(session != null && session.HasMechanism(stableId));
        }
        private void OnEnable() { if (session != null) session.ProgressReset += ResetProgress; }
        private void OnDisable() { if (session != null) session.ProgressReset -= ResetProgress; }
        public void Strike()
        {
            if (kind == StarInstrumentKind.Lyre) { session?.TryTriggerMechanism(stableId); SetValue(true); }
            else
            {
                session?.TryTriggerMechanism(stableId); SetValue(true);
                foreach (var target in waveTargets)
                    if (target != null && Vector2.Distance(transform.position, target.transform.position) <= waveRadius) target.Break();
            }
            Struck?.Invoke();
        }
        private void ResetProgress() => SetValue(false);
    }
}
