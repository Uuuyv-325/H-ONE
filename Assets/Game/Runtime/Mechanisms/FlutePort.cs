using System;
using UnityEngine;

namespace HOne.Game
{
    public sealed class FlutePort : MonoBehaviour
    {
        [SerializeField] private PanFluteLift lift;
        [SerializeField] private bool raise;
        public bool Raise => raise;
        public event Action Activated;
        public void Configure(PanFluteLift platform, bool up) { lift = platform; raise = up; }
        public void Strike() { if (lift == null) return; lift.SetRaised(raise); Activated?.Invoke(); }
    }
}
