using System.Collections;
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
    public sealed class CoreGameplayTests
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private PlayerStateMachine player;
        private Rigidbody2D body;
        private Scene scene;
        private float fixedStep, scale;
        private InputSettings.EditorInputBehaviorInPlayMode inputBehavior;

        [UnitySetUp] public IEnumerator LoadCoreScene()
        {
            fixedStep = Time.fixedDeltaTime; scale = Time.timeScale;
            inputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Chapter01_Core.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("Chapter01_Core", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath("Assets/Game/Scenes/Chapter01_Core.unity");
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<PlayerStateMachine>() != null)
                    player = root.GetComponentInChildren<PlayerStateMachine>();
            Assert.IsNotNull(player); body = player.GetComponent<Rigidbody2D>();
            Assert.IsTrue(player.Input.isActiveAndEnabled, "Input reader must be active");
            player.Input.Actions.devices = new InputDevice[] { keyboard, mouse };
            player.Input.Actions.Disable();
            player.Input.Actions.Enable();
            for (var i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTearDown] public IEnumerator UnloadCoreScene()
        {
            if (player != null && player.Paused) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = inputBehavior;
            Time.fixedDeltaTime = fixedStep; Time.timeScale = scale;
        }

        private static void Set<T>(InputControl<T> control, T value) where T : struct
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device, out var eventPtr))
            {
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();
        }
        private static void Press(ButtonControl control) => Set(control, 1f);
        private static void Release(ButtonControl control) => Set(control, 0f);

        private void AimAt(Vector2 world)
        {
            var camera = player.GetComponentInParent<Transform>().root.GetComponentInChildren<Camera>();
            var screen = camera.WorldToScreenPoint(world);
            Set(mouse.position, new Vector2(screen.x, screen.y));
        }

        private IEnumerator ChargeAndRelease(Vector2 target, float seconds = 0.82f)
        {
            AimAt(target); yield return null;
            Press(keyboard.eKey);
            var until = Time.time + seconds;
            while (Time.time < until)
            {
                AimAt(target);
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.eKey);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        [UnityTest] public IEnumerator WalkJumpAndRejectAirCharge()
        {
            var start = body.position;
            Assert.IsTrue(player.Input.Actions.FindAction("Gameplay/Move").enabled, "Move action must be enabled");
            Press(keyboard.dKey);
            Assert.AreEqual(1f, keyboard.dKey.ReadValue(), "Virtual device state");
            var moveAction = player.Input.Actions.FindAction("Gameplay/Move");
            Assert.AreEqual(1f, moveAction.ReadValue<float>(), "Move controls: " + string.Join(",", moveAction.controls) +
                "; playing=" + Application.isPlaying + "; focused=" + Application.isFocused + "; mode=" + InputSystem.settings.updateMode);
            for (var i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Release(keyboard.dKey);
            Assert.That(body.position.x - start.x, Is.EqualTo(0.88f).Within(0.14f));
            var floorY = body.position.y;
            Press(keyboard.spaceKey); yield return new WaitForFixedUpdate(); Release(keyboard.spaceKey);
            var apex = body.position.y;
            Press(keyboard.eKey);
            for (var i = 0; i < 65; i++)
            {
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, body.position.y);
                Assert.AreEqual(PlayerState.Human, player.State);
            }
            Release(keyboard.eKey);
            Assert.That(apex - floorY, Is.EqualTo(2.2f).Within(0.08f));
            TestContext.WriteLine("Measured jump height: " + (apex - floorY).ToString("F3") + " units");
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest] public IEnumerator CancelPauseAndLostFocusCannotFireOnRelease()
        {
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Charging, player.State);
            Press(mouse.rightButton); yield return new WaitForFixedUpdate(); Release(mouse.rightButton);
            Release(keyboard.eKey); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null;
            Assert.IsTrue(player.Paused); Assert.AreEqual(PlayerState.Human, player.State);
            Release(keyboard.eKey); yield return null;
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null;
            Assert.IsFalse(player.Paused);
            yield return new WaitForFixedUpdate(); Assert.AreEqual(PlayerState.Human, player.State);
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            player.SendMessage("OnApplicationFocus", false);
            Release(keyboard.eKey); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Charging, player.State);
        }

        [UnityTest] public IEnumerator HeldChargeAcrossPauseRequiresPhysicalRelease()
        {
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            player.SetPaused(true); yield return null;
            player.SetPaused(false);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Release(keyboard.eKey); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Charging, player.State);
        }

        [UnityTest] public IEnumerator HumanContactDoesNotCollectAndStarFailureKeepsFragment()
        {
            var fragment = Object.FindFirstObjectByType<StarFragment>();
            body.position = new Vector2(fragment.transform.position.x, 0.7f);
            Physics2D.SyncTransforms();
            for (var i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, player.Session.FragmentCount);
            body.position = player.Session.CurrentCheckpoint.SpawnPosition;
            Physics2D.SyncTransforms();
            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            yield return ChargeAndRelease(fragment.transform.position);
            var deadline = Time.time + 3f;
            while (player.State != PlayerState.Human && Time.time < deadline) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, player.Session.FragmentCount);
            Assert.IsFalse(fragment.GetComponent<Collider2D>().enabled);
            Assert.AreEqual(PlayerState.Human, player.State);
            Assert.That(Vector2.Distance(body.position, player.Session.CurrentCheckpoint.SpawnPosition), Is.LessThan(0.1f));
            player.Fail(); player.Fail();
            var started = Time.time;
            while (player.State != PlayerState.Human && Time.time < started + 1f) yield return new WaitForFixedUpdate();
            Assert.That(Time.time - started, Is.EqualTo(0.5f).Within(0.05f));
            TestContext.WriteLine("Measured reform: " + (Time.time - started).ToString("F3") + " seconds");
            Assert.AreEqual(1, player.Session.FragmentCount);
        }

        [UnityTest] public IEnumerator FlightIsStraightUnsteerableAndPausesAtSamePosition()
        {
            yield return ChargeAndRelease(new Vector2(1f, 2.4f));
            Assert.AreEqual(PlayerState.StarFlight, player.State);
            var direction = player.Flight.Direction;
            Press(keyboard.aKey); AimAt(new Vector2(-10f, 8f));
            for (var i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(direction, player.Flight.Direction), Is.LessThan(0.001f));
            player.SetPaused(true); var frozen = body.position;
            for (var i = 0; i < 4; i++) yield return null;
            Assert.AreEqual(frozen, body.position); Assert.AreEqual(PlayerState.StarFlight, player.State);
            player.SetPaused(false); Release(keyboard.aKey);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Greater(Vector2.Distance(frozen, body.position), 0.05f);
        }

        [UnityTest] public IEnumerator LastVisitedCheckpointWinsAndBlockedSpawnUsesFallback()
        {
            Checkpoint second = null;
            foreach (var checkpoint in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                if (checkpoint.StableId == "core-second") second = checkpoint;
            Assert.IsNotNull(second);
            Assert.AreNotEqual(second, player.Session.CurrentCheckpoint);
            body.position = second.SpawnPosition; Physics2D.SyncTransforms();
            for (var i = 0; i < 6; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(second, player.Session.CurrentCheckpoint);
            var obstacle = new GameObject("Temporary spawn obstruction");
            SceneManager.MoveGameObjectToScene(obstacle, scene); obstacle.transform.position = second.SpawnPosition;
            obstacle.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.3f);
            Physics2D.SyncTransforms();
            player.Fail();
            var until = Time.time + 1f;
            while (player.State != PlayerState.Human && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Assert.That(body.position.x, Is.EqualTo(second.SpawnPosition.x - 0.8f).Within(0.08f));
        }

        [UnityTest] public IEnumerator ReformPauseDoesNotConsumeTimerOrQueuedInput()
        {
            player.Fail();
            yield return new WaitForFixedUpdate();
            player.SetPaused(true);
            Press(keyboard.eKey); Release(keyboard.eKey);
            for (var i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(PlayerState.Reforming, player.State);
            player.SetPaused(false);
            var start = Time.time;
            while (player.State != PlayerState.Human && Time.time < start + 1f) yield return new WaitForFixedUpdate();
            Assert.Greater(Time.time - start, 0.4f);
            for (var i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
        }

        [UnityTest] public IEnumerator RepeatedShotsAndCancellationReleaseAudioAndInput()
        {
            for (var shot = 0; shot < 3; shot++)
            {
                yield return ChargeAndRelease(new Vector2(-0.5f, 1.1f), 0.2f);
                Assert.AreEqual(PlayerState.StarFlight, player.State);
                var until = Time.time + 2f;
                while (player.State != PlayerState.Human && Time.time < until) yield return new WaitForFixedUpdate();
                Assert.AreEqual(PlayerState.Human, player.State);
            }
            Press(keyboard.eKey); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Charging, player.State);
            var sources = player.transform.root.GetComponentsInChildren<AudioSource>();
            Assert.AreEqual(2, sources.Length);
            Assert.IsTrue(System.Array.Exists(sources, source => source.loop && source.isPlaying));
            Press(mouse.rightButton); yield return new WaitForFixedUpdate();
            Release(mouse.rightButton); Release(keyboard.eKey);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(PlayerState.Human, player.State);
            Assert.IsFalse(System.Array.Exists(sources, source => source.loop && source.isPlaying));
            Assert.AreEqual(2, player.transform.root.GetComponentsInChildren<AudioSource>().Length);
        }
    }
}
