using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HOne.Game.Tests
{
    public sealed class DeliveredAudioTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode oldInput;
        private InputSettings.BackgroundBehavior oldBackground;
        private float oldTimeScale;

        [UnitySetUp] public IEnumerator Load()
        {
            oldTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameFlow.GalleryScene, new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            scene = SceneManager.GetSceneByPath(GameFlow.GalleryScene);
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerStateMachine>()).Single();
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            player.Input.Actions.devices = new InputDevice[] { keyboard, mouse };
            player.Input.Actions.Disable(); player.Input.Actions.Enable();
            yield return new WaitForSeconds(.2f);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
            InputSystem.settings.backgroundBehavior = oldBackground;
            Time.timeScale = oldTimeScale;
        }

        private static void Set<T>(InputControl<T> control, T value) where T : struct
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device, out var state))
            { control.WriteValueIntoEvent(value, state); InputSystem.QueueEvent(state); }
            InputSystem.Update();
        }

        private IEnumerator Shoot(Vector2 target)
        {
            var camera = player.transform.root.GetComponentInChildren<Camera>();
            void Aim()
            {
                var screen = camera.WorldToScreenPoint(target);
                Set(mouse.position, new Vector2(screen.x, screen.y));
            }
            Aim(); yield return null; Set(keyboard.eKey, 1f);
            for (var i = 0; i < 44; i++) { Aim(); yield return new WaitForFixedUpdate(); }
            Assert.AreEqual(PlayerState.Charging, player.State);
            Set(keyboard.eKey, 0f); yield return new WaitForSeconds(1.7f);
        }

        [UnityTest] public IEnumerator RealShotsUseAllSixDeliveredCuesAndKeepMaterialDeduplication()
        {
            var root = player.transform.root;
            var core = root.GetComponentInChildren<CoreAudioFeedback>();
            var shadow = root.GetComponentInChildren<ShadowFeedback>();
            var bank = ChapterAudioSet.LoadDefault();
            Assert.IsNotNull(bank);
            Assert.AreSame(bank, core.Delivery);
            var clips = new[] { bank.Stone, bank.Lyre, bank.Drum, bank.Ceramic, bank.Launch, bank.Embed };
            Assert.IsTrue(clips.All(clip => clip != null && clip.channels == 1 && clip.frequency == 48000));
            var impacts = new Dictionary<StarImpactKind, AudioClip>();
            AudioClip launch = null, ceramic = null;
            var events = 0;
            player.StateChanged += state => { if (state == PlayerState.StarFlight) launch = core.LastPlayedClip; };
            player.StarImpact += contact =>
            {
                if (contact.Kind == StarImpactKind.Stone || contact.Kind == StarImpactKind.Lyre || contact.Kind == StarImpactKind.Drum)
                { impacts[contact.Kind] = core.LastImpactClip; events++; }
            };
            player.Session.WallBroken += wall => ceramic = core.LastPlayedClip;
            yield return Shoot(player.Position + Vector2.down * 2f);
            Assert.AreSame(bank.Launch, launch);
            Assert.AreSame(bank.Stone, impacts[StarImpactKind.Stone]);

            var body = player.GetComponent<Rigidbody2D>();
            player.Session.TryActivate(root.GetComponentsInChildren<Checkpoint>().Single(c => c.name == "InstrumentDais"));
            body.position = new Vector2(51f, .7f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.2f);
            yield return Shoot(root.GetComponentsInChildren<StarInstrument>().Single(i => i.Kind == StarInstrumentKind.Lyre).transform.position);
            Assert.AreSame(bank.Lyre, impacts[StarImpactKind.Lyre]);

            body.position = new Vector2(59f, .7f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.2f);
            yield return Shoot(root.GetComponentsInChildren<StarInstrument>().Single(i => i.Kind == StarInstrumentKind.Drum).transform.position);
            Assert.AreSame(bank.Drum, impacts[StarImpactKind.Drum]);
            Assert.AreSame(bank.Ceramic, ceramic);
            Assert.AreEqual(events, core.ImpactPlaybackCount, "An instrument hit must not add a generic stone hit.");
            Assert.AreEqual(1, core.FracturePlaybackCount);

            var shadowDais = root.GetComponentsInChildren<Checkpoint>().Single(c => c.name == "ShadowDais");
            player.Session.TryActivate(shadowDais);
            Assert.IsTrue(player.BeginPuzzleReset(shadowDais));
            yield return new WaitForSeconds(.7f);
            yield return Shoot(new Vector2(12f, 1.5f));
            Assert.IsTrue(player.Session.Shadows.HasShadow);
            Assert.AreSame(bank.Embed, shadow.LastPlayedClip);
            Assert.IsTrue(shadow.GetComponents<AudioSource>().All(s => s.outputAudioMixerGroup != null && s.outputAudioMixerGroup.name == "SFX"));
            player.SetPaused(true); yield return null;
            Assert.IsTrue(shadow.GetComponents<AudioSource>().All(s => !s.isPlaying));
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player)); yield return null;
            Assert.IsTrue(shadow.GetComponents<AudioSource>().All(s => !s.isPlaying));
        }
    }
}
