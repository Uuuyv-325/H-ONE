using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HOne.Game.Tests
{
    public sealed class GameplayFeedbackTests
    {
        private const string Path = "Assets/Game/Scenes/Chapter01_ShadowSlice.unity";
        private Scene scene;
        private PlayerStateMachine player;
        private GameplayFeedbackView feedback;
        private float previousTimeScale;

        [UnitySetUp] public IEnumerator Load()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("Chapter01_ShadowSlice", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath(Path);
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerStateMachine>()).Single();
            yield return null;
            feedback = player.GetComponentInChildren<GameplayFeedbackView>();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest] public IEnumerator HudUsesCurrentSessionIdsAfterRuntimeContentChanges()
        {
            Assert.IsNotNull(feedback);
            player.Session.SetAllowedFragmentIds(new[] { "hud-a", "hud-a", "hud-b" });
            Assert.IsTrue(player.Session.TryCollect("hud-a"));
            var unrelated = new GameObject("UnrelatedHudSession");
            SceneManager.MoveGameObjectToScene(unrelated, scene);
            unrelated.AddComponent<LevelSession>().SetAllowedFragmentIds(new[] { "other-a", "other-b", "other-c", "other-d" });
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(2, feedback.DisplayedFragmentSlots, "The current allowed ID set replaces a stale definition, with duplicate IDs removed.");
            Assert.AreEqual(1, feedback.DisplayedCollectedFragments);
            Assert.IsTrue(player.Session.RestartLevel(player));
            yield return null;
            yield return null;
            Assert.AreEqual(2, feedback.DisplayedFragmentSlots);
            Assert.AreEqual(0, feedback.DisplayedCollectedFragments, "Restart clears the displayed progress without changing the runtime target set.");
        }

        [UnityTest] public IEnumerator RecallVisualsFreezeAndSupportRemainsUntilAtomicRemoval()
        {
            var shadows = player.Session.Shadows;
            var socket = shadows.GetComponentsInChildren<StarSocket>().First();
            Assert.IsTrue(socket.TryEmbed());
            Assert.IsTrue(shadows.BeginRecall());
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(FeedbackShadowState.Recalling, feedback.DisplayedShadowState);
            var support = socket.Shadow.GetComponent<Collider2D>();
            Assert.IsTrue(support.enabled && support.gameObject.activeInHierarchy);
            var filter = feedback.GetComponentsInChildren<MeshFilter>().Single(item => item.sharedMesh != null && item.sharedMesh.name == "ChargeAndShadowShapesMesh");
            var mesh = filter.sharedMesh;
            Assert.Greater(mesh.vertexCount, 0);
            player.SetPaused(true);
            yield return null;
            yield return null;
            var progress = shadows.RecallProgress;
            var points = mesh.vertices.Select(point => filter.transform.TransformPoint(point)).ToArray();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(progress, shadows.RecallProgress, 0.0001f);
            var frozen = mesh.vertices.Select(point => filter.transform.TransformPoint(point)).ToArray();
            Assert.AreEqual(points.Length, frozen.Length);
            for (var i = 0; i < points.Length; i++) Assert.Less(Vector3.Distance(points[i], frozen[i]), 0.0001f);
            Assert.IsTrue(support.gameObject.activeInHierarchy);
            player.SetPaused(false);
            yield return new WaitForSeconds(0.85f);
            Assert.IsFalse(shadows.HasShadow);
            Assert.IsFalse(support.gameObject.activeInHierarchy);
            Assert.AreEqual(FeedbackShadowState.Available, feedback.DisplayedShadowState);

            var host = feedback.gameObject;
            Object.Destroy(feedback);
            yield return null;
            yield return null;
            Assert.IsTrue(mesh == null);
            Assert.IsNull(host.transform.Find("ChargeAndShadowShapes"));
            Assert.IsNull(host.transform.Find("GameplayFeedbackHUD"));
        }
    }
}
