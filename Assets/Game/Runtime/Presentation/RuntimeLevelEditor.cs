using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HOne.Game
{
    public sealed class RuntimeLevelEditor : MonoBehaviour
    {
        private enum Mode { Playing, Editing }
        private enum Tool { Select, Place, Link }
        [SerializeField] private RuntimeLevelPieceCatalog catalog;
        [SerializeField] private LevelSession session;
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform levelRoot;

        private Canvas canvas;
        private GameObject editorPanel;
        private Button gameEditButton;
        private Button setEntryButton;
        private Text statusText, selectionText;
        private Text gameEditButtonText;
        private InputField nameInput, widthInput, heightInput;
        private Dropdown savedLevels, groups;
        private readonly Dictionary<RuntimeLevelPieceKind, Button> palette = new Dictionary<RuntimeLevelPieceKind, Button>();
        private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
        private RuntimeLevelDocument current;
        private InputActionMap editActions;
        private InputAction pointer, click, delete, cancel, scroll, pan;
        private Mode mode = Mode.Playing;
        private Tool tool = Tool.Select;
        private RuntimeLevelPieceKind placementKind;
        private LevelPieceMetadata selected;
        private LevelPieceMetadata pendingBarrier;
        private Vector2 dragOffset;
        private Vector2 previousPointer;
        private float grid = 0.25f;
        private string groupId;
        private bool dragging, panning, isPlaytesting;
        private float savedCameraSize;

        public bool IsEditing => mode == Mode.Editing;
        public string CurrentLevelName => current != null ? current.levelId : "untitled";

        public void Configure(RuntimeLevelPieceCatalog pieceCatalog, LevelSession level, PlayerStateMachine owner,
            Camera camera, Transform root)
        { catalog = pieceCatalog; session = level; player = owner; worldCamera = camera; levelRoot = root; }

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
#else
            if (catalog == null || session == null || player == null || worldCamera == null || levelRoot == null)
            {
                Debug.LogError("RuntimeLevelEditor needs catalog, session, player, camera and level root.", this);
                enabled = false;
                return;
            }
            editActions = new InputActionMap("RuntimeLevelEditor");
            pointer = editActions.AddAction("Pointer", InputActionType.PassThrough, "<Pointer>/position");
            click = editActions.AddAction("Click", InputActionType.Button, "<Pointer>/press");
            delete = editActions.AddAction("Delete", InputActionType.Button, "<Keyboard>/delete");
            cancel = editActions.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            scroll = editActions.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll");
            pan = editActions.AddAction("Pan", InputActionType.Button, "<Mouse>/middleButton");
            BuildInterface();
            current = CaptureDocument();
            nameInput.text = current.levelId;
            RefreshFileList();
#endif
        }

        private void OnDestroy()
        {
            editActions?.Disable();
            editActions?.Dispose();
            if (canvas != null) Destroy(canvas.gameObject);
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("RuntimeLevelEditorCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Keep the editor toolbar above the gallery's development navigation (order 50).
            canvas.sortingOrder = 60;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();

            gameEditButton = MakeButton(canvas.transform, "Edit level", new Vector2(0.81f, 0.91f), new Vector2(0.98f, 0.98f));
            gameEditButtonText = gameEditButton.GetComponentInChildren<Text>();
            gameEditButton.onClick.AddListener(HandleGameEditButton);
            editorPanel = Panel(canvas.transform, "EditorPanel", new Vector2(0f, 0f), Vector2.one,
                new Vector2(0f, 0f), Vector2.zero, new Color(0.02f, 0.035f, 0.07f, 0.22f));
            editorPanel.GetComponent<Image>().raycastTarget = false;
            editorPanel.SetActive(false);
            var header = Panel(editorPanel.transform, "Toolbar", new Vector2(0f, 0.9f), Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.07f, 0.1f, 0.17f, 0.98f));
            MakeLabel(header.transform, "IN-GAME LEVEL EDITOR", new Vector2(0.01f, 0.12f), new Vector2(0.16f, 0.88f), 17, TextAnchor.MiddleLeft);
            nameInput = MakeInput(header.transform, "level name", new Vector2(0.17f, 0.18f), new Vector2(0.29f, 0.82f));
            nameInput.text = current != null ? current.levelId : "untitled";
            MakeButton(header.transform, "Save", new Vector2(0.30f, 0.18f), new Vector2(0.36f, 0.82f)).onClick.AddListener(SaveCurrent);
            savedLevels = MakeDropdown(header.transform, new Vector2(0.37f, 0.18f), new Vector2(0.49f, 0.82f));
            MakeButton(header.transform, "Load", new Vector2(0.50f, 0.18f), new Vector2(0.56f, 0.82f)).onClick.AddListener(LoadSelected);
            MakeButton(header.transform, "New", new Vector2(0.57f, 0.18f), new Vector2(0.62f, 0.82f)).onClick.AddListener(NewLevel);
            MakeButton(header.transform, "Link signal", new Vector2(0.63f, 0.18f), new Vector2(0.69f, 0.82f), 11).onClick.AddListener(StartLink);
            MakeButton(header.transform, "Play test", new Vector2(0.70f, 0.18f), new Vector2(0.82f, 0.82f)).onClick.AddListener(PlayTest);
            MakeButton(header.transform, "Return", new Vector2(0.87f, 0.18f), new Vector2(0.98f, 0.82f)).onClick.AddListener(ReturnToGame);

            var side = Panel(editorPanel.transform, "Tools", new Vector2(0.01f, 0.04f), new Vector2(0.225f, 0.88f),
                Vector2.zero, Vector2.zero, new Color(0.055f, 0.075f, 0.12f, 0.96f));
            MakeLabel(side.transform, "TOOLS", new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), 14, TextAnchor.MiddleLeft);
            MakeButton(side.transform, "Select / move", new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.91f)).onClick.AddListener(() => SetTool(Tool.Select));
            MakeLabel(side.transform, "GRID", new Vector2(0.04f, 0.74f), new Vector2(0.23f, 0.82f), 12, TextAnchor.MiddleLeft);
            var gridDropdown = MakeDropdown(side.transform, new Vector2(0.25f, 0.74f), new Vector2(0.96f, 0.82f));
            gridDropdown.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("0.25 unit"), new Dropdown.OptionData("1 unit") };
            gridDropdown.value = 0; gridDropdown.onValueChanged.AddListener(value => grid = value == 0 ? 0.25f : 1f);
            MakeLabel(side.transform, "PUZZLE GROUP", new Vector2(0.04f, 0.65f), new Vector2(0.95f, 0.73f), 12, TextAnchor.MiddleLeft);
            groups = MakeDropdown(side.transform, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.65f));
            groups.onValueChanged.AddListener(ChangeGroup);
            MakeButton(side.transform, "+ Puzzle group", new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.57f)).onClick.AddListener(AddGroup);
            MakeLabel(side.transform, "PLACE PIECE", new Vector2(0.04f, 0.42f), new Vector2(0.95f, 0.49f), 12, TextAnchor.MiddleLeft);
            var paletteRoot = Panel(side.transform, "Palette", new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.42f),
                Vector2.zero, Vector2.zero, Color.clear);
            var gridLayout = paletteRoot.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gridLayout.constraintCount = 2;
            gridLayout.cellSize = new Vector2(112f, 30f); gridLayout.spacing = new Vector2(6f, 4f);
            gridLayout.padding = new RectOffset(2, 2, 2, 2); gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            foreach (RuntimeLevelPieceKind kind in Enum.GetValues(typeof(RuntimeLevelPieceKind)))
            {
                var button = MakeButton(paletteRoot.transform, PieceLabel(kind), Vector2.zero, Vector2.one, 12);
                button.onClick.AddListener(() => ChoosePiece(kind)); palette.Add(kind, button);
            }
            selectionText = MakeLabel(side.transform, "No selection", new Vector2(0.04f, 0.045f), new Vector2(0.96f, 0.11f), 12, TextAnchor.MiddleLeft);

            var footer = Panel(editorPanel.transform, "SelectionTools", new Vector2(0.24f, 0.04f), new Vector2(0.58f, 0.13f),
                Vector2.zero, Vector2.zero, new Color(0.055f, 0.075f, 0.12f, 0.96f));
            MakeLabel(footer.transform, "SIZE", new Vector2(0.02f, 0.12f), new Vector2(0.11f, 0.88f), 12, TextAnchor.MiddleLeft);
            widthInput = MakeInput(footer.transform, "width", new Vector2(0.12f, 0.14f), new Vector2(0.24f, 0.86f));
            heightInput = MakeInput(footer.transform, "height", new Vector2(0.25f, 0.14f), new Vector2(0.37f, 0.86f));
            MakeButton(footer.transform, "Apply size", new Vector2(0.39f, 0.14f), new Vector2(0.59f, 0.86f)).onClick.AddListener(ResizeSelection);
            setEntryButton = MakeButton(footer.transform, "Set entry", new Vector2(0.61f, 0.14f), new Vector2(0.80f, 0.86f));
            setEntryButton.onClick.AddListener(SetSelectedEntry);
            MakeButton(footer.transform, "Delete", new Vector2(0.82f, 0.14f), new Vector2(0.98f, 0.86f)).onClick.AddListener(DeleteSelection);
            statusText = MakeLabel(editorPanel.transform, "Click a piece to select it; drag to move.",
                new Vector2(0.60f, 0.045f), new Vector2(0.98f, 0.13f), 14, TextAnchor.MiddleLeft);
        }

        private void HandleGameEditButton()
        {
            if (isPlaytesting) StopPlaytest();
            else EnterEditor();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventObject.transform.SetParent(transform, false);
                eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            else if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void EnterEditor()
        {
            if (mode == Mode.Editing) return;
            savedCameraSize = worldCamera.orthographicSize;
            mode = Mode.Editing;
            player.SetPaused(true);
            player.Input.enabled = false;
            current = CaptureDocument();
            RefreshGroupResetTargets();
            nameInput.text = current.levelId;
            editorPanel.SetActive(true); gameEditButton.gameObject.SetActive(false);
            RefreshGroupList(); RefreshSelection(null); SetTool(Tool.Select);
            editActions.Enable();
        }

        private void ReturnToGame()
        {
            if (mode != Mode.Editing) return;
            current = CaptureDocument();
            session.SetAllowedFragmentIds(current.pieces
                .Where(piece => piece.kind == RuntimeLevelPieceKind.Fragment).Select(piece => piece.fragmentId));
            mode = Mode.Playing; tool = Tool.Select; pendingBarrier = null; dragging = false;
            editActions.Disable(); RestoreSelectionColor(); selected = null;
            editorPanel.SetActive(false); gameEditButton.gameObject.SetActive(true);
            isPlaytesting = false;
            gameEditButtonText.text = "Edit level";
            player.Input.enabled = true;
            worldCamera.orthographicSize = savedCameraSize;
            player.SetPaused(false);
        }

        private void PlayTest()
        {
            if (!TryCaptureAndValidate(out var document)) return;
            try
            {
                document.levelId = RuntimeLevelDocumentStore.SafeName(nameInput.text);
                current = document;
                RuntimeLevelDocumentStore.Save(current, current.levelId);
                nameInput.text = current.levelId;
                RefreshFileList();
                statusText.text = "Saved. Play test starts from the entry dais.";
                session.SetAllowedFragmentIds(document.pieces
                    .Where(piece => piece.kind == RuntimeLevelPieceKind.Fragment).Select(piece => piece.fragmentId));
                RefreshGroupResetTargets();
                isPlaytesting = true;
                gameEditButtonText.text = "Stop test";
                mode = Mode.Playing; tool = Tool.Select; pendingBarrier = null;
                editActions.Disable(); RestoreSelectionColor(); selected = null;
                editorPanel.SetActive(false); gameEditButton.gameObject.SetActive(true);
                if (!session.RestartLevel(player))
                {
                    mode = Mode.Editing; player.SetPaused(true); player.Input.enabled = false;
                    isPlaytesting = false; gameEditButtonText.text = "Edit level";
                    editorPanel.SetActive(true); gameEditButton.gameObject.SetActive(false); editActions.Enable();
                    statusText.text = "The entry dais has no clear respawn position.";
                    return;
                }
                player.Input.enabled = true;
                worldCamera.orthographicSize = savedCameraSize;
                player.SetPaused(false);
            }
            catch (Exception exception) { statusText.text = exception.Message; }
        }

        private void StopPlaytest()
        {
            if (!isPlaytesting || current == null) return;
            player.SetPaused(true);
            player.Input.enabled = false;
            mode = Mode.Editing;
            isPlaytesting = false;
            gameEditButtonText.text = "Edit level";
            gameEditButton.gameObject.SetActive(false);
            editorPanel.SetActive(true);
            try
            {
                ApplyDocument(RuntimeLevelDocumentStore.Deserialize(RuntimeLevelDocumentStore.Serialize(current)));
                nameInput.text = current.levelId;
                statusText.text = session.RestartLevel(player)
                    ? "Playtest stopped. The saved layout is restored at its entry dais."
                    : "Layout restored. The entry dais has no clear respawn position.";
                editActions.Enable();
            }
            catch (Exception exception)
            {
                statusText.text = "Could not restore the saved layout: " + exception.Message;
            }
        }

        private void Update()
        {
            if (mode != Mode.Editing || !editActions.enabled) return;
            var screen = pointer.ReadValue<Vector2>();
            if (cancel.WasPressedThisFrame())
            {
                pendingBarrier = null; SetTool(Tool.Select);
            }
            if (pan.WasPressedThisFrame() && !PointerOverUI(screen))
            { panning = true; previousPointer = screen; }
            if (pan.WasReleasedThisFrame()) panning = false;
            if (panning && pan.IsPressed() && !PointerOverUI(screen)) PanCamera(screen);
            var scrollAmount = scroll.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollAmount) > 0.01f && !PointerOverUI(screen))
                worldCamera.orthographicSize = Mathf.Clamp(worldCamera.orthographicSize - scrollAmount * 0.01f, 2f, 12f);
            if (delete.WasPressedThisFrame() && !PointerOverUI(screen)) DeleteSelection();
            if (click.WasPressedThisFrame())
            {
                if (!PointerOverUI(screen)) WorldClicked(screen);
            }
            else if (click.IsPressed() && dragging && selected != null && !PointerOverUI(screen))
                MoveSelected(screen);
            else if (click.WasReleasedThisFrame()) dragging = false;
        }

        private bool PointerOverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void PanCamera(Vector2 screen)
        {
            var depth = -worldCamera.transform.position.z;
            var previous = worldCamera.ScreenToWorldPoint(new Vector3(previousPointer.x, previousPointer.y, depth));
            var currentPosition = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            worldCamera.transform.position += previous - currentPosition;
            previousPointer = screen;
        }

        private Vector2 WorldPoint(Vector2 screen)
        {
            var point = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return Snap(point);
        }

        private Vector2 Snap(Vector2 point) => new Vector2(Mathf.Round(point.x / grid) * grid, Mathf.Round(point.y / grid) * grid);

        private void WorldClicked(Vector2 screen)
        {
            var point = WorldPoint(screen);
            var hit = FindPiece(point);
            if (tool == Tool.Place)
            {
                PlacePieceAt(placementKind, point);
                return;
            }
            if (tool == Tool.Link)
            {
                ConnectSignal(hit);
                return;
            }
            Select(hit);
            if (selected != null)
            {
                dragging = true;
                dragOffset = (Vector2)selected.transform.position - point;
            }
        }

        private LevelPieceMetadata FindPiece(Vector2 point)
        {
            var hits = Physics2D.OverlapPointAll(point);
            LevelPieceMetadata nearest = null;
            var bestOrder = int.MinValue;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                var metadata = hit.GetComponentInParent<LevelPieceMetadata>();
                if (metadata == null) continue;
                var renderer = metadata.GetComponentInChildren<SpriteRenderer>();
                var order = renderer != null ? renderer.sortingOrder : 0;
                if (order >= bestOrder) { nearest = metadata; bestOrder = order; }
            }
            return nearest;
        }

        private void MoveSelected(Vector2 screen)
        {
            var position = Snap(WorldPoint(screen) + dragOffset);
            var body = selected.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
            else selected.transform.position = position;
            if (selected.TryGetComponent<ResettableBody2D>(out var resettable)) resettable.CaptureInitialPose();
            Physics2D.SyncTransforms();
        }

        private void Select(LevelPieceMetadata metadata)
        {
            if (selected == metadata) return;
            RestoreSelectionColor(); selected = metadata;
            if (setEntryButton != null) setEntryButton.interactable = selected != null && selected.GetComponent<Checkpoint>() != null;
            if (selected != null)
            {
                foreach (var view in selected.GetComponentsInChildren<SpriteRenderer>(true))
                { originalColors[view] = view.color; view.color = Color.white; }
                if (selected.TryGetComponent<BoxCollider2D>(out var box))
                { widthInput.text = box.size.x.ToString("0.##", CultureInfo.InvariantCulture); heightInput.text = box.size.y.ToString("0.##", CultureInfo.InvariantCulture); }
            }
            RefreshSelection(selected);
        }

        private void RestoreSelectionColor()
        {
            foreach (var pair in originalColors) if (pair.Key != null) pair.Key.color = pair.Value;
            originalColors.Clear();
        }

        private void RefreshSelection(LevelPieceMetadata metadata)
        {
            if (selectionText != null) selectionText.text = metadata != null ? metadata.gameObject.name : "No selection";
        }

        private void ChoosePiece(RuntimeLevelPieceKind kind)
        {
            placementKind = kind; pendingBarrier = null; SetTool(Tool.Place);
            statusText.text = "Click in the level to place " + PieceLabel(kind) + ".";
        }

        private void SetTool(Tool value)
        {
            tool = value;
            foreach (var button in palette.Values) button.interactable = value != Tool.Link;
            if (statusText != null && value == Tool.Select) statusText.text = "Click a piece to select it; drag to move.";
        }

        public LevelPieceMetadata PlacePieceAt(RuntimeLevelPieceKind kind, Vector2 position)
        {
            var prefab = catalog != null ? catalog.Get(kind) : null;
            if (prefab == null) throw new InvalidOperationException("The runtime piece catalog is missing " + kind + ".");
            var puzzle = FindGroup(groupId) ?? EnsureDefaultGroup();
            var instance = Instantiate(prefab, puzzle.transform);
            var id = Guid.NewGuid().ToString("N");
            var record = NewRecord(kind, id, puzzle.StableId, Snap(position));
            var metadata = instance.GetComponent<LevelPieceMetadata>();
            if (metadata == null) metadata = instance.AddComponent<LevelPieceMetadata>();
            metadata.Configure(kind, id, puzzle.StableId);
            metadata.Apply(record, session, puzzle, null);
            RefreshGroupResetTargets();
            Select(metadata); Physics2D.SyncTransforms();
            return metadata;
        }

        private RuntimeLevelPieceRecord NewRecord(RuntimeLevelPieceKind kind, string id, string puzzleId, Vector2 position)
        {
            var prefab = catalog.Get(kind);
            var box = prefab.GetComponent<BoxCollider2D>();
            return new RuntimeLevelPieceRecord
            {
                id = id, name = kind.ToString(), kind = kind, position = position,
                size = box != null ? box.size : Vector2.one,
                groupId = puzzleId,
                barrierSolidWhenTrue = kind != RuntimeLevelPieceKind.Gate,
                fragmentId = kind == RuntimeLevelPieceKind.Fragment ? Guid.NewGuid().ToString("N") : null,
                checkpointId = kind == RuntimeLevelPieceKind.Checkpoint ? Guid.NewGuid().ToString("N") : null,
                socketId = kind == RuntimeLevelPieceKind.PlatformSocket || kind == RuntimeLevelPieceKind.PlateSocket ? Guid.NewGuid().ToString("N") : null,
                weightId = kind == RuntimeLevelPieceKind.Stone ? Guid.NewGuid().ToString("N") : null,
                shadowWeightId = kind == RuntimeLevelPieceKind.PlatformSocket || kind == RuntimeLevelPieceKind.PlateSocket ? Guid.NewGuid().ToString("N") : null
            };
        }

        private void ConnectSignal(LevelPieceMetadata sourcePiece)
        {
            if (pendingBarrier == null) { statusText.text = "Select a bridge or gate, then click Connect signal."; SetTool(Tool.Select); return; }
            var source = sourcePiece != null ? sourcePiece.GetComponentInChildren<BoolSignal>(true) : null;
            if (source == null) { statusText.text = "Choose a pressure plate as the signal source."; return; }
            var barrier = pendingBarrier.GetComponent<SignalBarrier>();
            if (barrier == null) { pendingBarrier = null; SetTool(Tool.Select); return; }
            barrier.Configure(source, barrier.SolidWhenTrue, pendingBarrier.GetComponent<BoxCollider2D>(), pendingBarrier.GetComponentInChildren<SpriteRenderer>());
            pendingBarrier.SetSource(sourcePiece.InstanceId);
            statusText.text = "Connected " + sourcePiece.name + " to " + pendingBarrier.name + ".";
            pendingBarrier = null; SetTool(Tool.Select);
        }

        private void StartLink()
        {
            if (selected == null || selected.GetComponent<SignalBarrier>() == null)
            { statusText.text = "Select a bridge or gate first."; return; }
            pendingBarrier = selected; SetTool(Tool.Link);
            statusText.text = "Click a pressure plate to connect it to " + selected.name + ".";
        }

        private void SetSelectedEntry()
        {
            var checkpoint = selected != null ? selected.GetComponent<Checkpoint>() : null;
            if (checkpoint == null) return;
            session.SetEntryCheckpoint(checkpoint);
            current.entryCheckpointId = checkpoint.StableId;
            statusText.text = checkpoint.name + " is the entry dais.";
        }

        private RuntimeLevelDocument CaptureDocument()
        {
            var document = new RuntimeLevelDocument { levelId = current != null ? current.levelId : session.LevelId };
            var metadata = levelRoot.GetComponentsInChildren<LevelPieceMetadata>(true);
            foreach (var item in metadata) if (item.gameObject.activeSelf) document.pieces.Add(item.Capture());
            foreach (var group in levelRoot.GetComponentsInChildren<PuzzleGroup>(true))
                if (group.gameObject.activeSelf) document.groups.Add(new RuntimeLevelGroupRecord { id = group.StableId, name = group.name });
            document.entryCheckpointId = session.EntryCheckpoint != null ? session.EntryCheckpoint.StableId : null;
            return document;
        }

        private bool TryCaptureAndValidate(out RuntimeLevelDocument document)
        {
            document = CaptureDocument();
            document.levelId = RuntimeLevelDocumentStore.SafeName(nameInput.text);
            var error = GetDocumentValidationError(document);
            if (error != null) { statusText.text = error; return false; }
            return true;
        }

        private string GetDocumentValidationError(RuntimeLevelDocument document)
        {
            if (document == null || document.version != RuntimeLevelDocument.CurrentVersion)
                return "Unsupported level file version.";
            if (catalog == null || session.Definition == null || session.Shadows == null)
                return "The level needs a definition and shadow service.";
            if (document.groups == null || document.pieces == null)
                return "The level file is missing its groups or pieces list.";
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in document.groups)
                if (group == null || string.IsNullOrWhiteSpace(group.id) || !groupIds.Add(group.id))
                    return "Puzzle groups need unique IDs.";
            var pieceIds = new HashSet<string>(StringComparer.Ordinal);
            var fragmentIds = new HashSet<string>(StringComparer.Ordinal);
            var checkpointIds = new HashSet<string>(StringComparer.Ordinal);
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            var weightIds = new HashSet<string>(StringComparer.Ordinal);
            var shadowWeightIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in document.pieces)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.id) || !pieceIds.Add(record.id))
                    return "Each level piece needs a unique ID.";
                if (!string.IsNullOrEmpty(record.groupId) && !groupIds.Contains(record.groupId))
                    return "A level piece references a missing puzzle group.";
                if (catalog.Get(record.kind) == null)
                    return "The runtime piece catalog is missing " + record.kind + ".";
                if (record.kind == RuntimeLevelPieceKind.Fragment &&
                    (string.IsNullOrWhiteSpace(record.fragmentId) || !fragmentIds.Add(record.fragmentId)))
                    return "Fragment IDs must be unique.";
                if (record.kind == RuntimeLevelPieceKind.Checkpoint &&
                    (string.IsNullOrWhiteSpace(record.checkpointId) || !checkpointIds.Add(record.checkpointId)))
                    return "Checkpoint IDs must be unique.";
                if ((record.kind == RuntimeLevelPieceKind.PlatformSocket || record.kind == RuntimeLevelPieceKind.PlateSocket) &&
                    (string.IsNullOrWhiteSpace(record.socketId) || !socketIds.Add(record.socketId)))
                    return "Socket IDs must be unique.";
                if (record.kind == RuntimeLevelPieceKind.Stone &&
                    (string.IsNullOrWhiteSpace(record.weightId) || !weightIds.Add(record.weightId)))
                    return "Stone weight IDs must be unique.";
                if ((record.kind == RuntimeLevelPieceKind.PlatformSocket || record.kind == RuntimeLevelPieceKind.PlateSocket) &&
                    (string.IsNullOrWhiteSpace(record.shadowWeightId) || !shadowWeightIds.Add(record.shadowWeightId)))
                    return "Shadow weight IDs must be unique.";
            }
            if (!document.pieces.Any(p => p.kind == RuntimeLevelPieceKind.Checkpoint && p.checkpointId == document.entryCheckpointId))
                return "Add at least one checkpoint and select it as the entry dais.";
            foreach(var record in document.pieces)
                foreach(var target in record.waveTargetIds ?? Array.Empty<string>())
                    if(record.kind != RuntimeLevelPieceKind.Drum || !document.pieces.Any(p=>p.id==target && p.kind==RuntimeLevelPieceKind.BrittleWall))
                        return "A drum wave must reference existing brittle walls.";
            foreach (var barrier in document.pieces.Where(p => p.kind == RuntimeLevelPieceKind.Bridge || p.kind == RuntimeLevelPieceKind.Gate))
            {
                if (string.IsNullOrEmpty(barrier.sourcePieceId)) continue;
                var source = document.pieces.FirstOrDefault(p => p.id == barrier.sourcePieceId);
                if (source == null || (source.kind != RuntimeLevelPieceKind.PressurePlate && source.kind != RuntimeLevelPieceKind.PlateSocket && source.kind != RuntimeLevelPieceKind.Lyre))
                    return "A bridge or gate must connect to a pressure plate or lyre.";
            }
            return null;
        }

        private void SaveCurrent()
        {
            if (!TryCaptureAndValidate(out current)) return;
            current.levelId = RuntimeLevelDocumentStore.SafeName(nameInput.text);
            try
            {
                var path = RuntimeLevelDocumentStore.Save(current, current.levelId);
                nameInput.text = current.levelId; RefreshFileList();
                statusText.text = "Saved " + Path.GetFileName(path) + ".";
            }
            catch (Exception exception) { statusText.text = exception.Message; }
        }

        private void LoadSelected()
        {
            var options = RuntimeLevelDocumentStore.List();
            if (options.Length == 0) { statusText.text = "No saved JSON levels yet."; return; }
            var name = savedLevels.options.Count > 0 ? savedLevels.options[savedLevels.value].text : nameInput.text;
            try
            {
                var document = RuntimeLevelDocumentStore.Load(name);
                ApplyDocument(document); nameInput.text = document.levelId;
                statusText.text = "Loaded " + document.levelId + " (" + document.pieces.Count + " pieces).";
            }
            catch (Exception exception) { statusText.text = exception.Message; }
        }

        private void RefreshFileList()
        {
            if (savedLevels == null) return;
            var names = RuntimeLevelDocumentStore.List();
            savedLevels.ClearOptions();
            savedLevels.AddOptions(names.Length > 0 ? names.ToList() : new List<string> { "No saved levels" });
        }

        public void ApplyDocument(RuntimeLevelDocument document)
        {
            document.groups ??= new List<RuntimeLevelGroupRecord>();
            document.pieces ??= new List<RuntimeLevelPieceRecord>();
            var error = GetDocumentValidationError(document);
            if (error != null) throw new InvalidDataException(error);
            ClearPiecesAndGroups();
            var groupMap = new Dictionary<string, PuzzleGroup>(StringComparer.Ordinal);
            foreach (var item in document.groups)
            {
                if (string.IsNullOrWhiteSpace(item.id) || groupMap.ContainsKey(item.id)) continue;
                var groupObject = new GameObject(string.IsNullOrWhiteSpace(item.name) ? "Puzzle" : item.name);
                groupObject.transform.SetParent(levelRoot.Find("Mechanisms") != null ? levelRoot.Find("Mechanisms") : levelRoot, false);
                var group = groupObject.AddComponent<PuzzleGroup>(); group.Configure(item.id);
                groupMap.Add(item.id, group);
            }
            var elements = new Dictionary<string, LevelPieceMetadata>(StringComparer.Ordinal);
            foreach (var record in document.pieces)
            {
                var prefab = catalog != null ? catalog.Get(record.kind) : null;
                if (prefab == null) throw new InvalidDataException("Missing piece prefab: " + record.kind);
                var group = FindGroupInMap(groupMap, record.groupId);
                var parent = group != null ? group.transform : levelRoot.Find("Geometry") ?? levelRoot;
                var instance = Instantiate(prefab, parent);
                var metadata = instance.GetComponent<LevelPieceMetadata>();
                if (metadata == null) metadata = instance.AddComponent<LevelPieceMetadata>();
                metadata.Configure(record.kind, record.id, record.groupId);
                metadata.Apply(record, session, group, null);
                if (!elements.ContainsKey(record.id)) elements.Add(record.id, metadata);
            }
            foreach (var record in document.pieces)
            {
                if (elements.TryGetValue(record.id, out var piece) && piece.TryGetComponent<StarInstrument>(out var instrument))
                    instrument.Configure(instrument.Kind,session,record.id,(record.waveTargetIds ?? Array.Empty<string>())
                        .Where(elements.ContainsKey).Select(id => elements[id].GetComponent<BrittleWall>()).Where(w => w != null).ToArray());
                if (record.kind != RuntimeLevelPieceKind.Bridge && record.kind != RuntimeLevelPieceKind.Gate) continue;
                if (!elements.TryGetValue(record.id, out var metadata) || !elements.TryGetValue(record.sourcePieceId ?? string.Empty, out var sourcePiece)) continue;
                var source = sourcePiece.GetComponentInChildren<BoolSignal>(true);
                var barrier = metadata.GetComponent<SignalBarrier>();
                if (source != null && barrier != null)
                    barrier.Configure(source, record.barrierSolidWhenTrue, metadata.GetComponent<BoxCollider2D>(), metadata.GetComponentInChildren<SpriteRenderer>());
            }
            foreach (var group in groupMap.Values)
            {
                var targets = group.GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(component => component is IPuzzleResettable && component.GetComponentInParent<PuzzleGroup>() == group).ToArray();
                group.Configure(group.StableId, targets);
            }
            var checkpointMeta = document.pieces.FirstOrDefault(p => p.kind == RuntimeLevelPieceKind.Checkpoint && p.checkpointId == document.entryCheckpointId)
                ?? document.pieces.First(p => p.kind == RuntimeLevelPieceKind.Checkpoint);
            var entry = elements[checkpointMeta.id].GetComponent<Checkpoint>();
            session.ReplaceLevelContent(document.levelId,
                document.pieces.Where(p => p.kind == RuntimeLevelPieceKind.Fragment).Select(p => p.fragmentId),
                entry, groupMap.Values.ToArray());
            current = document; groupId = entry.Puzzle != null ? entry.Puzzle.StableId : groupMap.Keys.FirstOrDefault();
            GetComponent<ShadowFeedback>()?.RefreshSockets();
            RefreshGroupList(); Select(null); Physics2D.SyncTransforms();
        }

        private static PuzzleGroup FindGroupInMap(Dictionary<string, PuzzleGroup> map, string id) =>
            !string.IsNullOrEmpty(id) && map.TryGetValue(id, out var group) ? group : null;

        private void ClearPiecesAndGroups()
        {
            var pieces = levelRoot.GetComponentsInChildren<LevelPieceMetadata>(true).Select(item => item.gameObject).Distinct().ToArray();
            var groupsToRemove = levelRoot.GetComponentsInChildren<PuzzleGroup>(true).Select(item => item.gameObject).Distinct().ToArray();
            foreach (var piece in pieces) if (piece != null) piece.SetActive(false);
            foreach (var group in groupsToRemove) if (group != null) group.SetActive(false);
            foreach (var piece in pieces) if (piece != null) Destroy(piece);
            foreach (var group in groupsToRemove) if (group != null) Destroy(group);
            session.Shadows.Clear(); Physics2D.SyncTransforms();
        }

        private void NewLevel()
        {
            var groupKey = Guid.NewGuid().ToString("N");
            var document = new RuntimeLevelDocument { levelId = "untitled", groups = new List<RuntimeLevelGroupRecord>
                { new RuntimeLevelGroupRecord { id = groupKey, name = "Puzzle A" } } };
            var floor = NewRecord(RuntimeLevelPieceKind.Solid, Guid.NewGuid().ToString("N"), groupKey, new Vector2(1f, -0.5f));
            floor.name = "Ground"; floor.size = new Vector2(20f, 1f);
            var entry = NewRecord(RuntimeLevelPieceKind.Checkpoint, Guid.NewGuid().ToString("N"), groupKey, new Vector2(-8f, 0f));
            entry.name = "Entry Dais"; entry.checkpointId = Guid.NewGuid().ToString("N");
            document.pieces.Add(floor); document.pieces.Add(entry); document.entryCheckpointId = entry.checkpointId;
            ApplyDocument(document); nameInput.text = document.levelId;
            statusText.text = "New level ready. Add pieces from the palette, then save.";
        }

        private void AddGroup()
        {
            var record = new RuntimeLevelGroupRecord { id = Guid.NewGuid().ToString("N"), name = "Puzzle " + (current.groups.Count + 1) };
            current.groups.Add(record);
            var gameObject = new GameObject(record.name); gameObject.transform.SetParent(levelRoot.Find("Mechanisms") ?? levelRoot, false);
            var group = gameObject.AddComponent<PuzzleGroup>(); group.Configure(record.id);
            groupId = record.id; RefreshGroupList();
            RefreshGroupResetTargets();
        }

        private void ChangeGroup(int index)
        {
            if (groups == null || index < 0 || index >= groups.options.Count) return;
            groupId = groups.options[index].text;
            var group = FindGroup(groupId);
            if (group == null || selected == null) return;
            selected.transform.SetParent(group.transform, true);
            selected.SetGroup(group.StableId);
            if (selected.TryGetComponent<Checkpoint>(out var checkpoint)) checkpoint.ConfigurePuzzle(group);
            RefreshGroupResetTargets();
        }

        private void RefreshGroupList()
        {
            if (groups == null) return;
            var list = levelRoot.GetComponentsInChildren<PuzzleGroup>(true);
            groups.ClearOptions(); groups.AddOptions(list.Length > 0 ? list.Select(item => item.StableId).ToList() : new List<string> { "No group" });
            if (list.Length > 0)
            {
                if (string.IsNullOrEmpty(groupId) || !list.Any(item => item.StableId == groupId)) groupId = list[0].StableId;
                groups.value = Mathf.Max(0, Array.FindIndex(list, item => item.StableId == groupId));
            }
        }

        private PuzzleGroup FindGroup(string id) => levelRoot.GetComponentsInChildren<PuzzleGroup>(true).FirstOrDefault(group => group.StableId == id);
        private PuzzleGroup EnsureDefaultGroup()
        {
            var found = FindGroup(groupId);
            if (found != null) return found;
            var gameObject = new GameObject("Puzzle A"); gameObject.transform.SetParent(levelRoot.Find("Mechanisms") ?? levelRoot, false);
            var group = gameObject.AddComponent<PuzzleGroup>(); group.Configure(string.IsNullOrEmpty(groupId) ? Guid.NewGuid().ToString("N") : groupId);
            groupId = group.StableId; RefreshGroupList(); return group;
        }

        private void RefreshGroupResetTargets()
        {
            var activeGroups = levelRoot.GetComponentsInChildren<PuzzleGroup>(true)
                .Where(group => group.gameObject.activeInHierarchy).ToArray();
            foreach (var group in activeGroups)
            {
                var targets = group.GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(component => component is IPuzzleResettable && component.GetComponentInParent<PuzzleGroup>() == group)
                    .ToArray();
                group.Configure(group.StableId, targets);
            }
            session.ConfigureMechanisms(session.Shadows, activeGroups);
            GetComponent<ShadowFeedback>()?.RefreshSockets();
        }

        private void ResizeSelection()
        {
            if (selected == null || selected.GetComponent<StarSocket>() != null || selected.GetComponent<PressurePlate>() != null ||
                !selected.TryGetComponent<BoxCollider2D>(out var box) ||
                !float.TryParse(widthInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !float.TryParse(heightInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var height) || width <= 0f || height <= 0f)
            { statusText.text = "Select a box piece and enter positive width and height."; return; }
            box.size = new Vector2(width, height);
            if (selected.TryGetComponent<GrassTerrainView>(out var terrain)) terrain.Rebuild();
            var view = selected.GetComponentInChildren<SpriteRenderer>();
            if (view != null && view.sprite != null && selected.GetComponent<StarInstrument>() == null)
            {
                var native = view.sprite.bounds.size;
                if (native.x > 0f && native.y > 0f) view.transform.localScale = new Vector3(width / native.x, height / native.y, 1f);
            }
            Physics2D.SyncTransforms(); statusText.text = "Piece size updated.";
        }

        private void DeleteSelection()
        {
            if (selected == null) return;
            var wasEntry = selected.GetComponent<Checkpoint>() == session.EntryCheckpoint;
            var checkpoint = levelRoot.GetComponentsInChildren<Checkpoint>(true).FirstOrDefault(item => item != session.EntryCheckpoint);
            if (wasEntry && checkpoint == null) { statusText.text = "Keep at least one checkpoint in the level."; return; }
            if (wasEntry) session.SetEntryCheckpoint(checkpoint);
            var old = selected.gameObject; Select(null); old.SetActive(false); Destroy(old); Physics2D.SyncTransforms();
            RefreshGroupResetTargets();
            RefreshGroupList(); statusText.text = "Piece deleted.";
        }

        private static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = background; return go;
        }

        private static Button MakeButton(Transform parent, string title, Vector2 anchorMin, Vector2 anchorMax, int fontSize = 14)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.color = new Color(0.17f, 0.27f, 0.39f, 1f);
            var button = go.GetComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(0.27f, 0.4f, 0.55f); colors.pressedColor = new Color(0.74f, 0.58f, 0.28f); button.colors = colors;
            AddText(go.transform, title, fontSize, Color.white); return button;
        }

        private static Text MakeLabel(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor align)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.color = Color.white; text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void AddText(Transform parent, string value, int size, Color color)
        {
            var text = MakeLabel(parent, value, Vector2.zero, Vector2.one, size, TextAnchor.MiddleCenter);
            text.transform.SetAsLastSibling();
        }

        private static InputField MakeInput(Transform parent, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Input_" + placeholder, typeof(RectTransform), typeof(Image), typeof(InputField)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.17f, 1f);
            var value = MakeLabel(go.transform, string.Empty, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 14, TextAnchor.MiddleLeft);
            var hint = MakeLabel(go.transform, placeholder, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 13, TextAnchor.MiddleLeft);
            hint.color = new Color(0.65f, 0.7f, 0.77f, 1f);
            var field = go.GetComponent<InputField>(); field.textComponent = value; field.placeholder = hint;
            return field;
        }

        private static Dropdown MakeDropdown(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.11f, 0.16f, 0.23f, 1f);
            var caption = MakeLabel(go.transform, "Select", new Vector2(0.05f, 0.05f), new Vector2(0.93f, 0.95f), 13, TextAnchor.MiddleLeft);
            var template = Panel(go.transform, "Template", new Vector2(0f, -4f), new Vector2(1f, 0f), new Vector2(0f, -120f), Vector2.zero, new Color(0.08f, 0.12f, 0.18f, 1f));
            template.SetActive(false);
            var item = Panel(template.transform, "Item", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.18f, 1f));
            var toggle = item.AddComponent<Toggle>(); toggle.targetGraphic = item.GetComponent<Image>();
            var itemText = MakeLabel(item.transform, "Option", new Vector2(0.08f, 0f), Vector2.one, 13, TextAnchor.MiddleLeft);
            var dropdown = go.GetComponent<Dropdown>(); dropdown.captionText = caption; dropdown.template = template.GetComponent<RectTransform>();
            dropdown.itemText = itemText; dropdown.options.Add(new Dropdown.OptionData("Select"));
            return dropdown;
        }

        private static string PieceLabel(RuntimeLevelPieceKind kind) => kind switch
        {
            RuntimeLevelPieceKind.PlatformSocket => "Platform socket",
            RuntimeLevelPieceKind.PlateSocket => "Plate socket",
            RuntimeLevelPieceKind.PressurePlate => "Pressure plate",
            _ => kind.ToString()
        };
    }
}
