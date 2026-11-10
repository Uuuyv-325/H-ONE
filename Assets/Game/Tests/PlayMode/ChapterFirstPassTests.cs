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
    public sealed class ChapterFirstPassTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode oldInput;
        private InputSettings.BackgroundBehavior oldBackground;
        private GameObject Root => player.transform.root.gameObject;
        [UnitySetUp] public IEnumerator Setup()
        {
            oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale=1;
            keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
            yield return null;
        }
        private IEnumerator Load(string path)
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            scene=SceneManager.GetSceneByPath(path);
            player=scene.GetRootGameObjects().Single(g=>g.GetComponent<LevelSession>()!=null).GetComponentInChildren<PlayerStateMachine>();
            player.Input.Actions.devices=new InputDevice[]{keyboard,mouse};
            player.Input.Actions.Disable(); player.Input.Actions.Enable();
            yield return new WaitForSeconds(.25f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(player!=null) player.SetPaused(false);
            Time.timeScale=1;
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if(mouse!=null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;
            InputSystem.settings.backgroundBehavior=oldBackground;
        }
        private static void Set<T>(InputControl<T> control,T value) where T:struct
        {
            using(UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device,out var state))
            { control.WriteValueIntoEvent(value,state); InputSystem.QueueEvent(state); }
            InputSystem.Update();
        }
        private IEnumerator Move(float x,bool jump=false)
        {
            var deadline=Time.time+10f;
            var nextJump=Time.time;
            while(Mathf.Abs(player.Position.x-x)>.12f && Time.time<deadline)
            {
                Set(keyboard.dKey,player.Position.x<x?1f:0f); Set(keyboard.aKey,player.Position.x>x?1f:0f);
                if(jump && player.IsGrounded && Time.time>=nextJump)
                { Set(keyboard.spaceKey,1f); nextJump=Time.time+.8f; }
                else Set(keyboard.spaceKey,0f);
                yield return new WaitForFixedUpdate();
            }
            Set(keyboard.dKey,0f);Set(keyboard.aKey,0f);Set(keyboard.spaceKey,0f);
            Assert.That(player.Position.x,Is.EqualTo(x).Within(.2f),"Walk/jump route blocked at "+player.Position);
            var landing=Time.time+2;
            while(!player.IsGrounded && Time.time<landing) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(.2f);
        }
        private void Aim(Vector2 target)
        {
            var camera=Root.GetComponentInChildren<Camera>();
            var screen=camera.WorldToScreenPoint(target); Set(mouse.position,new Vector2(screen.x,screen.y));
        }
        private IEnumerator Shoot(Vector2 target)
        {
            Assert.IsTrue(player.IsGrounded,"Shot must start from a reachable standing surface at "+player.Position);
            Aim(target); yield return null; Set(keyboard.eKey,1);
            for(var i=0;i<44;i++){Aim(target);yield return new WaitForFixedUpdate();}
            Assert.AreEqual(PlayerState.Charging,player.State);
            Set(keyboard.eKey,0);yield return new WaitForSeconds(1.6f);
        }
        private List<StarContact> Predict(Vector2 from,Vector2 target)
        {
            var path=new StarFlightState(from,target-from);var contacts=new List<StarContact>();
            new StarPathSolver(player.Tuning.StarRadius,player.WorldMask,player.transform).Advance(ref path,player.Tuning.FlightDistance,null,contacts);
            return contacts;
        }
        [UnityTest] public IEnumerator FirstLevelUsesActualInputForAllThreeFragmentsAndProgressiveTeaching()
        {
            yield return Load(GameFlow.FirstLevelScene);
            var tutorial=Root.GetComponentInChildren<ChapterTutorial>();
            Assert.IsFalse(tutorial.ChargeVisible);Assert.IsFalse(tutorial.ShadowVisible);Assert.IsFalse(tutorial.RecallVisible);Assert.IsFalse(tutorial.CollectionVisible);
            yield return Move(4.5f,true);
            Assert.IsTrue(tutorial.ChargeVisible); Assert.IsFalse(tutorial.ShadowVisible);
            yield return Shoot(new Vector2(6,2.2f)); Assert.IsTrue(player.Session.HasFragment("l1-f1"));
            Assert.IsTrue(tutorial.CollectionVisible);
            yield return Move(12.6f);
            Assert.IsFalse(Predict(player.Position,new Vector2(16.62f,4.6f)).Any(c=>c.Collider.GetComponent<StarFragment>()!=null),"Direct shot must be occluded.");
            yield return Shoot(new Vector2(17,.7f)); Assert.IsTrue(player.Session.HasFragment("l1-f2"),"Lyre reflection must reach F2.");
            yield return Move(23,true);
            Assert.IsTrue(tutorial.ShadowVisible);Assert.IsFalse(tutorial.RecallVisible);
            Assert.IsFalse(Predict(new Vector2(25,.7f),new Vector2(25,9.5f)).Any(c=>c.Collider.GetComponent<StarFragment>()!=null),"Ground shot must run out before F3.");
            yield return Shoot(new Vector2(25,1.6f)); Assert.IsTrue(player.Session.Shadows.HasShadow);
            player.Fail();yield return new WaitForSeconds(.6f);Assert.IsTrue(player.Session.Shadows.HasShadow);
            yield return Move(25,true);yield return new WaitForSeconds(.7f);
            Assert.Greater(player.Position.y,2.2f,"Human must stand on the shadow support.");
            Assert.IsTrue(tutorial.RecallVisible,"Recall is shown after the support has been used.");
            yield return Shoot(new Vector2(25,9.5f));Assert.AreEqual(3,player.Session.FragmentCount);
            Assert.IsTrue(Root.GetComponentInChildren<ChapterExit>().Condition.Value);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));yield return new WaitForSeconds(.6f);
            Assert.AreEqual(3,player.Session.FragmentCount);Assert.IsFalse(player.Session.Shadows.HasShadow);
            Assert.IsTrue(tutorial.RecallVisible,"Puzzle reset keeps learned controls.");
            Assert.IsTrue(player.Session.RestartLevel(player));yield return new WaitForSeconds(.6f);
            Assert.AreEqual(0,player.Session.FragmentCount);Assert.IsFalse(tutorial.RecallVisible);Assert.IsFalse(tutorial.ChargeVisible);
        }
        [UnityTest] public IEnumerator SecondLevelConnectsStoneBridgeDrumLyreAndFluteWithoutGrantingProgress()
        {
            yield return Load(GameFlow.SecondLevelScene);
            var plate=Root.GetComponentsInChildren<PressurePlate>().Single(c=>c.name=="StonePlate");
            Set(keyboard.dKey,1);
            var deadline=Time.time+5;
            while(!plate.Value && Time.time<deadline) yield return new WaitForFixedUpdate();
            Set(keyboard.dKey,0);yield return new WaitForSeconds(.3f);Assert.IsTrue(plate.Value);
            yield return Move(9,true);yield return new WaitForSeconds(.5f);
            yield return Shoot(new Vector2(9,2.2f));Assert.IsTrue(player.Session.HasFragment("l2-f1"));
            yield return Move(12.3f);
            yield return Shoot(new Vector2(14,3));Assert.AreEqual("BridgeSocket",player.Session.Shadows.ActiveSocket?.name);
            yield return Move(24);yield return Shoot(new Vector2(23.5f,2.2f));Assert.IsTrue(player.Session.HasFragment("l2-f2"));
            yield return Move(26);
            var wall=Root.GetComponentInChildren<BrittleWall>();
            Predict(player.Position,new Vector2(29,1.6f));Assert.IsFalse(wall.Broken,"Preview must not dispatch drum waves.");
            yield return Shoot(new Vector2(29,1.6f));Assert.IsTrue(wall.Broken);Assert.IsTrue(player.Session.HasMechanism("l2-drum"));
            yield return Move(34.6f,true);yield return new WaitForSeconds(.4f);
            Assert.IsFalse(Predict(player.Position,new Vector2(38.62f,4.6f)).Any(c=>c.Collider.GetComponent<StarSwitch>()!=null));
            yield return Shoot(new Vector2(39,.7f));Assert.IsTrue(player.Session.HasMechanism("l2-s1"));
            yield return Move(44,true);yield return new WaitForSeconds(.4f);
            var lift=Root.GetComponentInChildren<PanFluteLift>();
            var ports=Root.GetComponentsInChildren<FlutePort>();
            var up=ports.Single(f=>f.Raise);var down=ports.Single(f=>!f.Raise);
            Assert.IsFalse(Predict(new Vector2(48,1f),new Vector2(48,10.3f)).Any(c=>c.Collider.GetComponent<StarSwitch>()!=null || c.Collider.GetComponent<StarFragment>()!=null),"Even the lower platform must not reach S2/F3.");
            Predict(player.Position,up.transform.position);Assert.IsFalse(lift.Raised);
            yield return Shoot(up.transform.position);Assert.IsTrue(lift.Raised);Assert.That(lift.transform.position.y,Is.EqualTo(lift.Upper.y).Within(.03f));
            yield return Move(48,true);yield return new WaitForSeconds(.6f);
            Assert.Greater(player.Position.y,2.5f,"The raised flute platform must be reachable by jumping.");
            yield return Shoot(new Vector2(48,10.3f));Assert.IsTrue(player.Session.HasMechanism("l2-s2"));Assert.AreEqual(3,player.Session.FragmentCount);
            var exit=Root.GetComponentInChildren<ChapterExit>();Assert.IsTrue(exit.Condition.Value);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));yield return new WaitForSeconds(.65f);
            Assert.IsFalse(lift.Raised);Assert.IsTrue(wall.Broken);Assert.IsTrue(player.Session.HasMechanism("l2-s1"));Assert.IsTrue(exit.Condition.Value);
            yield return Shoot(up.transform.position);
            yield return Shoot(down.transform.position);Assert.IsFalse(lift.Raised);
            Set(keyboard.dKey,1);
            var exitDeadline=Time.realtimeSinceStartup+6;
            var jumpAgain=Time.time;
            while(!exit.Completed && Time.realtimeSinceStartup<exitDeadline)
            {
                if(player.IsGrounded && Time.time>=jumpAgain) { Set(keyboard.spaceKey,1);jumpAgain=Time.time+.8f; }
                else Set(keyboard.spaceKey,0);
                yield return null;
            }
            Set(keyboard.dKey,0);Set(keyboard.spaceKey,0);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(exit.Completed);Assert.IsTrue(player.Paused);
            Assert.IsTrue(Root.GetComponentInChildren<ChapterCompletionView>(true).gameObject.activeSelf);
            Assert.IsTrue(player.Session.Completed);
            Assert.IsFalse(Root.GetComponentInChildren<PuzzlePauseMenu>(true).transform.Find("Panel").gameObject.activeInHierarchy,
                "Completion must not expose the ordinary pause actions.");
            Set(keyboard.escapeKey,1);yield return null;Set(keyboard.escapeKey,0);yield return null;
            Assert.IsTrue(player.Paused,"Esc must not resume an already completed level.");
        }
        [UnityTest] public IEnumerator LiftCarriesHumanAndPauseAndPuzzleResetRestoreItsEndpoint()
        {
            yield return Load(GameFlow.SecondLevelScene);
            var lift=Root.GetComponentInChildren<PanFluteLift>();
            player.Session.TryActivate(Root.GetComponentsInChildren<Checkpoint>().Single(c=>c.name=="FluteDais"));
            var body=player.GetComponent<Rigidbody2D>();body.position=new Vector2(48,1);body.linearVelocity=Vector2.zero;
            yield return new WaitForSeconds(.25f);Assert.IsTrue(player.IsGrounded);
            lift.SetRaised(true);yield return new WaitForSeconds(.6f);
            Assert.Greater(player.Position.y,1.5f);
            player.SetPaused(true);var position=(Vector2)lift.transform.position;var rider=player.Position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(position,(Vector2)lift.transform.position);Assert.AreEqual(rider,player.Position);
            player.SetPaused(false);yield return new WaitForSeconds(1.2f);
            Assert.That(player.Position.y,Is.EqualTo(2.69f).Within(.08f));
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));yield return new WaitForSeconds(.6f);
            Assert.That(lift.transform.position.y,Is.EqualTo(lift.Lower.y).Within(.01f));
            Assert.That(player.Position.x,Is.EqualTo(44).Within(.05f));
        }
        [UnityTest] public IEnumerator RightBankCanRebuildTheBridgeAfterRecallOrReset()
        {
            yield return Load(GameFlow.SecondLevelScene);
            player.Session.TryActivate(Root.GetComponentsInChildren<Checkpoint>().Single(c=>c.name=="DrumDais"));
            var body=player.GetComponent<Rigidbody2D>();body.position=new Vector2(22.1f,.7f);body.linearVelocity=Vector2.zero;
            yield return new WaitForSeconds(.2f);
            yield return Shoot(new Vector2(14,3));Assert.IsTrue(player.Session.Shadows.HasShadow);
            Set(keyboard.rKey,1);yield return new WaitForFixedUpdate();Set(keyboard.rKey,0);
            yield return new WaitForSeconds(1.1f);Assert.IsFalse(player.Session.Shadows.HasShadow);
            yield return Move(22.1f);
            yield return Shoot(new Vector2(14,3));Assert.IsTrue(player.Session.Shadows.HasShadow);
        }
    }
}
