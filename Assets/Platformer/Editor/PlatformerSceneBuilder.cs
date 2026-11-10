using System.Collections.Generic;
using HOne.Platformer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HOne.Platformer.Editor
{
    public static class PlatformerSceneBuilder
    {
        private const string RootName = "Platformer Level";
        private const string ArtFolder = "Assets/Platformer/Art";
        private const string PhysicsMaterialPath = "Assets/Platformer/Zero Friction.physicsMaterial2D";
        private const string RevealMaterialPath = "Assets/Platformer/MapRevealFog.mat";
        private const float GroundThickness = 0.7f;
        private const float GridSpacing = 1f;
        private const int MajorGridInterval = 5;
        private static readonly Vector2 MapMinimum = Vector2.zero;
        private static readonly Vector2 MapMaximum = new Vector2(54f, 14f);

        [MenuItem("H-ONE/Build Platformer Level")]
        public static void BuildScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                throw new System.InvalidOperationException("Create or open a saved scene before building the level.");

            if (AssetDatabase.IsValidFolder(ArtFolder))
                AssetDatabase.DeleteAsset(ArtFolder);

            var noFriction = EnsurePhysicsMaterial();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == RootName || root.name == "NORTHLINE LEVEL")
                    Object.DestroyImmediate(root);
            }

            var rootObject = new GameObject(RootName);
            var world = CreateGroup("World", rootObject.transform);
            var geometry = CreateGroup("Geometry", rootObject.transform);
            var floorGrid = CreateGroup("Floor Grid", geometry);
            var actors = CreateGroup("Actors", rootObject.transform);

            var boundsObject = new GameObject("Map Bounds");
            boundsObject.transform.SetParent(rootObject.transform, false);
            var bounds = boundsObject.AddComponent<LevelBounds2D>();
            bounds.SetBounds(MapMinimum, MapMaximum);

            BuildWorld(world, geometry, noFriction);
            BuildFloorGrid(floorGrid);
            var player = BuildPlayer(actors, noFriction);
            BuildCamera(rootObject.transform, player.transform, bounds);
            BuildReveal(rootObject.transform, player.transform, bounds, EnsureRevealMaterial());

            var buildScenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(scene.path, true)
            };
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.path != scene.path)
                    buildScenes.Add(buildScene);
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Platformer level built at " + scene.path);
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void BuildWorld(Transform world, Transform geometry, PhysicsMaterial2D material)
        {
            var mapWidth = MapMaximum.x - MapMinimum.x;
            var mapCenterX = (MapMinimum.x + MapMaximum.x) * 0.5f;
            CreateBox("Map Background", new Vector2(27f, 7f), new Vector2(54f, 14f),
                new Color(0.16f, 0.16f, 0.16f), world, null, -10);
            CreateBox("Ground", new Vector2(mapCenterX, GroundThickness * 0.5f), new Vector2(mapWidth, GroundThickness),
                new Color(0.38f, 0.38f, 0.38f), geometry, material, 0);
            CreateBox("West Boundary", new Vector2(-0.45f, 7f), new Vector2(0.9f, 14f),
                new Color(0.38f, 0.38f, 0.38f), geometry, material, 0);
            CreateBox("East Boundary", new Vector2(MapMaximum.x + 0.45f, 7f), new Vector2(0.9f, 14f),
                new Color(0.38f, 0.38f, 0.38f), geometry, material, 0);
        }

        private static void BuildFloorGrid(Transform parent)
        {
            var mapWidth = MapMaximum.x - MapMinimum.x;
            var mapCenterX = (MapMinimum.x + MapMaximum.x) * 0.5f;
            var gridHeight = GroundThickness - 0.04f;
            var lineIndex = 0;

            for (var x = MapMinimum.x + GridSpacing; x < MapMaximum.x; x += GridSpacing)
            {
                lineIndex++;
                var isMajorLine = lineIndex % MajorGridInterval == 0;
                var lineWidth = isMajorLine ? 0.05f : 0.025f;
                var lineColor = isMajorLine
                    ? new Color(0.16f, 0.16f, 0.16f)
                    : new Color(0.25f, 0.25f, 0.25f);

                CreateBox("Vertical " + lineIndex.ToString("00"),
                    new Vector2(x, GroundThickness * 0.5f), new Vector2(lineWidth, gridHeight),
                    lineColor, parent, null, 2);
            }

            CreateBox("Horizontal Center", new Vector2(mapCenterX, GroundThickness * 0.5f),
                new Vector2(mapWidth, 0.02f), new Color(0.25f, 0.25f, 0.25f), parent, null, 2);
            CreateBox("Top Edge", new Vector2(mapCenterX, GroundThickness - 0.015f),
                new Vector2(mapWidth, 0.03f), new Color(0.52f, 0.52f, 0.52f), parent, null, 2);
        }

        private static GameObject BuildPlayer(Transform parent, PhysicsMaterial2D material)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(2.5f, 1.45f, 0f);
            player.tag = "Player";

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sharedMaterial = material;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.72f, 1.05f);
            collider.sharedMaterial = material;
            player.AddComponent<PlayerMovement2D>();

            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            player.transform.localScale = new Vector3(0.68f, 1f, 1f);
            player.AddComponent<SolidShape2D>();
            player.AddComponent<DistanceFootstepEmitter2D>();
            player.AddComponent<AudioSource>();
            player.AddComponent<FootstepSound2D>();
            return player;
        }

        private static void BuildReveal(Transform parent, Transform player, LevelBounds2D bounds, Material material)
        {
            var revealObject = new GameObject("Map Reveal");
            revealObject.transform.SetParent(parent, false);
            var reveal = revealObject.AddComponent<MapReveal2D>();
            reveal.Configure(bounds, player, player.GetComponent<DistanceFootstepEmitter2D>(), material);
        }

        [MenuItem("H-ONE/Install Map Reveal in Current Level")]
        public static void InstallRevealInCurrentLevel()
        {
            var scene = SceneManager.GetActiveScene();
            var root = GameObject.Find(RootName);
            var bounds = root != null ? root.GetComponentInChildren<LevelBounds2D>() : null;
            var player = root != null ? root.transform.Find("Actors/Player") : null;
            var cameraTransform = root != null ? root.transform.Find("Main Camera") : null;
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || bounds == null || player == null)
                throw new System.InvalidOperationException("Open a saved platformer level first.");

            var steps = player.GetComponent<DistanceFootstepEmitter2D>();
            if (steps == null)
                steps = player.gameObject.AddComponent<DistanceFootstepEmitter2D>();
            if (player.GetComponent<FootstepSound2D>() == null)
                player.gameObject.AddComponent<FootstepSound2D>();
            if (cameraTransform != null && cameraTransform.GetComponent<AudioListener>() == null)
                cameraTransform.gameObject.AddComponent<AudioListener>();

            var revealTransform = root.transform.Find("Map Reveal");
            if (revealTransform == null)
            {
                BuildReveal(root.transform, player, bounds, EnsureRevealMaterial());
            }
            else
            {
                var reveal = revealTransform.GetComponent<MapReveal2D>();
                if (reveal == null)
                    reveal = revealTransform.gameObject.AddComponent<MapReveal2D>();
                reveal.Configure(bounds, player, steps, EnsureRevealMaterial());
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static Material EnsureRevealMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RevealMaterialPath);
            if (material != null)
                return material;

            var shader = Shader.Find("HOne/Map Reveal Fog");
            if (shader == null)
                throw new System.InvalidOperationException("Map reveal shader has not imported.");
            material = new Material(shader) { name = "Map Reveal Fog" };
            AssetDatabase.CreateAsset(material, RevealMaterialPath);
            return material;
        }

        private static void BuildCamera(Transform parent, Transform player, LevelBounds2D bounds)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);

            var follow = cameraObject.AddComponent<CameraFollow2D>();
            follow.Configure(player, bounds);
            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * 16f / 9f;
            var focus = player.position + new Vector3(halfWidth * 0.42f, halfHeight * 0.5f, 0f);
            cameraObject.transform.position = bounds.ClampCameraCenter(focus, halfWidth, halfHeight);
            cameraObject.transform.position = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y, -10f);
        }

        private static GameObject CreateBox(string name, Vector2 position, Vector2 size, Color color,
            Transform parent, PhysicsMaterial2D material, int sortingOrder)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            gameObject.AddComponent<SolidShape2D>();

            if (material != null)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.sharedMaterial = material;
            }
            return gameObject;
        }

        private static PhysicsMaterial2D EnsurePhysicsMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("Zero Friction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            }
            return material;
        }
    }
}
