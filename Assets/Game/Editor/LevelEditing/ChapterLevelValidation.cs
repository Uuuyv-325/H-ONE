using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    public readonly struct LevelIssue
    {
        public readonly string Message;
        public readonly UnityEngine.Object Target;
        public LevelIssue(string message, UnityEngine.Object target) { Message = message; Target = target; }
    }
    public static class ChapterLevelValidation
    {
        [MenuItem("H-ONE/Validate Active Chapter Level")]
        public static void ValidateOrThrow()
        {
            var issues = Validate();
            if (issues.Count > 0) throw new InvalidOperationException(string.Join("\n", issues.Select(i => i.Message)));
        }
        public static List<LevelIssue> Validate()
        {
            Physics2D.SyncTransforms();
            var issues = new List<LevelIssue>();
            var scene = SceneManager.GetActiveScene();
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(m => m != null).ToArray();
            foreach (var message in CoreSceneValidation.Validate()) issues.Add(new LevelIssue(message, null));
            var sessions = all.OfType<LevelSession>().ToArray();
            if (sessions.Length != 1) issues.Add(new LevelIssue("Needs exactly one LevelSession.", null));
            var session = sessions.FirstOrDefault();
            if (session == null) return issues;
            if (session.Shadows == null || session.Shadows.gameObject.scene != scene)
                issues.Add(new LevelIssue("LevelSession needs its own ShadowService.", session));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var pieceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var piece in all.OfType<LevelPieceMetadata>())
                if (string.IsNullOrWhiteSpace(piece.InstanceId) || !pieceIds.Add(piece.InstanceId))
                    issues.Add(new LevelIssue("Missing or duplicate piece ID: " + piece.name, piece));
            foreach (var component in all)
            {
                var serialized = new SerializedObject(component);
                var id = serialized.FindProperty("stableId");
                if (id != null && (string.IsNullOrWhiteSpace(id.stringValue) || !ids.Add(id.stringValue)))
                    issues.Add(new LevelIssue("Missing or duplicate ID: " + component.name, component));
                var property = serialized.GetIterator();
                if (property.NextVisible(true)) do
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue is Component reference && reference.gameObject.scene.IsValid() && reference.gameObject.scene != scene)
                        issues.Add(new LevelIssue("Cross-scene reference: " + component.name + "." + property.propertyPath, component));
                } while (property.NextVisible(true));
            }
            var declaredGroups = new SerializedObject(session).FindProperty("puzzles");
            var registeredGroups = new HashSet<UnityEngine.Object>();
            for (var i = 0; i < declaredGroups.arraySize; i++) registeredGroups.Add(declaredGroups.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var group in all.OfType<PuzzleGroup>())
            {
                if (!registeredGroups.Contains(group)) issues.Add(new LevelIssue("Group is not registered in LevelSession: " + group.name, group));
                foreach (var target in group.ResetTargets)
                    if (target == null || !(target is IPuzzleResettable)) issues.Add(new LevelIssue("Invalid reset target: " + group.name, group));
                foreach (var target in group.GetComponentsInChildren<MonoBehaviour>(true))
                    if (target is IPuzzleResettable && target.GetComponentInParent<PuzzleGroup>() == group && Array.IndexOf(group.ResetTargets, target) < 0)
                        issues.Add(new LevelIssue("Missing reset target: " + target.name, group));
            }
            foreach (var checkpoint in all.OfType<Checkpoint>())
                if (checkpoint.Puzzle == null || !registeredGroups.Contains(checkpoint.Puzzle))
                    issues.Add(new LevelIssue("Dais needs a registered puzzle group: " + checkpoint.name, checkpoint));
            foreach (var socket in all.OfType<StarSocket>())
            {
                if (socket.Shadows != session.Shadows || socket.Shadows == null)
                    issues.Add(new LevelIssue("Socket needs this level's ShadowService: " + socket.name, socket));
                if (socket.Shadow == null || socket.Shadow.GetComponent<PressureWeight>() == null || socket.Shadow.GetComponent<BoxCollider2D>() == null)
                    issues.Add(new LevelIssue("Socket needs a shadow with support collider and weight: " + socket.name, socket));
                if (!socket.GetComponent<Collider2D>().isTrigger)
                    issues.Add(new LevelIssue("Socket capture area must be a trigger: " + socket.name, socket));
                foreach (var overlap in Physics2D.OverlapCircleAll(socket.transform.position, 0.3f))
                    if (overlap.GetComponentInParent<Hazard2D>() != null)
                        issues.Add(new LevelIssue("Socket overlaps hazard: " + socket.name, socket));
            }
            foreach (var plate in all.OfType<PressurePlate>())
            {
                var sensor = new SerializedObject(plate).FindProperty("sensor").objectReferenceValue as BoxCollider2D;
                if (sensor == null || !sensor.isTrigger) issues.Add(new LevelIssue("Plate needs a trigger sensor: " + plate.name, plate));
            }
            foreach (var barrier in all.OfType<SignalBarrier>())
                if (barrier.Source == null) issues.Add(new LevelIssue("Connect barrier source: " + barrier.name, barrier));
            foreach (var body in all.OfType<ResettableBody2D>())
                if (body.GetComponentInParent<PuzzleGroup>() == null) issues.Add(new LevelIssue("Resettable body needs a puzzle group: " + body.name, body));
            if (session.Definition == null) issues.Add(new LevelIssue("Level needs a definition.", session));
            else
            {
                var fragments = all.OfType<StarFragment>().Select(f => f.StableId).ToArray();
                foreach (var id in session.Definition.FragmentIds)
                    if (!fragments.Contains(id)) issues.Add(new LevelIssue("Declared fragment has no scene piece: " + id, session.Definition));
                if (session.Definition.FragmentIds.Distinct().Count() != session.Definition.FragmentIds.Length)
                    issues.Add(new LevelIssue("Definition contains duplicate fragment IDs.", session.Definition));
            }
            return issues;
        }
    }
}
