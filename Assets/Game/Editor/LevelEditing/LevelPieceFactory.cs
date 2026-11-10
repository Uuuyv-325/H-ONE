using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HOne.Game.Editor
{
    public enum LevelPiece { Solid, Hazard, Checkpoint, Fragment, PlatformSocket, PlateSocket, PressurePlate, Bridge, Gate, Stone, Lyre, Drum, BrittleWall }

    public static class LevelPieceFactory
    {
        public const string PrefabFolder = "Assets/Game/Prefabs/Mechanisms/";
        public static string NewId(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/P0/" + name + ".png");
        public static Transform Child(Transform parent, string name)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
        public static SpriteRenderer View(Transform parent, string asset, Vector2 size, Color color)
        {
            var view = Child(parent, "Visual").gameObject.AddComponent<SpriteRenderer>();
            view.sprite = Sprite(asset); SetVisualSize(view, size); view.color = color;
            return view;
        }
        public static void SetVisualSize(SpriteRenderer view, Vector2 size)
        {
            view.drawMode = SpriteDrawMode.Simple;
            var native = view.sprite.bounds.size;
            view.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }
        public static BoxCollider2D Box(GameObject go, Vector2 size, bool trigger = false)
        {
            var box = go.AddComponent<BoxCollider2D>(); box.size = size; box.isTrigger = trigger;
            box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Game/Data/Player/CoreFrictionless.physicsMaterial2D");
            return box;
        }

        // Templates have no scene references. Place() binds the active level's explicit dependencies.
        public static GameObject Make(LevelPiece kind)
        {
            var go = new GameObject(kind.ToString());
            var gold = new Color(1f, 0.8f, 0.3f);
            var blue = new Color(0.36f, 0.55f, 0.75f);
            switch (kind)
            {
                case LevelPiece.Lyre:
                case LevelPiece.Drum:
                    var instrument = go.AddComponent<StarInstrument>();
                    instrument.Configure(kind == LevelPiece.Lyre ? StarInstrumentKind.Lyre : StarInstrumentKind.Drum, null, NewId("instrument"));
                    var face = Box(go, new Vector2(.12f, kind == LevelPiece.Lyre ? 1.8f : 1.6f));
                    face.offset = new Vector2(kind == LevelPiece.Lyre ? 0f : -.12f, 0f);
                    View(go.transform,"stone",Vector2.one,Color.white);
                    break;
                case LevelPiece.BrittleWall:
                    var brittleShape = Box(go, new Vector2(.35f,2f));
                    var brittleView = View(go.transform,"stone",new Vector2(.35f,2f),new Color(.7f,.35f,.2f));
                    go.AddComponent<BrittleWall>().Configure(null,brittleShape,brittleView.gameObject,NewId("brittle"));
                    break;
                case LevelPiece.Solid:
                    Box(go, new Vector2(4f, 0.5f)); View(go.transform, "stone", new Vector2(4f, 0.5f), blue); break;
                case LevelPiece.Hazard:
                    Box(go, new Vector2(3f, 0.5f), true); go.AddComponent<Hazard2D>();
                    View(go.transform, "stone", new Vector2(3f, 0.5f), new Color(0.7f, 0.25f, 0.2f)); break;
                case LevelPiece.Checkpoint:
                    var trigger = Box(go, new Vector2(1.5f, 0.3f), true); trigger.offset = Vector2.up * 0.15f;
                    var spawn = Child(go.transform, "SafeSpawn"); spawn.localPosition = Vector2.up * 0.7f;
                    var fallback = Child(go.transform, "FallbackSpawn"); fallback.localPosition = new Vector2(-0.8f, 0.7f);
                    go.AddComponent<Checkpoint>().Configure(NewId("dais"), spawn, fallback);
                    View(go.transform, "dais", new Vector2(1.5f, 0.3f), gold); break;
                case LevelPiece.Fragment:
                    var circle = go.AddComponent<CircleCollider2D>(); circle.radius = 0.3f; circle.isTrigger = true;
                    var fragmentView = View(go.transform, "fragment", Vector2.one * 0.65f, gold);
                    go.AddComponent<StarFragment>().Configure(NewId("fragment"), null, fragmentView.gameObject); break;
                case LevelPiece.PlatformSocket:
                case LevelPiece.PlateSocket:
                    var socketShape = Box(go, Vector2.one * 0.55f, true);
                    var socket = go.AddComponent<StarSocket>();
                    View(go.transform, "empty_node", Vector2.one * 0.7f, gold);
                    var shadow = Child(go.transform, "ShadowPlatform").gameObject;
                    var support = Box(shadow, new Vector2(2f, 0.18f)); support.offset = new Vector2(0f, 0.3f);
                    var visuals = Child(shadow.transform, "Visual");
                    var starView = View(visuals, "star", Vector2.one * 0.8f, gold);
                    var topView = View(visuals, "stone", new Vector2(2f, 0.18f), gold);
                    topView.transform.localPosition = new Vector3(0f, 0.3f);
                    var weight = shadow.AddComponent<PressureWeight>(); weight.Configure(NewId("shadow"), PressureWeightKind.Shadow);
                    if (kind == LevelPiece.PlateSocket)
                    {
                        var plate = Child(go.transform, "PressurePlate").gameObject;
                        var sensor = Box(plate, new Vector2(1.2f, 0.3f), true);
                        sensor.offset = new Vector2(0f, 0.2f);
                        plate.AddComponent<PressurePlate>().Configure(sensor, View(plate.transform, "dais", new Vector2(1.2f, 0.18f), blue));
                        shadow.GetComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.18f);
                        SetVisualSize(topView, new Vector2(0.9f, 0.18f));
                    }
                    socket.Configure(NewId("socket"), null, shadow); break;
                case LevelPiece.PressurePlate:
                    var area = Box(go, new Vector2(1.5f, 0.35f), true); area.offset = new Vector2(0f, 0.2f);
                    go.AddComponent<PressurePlate>().Configure(area, View(go.transform, "dais", new Vector2(1.5f, 0.18f), blue)); break;
                case LevelPiece.Bridge:
                case LevelPiece.Gate:
                    var size = kind == LevelPiece.Bridge ? new Vector2(4f, 0.2f) : new Vector2(0.5f, 2.5f);
                    var collision = Box(go, size);
                    go.AddComponent<SignalBarrier>().Configure(null, kind == LevelPiece.Bridge, collision,
                        View(go.transform, "stone", size, kind == LevelPiece.Bridge ? gold : blue)); break;
                case LevelPiece.Stone:
                    var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Dynamic;
                    body.mass = 10f; body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    Box(go, Vector2.one); View(go.transform, "stone", Vector2.one, blue);
                    go.AddComponent<PressureWeight>().Configure(NewId("stone"), PressureWeightKind.Stone);
                    go.AddComponent<ResettableBody2D>(); break;
            }
            DeliveredArtAuthoring.ApplyToPiece(go, kind);
            return go;
        }

        public static void EnsurePrefabs()
        {
            Directory.CreateDirectory(PrefabFolder);
            foreach (LevelPiece kind in Enum.GetValues(typeof(LevelPiece)))
            {
                var path = PrefabFolder + kind + ".prefab";
                if (File.Exists(path)) continue;
                var template = Make(kind);
                PrefabUtility.SaveAsPrefabAsset(template, path);
                UnityEngine.Object.DestroyImmediate(template);
            }
            EnsureRuntimeCatalog();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureRuntimeCatalog()
        {
            const string path = "Assets/Game/Data/RuntimeLevelPieceCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<RuntimeLevelPieceCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RuntimeLevelPieceCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = Enum.GetValues(typeof(LevelPiece)).Length;
            foreach (LevelPiece kind in Enum.GetValues(typeof(LevelPiece)))
            {
                var entry = entries.GetArrayElementAtIndex((int)kind);
                entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
                entry.FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + kind + ".prefab");
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        public static GameObject Place(LevelPiece kind, Transform parent, Vector2 position, LevelSession session)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + kind + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Create the M2 kit first.");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place " + kind);
            go.transform.position = position;
            if (go.TryGetComponent<Checkpoint>(out var checkpoint))
            {
                checkpoint.Configure(NewId("dais"), go.transform.Find("SafeSpawn"), go.transform.Find("FallbackSpawn"));
                checkpoint.ConfigurePuzzle(parent.GetComponentInParent<PuzzleGroup>());
            }
            if (go.TryGetComponent<StarFragment>(out var fragment))
                fragment.Configure(NewId("fragment"), session, go.transform.Find("Visual").gameObject);
            if (go.TryGetComponent<StarSocket>(out var socket))
                socket.Configure(NewId("socket"), session.Shadows, go.transform.Find("ShadowPlatform").gameObject);
            foreach (var weight in go.GetComponentsInChildren<PressureWeight>(true)) weight.Configure(NewId("weight"), weight.Kind);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            var metadata = go.GetComponent<LevelPieceMetadata>();
            if (metadata == null) metadata = Undo.AddComponent<LevelPieceMetadata>(go);
            var group = go.GetComponent<Checkpoint>()?.Puzzle ?? go.GetComponentInParent<PuzzleGroup>();
            metadata.Configure((RuntimeLevelPieceKind)Enum.Parse(typeof(RuntimeLevelPieceKind), kind.ToString()), NewId("piece"),
                group != null ? group.StableId : null);
            if (go.TryGetComponent<StarInstrument>(out var instrument))
                instrument.Configure(instrument.Kind, session, metadata.InstanceId);
            if (go.TryGetComponent<BrittleWall>(out var brittle))
                brittle.Configure(session, go.GetComponent<BoxCollider2D>(), go.transform.Find("Visual").gameObject, metadata.InstanceId);
            PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
            return go;
        }
    }
}
