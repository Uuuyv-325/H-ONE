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
    public sealed class ImpactVisualTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private ImpactFeedbackView feedback;
        private float previousTimeScale;

        [UnitySetUp] public IEnumerator Load()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Chapter01_Core.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("Chapter01_Core", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath("Assets/Game/Scenes/Chapter01_Core.unity");
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerStateMachine>()).Single();
            yield return null;
            feedback = player.GetComponentInChildren<ImpactFeedbackView>();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest] public IEnumerator RealWallBreaksUseBoundedGeometryAndFreezeWithPause()
        {
            Assert.IsNotNull(feedback);
            var first = Wall("visual-wall-first");
            first.Break();
            var count = feedback.ActiveBurstCount;
            Assert.Greater(count, 0);
            first.Break();
            Assert.AreEqual(count, feedback.ActiveBurstCount, "An already broken wall must not emit another burst.");

            for (var i = 0; i < 40; i++) Wall("visual-wall-" + i).Break();
            Assert.LessOrEqual(feedback.ActiveBurstCount, 24);
            yield return null;
            Assert.AreEqual(1, feedback.GetComponentsInChildren<MeshFilter>().Count(item => item.sharedMesh != null && item.sharedMesh.name == "Procedural impact feedback"),
                "A burst uses the shared geometry buffer instead of creating another renderer.");
            var filter = feedback.GetComponentsInChildren<MeshFilter>().Single(item => item.sharedMesh != null && item.sharedMesh.name == "Procedural impact feedback");
            var ownedMesh = filter.sharedMesh;
            var ownedMaterial = filter.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.Greater(ownedMesh.vertexCount, 0, "A real drum-compatible break event must produce visible geometry.");

            player.SetPaused(true);
            yield return null;
            yield return null;
            var frozen = ownedMesh.vertices.Select(point => filter.transform.TransformPoint(point)).ToArray();
            var bursts = feedback.ActiveBurstCount;
            yield return new WaitForSecondsRealtime(.1f);
            var current = ownedMesh.vertices.Select(point => filter.transform.TransformPoint(point)).ToArray();
            Assert.AreEqual(frozen.Length, current.Length);
            for (var i = 0; i < frozen.Length; i++) Assert.Less(Vector3.Distance(frozen[i], current[i]), .0001f);
            Assert.AreEqual(bursts, feedback.ActiveBurstCount, "Paused game time cannot consume effect lifetimes.");
            player.SetPaused(false);

            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));
            Assert.AreEqual(0, feedback.ActiveBurstCount);
            Assert.AreEqual(0, feedback.TrailPointCount);
            Assert.AreEqual(0, ownedMesh.vertexCount);

            var host = feedback.gameObject;
            Object.Destroy(feedback);
            yield return null;
            yield return null;
            Assert.IsTrue(ownedMesh == null && ownedMaterial == null, "Owned rendering resources must be released.");
            Assert.IsNull(host.transform.Find("ImpactGeometry"));
        }

        private BrittleWall Wall(string id)
        {
            var root = new GameObject(id);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(100f, 100f);
            var shape = root.AddComponent<BoxCollider2D>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var wall = root.AddComponent<BrittleWall>();
            wall.Configure(player.Session, shape, visual, id);
            return wall;
        }
    }
}
