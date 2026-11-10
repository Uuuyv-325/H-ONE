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
    public sealed class ShadowGameplayTests
    {
        private const string Path = "Assets/Game/Scenes/Chapter01_ShadowSlice.unity";
        private Scene scene;
        private PlayerStateMachine player;
        private Rigidbody2D body;
        private Keyboard keyboard;
        private Mouse mouse;
        private StarSocket platform, plateSocket;
        private ShadowService shadows;
        private float oldScale, oldStep;
        private InputSettings.EditorInputBehaviorInPlayMode oldBehavior;
        private InputSettings.BackgroundBehavior oldBackground;
        [UnitySetUp] public IEnumerator Load()
        {
            oldScale = Time.timeScale; oldStep = Time.fixedDeltaTime;
            oldBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("Chapter01_ShadowSlice", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath(Path);
            foreach (var root in scene.GetRootGameObjects())
            {
                player = root.GetComponentInChildren<PlayerStateMachine>();
                if (player == null) continue;
                foreach (var socket in root.GetComponentsInChildren<StarSocket>())
                    if (socket.name == "PlatformSocket") platform = socket; else plateSocket = socket;
                break;
            }
            Assert.IsNotNull(player); Assert.IsNotNull(platform); Assert.IsNotNull(plateSocket);
            body = player.GetComponent<Rigidbody2D>(); shadows = player.Session.Shadows;
            player.Input.Actions.devices = new InputDevice[] { keyboard, mouse };
            player.Input.Actions.Disable(); player.Input.Actions.Enable();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(player.IsGrounded);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = oldBehavior;
            InputSystem.settings.backgroundBehavior = oldBackground;
            Time.timeScale = oldScale; Time.fixedDeltaTime = oldStep;
        }
        private static void Set<T>(InputControl<T> control, T value) where T : struct
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device, out var state))
            { control.WriteValueIntoEvent(value, state); InputSystem.QueueEvent(state); }
            InputSystem.Update();
        }
        private void Aim(Vector2 target)
        {
            var camera = player.transform.root.GetComponentInChildren<Camera>();
            var point = camera.WorldToScreenPoint(target); Set(mouse.position, new Vector2(point.x, point.y));
        }
        private IEnumerator Shoot(Vector2 target)
        {
            Aim(target); yield return null; Set(keyboard.eKey, 1f);
            for (var i = 0; i < 42; i++) { Aim(target); yield return new WaitForFixedUpdate(); }
            Set(keyboard.eKey, 0f); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        }
        private SignalBarrier Bridge() => player.transform.root.GetComponentInChildren<SignalBarrier>();

        [UnityTest] public IEnumerator EmbeddingReformsOnceAndHumanCanJumpOntoShadowThenLaunchAgain()
        {
            var reforms = 0; player.Reformed += () => reforms++;
            yield return Shoot(platform.transform.position);
            yield return new WaitForSeconds(0.9f);
            Assert.AreSame(platform, shadows.ActiveSocket); Assert.AreEqual(1, reforms);
            Assert.AreEqual(PlayerState.Human, player.State);
            Assert.That(Vector2.Distance(body.position, player.Session.CurrentCheckpoint.SpawnPosition), Is.LessThan(0.1f));
            body.position = new Vector2(-6f, 0.7f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.1f);
            Set(keyboard.spaceKey, 1f); Set(keyboard.dKey, 1f);
            yield return new WaitForFixedUpdate(); Set(keyboard.spaceKey, 0f);
            yield return new WaitForSeconds(0.43f); Set(keyboard.dKey, 0f);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(player.IsGrounded); Assert.That(body.position.y, Is.EqualTo(2.565f).Within(0.08f));
            yield return Shoot(new Vector2(-4f, 5f));
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(player.Session.HasFragment("slice-f2")); Assert.IsTrue(shadows.HasShadow);
        }

        [UnityTest] public IEnumerator RecallIsOneSecondNotQueuedAndPausesWithGameTime()
        {
            Assert.IsTrue(platform.TryEmbed());
            Set(keyboard.rKey, 1f); yield return new WaitForFixedUpdate(); Set(keyboard.rKey, 0f);
            yield return new WaitForSeconds(0.35f);
            Assert.IsFalse(shadows.BeginRecall()); Assert.IsTrue(shadows.HasShadow);
            player.SetPaused(true); var progress = shadows.RecallProgress;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.AreEqual(progress, shadows.RecallProgress); Assert.IsTrue(shadows.HasShadow);
            player.SetPaused(false); yield return new WaitForSeconds(0.5f); Assert.IsTrue(shadows.HasShadow);
            yield return new WaitForSeconds(0.25f); Assert.IsFalse(shadows.HasShadow); Assert.IsFalse(shadows.Recalling);
            Assert.IsTrue(plateSocket.TryEmbed()); yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(shadows.HasShadow, "An earlier R press must not queue a second recall.");
        }

        [UnityTest] public IEnumerator FailurePreservesShadowAndCollectionAndDoesNotCancelRecall()
        {
            platform.TryEmbed(); player.Session.TryCollect("slice-f1");
            shadows.BeginRecall(); yield return new WaitForSeconds(0.15f);
            player.Fail(); player.Fail(); Assert.IsFalse(shadows.BeginRecall()); yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(PlayerState.Human, player.State); Assert.IsTrue(shadows.HasShadow);
            Assert.IsTrue(player.Session.HasFragment("slice-f1"));
            yield return new WaitForSeconds(0.4f); Assert.IsFalse(shadows.HasShadow);
        }

        [UnityTest] public IEnumerator OccupiedSocketActsAsSolidAndCannotReplaceExistingShadow()
        {
            platform.TryEmbed(); Assert.IsFalse(plateSocket.TryEmbed());
            yield return Shoot(plateSocket.transform.position); yield return new WaitForSeconds(1.5f);
            Assert.AreSame(platform, shadows.ActiveSocket); Assert.AreEqual(PlayerState.Human, player.State);
        }

        [UnityTest] public IEnumerator RecallWithdrawsBridgeAndFallingPlayerCanRecover()
        {
            plateSocket.TryEmbed(); yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(Bridge().IsSolid);
            body.position = new Vector2(3f, 0.7f); body.linearVelocity = Vector2.zero;
            shadows.BeginRecall(); yield return new WaitForSeconds(0.8f); Assert.IsTrue(Bridge().IsSolid);
            yield return new WaitForSeconds(0.4f); Assert.IsFalse(Bridge().IsSolid);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(PlayerState.Human, player.State); Assert.IsTrue(player.IsGrounded);
            Assert.That(body.position.x, Is.EqualTo(-8f).Within(0.1f));
            Assert.IsTrue(plateSocket.TryEmbed()); yield return new WaitForSeconds(0.3f); Assert.IsTrue(Bridge().IsSolid);
        }

        [UnityTest] public IEnumerator StandingOnShadowAndRecallingDoesNotLeaveGhostGround()
        {
            platform.TryEmbed(); body.position = new Vector2(-4f, 2.6f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.25f); Assert.IsTrue(player.IsGrounded);
            shadows.BeginRecall(); yield return new WaitForSeconds(1.15f);
            Assert.IsFalse(shadows.HasShadow); Assert.Less(body.position.y, 2.55f);
            yield return new WaitForSeconds(0.65f); Assert.IsTrue(player.IsGrounded);
            Assert.That(body.position.y, Is.EqualTo(0.675f).Within(0.08f));
        }

        [UnityTest] public IEnumerator PuzzleResetPreservesProgressButRestartClearsItAndFragmentReturns()
        {
            player.Session.TryCollect("slice-f1"); plateSocket.TryEmbed(); yield return new WaitForSeconds(0.3f);
            shadows.BeginRecall(); player.SetPaused(true);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));
            Assert.IsFalse(shadows.HasShadow); Assert.IsFalse(shadows.Recalling); Assert.IsFalse(Bridge().IsSolid);
            Assert.IsTrue(player.Session.HasFragment("slice-f1"));
            player.SetPaused(false); yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(player.Session.RestartLevel(player)); yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, player.Session.FragmentCount);
            var fragments = player.transform.root.GetComponentsInChildren<StarFragment>();
            foreach (var fragment in fragments) Assert.IsTrue(fragment.GetComponent<Collider2D>().enabled);
        }

        [UnityTest] public IEnumerator WeightSetDeduplicatesCollidersAndRecomputesOnDisable()
        {
            var plate = plateSocket.GetComponentInChildren<PressurePlate>();
            var a = new GameObject("weight a"); SceneManager.MoveGameObjectToScene(a, scene); a.transform.position = plate.transform.position;
            var b = new GameObject("weight b"); SceneManager.MoveGameObjectToScene(b, scene); b.transform.position = plate.transform.position;
            a.AddComponent<PressureWeight>().Configure("a", PressureWeightKind.Stone);
            a.AddComponent<BoxCollider2D>(); a.AddComponent<CircleCollider2D>();
            b.AddComponent<PressureWeight>().Configure("b", PressureWeightKind.Stone); b.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms(); plate.Recompute(); Assert.AreEqual(2, plate.OccupantCount);
            a.SetActive(false); Physics2D.SyncTransforms(); plate.Recompute(); Assert.IsTrue(plate.Value); Assert.AreEqual(1, plate.OccupantCount);
            b.GetComponent<PressureWeight>().enabled = false; plate.Recompute(); Assert.IsFalse(plate.Value);
            Object.Destroy(a); Object.Destroy(b); yield return null;
        }

        [UnityTest] public IEnumerator GateWaitsForPlayerToClearBeforeClosing()
        {
            var plate = plateSocket.GetComponentInChildren<PressurePlate>();
            plateSocket.TryEmbed(); yield return new WaitForSeconds(0.2f);
            var go = new GameObject("test gate"); SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = body.position;
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(1f, 2f); collider.enabled = false;
            var gate = go.AddComponent<SignalBarrier>(); gate.Configure(plate, false, collider, null);
            yield return new WaitForSeconds(0.1f); Assert.IsFalse(gate.IsSolid);
            shadows.Clear(); yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(plate.Value); Assert.IsFalse(gate.IsSolid); Assert.IsTrue(gate.WaitingForClearance);
            body.position += Vector2.right * 2f; yield return new WaitForSeconds(0.3f); Assert.IsTrue(gate.IsSolid);
            Object.Destroy(go);
        }

        [UnityTest] public IEnumerator DisablingSocketReleasesItsShadowAndRecall()
        {
            platform.TryEmbed(); shadows.BeginRecall(); platform.enabled = false;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(shadows.HasShadow); Assert.IsFalse(shadows.Recalling); Assert.IsFalse(platform.Shadow.activeSelf);
        }

        [UnityTest] public IEnumerator PuzzleResetRestoresBodiesBeforeRecomputingOccupancy()
        {
            var group = player.Session.CurrentCheckpoint.Puzzle;
            var plate = plateSocket.GetComponentInChildren<PressurePlate>();
            var go = new GameObject("reset body"); go.transform.SetParent(group.transform);
            go.transform.position = plate.transform.position + Vector3.up * 0.2f;
            var initial = (Vector2)go.transform.position;
            var stone = go.AddComponent<Rigidbody2D>(); stone.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<BoxCollider2D>().size = Vector2.one * 0.25f;
            go.AddComponent<PressureWeight>().Configure("reset-body", PressureWeightKind.Stone);
            var reset = go.AddComponent<ResettableBody2D>();
            group.Configure(group.StableId, plate, Bridge(), reset);
            yield return new WaitForSeconds(0.25f); Assert.IsTrue(plate.Value);
            stone.position += Vector2.left * 5f; yield return new WaitForSeconds(0.25f); Assert.IsFalse(plate.Value);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));
            Assert.That(Vector2.Distance(stone.position, initial), Is.LessThan(0.01f));
            Assert.IsTrue(plate.Value); Assert.IsTrue(Bridge().IsSolid);
            Object.Destroy(go);
        }

        [UnityTest] public IEnumerator UnloadingClearsShadowAndPendingRecall()
        {
            var service = shadows; plateSocket.TryEmbed(); shadows.BeginRecall();
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsTrue(service == null); Assert.IsTrue(platform == null);
            scene = default;
        }
    }
}
