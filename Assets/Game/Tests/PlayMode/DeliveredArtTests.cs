using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace HOne.Game.Tests
{
    public sealed class DeliveredArtTests
    {
        private const string Path = "Assets/Game/Scenes/Chapter01_ShadowSlice.unity";
        private Scene scene;
        private GameObject root;
        private PlayerStateMachine player;
        private DeliveredArtSet art;
        private float oldScale;

        [UnitySetUp] public IEnumerator Load()
        {
            oldScale = Time.timeScale; Time.timeScale = 1f;
#if UNITY_EDITOR
            art = AssetDatabase.LoadAssetAtPath<DeliveredArtSet>("Assets/Game/Art/Delivery01/ChapterArt.asset");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            scene = SceneManager.GetSceneByPath(Path); root = scene.GetRootGameObjects()[0];
            player = root.GetComponentInChildren<PlayerStateMachine>();
            Assert.IsNotNull(art); Assert.IsNotNull(player);
            yield return new WaitForSeconds(0.2f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = oldScale;
        }

        [UnityTest] public IEnumerator PauseShowsActualProgressFreezesAnimationAndReleasesCapture()
        {
            var human = player.transform.Find("Visual/Human").GetComponent<SpriteRenderer>();
            var pause = root.GetComponentInChildren<PuzzlePauseMenu>(true);
            var panel = pause.transform.Find("Panel").gameObject;
            var image = panel.transform.Find("ArtworkFrame/Illustration").GetComponent<UnityEngine.UI.Image>();
            var background = panel.transform.Find("BlurredGame").GetComponent<UnityEngine.UI.RawImage>();
            for (var progress = 0; progress <= 3; progress++)
            {
                if (progress > 0) Assert.IsTrue(player.Session.TryCollect("slice-f" + progress));
                player.SetPaused(true);
                yield return null; yield return new WaitForEndOfFrame(); yield return null;
                Assert.IsTrue(panel.activeSelf); Assert.AreEqual(art.PauseProgress[progress], image.sprite);
                Assert.IsNotNull(background.texture);
                var frame = human.sprite; var position = player.Position;
                yield return new WaitForSecondsRealtime(0.28f);
                Assert.AreEqual(frame, human.sprite); Assert.AreEqual(position, player.Position);
                pause.Resume(); yield return null; yield return null;
                Assert.IsFalse(panel.activeSelf); Assert.IsNull(background.texture);
            }
            Assert.AreEqual(3, player.Session.FragmentCount);
            Assert.IsTrue(player.Session.RestartLevel(player)); yield return new WaitForSeconds(0.6f);
            player.SetPaused(true); yield return null; yield return new WaitForEndOfFrame(); yield return null;
            Assert.AreEqual(art.PauseProgress[0], image.sprite);
        }

        [UnityTest] public IEnumerator PressureArtFollowsRealShadowOccupancyAndRecall()
        {
            StarSocket socket = null;
            foreach (var candidate in root.GetComponentsInChildren<StarSocket>())
                if (candidate.GetComponentInChildren<PressurePlate>(true) != null) socket = candidate;
            Assert.IsNotNull(socket);
            var plate = socket.GetComponentInChildren<PressurePlate>();
            var image = plate.transform.Find("Visual").GetComponent<SpriteRenderer>();
            Assert.IsFalse(plate.Value); Assert.AreEqual(art.PlateUp, image.sprite);
            Assert.IsTrue(socket.TryEmbed()); yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(plate.Value); Assert.AreEqual(art.PlateDown, image.sprite);
            Assert.IsTrue(player.Session.Shadows.BeginRecall());
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(plate.Value); Assert.AreEqual(art.PlateDown, image.sprite);
            yield return new WaitForSeconds(0.35f);
            Assert.IsFalse(plate.Value); Assert.AreEqual(art.PlateUp, image.sprite);
        }
    }
}
