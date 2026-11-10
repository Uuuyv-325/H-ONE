using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HOne.Game.Editor
{
    public sealed class ChapterLevelEditorWindow : EditorWindow
    {
        private LevelSession session;
        private ObjectField parentField, sourceField, barrierField, groupField;
        private EnumField pieceField;
        private Vector2Field positionField, sizeField;
        private FloatField gridField;
        private Toggle paint;
        private Toggle grassBrush;
        private FloatField brushRadius;
        private VisualElement results, inspector;
        [MenuItem("H-ONE/Chapter Level Editor")]
        public static void Open() => GetWindow<ChapterLevelEditorWindow>("H-ONE Level Editor");
        private void OnEnable() { SceneView.duringSceneGui += SceneGUI; Selection.selectionChanged += RefreshInspector; }
        private void OnDisable() { SceneView.duringSceneGui -= SceneGUI; Selection.selectionChanged -= RefreshInspector; }
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Game/Editor/LevelEditing/ChapterLevelEditorWindow.uxml").CloneTree(rootVisualElement);
            var scenes = rootVisualElement.Q("sceneTools"); Title(scenes, "1 · Scene");
            AddButton(scenes, "Create new level…", () =>
            {
                var path = EditorUtility.SaveFilePanelInProject("New chapter level", "Chapter01_New", "unity", "Create a new level without replacing an existing scene.", "Assets/Game/Scenes");
                if (string.IsNullOrEmpty(path)) return;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                M2SceneAuthoring.CreateLevel(path, false);
                EditorSceneManager.OpenScene(path);
            });
            AddButton(scenes, "Create missing piece prefabs", LevelPieceFactory.EnsurePrefabs);
            AddButton(scenes, "Save active scene", () => { if (!Application.isPlaying) EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); });
            var palette = rootVisualElement.Q("palette"); Title(palette, "2 · Place / edit");
            parentField = Field<Transform>(palette, "Parent (optional)");
            pieceField = new EnumField("Piece", LevelPiece.PlatformSocket); palette.Add(pieceField);
            gridField = new FloatField("Grid (1 or 0.25)") { value = 0.25f }; palette.Add(gridField);
            positionField = new Vector2Field("World position"); palette.Add(positionField);
            paint = new Toggle("Place with left click in Scene view"); palette.Add(paint);
            grassBrush = new Toggle("Round grass brush (fills 1-unit grid cells)"); palette.Add(grassBrush);
            brushRadius = new FloatField("Brush radius") { value = 2f }; palette.Add(brushRadius);
            var brushPreview = new UnityEngine.UIElements.Image { sprite = DeliveredArtAuthoring.Art != null ? DeliveredArtAuthoring.Art.TerrainBrush : null };
            brushPreview.AddToClassList("terrain-brush-preview"); palette.Add(brushPreview);
            palette.Add(new Label("Alt navigates; Esc stops placing. Selection and transforms use normal Unity tools."));
            AddButton(palette, "Place piece", () => Place(positionField.value));
            sizeField = new Vector2Field("Box / Visual size") { value = new Vector2(4f, 0.5f) }; palette.Add(sizeField);
            AddButton(palette, "Resize selected solid / bridge / gate", ResizeSelected);
            AddButton(palette, "Duplicate selected with fresh IDs", DuplicateSelected);
            var connections = rootVisualElement.Q("connections"); Title(connections, "3 · Connect / reset group");
            sourceField = Field<BoolSignal>(connections, "Signal source");
            barrierField = Field<SignalBarrier>(connections, "Bridge / gate");
            AddButton(connections, "Connect source → bridge / gate", () =>
            {
                if (sourceField.value == null || barrierField.value == null) throw new InvalidOperationException("Assign both ends.");
                SetReference(barrierField.value, "source", sourceField.value);
            });
            groupField = Field<PuzzleGroup>(connections, "Puzzle group");
            AddButton(connections, "Create puzzle group", CreateGroup);
            AddButton(connections, "Assign selected dais to group", () =>
            {
                var checkpoint = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Checkpoint>() : null;
                if (checkpoint == null || groupField.value == null) throw new InvalidOperationException("Select a dais and choose a group.");
                SetReference(checkpoint, "puzzle", groupField.value);
            });
            AddButton(connections, "Gather group's reset targets", GatherGroup);
            connections.Add(new Label("Place group-owned pieces beneath its Transform. Reset restores bodies, recomputes plates, then bridges/gates. Collected IDs are preserved."));
            var validation = rootVisualElement.Q("validation"); Title(validation, "4 · Validate");
            AddButton(validation, "Validate active level", ValidateScene);
            AddButton(validation, "Sync fragment declarations from scene", () =>
            {
                var level = GetSession();
                Undo.RecordObject(level.Definition, "Sync fragment declarations");
                level.Definition.FragmentIds = level.GetComponentsInChildren<StarFragment>(true).Select(f => f.StableId).ToArray();
                EditorUtility.SetDirty(level.Definition);
            });
            results = new VisualElement(); validation.Add(results);
            inspector = rootVisualElement.Q("selectionInspector"); RefreshInspector();
        }
        private static void Title(VisualElement parent, string text)
        { var label = new Label(text); label.AddToClassList("section-title"); parent.Add(label); }
        private static void AddButton(VisualElement parent, string label, Action action) => parent.Add(new Button(() =>
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("H-ONE", "Stop Play Mode before authoring.", "OK"); return; }
            try { action(); } catch (Exception exception) { EditorUtility.DisplayDialog("H-ONE", exception.Message, "OK"); }
        }) { text = label });
        private static ObjectField Field<T>(VisualElement parent, string label) where T : UnityEngine.Object
        { var field = new ObjectField(label) { objectType = typeof(T), allowSceneObjects = true }; parent.Add(field); return field; }
        private LevelSession GetSession()
        {
            var scene = SceneManager.GetActiveScene();
            var found = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelSession>(true)).ToArray();
            if (found.Length != 1 || found[0].Shadows == null) throw new InvalidOperationException("Active scene needs one configured M2 LevelSession. Use Create new level.");
            return session = found[0];
        }
        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            if (target is Component a && value is Component b && a.gameObject.scene != b.gameObject.scene)
                throw new InvalidOperationException("Connect objects in the same scene.");
            var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
        private void Place(Vector2 position)
        {
            var level = GetSession();
            var parent = parentField.value as Transform;
            if (grassBrush.value)
            {
                parent = parent != null ? parent : level.transform.Find("Geometry");
                if (parent.gameObject.scene != level.gameObject.scene) throw new InvalidOperationException("Parent must belong to the active level.");
                Undo.IncrementCurrentGroup(); var brushUndo = Undo.GetCurrentGroup();
                RemainingArtAuthoring.PaintGrassDisk(parent,new Vector2(Mathf.Round(position.x),Mathf.Round(position.y)),Mathf.Clamp(brushRadius.value,.5f,16f),level);
                Undo.CollapseUndoOperations(brushUndo); EditorSceneManager.MarkSceneDirty(level.gameObject.scene); return;
            }
            var kind = (LevelPiece)pieceField.value;
            if (parent == null && groupField.value is PuzzleGroup group && kind != LevelPiece.Solid && kind != LevelPiece.Hazard &&
                kind != LevelPiece.Fragment && kind != LevelPiece.Checkpoint) parent = group.transform;
            if (parent == null) parent = level.transform.Find(kind == LevelPiece.Solid || kind == LevelPiece.Hazard ? "Geometry" :
                kind == LevelPiece.Fragment ? "Collectibles" : kind == LevelPiece.Checkpoint ? "Checkpoints" : "Mechanisms");
            if (parent.gameObject.scene != level.gameObject.scene) throw new InvalidOperationException("Parent must belong to the active level.");
            Undo.IncrementCurrentGroup(); var undoGroup = Undo.GetCurrentGroup();
            var go = LevelPieceFactory.Place(kind, parent, Snap(position), level);
            DeclareFragments(go, level);
            Selection.activeGameObject = go; Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        private Vector2 Snap(Vector2 point)
        { var grid = Mathf.Max(0.01f, gridField.value); return new Vector2(Mathf.Round(point.x / grid) * grid, Mathf.Round(point.y / grid) * grid); }
        private void SceneGUI(SceneView view)
        {
            DrawConnections();
            if (paint == null || !paint.value || Application.isPlaying) return;
            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape) { paint.value = false; current.Use(); return; }
            if (current.alt) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            if (!new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out var distance)) return;
            var point = Snap(ray.GetPoint(distance));
            Handles.color = Color.yellow;
            if (grassBrush.value) Handles.DrawWireDisc(point,Vector3.forward,Mathf.Clamp(brushRadius.value,.5f,16f));
            else Handles.DrawWireCube(point, new Vector3(1f, 1f, 0f));
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                try { Place(point); } catch (Exception exception) { paint.value = false; Debug.LogWarning(exception.Message); }
                current.Use();
            }
            view.Repaint();
        }
        private static void DrawConnections()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var barrier in root.GetComponentsInChildren<SignalBarrier>(true))
                    if (barrier.Source != null)
                    {
                        Handles.color = Color.cyan;
                        Handles.DrawDottedLine(barrier.Source.transform.position, barrier.transform.position, 5f);
                        Handles.Label((barrier.Source.transform.position + barrier.transform.position) * 0.5f, "signal");
                    }
        }
        private void CreateGroup()
        {
            var level = GetSession(); var go = new GameObject("Puzzle_" + LevelPieceFactory.NewId("group"));
            go.transform.SetParent(level.transform.Find("Mechanisms"), false); Undo.RegisterCreatedObjectUndo(go, "Create puzzle group");
            var group = go.AddComponent<PuzzleGroup>(); group.Configure(LevelPieceFactory.NewId("puzzle"));
            var serialized = new SerializedObject(level); var puzzles = serialized.FindProperty("puzzles");
            puzzles.InsertArrayElementAtIndex(puzzles.arraySize); puzzles.GetArrayElementAtIndex(puzzles.arraySize - 1).objectReferenceValue = group;
            serialized.ApplyModifiedProperties(); groupField.value = group; parentField.value = go.transform; Selection.activeGameObject = go;
        }
        private void GatherGroup()
        {
            if (!(groupField.value is PuzzleGroup group)) throw new InvalidOperationException("Choose a puzzle group.");
            var targets = group.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m is IPuzzleResettable && m.GetComponentInParent<PuzzleGroup>() == group).ToArray();
            var serialized = new SerializedObject(group); var array = serialized.FindProperty("resetTargets"); array.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            serialized.ApplyModifiedProperties();
        }
        private void ResizeSelected()
        {
            var go = Selection.activeGameObject;
            if (go == null || go.GetComponent<BoxCollider2D>() == null || go.GetComponent<StarSocket>() != null || go.GetComponent<PressurePlate>() != null)
                throw new InvalidOperationException("Select a box solid, bridge, gate or stone.");
            if (sizeField.value.x <= 0f || sizeField.value.y <= 0f) throw new InvalidOperationException("Size must be positive.");
            Undo.RecordObjects(new UnityEngine.Object[] { go.GetComponent<BoxCollider2D>(), go.GetComponentInChildren<SpriteRenderer>(), go.GetComponentInChildren<SpriteRenderer>().transform }, "Resize level piece");
            M2SceneAuthoring.Resize(go, sizeField.value); EditorSceneManager.MarkSceneDirty(go.scene);
        }
        private void DuplicateSelected()
        {
            var original = Selection.activeGameObject;
            if (original == null || original.GetComponent<LevelSession>() != null) throw new InvalidOperationException("Select a piece or puzzle group.");
            var level = GetSession(); var copy = Instantiate(original, original.transform.parent);
            Undo.RegisterCreatedObjectUndo(copy, "Duplicate level piece"); copy.transform.position += Vector3.right * Mathf.Max(0.01f, gridField.value);
            foreach (var component in copy.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(component); var id = serialized.FindProperty("stableId");
                if (id == null) continue;
                id.stringValue = LevelPieceFactory.NewId(component.GetType().Name.ToLowerInvariant()); serialized.ApplyModifiedProperties();
            }
            foreach (var group in copy.GetComponentsInChildren<PuzzleGroup>(true))
            {
                var serialized = new SerializedObject(level); var groups = serialized.FindProperty("puzzles");
                groups.InsertArrayElementAtIndex(groups.arraySize);
                groups.GetArrayElementAtIndex(groups.arraySize - 1).objectReferenceValue = group;
                serialized.ApplyModifiedProperties();
            }
            foreach (var metadata in copy.GetComponentsInChildren<LevelPieceMetadata>(true))
            {
                var group = metadata.GetComponent<Checkpoint>()?.Puzzle ?? metadata.GetComponentInParent<PuzzleGroup>();
                metadata.Configure(metadata.Kind, LevelPieceFactory.NewId("piece"), group != null ? group.StableId : null);
                EditorUtility.SetDirty(metadata);
            }
            // Capture after all IDs changed so copied internal links point at the copied source.
            foreach (var metadata in copy.GetComponentsInChildren<LevelPieceMetadata>(true))
                metadata.Capture();
            DeclareFragments(copy, level); Selection.activeGameObject = copy;
            EditorSceneManager.MarkSceneDirty(copy.scene);
        }
        private static void DeclareFragments(GameObject go, LevelSession level)
        {
            var ids = go.GetComponentsInChildren<StarFragment>(true).Select(f => f.StableId).ToArray();
            if (ids.Length == 0) return;
            Undo.RecordObject(level.Definition, "Declare fragment IDs");
            level.Definition.FragmentIds = level.Definition.FragmentIds.Concat(ids).Distinct().ToArray(); EditorUtility.SetDirty(level.Definition);
        }
        private void RefreshInspector()
        {
            if (inspector == null) return;
            inspector.Clear(); Title(inspector, "Selection · serialized properties / Undo");
            var selected = Selection.activeGameObject;
            if (selected == null) return;
            foreach (var component in selected.GetComponents<Component>())
                if (component != null) inspector.Add(new InspectorElement(component));
        }
        private void ValidateScene()
        {
            results.Clear(); var errors = ChapterLevelValidation.Validate();
            results.Add(new Label(errors.Count == 0 ? "Ready: no validation errors." : errors.Count + " issue(s)"));
            foreach (var error in errors)
            {
                var issue = error;
                var button = new Button(() => { if (issue.Target != null) { Selection.activeObject = issue.Target; EditorGUIUtility.PingObject(issue.Target); } }) { text = issue.Message };
                results.Add(button);
            }
        }
    }
}
