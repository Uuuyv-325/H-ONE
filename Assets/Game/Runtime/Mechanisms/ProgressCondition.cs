using System;
using UnityEngine;

namespace HOne.Game
{
    // Latched session progress and live occupancy signals deliberately remain separate inputs.
    public sealed class ProgressCondition : BoolSignal
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private string[] fragments = Array.Empty<string>();
        [SerializeField] private string[] mechanisms = Array.Empty<string>();
        [SerializeField] private BoolSignal[] signals = Array.Empty<BoolSignal>();
        public string[] Fragments => fragments;
        public string[] Mechanisms => mechanisms;
        public void Configure(LevelSession level, string[] pieces, string[] switches, params BoolSignal[] inputs)
        {
            Unbind(); session = level; fragments = pieces ?? Array.Empty<string>();
            mechanisms = switches ?? Array.Empty<string>(); signals = inputs ?? Array.Empty<BoolSignal>();
            if (isActiveAndEnabled) Bind(); Refresh();
        }
        private void OnEnable() { Bind(); Refresh(); }
        private void OnDisable() => Unbind();
        private void Bind()
        {
            if (session != null) { session.FragmentCollected += ChangedProgress; session.MechanismTriggered += ChangedProgress; session.ProgressReset += Refresh; }
            foreach (var signal in signals) if (signal != null) signal.Changed += ChangedSignal;
        }
        private void Unbind()
        {
            if (session != null) { session.FragmentCollected -= ChangedProgress; session.MechanismTriggered -= ChangedProgress; session.ProgressReset -= Refresh; }
            foreach (var signal in signals) if (signal != null) signal.Changed -= ChangedSignal;
        }
        private void ChangedProgress(string _) => Refresh();
        private void ChangedSignal(bool _) => Refresh();
        private void Refresh()
        {
            var ready = session != null;
            foreach (var id in fragments) ready &= session != null && session.HasFragment(id);
            foreach (var id in mechanisms) ready &= session != null && session.HasMechanism(id);
            foreach (var signal in signals) ready &= signal != null && signal.Value;
            SetValue(ready);
        }
    }
}
