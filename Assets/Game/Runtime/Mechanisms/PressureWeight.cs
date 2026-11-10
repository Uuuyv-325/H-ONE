using UnityEngine;

namespace HOne.Game
{
    public enum PressureWeightKind { Stone, Shadow }

    // Human and flying star deliberately do not carry this marker.
    public sealed class PressureWeight : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private PressureWeightKind kind;
        public string StableId => stableId;
        public PressureWeightKind Kind => kind;
        public void Configure(string id, PressureWeightKind type) { stableId = id; kind = type; }
    }
}
