using System;
using UnityEngine;

namespace HOne.Game
{
    // A swept, nonblocking star target; human trigger callbacks never activate it.
    public sealed class StarSwitch : BoolSignal
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private string stableId;
        [SerializeField] private BoolSignal prerequisite;
        public string StableId => stableId;
        public event Action Activated;
        public void Configure(LevelSession level, string id, BoolSignal required = null)
        {
            if (session != null) session.ProgressReset -= Refresh;
            session = level; stableId = id; prerequisite = required;
            if (isActiveAndEnabled && session != null) session.ProgressReset += Refresh;
            Refresh();
        }
        private void OnEnable() { if (session != null) session.ProgressReset += Refresh; Refresh(); }
        private void OnDisable() { if (session != null) session.ProgressReset -= Refresh; }
        private void Refresh() => SetValue(session != null && session.HasMechanism(stableId));
        public bool Strike()
        {
            if (session == null || (prerequisite != null && !prerequisite.Value) || !session.TryTriggerMechanism(stableId)) return false;
            SetValue(true); Activated?.Invoke(); return true;
        }
    }
}
