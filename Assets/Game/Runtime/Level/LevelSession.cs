using System;
using System.Collections.Generic;
using UnityEngine;

namespace HOne.Game
{
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelDefinition definition;
        [SerializeField] private Checkpoint entry;
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);

        public event Action<string> FragmentCollected;
        public event Action<Checkpoint> CheckpointChanged;
        public int FragmentCount => collected.Count;
        public LevelDefinition Definition => definition;
        private Checkpoint currentCheckpoint;
        public Checkpoint CurrentCheckpoint => currentCheckpoint != null ? currentCheckpoint : entry;

        public void Configure(LevelDefinition config, Checkpoint initial)
        {
            definition = config;
            entry = initial;
            currentCheckpoint = entry;
        }

        private void Awake() => currentCheckpoint = entry;
        public bool HasFragment(string id) => collected.Contains(id);

        public bool TryCollect(string id)
        {
            if (definition == null || string.IsNullOrWhiteSpace(id) ||
                Array.IndexOf(definition.FragmentIds, id) < 0 || !collected.Add(id))
                return false;
            FragmentCollected?.Invoke(id);
            return true;
        }

        public bool TryActivate(Checkpoint checkpoint)
        {
            if (checkpoint == null || CurrentCheckpoint == checkpoint)
                return false;
            currentCheckpoint = checkpoint;
            CheckpointChanged?.Invoke(checkpoint);
            return true;
        }
    }
}
