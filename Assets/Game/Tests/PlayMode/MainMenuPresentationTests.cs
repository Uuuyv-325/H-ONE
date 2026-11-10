using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HOne.Game.Tests
{
    public sealed class MainMenuPresentationTests
    {
        private Scene menu;
        private MainMenuCredits credits;
        private Keyboard keyboard;
        private InputSettings.EditorInputBehaviorInPlayMode oldInput;
        private InputSettings.BackgroundBehavior oldBackground;
        private float musicPreference, sfxPreference;
        private bool hadMusicPreference, hadSfxPreference;

        [UnitySetUp] public IEnumerator LoadMenu()
        {
            oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            hadMusicPreference = PlayerPrefs.HasKey("MusicVol"); musicPreference = PlayerPrefs.GetFloat("MusicVol", .8f);
            hadSfxPreference = PlayerPrefs.HasKey("SFXVol"); sfxPreference = PlayerPrefs.GetFloat("SFXVol", .8f);
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameFlow.MainMenuScene, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            menu = SceneManager.GetSceneByPath(GameFlow.MainMenuScene);
            credits = Object.FindFirstObjectByType<MainMenuCredits>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (menu.IsValid() && menu.isLoaded)
            {
                var cleanup = SceneManager.CreateScene("MainMenuPresentationCleanup");
                SceneManager.SetActiveScene(cleanup);
                yield return SceneManager.UnloadSceneAsync(menu);
            }
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
            InputSystem.settings.backgroundBehavior = oldBackground;
            if (hadMusicPreference) PlayerPrefs.SetFloat("MusicVol", musicPreference); else PlayerPrefs.DeleteKey("MusicVol");
            if (hadSfxPreference) PlayerPrefs.SetFloat("SFXVol", sfxPreference); else PlayerPrefs.DeleteKey("SFXVol");
            PlayerPrefs.Save();
        }

        [UnityTest] public IEnumerator CreditsAreModalAndReleaseTheirCaptureOnBackCancelAndInterruptedOpen()
        {
            Assert.IsNotNull(credits);
            var canvas = credits.transform;
            var open = canvas.Find("Credits").GetComponent<Button>();
            var panel = canvas.Find("CreditsPanel");
            var back = panel.Find("List/Back").GetComponent<Button>();
            var start = canvas.Find("Home/Start").GetComponent<Button>();
            EventSystem.current.SetSelectedGameObject(open.gameObject);
            open.onClick.Invoke();
            yield return new WaitForEndOfFrame(); yield return null;
            Assert.IsTrue(credits.IsOpen); Assert.IsTrue(panel.gameObject.activeSelf);
            Assert.IsNotNull(credits.Backdrop); Assert.IsTrue(credits.Backdrop.IsCreated());
            Assert.IsFalse(start.IsInteractable()); Assert.IsFalse(open.IsInteractable());
            Assert.AreSame(back.gameObject, EventSystem.current.currentSelectedGameObject);
            var text = string.Join("\n", panel.GetComponentsInChildren<Text>().Select(t => t.text));
            foreach (var name in new[] { "启程之前", "Larse", "阿锰", "不眠巷", "OriSnow420", "游里", "Uuuyv-325/H-ONE" })
                StringAssert.Contains(name, text);
            foreach (var c in text.Where(c => !char.IsWhiteSpace(c)))
                Assert.IsTrue(panel.GetComponentInChildren<Text>().font.HasCharacter(c), "Missing credits glyph: " + c);
            Assert.AreEqual("https://space.bilibili.com/18212244/", MainMenuCredits.ArtistUrl);
            Assert.AreEqual("https://www.bilibili.com/video/BV1QhA5efEq9/", MainMenuCredits.TrackUrl);
            Assert.AreEqual("https://github.com/OriSnow420", MainMenuCredits.ProgrammerUrl);
            Assert.AreEqual("https://github.com/Uuuyv-325/H-ONE", MainMenuCredits.ProjectUrl);
            back.onClick.Invoke(); yield return null;
            Assert.IsFalse(credits.IsOpen); Assert.IsNull(credits.Backdrop); Assert.IsTrue(start.IsInteractable());
            Assert.AreSame(open.gameObject, EventSystem.current.currentSelectedGameObject);
            open.onClick.Invoke(); yield return new WaitForEndOfFrame(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(Key.Escape));
            yield return null; yield return null;
            Assert.IsFalse(credits.IsOpen); Assert.IsNull(credits.Backdrop);
            InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            open.onClick.Invoke(); credits.Close();
            yield return new WaitForEndOfFrame(); yield return null;
            Assert.IsFalse(panel.gameObject.activeSelf); Assert.IsNull(credits.Backdrop);
        }

        [UnityTest] public IEnumerator MenuMusicContinuesAcrossPagesUsesMusicBusAndUnloadsWithTheScene()
        {
            var music = Object.FindFirstObjectByType<MenuMusic>(); Assert.IsNotNull(music);
            var source = music.Source;
            Assert.IsNotNull(source.clip); Assert.AreEqual("larse_before_departure", source.clip.name);
            Assert.AreEqual(AudioClipLoadType.Streaming, source.clip.loadType);
            Assert.AreEqual(2, source.clip.channels); Assert.IsTrue(source.loop);
            Assert.AreEqual("Music", source.outputAudioMixerGroup.name);
            Assert.AreEqual(1, Object.FindObjectsByType<MenuMusic>(FindObjectsSortMode.None).Length);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(source.isPlaying); Assert.Greater(source.volume, .1f);
            var before = source.time;
            credits.transform.Find("Home/Settings").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.15f);
            var mixer = source.outputAudioMixerGroup.audioMixer;
            credits.transform.Find("Settings/Music").GetComponent<Slider>().value = 0f;
            Assert.IsTrue(mixer.GetFloat("MusicVol", out var db)); Assert.AreEqual(-80f, db);
            credits.transform.Find("Settings/Music").GetComponent<Slider>().value = .8f;
            credits.transform.Find("Settings/SFX").GetComponent<Slider>().value = 0f;
            mixer.GetFloat("MusicVol", out db); Assert.Greater(db, -3f);
            credits.Open(); yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(.15f);
            Assert.Greater(source.time, before); Assert.IsTrue(source.isPlaying);
            credits.Close();
            GameFlow.StartChapter();
            yield return new WaitWhile(() => SceneTransition.IsActive);
            yield return null;
            Assert.IsTrue(music == null); Assert.IsTrue(source == null);
            Assert.IsNull(Object.FindFirstObjectByType<MenuMusic>());
            GameFlow.ReturnToMenu(Object.FindFirstObjectByType<PlayerStateMachine>());
            yield return new WaitWhile(() => SceneTransition.IsActive);
            yield return null;
            menu = SceneManager.GetSceneByPath(GameFlow.MainMenuScene);
            Assert.AreEqual(1, Object.FindObjectsByType<MenuMusic>(FindObjectsSortMode.None).Length);
            Assert.IsTrue(Object.FindFirstObjectByType<MenuMusic>().Source.isPlaying);
        }

        [UnityTest] public IEnumerator StartFlashesBeforeBlackFadesMusicAndReleasesInputAfterLoading()
        {
            yield return new WaitForSecondsRealtime(1f);
            var canvas = credits.transform;
            var start = canvas.Find("Home/Start").GetComponent<Button>();
            var music = Object.FindFirstObjectByType<MenuMusic>();
            var volume = music.Source.volume;
            start.onClick.Invoke();
            // Further requests must not replace the destination or create overlapping transitions.
            GameFlow.OpenGallery();
            var transition = Object.FindFirstObjectByType<SceneTransition>();
            Assert.AreEqual(1, Object.FindObjectsByType<SceneTransition>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(GameFlow.MainMenuScene, SceneManager.GetActiveScene().path);
            Assert.AreEqual(0f, transition.Opacity);
            Assert.Less(start.GetComponent<CanvasGroup>().alpha, .5f);
            Assert.IsFalse(EventSystem.current.sendNavigationEvents);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.Greater(start.GetComponent<CanvasGroup>().alpha, .9f);
            Assert.AreEqual(0f, transition.Opacity);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (transition.Opacity < .25f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(GameFlow.MainMenuScene, SceneManager.GetActiveScene().path);
            Assert.That(transition.Opacity, Is.InRange(.25f, .99f));
            Assert.That(music.Source.volume, Is.InRange(0f, volume * .9f));
            deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().path == GameFlow.MainMenuScene && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(SceneTransition.IsActive, "New scene must arrive under the veil");
            Assert.Greater(transition.Opacity, .9f);
            var revealedGradually = false;
            while (SceneTransition.IsActive && Time.realtimeSinceStartup < deadline)
            {
                if (transition.Opacity > 0f && transition.Opacity < .95f) revealedGradually = true;
                yield return null;
            }
            Assert.IsTrue(revealedGradually, "Loading time must not skip the new scene's fade-in");
            Assert.IsFalse(SceneTransition.IsActive, "Transition did not finish");
            Assert.AreEqual(GameFlow.FirstLevelScene, SceneManager.GetActiveScene().path);
            Assert.IsTrue(music == null);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerInputReader>().enabled);
            Assert.IsTrue(EventSystem.current.sendNavigationEvents);
            GameFlow.ReturnToMenu(Object.FindFirstObjectByType<PlayerStateMachine>());
            yield return new WaitWhile(() => SceneTransition.IsActive);
            menu = SceneManager.GetSceneByPath(GameFlow.MainMenuScene);
        }
    }
}
