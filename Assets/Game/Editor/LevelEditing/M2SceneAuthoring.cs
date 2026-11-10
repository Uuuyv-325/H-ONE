using System;
using System.IO;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    public static class M2SceneAuthoring
    {
        public const string ScenePath = "Assets/Game/Scenes/Chapter01_ShadowSlice.unity";
        [MenuItem("H-ONE/Create M2 Kit and Sample (new only)")]
        public static void CreateSample() => CreateLevel(ScenePath, true);

        [MenuItem("H-ONE/Attach Runtime Editor to M2 Sample")]
        public static void AttachRuntimeEditorToSample()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing the sample scene.");
            LevelPieceFactory.EnsurePrefabs();
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject root = null;
                foreach (var rootObject in scene.GetRootGameObjects())
                    if (rootObject.name == "LevelRoot") { root = rootObject; break; }
                if (root == null) throw new InvalidOperationException("The M2 sample has no LevelRoot.");
                EnsureRuntimeMetadata(root);
                var session = root.GetComponent<LevelSession>();
                var player = root.GetComponentInChildren<PlayerStateMachine>(true);
                var camera = root.GetComponentInChildren<Camera>(true);
                var catalog = AssetDatabase.LoadAssetAtPath<RuntimeLevelPieceCatalog>("Assets/Game/Data/RuntimeLevelPieceCatalog.asset");
                if (session == null || player == null || camera == null || catalog == null)
                    throw new InvalidOperationException("The M2 sample is missing its session, player, camera, or piece catalog.");
                var editor = root.GetComponent<RuntimeLevelEditor>() ?? Undo.AddComponent<RuntimeLevelEditor>(root);
                editor.Configure(catalog, session, player, camera, root.transform);
                EditorUtility.SetDirty(editor);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        public static void CreateLevel(string path, bool sample)
        {
            if (Application.isPlaying || File.Exists(path)) throw new InvalidOperationException("Needs a new scene path and stopped Play Mode.");
            LevelPieceFactory.EnsurePrefabs();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("LevelRoot");
                foreach (var folder in new[] { "Geometry", "Background", "Mechanisms", "Collectibles", "Checkpoints", "Tutorial", "Actors", "Presentation" })
                    LevelPieceFactory.Child(root.transform, folder);
                var bounds = root.AddComponent<LevelBounds2D>(); bounds.SetBounds(new Vector2(-12f, -4f), new Vector2(13f, 9f));
                var shadows = root.AddComponent<ShadowService>();
                var session = root.AddComponent<LevelSession>();
                var definition = ScriptableObject.CreateInstance<LevelDefinition>();
                definition.LevelId = Path.GetFileNameWithoutExtension(path);
                definition.FragmentIds = sample ? new[] { "slice-f1", "slice-f2", "slice-f3" } : new string[0];
                var configPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/Data/Levels/" + definition.LevelId + ".asset");
                AssetDatabase.CreateAsset(definition, configPath);
                var group = LevelPieceFactory.Child(root.transform.Find("Mechanisms"), "Puzzle_A").gameObject.AddComponent<PuzzleGroup>();
                group.Configure(LevelPieceFactory.NewId("puzzle"));
                session.ConfigureMechanisms(shadows, group);
                var entry = LevelPieceFactory.Place(LevelPiece.Checkpoint, root.transform.Find("Checkpoints"), new Vector2(-8f, 0f), session).GetComponent<Checkpoint>();
                entry.ConfigurePuzzle(group); session.Configure(definition, entry);
                var cameraObject = LevelPieceFactory.Child(root.transform.Find("Presentation"), "Main Camera").gameObject;
                cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(-4f, 3f, -10f);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.4f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.045f, 0.075f, 0.14f);
                cameraObject.AddComponent<AudioListener>();
                var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/ChapterPlayer.prefab"), root.transform.Find("Actors"));
                var player = playerObject.GetComponent<PlayerStateMachine>();
                player.Configure(AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Game/Data/Player/CorePlayerTuning.asset"), session, bounds, camera);
                playerObject.transform.position = entry.SpawnPosition;
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);
                PrefabUtility.RecordPrefabInstancePropertyModifications(playerObject.transform);
                var levelEditor = root.AddComponent<RuntimeLevelEditor>();
                levelEditor.Configure(AssetDatabase.LoadAssetAtPath<RuntimeLevelPieceCatalog>("Assets/Game/Data/RuntimeLevelPieceCatalog.asset"),
                    session, player, camera, root.transform);
                EditorUtility.SetDirty(levelEditor);
                cameraObject.AddComponent<ChapterCamera2D>().Configure(player, bounds);
                var preview = LevelPieceFactory.Child(root.transform.Find("Presentation"), "Trajectory").gameObject;
                var line = preview.AddComponent<LineRenderer>(); line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/P0/Trajectory.mat");
                line.useWorldSpace = true; line.startWidth = line.endWidth = 0.04f; line.sortingOrder = 20;
                line.startColor = new Color(1f, 0.85f, 0.48f); line.endColor = new Color(1f, 0.85f, 0.48f, 0.4f);
                var end = LevelPieceFactory.View(root.transform.Find("Presentation"), "ring", Vector2.one * 0.25f, Color.yellow);
                end.name = "TrajectoryEnd"; preview.AddComponent<TrajectoryPreview>().Configure(player, end.transform);
                var bus = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Game/Audio/Mixers/Core.mixer").FindMatchingGroups("SFX")[0];
                root.AddComponent<CoreAudioFeedback>().Configure(player, bus, Cue("charge_start"), Cue("charge_loop"), Cue("launch"), Cue("wall"), Cue("fail"), Cue("reform"), Cue("collect"));
                root.AddComponent<ShadowFeedback>().Configure(shadows, player, bus, Cue("collect"), Cue("charge_start"), Cue("reform"));
                MakePauseMenu(root.transform.Find("Presentation"), player);
                Solid(root, session, "StartFloor", new Vector2(-5.5f, -0.5f), new Vector2(13f, 1f));
                if (sample)
                {
                    Solid(root, session, "FarFloor", new Vector2(9f, -0.5f), new Vector2(8f, 1f));
                    var platform = LevelPieceFactory.Place(LevelPiece.PlatformSocket, group.transform, new Vector2(-4f, 1.5f), session);
                    platform.name = "PlatformSocket";
                    var plateSocket = LevelPieceFactory.Place(LevelPiece.PlateSocket, group.transform, new Vector2(-0.75f, 3.2f), session);
                    plateSocket.name = "PlateSocket";
                    var plate = plateSocket.GetComponentInChildren<PressurePlate>();
                    var bridgeObject = LevelPieceFactory.Place(LevelPiece.Bridge, group.transform, new Vector2(3f, -0.1f), session);
                    var bridge = bridgeObject.GetComponent<SignalBarrier>();
                    bridge.Configure(plate, true, bridgeObject.GetComponent<BoxCollider2D>(), bridgeObject.GetComponentInChildren<SpriteRenderer>());
                    PrefabUtility.RecordPrefabInstancePropertyModifications(bridge);
                    group.Configure(group.StableId, plate, bridge);
                    var far = LevelPieceFactory.Place(LevelPiece.Checkpoint, root.transform.Find("Checkpoints"), new Vector2(7.5f, 0f), session).GetComponent<Checkpoint>();
                    far.ConfigurePuzzle(group);
                    var positions = new[] { new Vector2(-5f, 1f), new Vector2(-4f, 5f), new Vector2(8f, 2f) };
                    for (var i = 0; i < positions.Length; i++)
                    {
                        var fragment = LevelPieceFactory.Place(LevelPiece.Fragment, root.transform.Find("Collectibles"), positions[i], session).GetComponent<StarFragment>();
                        fragment.Configure(definition.FragmentIds[i], session, fragment.transform.Find("Visual").gameObject);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(fragment);
                    }
                    var hazard = LevelPieceFactory.Place(LevelPiece.Hazard, root.transform.Find("Geometry"), new Vector2(3f, -2.5f), session);
                    Resize(hazard, new Vector2(4f, 0.5f));
                    // World teaching: one body at dais, arrow up into socket, and a footstep row on the support.
                    var tutorial = root.transform.Find("Tutorial");
                    var ghost = LevelPieceFactory.View(tutorial, "human", new Vector2(0.45f, 0.8f), new Color(0.5f, 0.7f, 1f, 0.35f));
                    ghost.transform.position = new Vector2(-4f, 2.4f);
                    for (var i = 0; i < 3; i++)
                    {
                        var mark = LevelPieceFactory.View(tutorial, "ring", Vector2.one * 0.13f, new Color(1f, 0.8f, 0.3f, 0.5f));
                        mark.transform.position = new Vector2(-6.5f + i * 0.65f, 0.1f);
                    }
                }
                else Solid(root, session, "FinishFloor", new Vector2(7f, -0.5f), new Vector2(12f, 1f));
                Solid(root, session, "LeftWall", new Vector2(-12.25f, 3f), new Vector2(0.5f, 8f));
                Solid(root, session, "RightWall", new Vector2(13.25f, 3f), new Vector2(0.5f, 8f));
                Solid(root, session, "Ceiling", new Vector2(0.5f, 8.75f), new Vector2(25f, 0.5f));
                EnsureRuntimeMetadata(root);
                DeliveredArtAuthoring.ApplyToScene(root);
                Physics2D.SyncTransforms();
                EditorSceneManager.SaveScene(scene, path); AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }

        private static AudioClip Cue(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/P0/p0_" + name + ".wav");

        private static void EnsureRuntimeMetadata(GameObject root)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                var go = item.gameObject;
                if (go.GetComponent<PlayerStateMachine>() != null)
                {
                    var misplacedMetadata = go.GetComponent<LevelPieceMetadata>();
                    if (misplacedMetadata != null) Undo.DestroyObjectImmediate(misplacedMetadata);
                    continue;
                }
                if (go.GetComponentInParent<StarSocket>() != null && go.GetComponent<StarSocket>() == null) continue;
                RuntimeLevelPieceKind? kind = null;
                if (go.TryGetComponent<StarInstrument>(out var instrument)) kind = instrument.Kind == StarInstrumentKind.Lyre ? RuntimeLevelPieceKind.Lyre : RuntimeLevelPieceKind.Drum;
                else if (go.GetComponent<BrittleWall>() != null) kind = RuntimeLevelPieceKind.BrittleWall;
                else if (go.GetComponent<Checkpoint>() != null) kind = RuntimeLevelPieceKind.Checkpoint;
                else if (go.GetComponent<StarFragment>() != null) kind = RuntimeLevelPieceKind.Fragment;
                else if (go.GetComponent<StarSocket>() != null)
                    kind = go.GetComponentInChildren<PressurePlate>(true) != null ? RuntimeLevelPieceKind.PlateSocket : RuntimeLevelPieceKind.PlatformSocket;
                else if (go.GetComponent<PressurePlate>() != null) kind = RuntimeLevelPieceKind.PressurePlate;
                else if (go.TryGetComponent<SignalBarrier>(out var barrier))
                    kind = barrier.SolidWhenTrue ? RuntimeLevelPieceKind.Bridge : RuntimeLevelPieceKind.Gate;
                else if (go.TryGetComponent<PressureWeight>(out var weight) && weight.Kind == PressureWeightKind.Stone)
                    kind = RuntimeLevelPieceKind.Stone;
                else if (go.GetComponent<Hazard2D>() != null) kind = RuntimeLevelPieceKind.Hazard;
                else if (go.GetComponent<BoxCollider2D>() != null && go.GetComponentInChildren<SpriteRenderer>(true) != null)
                    kind = RuntimeLevelPieceKind.Solid;
                if (!kind.HasValue) continue;

                var metadata = go.GetComponent<LevelPieceMetadata>();
                if (metadata == null) metadata = Undo.AddComponent<LevelPieceMetadata>(go);
                var group = go.GetComponent<Checkpoint>()?.Puzzle ?? go.GetComponentInParent<PuzzleGroup>();
                if (string.IsNullOrWhiteSpace(metadata.InstanceId))
                    metadata.Configure(kind.Value, NewPieceId(kind.Value, go), group != null ? group.StableId : null);
                else
                    metadata.Configure(kind.Value, metadata.InstanceId, group != null ? group.StableId : null);
                PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
            }
        }

        private static string NewPieceId(RuntimeLevelPieceKind kind, GameObject go)
        {
            switch (kind)
            {
                case RuntimeLevelPieceKind.Checkpoint: return go.GetComponent<Checkpoint>().StableId;
                case RuntimeLevelPieceKind.Fragment: return go.GetComponent<StarFragment>().StableId;
                case RuntimeLevelPieceKind.PlatformSocket:
                case RuntimeLevelPieceKind.PlateSocket: return go.GetComponent<StarSocket>().StableId;
                case RuntimeLevelPieceKind.Stone: return go.GetComponent<PressureWeight>().StableId;
                default: return LevelPieceFactory.NewId("piece");
            }
        }

        private static void Solid(GameObject root, LevelSession session, string name, Vector2 position, Vector2 size)
        { var go = LevelPieceFactory.Place(LevelPiece.Solid, root.transform.Find("Geometry"), position, session); go.name = name; Resize(go, size); }
        public static void Resize(GameObject go, Vector2 size)
        {
            go.GetComponent<BoxCollider2D>().size = size;
            if (go.TryGetComponent<GrassTerrainView>(out var terrain)) terrain.Rebuild();
            LevelPieceFactory.SetVisualSize(go.GetComponentInChildren<SpriteRenderer>(), size);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<BoxCollider2D>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponentInChildren<SpriteRenderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponentInChildren<SpriteRenderer>().transform);
        }

        private static void MakePauseMenu(Transform parent, PlayerStateMachine player)
        {
            var canvasObject = new GameObject("PuzzlePauseMenu", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image)); panel.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)panel.transform; rect.sizeDelta = new Vector2(380f, 260f); rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.04f, 0.07f, 0.13f, 0.97f);
            var resume = Button(panel.transform, "Resume", 75f);
            var reset = Button(panel.transform, "Reset puzzle", 0f);
            var restart = Button(panel.transform, "Restart level", -75f);
            panel.SetActive(false);
            canvasObject.AddComponent<PuzzlePauseMenu>().Configure(player, panel, resume, reset, restart);
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(parent, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        private static UnityEngine.UI.Button Button(Transform parent, string title, float y)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(300f, 52f); rect.anchoredPosition = new Vector2(0f, y);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.3f, 0.43f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text)); label.transform.SetParent(go.transform, false);
            var text = label.GetComponent<UnityEngine.UI.Text>(); text.text = title; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.sizeDelta = Vector2.zero;
            return go.GetComponent<UnityEngine.UI.Button>();
        }
    }
}
