using System;
using System.IO;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HOne.Game.Editor
{
    public static class CoreSceneAuthoring
    {
        public const string ScenePath = "Assets/Game/Scenes/Chapter01_Core.unity";
        private const string Root = "Assets/Game/";

        [MenuItem("H-ONE/Create M0-M1 Core Scene (new only)")]
        public static void Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Core scene already exists; edit it in the Editor.");
            var previous = SceneManager.GetActiveScene();
            foreach (var folder in new[] { "Scenes", "Data/Player", "Data/Levels", "Prefabs/Player", "Art/P0", "Input" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "Art/P0" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 128f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            AssetDatabase.CreateAsset(tuning, Root + "Data/Player/CorePlayerTuning.asset");
            var definition = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(definition, Root + "Data/Levels/CoreLevel.asset");
            var input = CreateActions();
            File.WriteAllText(Root + "Input/ChapterPlayer.inputactions", input.ToJson());
            UnityEngine.Object.DestroyImmediate(input);
            AssetDatabase.ImportAsset(Root + "Input/ChapterPlayer.inputactions");
            input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "Input/ChapterPlayer.inputactions");
            var surface = new PhysicsMaterial2D("CoreFrictionless") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(surface, Root + "Data/Player/CoreFrictionless.physicsMaterial2D");
            var trajectoryMaterial = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(trajectoryMaterial, Root + "Art/P0/Trajectory.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("LevelRoot");
            var geometry = Child(root.transform, "Geometry");
            var background = Child(root.transform, "Background");
            var mechanisms = Child(root.transform, "Mechanisms");
            var collectibles = Child(root.transform, "Collectibles");
            var checkpoints = Child(root.transform, "Checkpoints");
            Child(root.transform, "Tutorial");
            var actors = Child(root.transform, "Actors");
            var presentation = Child(root.transform, "Presentation");
            var bounds = root.AddComponent<LevelBounds2D>();
            bounds.SetBounds(new Vector2(-12f, -3f), new Vector2(14f, 9f));
            var session = root.AddComponent<LevelSession>();

            Solid(geometry, "Floor", new Vector2(-1.5f, -0.5f), new Vector2(21f, 1f), surface);
            Solid(geometry, "FarLanding", new Vector2(13f, -0.5f), new Vector2(2f, 1f), surface);
            Solid(geometry, "LeftWall", new Vector2(-12.25f, 3f), new Vector2(0.5f, 8f), surface);
            Solid(geometry, "RightWall", new Vector2(14.25f, 3f), new Vector2(0.5f, 8f), surface);
            Solid(geometry, "Ceiling", new Vector2(1f, 8.75f), new Vector2(26f, 0.5f), surface);
            Solid(geometry, "JumpLedge", new Vector2(-9.5f, 0.7f), new Vector2(2f, 1.4f), surface);
            Solid(geometry, "ThinWall_006", new Vector2(5f, 0.9f), new Vector2(0.06f, 1.8f), surface);
            var angled = Solid(geometry, "AngledStone", new Vector2(0f, 4.8f), new Vector2(3f, 0.15f), surface);
            angled.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            var hazard = Child(geometry, "PitHazard");
            hazard.localPosition = new Vector3(10.5f, -1.5f);
            var hazardShape = hazard.gameObject.AddComponent<BoxCollider2D>();
            hazardShape.size = new Vector2(3f, 2f); hazardShape.isTrigger = true;
            hazard.gameObject.AddComponent<Hazard2D>();

            // Low-contrast columns are decorations and have no collision.
            for (var i = 0; i < 6; i++)
            {
                var pillar = Sprite(background, "BackgroundColumn", "stone", new Vector2(-10f + i * 4.5f, 3f));
                pillar.transform.localScale = new Vector3(0.65f, 6f, 1f);
                pillar.color = new Color(0.24f, 0.31f, 0.42f, 0.24f);
                pillar.sortingOrder = -10;
            }
            var entry = Dais(checkpoints, "EntryDais", "core-entry", -6f);
            Dais(checkpoints, "SecondDais", "core-second", 2f);
            session.Configure(definition, entry);

            var cameraObject = Child(presentation, "Main Camera").gameObject;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-2f, 3f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.045f, 0.075f, 0.14f);
            cameraObject.AddComponent<AudioListener>();

            var template = new GameObject("ChapterPlayer");
            template.SetActive(false);
            template.layer = 2;
            var body = template.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var shape = template.AddComponent<BoxCollider2D>();
            shape.size = new Vector2(0.65f, 1.35f); shape.sharedMaterial = surface;
            var reader = template.AddComponent<PlayerInputReader>(); reader.Configure(input);
            var player = template.AddComponent<PlayerStateMachine>(); player.Configure(tuning, null, null, null);
            var visuals = Child(template.transform, "Visual");
            var human = Sprite(visuals, "Human", "human", new Vector2(0f, -0.675f));
            // Preserve imported sprite IDs, including the project's auto-trimmed samples.
            human.transform.localPosition = new Vector3(0f, -shape.size.y * 0.5f - human.sprite.bounds.min.y, 0f);
            var star = Sprite(visuals, "Star", "star", Vector2.zero); star.transform.localScale = Vector3.one * 0.7f;
            var ring = Sprite(visuals, "ChargeRing", "ring", Vector2.zero);
            template.AddComponent<PlayerView>().Configure(player, human, star, ring);
            template.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, Root + "Prefabs/Player/ChapterPlayer.prefab");
            UnityEngine.Object.DestroyImmediate(template);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, actors);
            player = instance.GetComponent<PlayerStateMachine>();
            player.Configure(tuning, session, bounds, camera);
            instance.transform.position = entry.SpawnPosition;
            instance.SetActive(true);
            cameraObject.AddComponent<ChapterCamera2D>().Configure(player, bounds);

            var previewObject = Child(presentation, "Trajectory").gameObject;
            var line = previewObject.AddComponent<LineRenderer>();
            line.sharedMaterial = trajectoryMaterial; line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.035f;
            line.startColor = new Color(1f, 0.85f, 0.48f, 0.85f); line.endColor = new Color(1f, 0.85f, 0.48f, 0.35f);
            line.sortingOrder = 20;
            var endpoint = Sprite(presentation, "TrajectoryEnd", "ring", Vector2.zero);
            endpoint.transform.localScale = Vector3.one * 0.3f;
            previewObject.AddComponent<TrajectoryPreview>().Configure(player, endpoint.transform);

            var fragment = Child(collectibles, "F1").gameObject;
            fragment.transform.position = new Vector3(-0.5f, 1.1f);
            var collectShape = fragment.AddComponent<CircleCollider2D>(); collectShape.radius = 0.35f; collectShape.isTrigger = true;
            var fragmentVisual = Sprite(fragment.transform, "Visual", "fragment", Vector2.zero);
            fragmentVisual.transform.localScale = Vector3.one * 0.8f;
            fragment.AddComponent<StarFragment>().Configure("core-f1", session, fragmentVisual.gameObject);
            var node = Sprite(mechanisms, "CollectedFragmentNode", "empty_node", new Vector2(-6f, 3.2f));
            root.AddComponent<SessionView>().Configure(session, node, LoadSprite("empty_node"), LoadSprite("fragment"));
            var audio = presentation.gameObject.AddComponent<CoreAudioFeedback>();
            audio.Configure(player, AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(Root + "Audio/Mixers/Core.mixer").FindMatchingGroups("SFX")[0],
                Cue("charge_start"), Cue("charge_loop"), Cue("launch"), Cue("wall"), Cue("fail"), Cue("reform"), Cue("collect"));

            DeliveredArtAuthoring.ApplyToScene(root);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
        }

        public static InputActionAsset CreateActions()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplay = asset.AddActionMap("Gameplay");
            gameplay.AddAction("Move", InputActionType.Value).AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            gameplay.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            gameplay.AddAction("Charge", InputActionType.Button, "<Keyboard>/e");
            gameplay.AddAction("Cancel", InputActionType.Button, "<Mouse>/rightButton");
            gameplay.AddAction("Recall", InputActionType.Button, "<Keyboard>/r");
            gameplay.AddAction("Aim", InputActionType.Value, "<Mouse>/position");
            asset.AddActionMap("System").AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            return asset;
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        private static UnityEngine.Sprite LoadSprite(string name) =>
            AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Root + "Art/P0/" + name + ".png");
        private static AudioClip Cue(string name) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/P0/p0_" + name + ".wav");
        private static SpriteRenderer Sprite(Transform parent, string name, string asset, Vector2 position)
        {
            var child = Child(parent, name); child.localPosition = position;
            var sprite = child.gameObject.AddComponent<SpriteRenderer>(); sprite.sprite = LoadSprite(asset); sprite.sortingOrder = 5;
            return sprite;
        }
        private static GameObject Solid(Transform parent, string name, Vector2 position, Vector2 size, PhysicsMaterial2D material)
        {
            var go = Child(parent, name).gameObject; go.transform.position = position;
            var shape = go.AddComponent<BoxCollider2D>(); shape.size = size; shape.sharedMaterial = material;
            var visual = Sprite(go.transform, "Visual", "stone", Vector2.zero);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f); visual.sortingOrder = 0;
            return go;
        }
        private static Checkpoint Dais(Transform parent, string name, string id, float x)
        {
            var go = Child(parent, name).gameObject; go.transform.position = new Vector2(x, 0f);
            var trigger = go.AddComponent<BoxCollider2D>(); trigger.size = new Vector2(1.5f, 0.3f);
            trigger.offset = new Vector2(0f, 0.15f); trigger.isTrigger = true;
            var spawn = Child(go.transform, "SafeSpawn"); spawn.localPosition = new Vector3(0f, 0.7f);
            var fallback = Child(go.transform, "FallbackSpawn"); fallback.localPosition = new Vector3(-0.8f, 0.7f);
            var checkpoint = go.AddComponent<Checkpoint>(); checkpoint.Configure(id, spawn, fallback);
            var visual = Sprite(go.transform, "Visual", "dais", new Vector2(0f, 0.2f));
            visual.transform.localScale = new Vector3(2f, 0.6f, 1f);
            return checkpoint;
        }
    }
}
