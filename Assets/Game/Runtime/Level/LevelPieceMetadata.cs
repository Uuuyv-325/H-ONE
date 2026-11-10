using System;
using System.Linq;
using UnityEngine;

namespace HOne.Game
{
    [DisallowMultipleComponent]
    public sealed class LevelPieceMetadata : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private RuntimeLevelPieceKind kind;
        [SerializeField] private string groupId;
        [SerializeField] private string sourcePieceId;
        [SerializeField] private string fragmentId;
        [SerializeField] private string checkpointId;
        [SerializeField] private string socketId;
        [SerializeField] private string weightId;
        [SerializeField] private string shadowWeightId;

        public string InstanceId => instanceId;
        public RuntimeLevelPieceKind Kind => kind;
        public string GroupId => groupId;
        public string SourcePieceId => sourcePieceId;

        public void Configure(RuntimeLevelPieceKind pieceKind, string id, string puzzleGroupId)
        {
            kind = pieceKind;
            instanceId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            groupId = puzzleGroupId;
            RefreshReferences();
        }

        public void SetGroup(string value) => groupId = value;
        public void SetSource(string value) => sourcePieceId = value;

        public RuntimeLevelPieceRecord Capture()
        {
            RefreshReferences();
            var record = new RuntimeLevelPieceRecord
            {
                id = instanceId,
                name = gameObject.name,
                kind = kind,
                position = transform.position,
                rotation = transform.eulerAngles.z,
                groupId = groupId,
                fragmentId = fragmentId,
                checkpointId = checkpointId,
                socketId = socketId,
                weightId = weightId,
                shadowWeightId = shadowWeightId,
                sourcePieceId = sourcePieceId
            };
            var box = GetComponent<BoxCollider2D>();
            if (TryGetComponent<StarInstrument>(out var instrument))
                record.waveTargetIds = instrument.WaveTargets.Where(t => t != null).Select(t => t.GetComponent<LevelPieceMetadata>()?.InstanceId).Where(id => id != null).ToArray();
            if (box != null) record.size = box.size;
            if (TryGetComponent<SignalBarrier>(out var barrier))
            {
                record.barrierSolidWhenTrue = barrier.SolidWhenTrue;
                var sourceMetadata = barrier.Source != null ? barrier.Source.GetComponentInParent<LevelPieceMetadata>() : null;
                record.sourcePieceId = sourceMetadata != null ? sourceMetadata.InstanceId : sourcePieceId;
            }
            return record;
        }

        public void Apply(RuntimeLevelPieceRecord record, LevelSession session, PuzzleGroup group, BoolSignal source)
        {
            instanceId = record.id;
            kind = record.kind;
            groupId = record.groupId;
            sourcePieceId = record.sourcePieceId;
            transform.position = record.position;
            transform.rotation = Quaternion.Euler(0f, 0f, record.rotation);
            gameObject.name = record.name;
            if (TryGetComponent<Rigidbody2D>(out var body))
            { body.position = record.position; body.rotation = record.rotation; }
            if (TryGetComponent<ResettableBody2D>(out var resettable)) resettable.CaptureInitialPose();
            if (record.size.x > 0f && record.size.y > 0f && TryGetComponent<BoxCollider2D>(out var box) &&
                kind != RuntimeLevelPieceKind.PlatformSocket && kind != RuntimeLevelPieceKind.PlateSocket &&
                kind != RuntimeLevelPieceKind.PressurePlate)
            {
                box.size = record.size;
                if (TryGetComponent<GrassTerrainView>(out var terrain)) terrain.Rebuild();
                var view = GetComponentInChildren<SpriteRenderer>();
                if (view != null && GetComponent<StarInstrument>() == null) ResizeVisual(view, record.size);
            }
            if (TryGetComponent<Checkpoint>(out var checkpoint))
            {
                checkpointId = record.checkpointId;
                checkpoint.Configure(checkpointId, transform.Find("SafeSpawn"), transform.Find("FallbackSpawn"));
                checkpoint.ConfigurePuzzle(group);
            }
            if (TryGetComponent<StarFragment>(out var fragment))
            {
                fragmentId = record.fragmentId;
                fragment.Configure(fragmentId, session, transform.Find("Visual").gameObject);
            }
            if (TryGetComponent<StarSocket>(out var socket))
            {
                socketId = record.socketId;
                var shadow = transform.Find("ShadowPlatform").gameObject;
                socket.Configure(socketId, session.Shadows, shadow);
                var shadowWeight = shadow.GetComponent<PressureWeight>();
                if (shadowWeight != null) shadowWeight.Configure(record.shadowWeightId, PressureWeightKind.Shadow);
            }
            if (TryGetComponent<PressureWeight>(out var weight))
            {
                weightId = record.weightId;
                weight.Configure(weightId, weight.Kind);
            }
            if (TryGetComponent<SignalBarrier>(out var barrier))
                barrier.Configure(source, record.barrierSolidWhenTrue, GetComponent<BoxCollider2D>(), GetComponentInChildren<SpriteRenderer>());
            if (TryGetComponent<StarInstrument>(out var instrument)) instrument.Configure(instrument.Kind,session,record.id);
            if (TryGetComponent<BrittleWall>(out var brittle))
                brittle.Configure(session,GetComponent<BoxCollider2D>(),transform.Find("Visual").gameObject,record.id);
            RefreshReferences();
        }

        private void RefreshReferences()
        {
            if (TryGetComponent<Checkpoint>(out var checkpoint)) checkpointId = checkpoint.StableId;
            if (TryGetComponent<StarFragment>(out var fragment)) fragmentId = fragment.StableId;
            if (TryGetComponent<StarSocket>(out var socket)) socketId = socket.StableId;
            var weight = GetComponent<PressureWeight>();
            if (weight != null) weightId = weight.StableId;
            var shadowWeight = transform.Find("ShadowPlatform")?.GetComponent<PressureWeight>();
            if (shadowWeight != null) shadowWeightId = shadowWeight.StableId;
            var puzzle = GetComponent<Checkpoint>()?.Puzzle ?? GetComponentInParent<PuzzleGroup>();
            if (puzzle != null) groupId = puzzle.StableId;
            var barrier = GetComponent<SignalBarrier>();
            if (barrier != null && barrier.Source != null)
            {
                var source = barrier.Source.GetComponentInParent<LevelPieceMetadata>();
                if (source != null) sourcePieceId = source.InstanceId;
            }
        }

        private static void ResizeVisual(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer.sprite == null) return;
            var native = renderer.sprite.bounds.size;
            if (native.x <= 0f || native.y <= 0f) return;
            renderer.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
    }
}
