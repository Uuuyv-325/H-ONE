using System.Collections;
using System.Collections.Generic;
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
    public sealed class ImpactAudioTests
    {
        private Scene scene;
        private PlayerStateMachine player;
        private CoreAudioFeedback feedback;
        private AudioClip[] generated;
        private float previousTimeScale;

        [UnitySetUp] public IEnumerator Load()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            var existing = new HashSet<int>(Resources.FindObjectsOfTypeAll<AudioClip>().Select(clip => clip.GetInstanceID()));
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Chapter01_Core.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("Chapter01_Core", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath("Assets/Game/Scenes/Chapter01_Core.unity");
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerStateMachine>()).Single();
            feedback = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CoreAudioFeedback>()).Single();
            generated = Resources.FindObjectsOfTypeAll<AudioClip>()
                .Where(clip => clip.name.StartsWith("Procedural_") && !existing.Contains(clip.GetInstanceID())).ToArray();
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.SetPaused(false);
            Time.timeScale = 1f;
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest] public IEnumerator RealProgressEventsHaveFixedStagesAndNeverThrottleIndependentCollections()
        {
            Assert.AreEqual(12, feedback.GeneratedClipCount);
            var sources = feedback.GetComponents<AudioSource>();
            Assert.AreEqual(2, sources.Length);
            Assert.IsTrue(sources.All(source => source.outputAudioMixerGroup != null &&
                source.outputAudioMixerGroup.name == "SFX"), "Every generated source must use the user-controlled SFX bus.");

            player.Session.SetAllowedFragmentIds(new[] { "audio-a", "audio-b", "audio-c" });
            var before = feedback.CollectionPlaybackCount;
            Assert.IsTrue(player.Session.TryCollect("audio-a"));
            Assert.AreEqual(1, feedback.LastCollectionStage);
            Assert.IsFalse(player.Session.TryCollect("audio-a"));
            Assert.AreEqual(before + 1, feedback.CollectionPlaybackCount);
            Assert.IsTrue(player.Session.TryCollect("audio-b"));
            Assert.AreEqual(2, feedback.LastCollectionStage);
            Assert.IsTrue(player.Session.TryCollect("audio-c"));
            Assert.AreEqual(3, feedback.LastCollectionStage);
            Assert.AreEqual(before + 3, feedback.CollectionPlaybackCount,
                "Three real collections in one frame must each dispatch their own progress cue.");

            player.SetPaused(true);
            yield return null;
            Assert.IsTrue(feedback.SourcesPaused);
            player.SetPaused(false);
            yield return null;
            Assert.IsFalse(feedback.SourcesPaused);
            Assert.IsTrue(player.Session.ResetCurrentPuzzle(player));
            Assert.IsTrue(sources.All(source => !source.isPlaying), "Puzzle reset must stop all current sounds.");

            feedback.enabled = false;
            feedback.enabled = true;
            Assert.AreEqual(2, feedback.GetComponents<AudioSource>().Length);
            Assert.AreEqual(12, feedback.GeneratedClipCount, "Re-enabling must reuse the existing clip bank.");
        }

        [UnityTest] public IEnumerator CeramicBreakClustersAreLimitedAndMissingMixerCannotBypassVolume()
        {
            var before = feedback.FracturePlaybackCount;
            var first = Wall("audio-wall-a");
            first.Break();
            first.Break();
            Wall("audio-wall-b").Break();
            Wall("audio-wall-c").Break();
            Assert.AreEqual(before + 1, feedback.FracturePlaybackCount,
                "Duplicate and simultaneous real wall breaks share one short ceramic cluster.");
            yield return new WaitForSeconds(0.09f);
            Wall("audio-wall-d").Break();
            Assert.AreEqual(before + 2, feedback.FracturePlaybackCount);

            feedback.Configure(player, null, null, null, null, null, null, null, null);
            player.Session.SetAllowedFragmentIds(new[] { "silent-a" });
            before = feedback.CollectionPlaybackCount;
            Assert.IsTrue(player.Session.TryCollect("silent-a"));
            Assert.AreEqual(before, feedback.CollectionPlaybackCount);
            Assert.IsTrue(feedback.GetComponents<AudioSource>().All(source => !source.isPlaying),
                "A missing SFX route must remain silent.");

            Assert.AreEqual(12, generated.Length);
            var target = feedback.gameObject;
            Object.Destroy(feedback);
            yield return null;
            yield return null;
            Assert.IsTrue(generated.All(clip => clip == null), "Owned PCM clips must be released when the feedback is destroyed.");
            Assert.AreEqual(0, target.GetComponents<AudioSource>().Length, "Owned sources must not survive the feedback component.");
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
