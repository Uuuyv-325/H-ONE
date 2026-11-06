using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    public static class CoreSceneValidation
    {
        [MenuItem("H-ONE/Validate Active Core Scene")]
        public static void ValidateOrThrow()
        {
            var errors = Validate();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        public static List<string> Validate()
        {
            var errors = new List<string>();
            var scene = SceneManager.GetActiveScene();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var players = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var player in root.GetComponentsInChildren<PlayerStateMachine>(true))
                {
                    players++;
                    foreach (var field in new[] { "tuning", "session", "bounds", "aimCamera" })
                        if (new SerializedObject(player).FindProperty(field).objectReferenceValue == null)
                            errors.Add("Player missing " + field);
                    if (player.transform.localScale != Vector3.one) errors.Add("Player root must have unit scale.");
                    if (player.GetComponents<HOne.Platformer.PlayerMovement2D>().Length != 0)
                        errors.Add("Prototype motor must not own the chapter player.");
                }
                foreach (var checkpoint in root.GetComponentsInChildren<Checkpoint>(true))
                {
                    CheckId(checkpoint.StableId, ids, errors);
                    if (new SerializedObject(checkpoint).FindProperty("spawn").objectReferenceValue == null)
                        errors.Add("Checkpoint missing spawn: " + checkpoint.name);
                }
                foreach (var fragment in root.GetComponentsInChildren<StarFragment>(true))
                {
                    CheckId(fragment.StableId, ids, errors);
                    var serialized = new SerializedObject(fragment);
                    var session = serialized.FindProperty("session").objectReferenceValue as LevelSession;
                    if (session == null || session.Definition == null ||
                        Array.IndexOf(session.Definition.FragmentIds, fragment.StableId) < 0)
                        errors.Add("Fragment is not declared by its level: " + fragment.name);
                    if (!fragment.GetComponent<Collider2D>().isTrigger) errors.Add("Fragment must be a trigger.");
                    var overlaps = Physics2D.OverlapCircleAll(fragment.transform.position, 0.32f, 1);
                    foreach (var collider in overlaps)
                        if (collider.gameObject.scene == scene && !collider.isTrigger)
                            errors.Add("Fragment overlaps solid: " + collider.name);
                }
            }
            if (players != 1) errors.Add("Core scene needs exactly one player.");
            return errors;
        }

        private static void CheckId(string id, HashSet<string> ids, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) errors.Add("Missing or duplicate stable ID: " + id);
        }
    }
}
