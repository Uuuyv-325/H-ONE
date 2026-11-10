using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HOne.Game
{
    public sealed class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelDefinition definition;
        [SerializeField] private Checkpoint entry;
        [SerializeField] private ShadowService shadows;
        [SerializeField] private PuzzleGroup[] puzzles = new PuzzleGroup[0];
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> mechanisms = new HashSet<string>(StringComparer.Ordinal);
        public bool HasMechanism(string id) => mechanisms.Contains(id);
        public bool TryTriggerMechanism(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !mechanisms.Add(id)) return false;
            MechanismTriggered?.Invoke(id);
            return true;
        }
        private HashSet<string> allowedFragments = new HashSet<string>(StringComparer.Ordinal);
        private string runtimeLevelId;

        public event Action<string> FragmentCollected;
        public event Action<string> MechanismTriggered;
        public event Action<Checkpoint> CheckpointChanged;
        public event Action ProgressReset;
        public event Action PuzzleReset;
        public event Action<BrittleWall> WallBroken;
        public IReadOnlyCollection<string> AllowedFragmentIds => allowedFragments;
        internal void NotifyWallBroken(BrittleWall wall) => WallBroken?.Invoke(wall);
        public ShadowService Shadows => shadows;
        public int FragmentCount => collected.Count;
        public bool Completed { get; private set; }
        internal void Complete() => Completed = true;
        public LevelDefinition Definition => definition;
        public string LevelId => string.IsNullOrEmpty(runtimeLevelId) ? definition != null ? definition.LevelId : "untitled" : runtimeLevelId;
        private Checkpoint currentCheckpoint;
        public Checkpoint CurrentCheckpoint => currentCheckpoint != null ? currentCheckpoint : entry;
        public Checkpoint EntryCheckpoint => entry;

        public void Configure(LevelDefinition config, Checkpoint initial)
        {
            definition = config;
            entry = initial;
            currentCheckpoint = entry;
            runtimeLevelId = config != null ? config.LevelId : "untitled";
            allowedFragments = new HashSet<string>(config != null && config.FragmentIds != null ? config.FragmentIds : Array.Empty<string>(), StringComparer.Ordinal);
        }

        private void Awake()
        {
            currentCheckpoint = entry;
            runtimeLevelId = definition != null ? definition.LevelId : "untitled";
            allowedFragments = new HashSet<string>(definition != null && definition.FragmentIds != null ? definition.FragmentIds : Array.Empty<string>(), StringComparer.Ordinal);
        }

        public void SetEntryCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null) throw new ArgumentNullException(nameof(checkpoint));
            var previousEntry = entry;
            entry = checkpoint;
            if (currentCheckpoint == null || currentCheckpoint == previousEntry) currentCheckpoint = checkpoint;
        }

        public void ReplaceLevelContent(string levelId, IEnumerable<string> fragmentIds, Checkpoint initial,
            params PuzzleGroup[] groups)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            entry = currentCheckpoint = initial;
            runtimeLevelId = string.IsNullOrWhiteSpace(levelId) ? "untitled" : levelId;
            allowedFragments = new HashSet<string>(fragmentIds ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            collected.Clear();
            mechanisms.Clear();
            Completed = false;
            puzzles = groups ?? Array.Empty<PuzzleGroup>();
            shadows?.Clear();
            ProgressReset?.Invoke();
            CheckpointChanged?.Invoke(initial);
        }

        public void SetAllowedFragmentIds(IEnumerable<string> fragmentIds) =>
            allowedFragments = new HashSet<string>(fragmentIds ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        public void ConfigureMechanisms(ShadowService service, params PuzzleGroup[] groups)
        { shadows = service; puzzles = groups; }

        public bool ResetCurrentPuzzle(PlayerStateMachine player)
        {
            if (player == null || player.Session != this || !player.BeginPuzzleReset(CurrentCheckpoint)) return false;
            if (shadows != null) shadows.Clear();
            if (CurrentCheckpoint.Puzzle != null) CurrentCheckpoint.Puzzle.ResetObjects();
            PuzzleReset?.Invoke();
            return true;
        }

        public bool RestartLevel(PlayerStateMachine player)
        {
            if (player == null || player.Session != this || !player.BeginPuzzleReset(entry)) return false;
            currentCheckpoint = entry;
            collected.Clear();
            mechanisms.Clear();
            Completed = false;
            if (shadows != null) shadows.Clear();
            ProgressReset?.Invoke();
            foreach (var puzzle in puzzles) if (puzzle != null) puzzle.ResetObjects();
            CheckpointChanged?.Invoke(entry);
            return true;
        }
        public bool HasFragment(string id) => collected.Contains(id);

        public bool TryCollect(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !allowedFragments.Contains(id) || !collected.Add(id))
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
