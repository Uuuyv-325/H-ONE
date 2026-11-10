using System;
using System.IO;
using System.Linq;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    public static class M2ReleaseAuthoring
    {
        [MenuItem("H-ONE/Create M2 Entry and Gallery (new only)")]
        public static void Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (File.Exists(GameFlow.MainMenuScene) || File.Exists(GameFlow.GalleryScene))
                throw new InvalidOperationException("Entry/gallery already exist; edit their authored scenes directly.");
            var previous = SceneManager.GetActiveScene();
            M2SceneAuthoring.CreateLevel(GameFlow.GalleryScene, false);
            var scene = EditorSceneManager.OpenScene(GameFlow.GalleryScene, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try { AuthorGallery(scene.GetRootGameObjects()[0]); EditorSceneManager.SaveScene(scene, GameFlow.GalleryScene); }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("MainMenu");
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera.transform.SetParent(root.transform); camera.tag = "MainCamera";
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                camera.GetComponent<Camera>().backgroundColor = new Color(0.035f, 0.065f, 0.12f);
                M2MenuAuthoring.CreateMainMenu(root.transform);
                EditorSceneManager.SaveScene(scene, GameFlow.MainMenuScene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            ConfigureBuild(); AssetDatabase.SaveAssets();
        }

        private static void AuthorGallery(GameObject root)
        {
            root.name = "MechanismGallery";
            var session = root.GetComponent<LevelSession>();
            var player = root.GetComponentInChildren<PlayerStateMachine>();
            root.GetComponent<LevelBounds2D>().SetBounds(new Vector2(-12, -4), new Vector2(49, 9));
            var geometry = root.transform.Find("Geometry");
            // This is a newly created scene. Preserve the existing Core/ShadowSlice scenes and shared prefabs.
            UnityEngine.Object.DestroyImmediate(geometry.Find("FinishFloor").gameObject);
            ResizeAt(geometry.Find("StartFloor").gameObject, new Vector2(4, -0.5f), new Vector2(32, 1));
            ResizeAt(geometry.Find("RightWall").gameObject, new Vector2(49.25f, 3), new Vector2(0.5f, 12));
            ResizeAt(geometry.Find("Ceiling").gameObject, new Vector2(18.5f, 8.75f), new Vector2(61, 0.5f));
            Place(root, session, LevelPiece.Solid, geometry, "EastFloor", new Vector2(36.5f, -0.5f), new Vector2(25, 1));
            Place(root, session, LevelPiece.Hazard, geometry, "BridgePit", new Vector2(22, -2.5f), new Vector2(4, 0.5f));

            var launch = root.GetComponentInChildren<PuzzleGroup>(); launch.name = "01_StarFlight";
            launch.Configure("gallery-launch");
            var platform = Group(root, "02_ShadowBridge", "gallery-shadow");
            var stone = Group(root, "03_StoneGate", "gallery-stone");
            var entry = session.EntryCheckpoint; entry.ConfigurePuzzle(launch);
            var shadowDais = Place(root, session, LevelPiece.Checkpoint, root.transform.Find("Checkpoints"), "ShadowDais", new Vector2(9, 0)).GetComponent<Checkpoint>();
            shadowDais.ConfigurePuzzle(platform);
            var stoneDais = Place(root, session, LevelPiece.Checkpoint, root.transform.Find("Checkpoints"), "StoneDais", new Vector2(29, 0)).GetComponent<Checkpoint>();
            stoneDais.ConfigurePuzzle(stone);
            Place(root, session, LevelPiece.Solid, launch.transform, "JumpLedge", new Vector2(-1, 0.65f), new Vector2(2, 0.3f));
            var reflector = Place(root, session, LevelPiece.Solid, launch.transform, "OrdinaryReflector", new Vector2(4, 3.8f), new Vector2(0.3f, 3));
            reflector.transform.rotation = Quaternion.Euler(0, 0, -35);
            PrefabUtility.RecordPrefabInstancePropertyModifications(reflector.transform);
            Place(root, session, LevelPiece.PlatformSocket, platform.transform, "PlatformSocket", new Vector2(12, 1.5f));
            var plateSocket = Place(root, session, LevelPiece.PlateSocket, platform.transform, "BridgeSocket", new Vector2(17, 2.6f));
            // Match the currently retained P0 support to the plate socket's real collision width.
            var supportViews = plateSocket.transform.Find("ShadowPlatform/Visual").GetComponentsInChildren<SpriteRenderer>();
            LevelPieceFactory.SetVisualSize(supportViews[1], new Vector2(0.9f, 0.18f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(supportViews[1].transform);
            var bridge = Place(root, session, LevelPiece.Bridge, platform.transform, "ShadowBridge", new Vector2(22, -0.1f), new Vector2(4, 0.2f));
            Connect(bridge, plateSocket.GetComponentInChildren<PressurePlate>());
            Place(root, session, LevelPiece.Stone, stone.transform, "PushableStone", new Vector2(33, 0.55f));
            var plate = Place(root, session, LevelPiece.PressurePlate, stone.transform, "StonePlate", new Vector2(36, 0)).GetComponent<PressurePlate>();
            Place(root, session, LevelPiece.Solid, stone.transform, "StoneStop", new Vector2(37.3f, 0.35f), new Vector2(0.5f, 0.7f));
            var gate = Place(root, session, LevelPiece.Gate, stone.transform, "PlateGate", new Vector2(40, 1.25f));
            Connect(gate, plate);
            var fragmentPositions = new[] { new Vector2(-4, 2), new Vector2(12, 5), new Vector2(44, 2) };
            var ids = new[] { "gallery-f1", "gallery-f2", "gallery-f3" };
            session.Definition.FragmentIds = ids;
            EditorUtility.SetDirty(session.Definition);
            for (var i = 0; i < ids.Length; i++)
            {
                var fragment = Place(root, session, LevelPiece.Fragment, root.transform.Find("Collectibles"), "Fragment" + (i + 1), fragmentPositions[i]).GetComponent<StarFragment>();
                fragment.Configure(ids[i], session, fragment.transform.Find("Visual").gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(fragment);
            }
            foreach (var group in new[] { launch, platform, stone })
            {
                var targets = group.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c is IPuzzleResettable).ToArray();
                group.Configure(group.StableId, targets);
            }
            session.ConfigureMechanisms(root.GetComponent<ShadowService>(), launch, platform, stone);
            M2MenuAuthoring.UpgradePause(root.GetComponentInChildren<PuzzlePauseMenu>(true));
            var canvas = M2MenuAuthoring.Canvas(root.transform.Find("Presentation"), "GalleryNavigation", 50);
            var strip = M2MenuAuthoring.Panel(canvas, "DevelopmentNavigation", new Vector2(0.25f, 0.92f), new Vector2(0.78f, 0.985f), new Color(0.04f, 0.07f, 0.12f, 0.96f));
            var buttons = new UnityEngine.UI.Button[3];
            var labels = new[] { "01 星弹反射", "02 星影与桥", "03 星石压板门" };
            for (var i = 0; i < 3; i++)
                buttons[i] = M2MenuAuthoring.Button(strip, "Station" + (i + 1), labels[i], new Vector2(i / 3f + 0.01f, 0.1f), new Vector2((i + 1) / 3f - 0.01f, 0.9f));
            strip.gameObject.AddComponent<GalleryDebugNavigation>().Configure(player, new[] { entry, shadowDais, stoneDais }, buttons);
            var hints = M2MenuAuthoring.Canvas(root.transform.Find("Presentation"), "ControlGlyphs", 30);
            M2MenuAuthoring.Label(hints, "Keys", "A / D   ↔      Space   ↑      E   ◎      R   ↶      Esc   Ⅱ", new Vector2(0.16f, 0.015f), new Vector2(0.84f, 0.07f), 25);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            Physics2D.SyncTransforms();
            ChapterLevelValidation.ValidateOrThrow();
        }
        private static PuzzleGroup Group(GameObject root, string name, string id)
        {
            var group = LevelPieceFactory.Child(root.transform.Find("Mechanisms"), name).gameObject.AddComponent<PuzzleGroup>();
            group.Configure(id); return group;
        }
        private static GameObject Place(GameObject root, LevelSession session, LevelPiece kind, Transform parent, string name, Vector2 position, Vector2? size = null)
        {
            var go = LevelPieceFactory.Place(kind, parent, position, session); go.name = name;
            if (size.HasValue) M2SceneAuthoring.Resize(go, size.Value);
            return go;
        }
        private static void ResizeAt(GameObject go, Vector2 position, Vector2 size)
        {
            go.transform.position = position; M2SceneAuthoring.Resize(go, size);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        private static void Connect(GameObject go, BoolSignal source)
        {
            var barrier = go.GetComponent<SignalBarrier>();
            barrier.Configure(source, barrier.SolidWhenTrue, go.GetComponent<BoxCollider2D>(), go.GetComponentInChildren<SpriteRenderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(barrier);
        }
        [MenuItem("H-ONE/Use M2 Entry for Play and Build")]
        public static void ConfigureBuild()
        {
            if (File.Exists(GameFlow.FirstLevelScene)) { ChapterFirstPassAuthoring.ConfigureBuild(); return; }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameFlow.MainMenuScene, true), new EditorBuildSettingsScene(GameFlow.GalleryScene, true) };
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameFlow.MainMenuScene);
            PlayerSettings.productName = GameFlow.GameTitle;
        }
        [InitializeOnLoadMethod]
        private static void SetDefaultEntry() => EditorApplication.delayCall += () =>
        {
            if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(GameFlow.MainMenuScene))
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameFlow.MainMenuScene);
        };
        [MenuItem("H-ONE/Play From Active Scene (debug and tests)")]
        public static void PlayFromActiveScene() => EditorSceneManager.playModeStartScene = null;
    }

    public sealed class M2BuildSceneGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            var published = CiBuild.ReadScenes();
            if (!published.SequenceEqual(new[] { GameFlow.MainMenuScene, GameFlow.FirstLevelScene, GameFlow.SecondLevelScene, GameFlow.GalleryScene }) ||
                !EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(published))
                throw new BuildFailedException("Build scenes must be MainMenu, Chapter01_01, Chapter01_02, MechanismGallery.");
        }
    }
}
