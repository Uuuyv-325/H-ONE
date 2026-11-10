using System.Collections;
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
    public sealed class M2GalleryGameplayTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode oldBehavior;
        private InputSettings.BackgroundBehavior oldBackground;
        [UnitySetUp] public IEnumerator Load()
        {
            oldBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameFlow.GalleryScene, new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            scene = SceneManager.GetSceneByPath(GameFlow.GalleryScene);
            player = scene.GetRootGameObjects()[0].GetComponentInChildren<PlayerStateMachine>();
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            player.Input.Actions.devices = new InputDevice[] { keyboard, mouse };
            player.Input.Actions.Disable(); player.Input.Actions.Enable();
            yield return new WaitForSeconds(0.2f);
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
        }
        private static void Set<T>(InputControl<T> control, T value) where T : struct
        {
            using (UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device, out var state))
            { control.WriteValueIntoEvent(value, state); InputSystem.QueueEvent(state); }
            InputSystem.Update();
        }
        private IEnumerator Shoot(Vector2 target)
        {
            Aim(target); yield return null; Set(keyboard.eKey, 1f);
            for (var i = 0; i < 42; i++) { Aim(target); yield return new WaitForFixedUpdate(); }
            Set(keyboard.eKey, 0f); yield return new WaitForSeconds(1.7f);
        }
        private void Aim(Vector2 target)
        {
            var camera = player.transform.root.GetComponentInChildren<Camera>();
            var point = camera.WorldToScreenPoint(target); Set(mouse.position, new Vector2(point.x, point.y));
        }

        [UnityTest] public IEnumerator EditorToolbarReceivesClicksAboveGalleryNavigation()
        {
            var editor = player.transform.root.GetComponent<RuntimeLevelEditor>();
            var canvas = editor.transform.Find("RuntimeLevelEditorCanvas");
            canvas.Find("Edit level").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var save = canvas.Find("EditorPanel/Toolbar/Save").GetComponent<UnityEngine.UI.Button>();
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, save.transform.position)
            };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.AreSame(save, hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(),
                "The visible Save button must receive the click instead of the gallery navigation.");
            canvas.Find("EditorPanel/Toolbar/Return").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsFalse(editor.IsEditing);
        }

        [UnityTest] public IEnumerator GrassSeamsJumpArtworkAndPauseKeepTheOriginalMotor()
        {
            var root = scene.GetRootGameObjects()[0];
            DeliveredArtSet art = null;
#if UNITY_EDITOR
            art = UnityEditor.AssetDatabase.LoadAssetAtPath<DeliveredArtSet>("Assets/Game/Art/Delivery01/ChapterArt.asset");
#endif
            if (art == null) Assert.Ignore("Requires the imported Editor delivery asset.");
            var body = player.GetComponent<Rigidbody2D>();
            var drawing = player.transform.Find("Visual/Human").GetComponent<SpriteRenderer>();
            var start = player.Position;
            Set(keyboard.dKey,1f); yield return new WaitForSeconds(.65f); Set(keyboard.dKey,0f);
            Assert.That(player.Position.x-start.x,Is.GreaterThan(2.5f));
            Assert.That(player.Position.y,Is.EqualTo(start.y).Within(.03f));
            Set(keyboard.spaceKey,1f); yield return new WaitForFixedUpdate(); Set(keyboard.spaceKey,0f);
            yield return new WaitForSeconds(.12f);
            Assert.IsFalse(player.IsGrounded); Assert.AreEqual(art.Jump,drawing.sprite);
            player.SetPaused(true); var position = body.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(position,body.position); Assert.AreEqual(art.Jump,drawing.sprite);
            player.SetPaused(false); yield return new WaitForSeconds(1f);
            Assert.IsTrue(player.IsGrounded); Assert.AreNotEqual(art.Jump,drawing.sprite);
            Assert.IsNotNull(root.GetComponentInChildren<GrassTerrainView>());
        }

        [UnityTest] public IEnumerator InstrumentPredictionIsPureAndProgressSurvivesFailureAndPuzzleReset()
        {
            var root = scene.GetRootGameObjects()[0];
            var lyre = root.GetComponentsInChildren<StarInstrument>().Single(i => i.Kind == StarInstrumentKind.Lyre);
            var drum = root.GetComponentsInChildren<StarInstrument>().Single(i => i.Kind == StarInstrumentKind.Drum);
            var wall = root.GetComponentInChildren<BrittleWall>();
            var dais = root.GetComponentsInChildren<Checkpoint>().Single(c => c.name == "InstrumentDais");
            player.Session.TryActivate(dais);
            var body = player.GetComponent<Rigidbody2D>(); body.position = new Vector2(51,.7f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.2f);
            var path = new StarFlightState(player.Position,(Vector2)lyre.transform.position-player.Position);
            new StarPathSolver(player.Tuning.StarRadius,player.WorldMask,player.transform).Advance(ref path,6f,null,null);
            Assert.AreEqual(0,path.BouncesLeft); Assert.IsFalse(lyre.Value); Assert.IsFalse(wall.Broken);
            yield return Shoot(lyre.transform.position); Assert.IsTrue(lyre.Value);
            body.position = new Vector2(59,.7f); body.linearVelocity = Vector2.zero; yield return new WaitForSeconds(.2f);
            yield return Shoot(drum.transform.position); Assert.IsTrue(wall.Broken);
            player.Fail(); yield return new WaitForSeconds(.6f);
            Assert.IsTrue(lyre.Value); Assert.IsTrue(wall.Broken);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player)); yield return new WaitForSeconds(.6f);
            Assert.IsTrue(lyre.Value); Assert.IsTrue(wall.Broken);
            Assert.IsTrue(player.Session.RestartLevel(player)); yield return new WaitForSeconds(.6f);
            Assert.IsFalse(lyre.Value); Assert.IsFalse(wall.Broken); Assert.IsTrue(wall.GetComponent<Collider2D>().enabled);
            drum.Strike(); yield return null;
            var wave = drum.GetComponentInChildren<LineRenderer>(); Assert.IsTrue(wave.enabled);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player)); yield return null;
            Assert.IsFalse(wave.enabled);
        }
        [UnityTest] public IEnumerator GallerySupportsCollectionEmbeddingBridgeCrossingAndStonePlate()
        {
            yield return Shoot(new Vector2(-4, 2));
            Assert.IsTrue(player.Session.HasFragment("gallery-f1"));
            var root = player.transform.root;
            root.Find("Presentation/GalleryNavigation/DevelopmentNavigation/Station2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSeconds(0.7f);
            yield return Shoot(new Vector2(12, 1.5f));
            Assert.IsTrue(player.Session.Shadows.HasShadow);
            Assert.AreEqual("PlatformSocket", player.Session.Shadows.ActiveSocket.name);
            Set(keyboard.rKey, 1f); yield return new WaitForFixedUpdate(); Set(keyboard.rKey, 0f);
            yield return new WaitForSeconds(1.1f); Assert.IsFalse(player.Session.Shadows.HasShadow);
            Set(keyboard.dKey, 1f); yield return new WaitForSeconds(1.35f); Set(keyboard.dKey, 0f);
            yield return new WaitForFixedUpdate();
            yield return Shoot(new Vector2(17, 2.6f));
            Assert.AreEqual("BridgeSocket", player.Session.Shadows.ActiveSocket?.name);
            Set(keyboard.dKey, 1f); yield return new WaitForSeconds(4.5f); Set(keyboard.dKey, 0f);
            yield return new WaitForFixedUpdate(); Assert.Greater(player.Position.x, 26f, "Human must walk across the formed bridge.");
            root.Find("Presentation/GalleryNavigation/DevelopmentNavigation/Station3").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSeconds(0.7f);
            var plate = root.GetComponentsInChildren<PressurePlate>().Single(p => p.name == "StonePlate");
            var gate = root.GetComponentsInChildren<SignalBarrier>().Single(p => p.name == "PlateGate");
            Set(keyboard.dKey, 1f);
            var deadline = Time.time + 5f;
            while (!plate.Value && Time.time < deadline) yield return new WaitForFixedUpdate();
            Set(keyboard.dKey, 0f); yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(plate.Value, "The pushed stone should remain against its backstop on the plate.");
            Assert.IsFalse(gate.GetComponent<BoxCollider2D>().enabled);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player)); yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(plate.Value); Assert.IsTrue(gate.GetComponent<BoxCollider2D>().enabled);
            Assert.IsTrue(player.Session.HasFragment("gallery-f1"));
        }
    }
}
