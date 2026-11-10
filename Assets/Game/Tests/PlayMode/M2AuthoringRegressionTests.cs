using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HOne.Game.Tests
{
    public sealed class M2AuthoringRegressionTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private RuntimeLevelEditor editor;
        private GameObject root;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Chapter01_ShadowSlice.unity", new LoadSceneParameters(LoadSceneMode.Additive));
#endif
            scene = SceneManager.GetSceneByPath("Assets/Game/Scenes/Chapter01_ShadowSlice.unity");
            root = scene.GetRootGameObjects()[0]; player = root.GetComponentInChildren<PlayerStateMachine>();
            editor = root.GetComponent<RuntimeLevelEditor>();
            yield return new WaitForSeconds(0.15f);
            Invoke("EnterEditor");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        private object Invoke(string method) => typeof(RuntimeLevelEditor).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, null);

        [UnityTest] public IEnumerator InstrumentLinksAndArtScaleSurviveLayoutRoundtrip()
        {
            var lyre = editor.PlacePieceAt(RuntimeLevelPieceKind.Lyre,new Vector2(-3,3));
            var drum = editor.PlacePieceAt(RuntimeLevelPieceKind.Drum,new Vector2(3,3));
            var brittle = editor.PlacePieceAt(RuntimeLevelPieceKind.BrittleWall,new Vector2(5,3));
            var gate = editor.PlacePieceAt(RuntimeLevelPieceKind.Gate,new Vector2(7,2));
            drum.GetComponent<StarInstrument>().Configure(StarInstrumentKind.Drum,player.Session,drum.InstanceId,brittle.GetComponent<BrittleWall>());
            gate.GetComponent<SignalBarrier>().Configure(lyre.GetComponent<StarInstrument>(),false,gate.GetComponent<BoxCollider2D>(),gate.GetComponentInChildren<SpriteRenderer>());
            var scale = lyre.transform.Find("Visual").localScale;
            var lyreId=lyre.InstanceId; var drumId=drum.InstanceId; var brittleId=brittle.InstanceId; var gateId=gate.InstanceId;
            var document=(RuntimeLevelDocument)Invoke("CaptureDocument");
            editor.ApplyDocument(RuntimeLevelDocumentStore.Deserialize(RuntimeLevelDocumentStore.Serialize(document)));
            yield return null;
            var pieces=root.GetComponentsInChildren<LevelPieceMetadata>();
            lyre=pieces.Single(p=>p.InstanceId==lyreId); drum=pieces.Single(p=>p.InstanceId==drumId);
            brittle=pieces.Single(p=>p.InstanceId==brittleId); gate=pieces.Single(p=>p.InstanceId==gateId);
            Assert.AreEqual(scale,lyre.transform.Find("Visual").localScale);
            Assert.AreSame(lyre.GetComponent<StarInstrument>(),gate.GetComponent<SignalBarrier>().Source);
            Assert.AreSame(brittle.GetComponent<BrittleWall>(),drum.GetComponent<StarInstrument>().WaveTargets.Single());
            drum.GetComponent<StarInstrument>().Strike(); Assert.IsTrue(brittle.GetComponent<BrittleWall>().Broken);
            Assert.IsTrue(player.Session.RestartLevel(player)); Assert.IsFalse(brittle.GetComponent<BrittleWall>().Broken);
        }

        [UnityTest] public IEnumerator DynamicPiecesSurviveLayoutRoundtripAndRestart()
        {
            Invoke("AddGroup");
            var stone = editor.PlacePieceAt(RuntimeLevelPieceKind.Stone, new Vector2(-10, 0.55f));
            var fragment = editor.PlacePieceAt(RuntimeLevelPieceKind.Fragment, new Vector2(-10, 3));
            var gate = editor.PlacePieceAt(RuntimeLevelPieceKind.Gate, new Vector2(10, 1.25f));
            Assert.IsFalse(gate.GetComponent<SignalBarrier>().SolidWhenTrue, "New gate must retain open-on-signal polarity.");
            var stoneId = stone.InstanceId; var fragmentId = fragment.InstanceId;
            var document = (RuntimeLevelDocument)Invoke("CaptureDocument");
            editor.ApplyDocument(RuntimeLevelDocumentStore.Deserialize(RuntimeLevelDocumentStore.Serialize(document)));
            yield return null;
            var pieces = root.GetComponentsInChildren<LevelPieceMetadata>();
            stone = pieces.Single(p => p.InstanceId == stoneId); fragment = pieces.Single(p => p.InstanceId == fragmentId);
            var origin = stone.transform.position;
            Assert.IsTrue(fragment.GetComponent<StarFragment>().TryCollect());
            stone.GetComponent<Rigidbody2D>().position += Vector2.left * 1.5f;
            Assert.IsTrue(player.Session.RestartLevel(player));
            Assert.That(Vector2.Distance(stone.GetComponent<Rigidbody2D>().position, origin), Is.LessThan(0.01f));
            Assert.IsTrue(fragment.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(fragment.transform.Find("Visual").gameObject.activeSelf);
            // A newly authored group must also reset before any Save/Load operation.
            Invoke("AddGroup");
            var direct = editor.PlacePieceAt(RuntimeLevelPieceKind.Stone, new Vector2(-9, 0.55f));
            var directOrigin = (Vector2)direct.transform.position;
            direct.GetComponent<Rigidbody2D>().position += Vector2.left;
            Assert.IsTrue(player.Session.RestartLevel(player));
            Assert.That(Vector2.Distance(direct.GetComponent<Rigidbody2D>().position, directOrigin), Is.LessThan(0.01f));
        }

        [UnityTest] public IEnumerator RebindingFragmentMovesResetSubscription()
        {
            var fragment = editor.PlacePieceAt(RuntimeLevelPieceKind.Fragment, new Vector2(-10, 3)).GetComponent<StarFragment>();
            var id = fragment.StableId;
            player.Session.SetAllowedFragmentIds(new[] { id });
            Assert.IsTrue(fragment.TryCollect());
            var other = new GameObject("OtherSession"); other.transform.SetParent(root.transform);
            var session = other.AddComponent<LevelSession>(); session.SetAllowedFragmentIds(new[] { id });
            fragment.Configure(id, session, fragment.transform.Find("Visual").gameObject);
            Assert.IsTrue(fragment.GetComponent<CircleCollider2D>().enabled);
            Assert.IsTrue(fragment.TryCollect());
            Assert.IsTrue(player.Session.RestartLevel(player));
            Assert.IsFalse(fragment.GetComponent<CircleCollider2D>().enabled, "Old session reset must not refresh a rebound fragment.");
            yield return null;
        }
    }
}
