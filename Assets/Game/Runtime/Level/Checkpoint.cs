using UnityEngine;

namespace HOne.Game
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private Transform spawn;
        [SerializeField] private Transform[] fallbackSpawns = new Transform[0];
        [SerializeField] private PuzzleGroup puzzle;
        public PuzzleGroup Puzzle => puzzle;
        public void ConfigurePuzzle(PuzzleGroup group) => puzzle = group;
        public string StableId => stableId;
        public Vector2 SpawnPosition => spawn != null ? (Vector2)spawn.position : (Vector2)transform.position;

        public void Configure(string id, Transform spawnPoint, params Transform[] alternatives)
        {
            stableId = id;
            spawn = spawnPoint;
            fallbackSpawns = alternatives;
        }

        public bool TryGetSafePosition(Vector2 bodySize, int worldMask, out Vector2 position)
        {
            position = spawn != null ? (Vector2)spawn.position : (Vector2)transform.position;
            if (spawn != null && IsClear(position, bodySize, worldMask))
                return true;
            foreach (var point in fallbackSpawns)
            {
                if (point == null || !IsClear(point.position, bodySize, worldMask))
                    continue;
                position = point.position;
                return true;
            }
            return false;
        }

        private static bool IsClear(Vector2 position, Vector2 size, int mask)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = false };
            var overlaps = new Collider2D[1];
            return Physics2D.OverlapBox(position, size - Vector2.one * 0.02f, 0f, filter, overlaps) == 0;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerStateMachine>();
            if (player != null && player.State == PlayerState.Human && player.IsGrounded)
                player.Session.TryActivate(this);
        }
    }
}
