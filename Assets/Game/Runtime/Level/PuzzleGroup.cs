using UnityEngine;

namespace HOne.Game
{
    public interface IPuzzleResettable { void ResetPuzzle(); }

    public sealed class PuzzleGroup : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private MonoBehaviour[] resetTargets = new MonoBehaviour[0];
        public string StableId => stableId;
        public MonoBehaviour[] ResetTargets => resetTargets;
        public void Configure(string id, params MonoBehaviour[] targets) { stableId = id; resetTargets = targets; }
        public void ResetObjects()
        {
            // Restore physical state before recomputing occupancy and then dependent barriers.
            foreach (var target in resetTargets)
                if (target is IPuzzleResettable reset && !(target is BoolSignal) && !(target is SignalBarrier)) reset.ResetPuzzle();
            Physics2D.SyncTransforms();
            foreach (var target in resetTargets) if (target is PressurePlate plate) plate.Recompute();
            foreach (var target in resetTargets) if (target is SignalBarrier barrier) barrier.ResetPuzzle();
        }
    }
}
