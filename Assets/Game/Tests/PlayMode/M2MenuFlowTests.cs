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
    public sealed class M2MenuFlowTests
    {
        [UnityTest] public IEnumerator MenuGalleryPauseAndReturnReleaseThePreviousSession()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameFlow.MainMenuScene, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            var home = Object.FindFirstObjectByType<MainMenuController>(); Assert.IsNotNull(home);
            home.transform.Find("Home/Start").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitWhile(() => SceneTransition.IsActive);
            yield return null;
            Assert.AreEqual(GameFlow.FirstLevelScene, SceneManager.GetActiveScene().path);
            for (var visit = 0; visit < 2; visit++)
            {
                var player = Object.FindFirstObjectByType<PlayerStateMachine>(); Assert.IsNotNull(player);
                Assert.AreEqual(0, player.Session.FragmentCount);
                Assert.IsTrue(player.Session.TryCollect(player.Session.AllowedFragmentIds.First()));
                var socket = player.transform.root.GetComponentInChildren<StarSocket>();
                Assert.IsTrue(socket.TryEmbed());
                player.SetPaused(true);
                yield return null; yield return new WaitForEndOfFrame(); yield return null;
                var pause = Object.FindFirstObjectByType<PuzzlePauseMenu>();
                Assert.IsTrue(pause.transform.Find("Panel").gameObject.activeSelf);
                var position = player.Position;
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.AreEqual(position, player.Position); Assert.AreEqual(0f, Time.timeScale);
                pause.ReturnToMenu();
                yield return new WaitWhile(() => SceneTransition.IsActive);
                yield return null;
                Assert.AreEqual(1f, Time.timeScale);
                Assert.IsNull(Object.FindFirstObjectByType<LevelSession>());
                Assert.IsNull(Object.FindFirstObjectByType<PlayerStateMachine>());
                Assert.AreEqual(1, Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length);
                if (visit == 0)
                {
                    GameFlow.OpenGallery();
                    yield return new WaitWhile(() => SceneTransition.IsActive);
                    yield return null;
                }
            }
            var menuScene = SceneManager.GetActiveScene();
            var cleanup = SceneManager.CreateScene("M2MenuFlowCleanup"); SceneManager.SetActiveScene(cleanup);
            yield return SceneManager.UnloadSceneAsync(menuScene);
        }
    }
}
