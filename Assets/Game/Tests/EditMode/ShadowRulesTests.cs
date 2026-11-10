using System.Collections.Generic;
using HOne.Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Game.Tests
{
    public sealed class ShadowRulesTests
    {
        private readonly List<Object> owned = new List<Object>();
        private const int Layer = 30, Mask = 1 << Layer;
        private GameObject New(string name) { var go = new GameObject(name); go.layer = Layer; owned.Add(go); return go; }
        private StarSocket Socket(Vector2 position, ShadowService service)
        {
            var go = New("socket"); go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>(); box.size = Vector2.one * 0.5f; box.isTrigger = true;
            var shadow = New("shadow"); shadow.transform.SetParent(go.transform, false);
            shadow.AddComponent<BoxCollider2D>(); shadow.AddComponent<PressureWeight>().Configure("shadow-1", PressureWeightKind.Shadow);
            var socket = go.AddComponent<StarSocket>(); socket.Configure("socket-1", service, shadow); Physics2D.SyncTransforms(); return socket;
        }
        [TearDown] public void Cleanup()
        { for (var i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }
        [Test] public void SocketPredictionEndsWithoutPlacingOrSpendingBounce()
        {
            var service = New("session").AddComponent<ShadowService>(); var socket = Socket(Vector2.right * 2f, service);
            var path = new StarFlightState(Vector2.zero, Vector2.right); var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 10f, null, contacts);
            Assert.IsTrue(path.Ended); Assert.AreEqual(1, path.BouncesLeft); Assert.IsTrue(contacts[0].Embedded);
            Assert.IsFalse(service.HasShadow); Assert.IsFalse(socket.Shadow.activeSelf);
        }
        [Test] public void OccupiedSocketPredictionReflectsAsOrdinarySurface()
        {
            var service = New("session").AddComponent<ShadowService>();
            var first = Socket(Vector2.left * 4f, service); first.TryEmbed();
            var second = Socket(Vector2.right * 2f, service);
            var path = new StarFlightState(Vector2.zero, Vector2.right); var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 3f, null, contacts);
            Assert.IsFalse(path.Ended); Assert.AreEqual(0, path.BouncesLeft); Assert.IsFalse(contacts[0].Embedded);
            Assert.IsTrue(contacts[0].Reflected); Assert.AreSame(first, service.ActiveSocket); Assert.IsFalse(second.TryEmbed());
        }
        [Test] public void HazardWinsOverSocketAtSameDistance()
        {
            var service = New("session").AddComponent<ShadowService>(); Socket(Vector2.right * 2f, service);
            var hazard = New("hazard"); hazard.transform.position = Vector2.right * 2f;
            var box = hazard.AddComponent<BoxCollider2D>(); box.size = Vector2.one * 0.5f; box.isTrigger = true; hazard.AddComponent<Hazard2D>();
            Physics2D.SyncTransforms(); var path = new StarFlightState(Vector2.zero, Vector2.right); var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 10f, null, contacts);
            Assert.IsTrue(path.Ended); Assert.AreEqual(1, contacts.Count); Assert.IsFalse(contacts[0].Embedded);
            Assert.AreSame(box, contacts[0].Collider);
        }
        [Test] public void FragmentBeforeSocketIsReportedAndFragmentBehindItIsOccluded()
        {
            var service = New("session").AddComponent<ShadowService>(); Socket(Vector2.right * 2f, service);
            foreach (var x in new[] { 1f, 3f })
            {
                var go = New("fragment " + x); go.transform.position = Vector2.right * x;
                var collider = go.AddComponent<CircleCollider2D>(); collider.radius = 0.1f; collider.isTrigger = true;
                go.AddComponent<StarFragment>().Configure("f" + x, null, null);
            }
            Physics2D.SyncTransforms(); var path = new StarFlightState(Vector2.zero, Vector2.right); var contacts = new List<StarContact>();
            new StarPathSolver(0.22f, Mask).Advance(ref path, 10f, null, contacts);
            Assert.AreEqual(2, contacts.Count); Assert.IsNotNull(contacts[0].Collider.GetComponent<StarFragment>()); Assert.IsTrue(contacts[1].Embedded);
        }

        [Test] public void AuthoredSampleSurvivesSaveReloadAndPassesValidation()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(M2SceneAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Assert.IsEmpty(ChapterLevelValidation.Validate(), string.Join("\n", ChapterLevelValidation.Validate().ConvertAll(i => i.Message)));
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
        [Test] public void PiecePlacementCreatesFreshIdsAndBindsOnlyItsLevel()
        {
            var session = New("session").AddComponent<LevelSession>(); var shadows = session.gameObject.AddComponent<ShadowService>();
            session.ConfigureMechanisms(shadows);
            var a = LevelPieceFactory.Place(LevelPiece.PlatformSocket, session.transform, Vector2.zero, session); owned.Add(a);
            var b = LevelPieceFactory.Place(LevelPiece.PlatformSocket, session.transform, Vector2.right * 4f, session); owned.Add(b);
            Assert.AreNotEqual(a.GetComponent<StarSocket>().StableId, b.GetComponent<StarSocket>().StableId);
            Assert.AreSame(shadows, a.GetComponent<StarSocket>().Shadows);
            Assert.AreNotEqual(a.GetComponentInChildren<PressureWeight>(true).StableId, b.GetComponentInChildren<PressureWeight>(true).StableId);
        }

        [Test] public void NewLevelTemplateCanBePlacedWiredSavedAndReloaded()
        {
            var path = "Assets/Game/Tests/AuthoringTest_" + System.Guid.NewGuid().ToString("N") + ".unity";
            var previous = SceneManager.GetActiveScene(); Scene scene = default; string definitionPath = null;
            var contextPath = "Assets/Game/Tests/AuthoringContext_" + System.Guid.NewGuid().ToString("N") + ".unity";
            try
            {
                // Unity's runner supplies an untitled scene; additive NewScene requires a saved context.
                if (string.IsNullOrEmpty(previous.path)) EditorSceneManager.SaveScene(previous, contextPath);
                M2SceneAuthoring.CreateLevel(path, false);
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
                var level = scene.GetRootGameObjects()[0].GetComponent<LevelSession>();
                definitionPath = AssetDatabase.GetAssetPath(level.Definition);
                var group = level.CurrentCheckpoint.Puzzle;
                var plate = LevelPieceFactory.Place(LevelPiece.PlateSocket, group.transform, Vector2.up * 3f, level).GetComponentInChildren<PressurePlate>();
                var bridgeObject = LevelPieceFactory.Place(LevelPiece.Bridge, group.transform, new Vector2(4f, 2f), level);
                var barrier = bridgeObject.GetComponent<SignalBarrier>();
                var serialized = new SerializedObject(barrier); serialized.FindProperty("source").objectReferenceValue = plate; serialized.ApplyModifiedProperties();
                group.Configure(group.StableId, plate, barrier);
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
                Assert.IsEmpty(ChapterLevelValidation.Validate(), string.Join("\n", ChapterLevelValidation.Validate().ConvertAll(i => i.Message)));
                var restored = scene.GetRootGameObjects()[0].GetComponentInChildren<SignalBarrier>();
                Assert.IsInstanceOf<PressurePlate>(restored.Source);
                Assert.AreEqual(scene, restored.Source.gameObject.scene);
                Assert.AreEqual(2, scene.GetRootGameObjects()[0].GetComponentInChildren<PuzzleGroup>().ResetTargets.Length);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.DeleteAsset(contextPath);
                if (definitionPath != null) AssetDatabase.DeleteAsset(definitionPath);
            }
        }
    }
}
