using UnityEditor;
using UnityEngine;

namespace HOne.Game.Editor
{
    // Targeted, repeatable presentation edits. Never rebuilds gameplay roots or scenes.
    public static class DeliveredArtAuthoring
    {
        public const string ArtPath = "Assets/Game/Art/Delivery01/ChapterArt.asset";
        public static DeliveredArtSet Art => AssetDatabase.LoadAssetAtPath<DeliveredArtSet>(ArtPath);

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            // Unity's missing-component wrappers require its overloaded null check.
            if (component == null) component = go.AddComponent<T>();
            return component;
        }
        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private static void Set(SpriteRenderer view, Sprite sprite, Vector3 position, float scale)
        {
            view.sprite = sprite; view.color = Color.white; view.transform.localPosition = position;
            view.transform.localScale = Vector3.one * scale;
        }

        public static void ApplyPlayer(GameObject go)
        {
            var art = Art;
            var view = go.GetComponent<PlayerView>();
            if (art == null || view == null) return;
            view.ConfigureArt(art); EditorUtility.SetDirty(view);
            var visual = go.transform.Find("Visual");
            Set(visual.Find("Human").GetComponent<SpriteRenderer>(), art.Idle[0], Vector3.down * 0.675f, 1f);
            Set(visual.Find("Star").GetComponent<SpriteRenderer>(), art.Star[0], Vector3.zero, 0.55f);
        }

        public static void ApplyToPiece(GameObject go, LevelPiece kind)
        {
            var art = Art;
            if (art == null) return;
            var visual = go.transform.Find("Visual");
            switch (kind)
            {
                case LevelPiece.Lyre:
                case LevelPiece.Drum:
                    var instrument = go.GetComponent<StarInstrument>();
                    var sprite = kind == LevelPiece.Lyre ? art.Lyre : art.DrumIdle;
                    if (sprite == null) break;
                    var instrumentDrawing = visual.GetComponent<SpriteRenderer>();
                    Set(instrumentDrawing,sprite,Vector3.zero,kind == LevelPiece.Lyre ? 1.2f : .9f);
                    LineRenderer wave = null;
                    if (kind == LevelPiece.Drum)
                    {
                        wave = Ensure<LineRenderer>(Child(go.transform,"DrumWave").gameObject);
                        wave.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/P0/Trajectory.mat");
                        wave.useWorldSpace = false; wave.loop = true; wave.positionCount = 48;
                        wave.startWidth = wave.endWidth = .035f; wave.sortingOrder = 15; wave.enabled = false;
                    }
                    Ensure<InstrumentArtView>(go).Configure(instrument,instrumentDrawing,art,wave);
                    break;
                case LevelPiece.Solid:
                    if (art.TerrainMiddle != null && art.TerrainMaterial != null) Ensure<GrassTerrainView>(go).Configure(art);
                    break;
                case LevelPiece.PlatformSocket:
                case LevelPiece.PlateSocket:
                    if (visual == null || art.RockDark == null || art.RockLight == null) break;
                    var socket = go.GetComponent<StarSocket>();
                    var socketView = visual.GetComponent<SpriteRenderer>();
                    // Keep the receiver inside its old 0.7-unit envelope and above the shadow core.
                    Set(socketView, art.RockDark, Vector3.zero,
                        0.7f / Mathf.Max(art.RockDark.bounds.size.x, art.RockDark.bounds.size.y));
                    var shadowVisuals = socket.Shadow.transform.Find("Visual");
                    var core = shadowVisuals.GetChild(0).GetComponent<SpriteRenderer>();
                    socketView.sortingOrder = core.sortingOrder + 1;
                    var support = shadowVisuals.GetChild(1).GetComponent<SpriteRenderer>();
                    support.sortingOrder = socketView.sortingOrder + 1;
                    Ensure<StarSocketArtView>(go).Configure(socket, socketView, art);
                    break;
                case LevelPiece.Fragment:
                    if (visual == null) break;
                    var fragment = visual.GetComponent<SpriteRenderer>();
                    Set(fragment, art.Fragment[0], Vector3.zero, 0.8f);
                    Ensure<SpriteSequenceView>(visual.gameObject).Configure(fragment, art.Fragment);
                    break;
                case LevelPiece.Checkpoint:
                    if (visual == null) break;
                    var checkpoint = visual.GetComponent<SpriteRenderer>();
                    Set(checkpoint, art.Checkpoint, Vector3.up * (art.Checkpoint.bounds.size.y * 0.5f), 1f);
                    checkpoint.sortingOrder = -2;
                    break;
                case LevelPiece.Stone:
                    if (visual == null) break;
                    var stone = visual.GetComponent<SpriteRenderer>();
                    Set(stone, art.RockDark, Vector3.zero, 1f / art.RockDark.bounds.size.x);
                    break;
            }
            foreach (var plate in go.GetComponentsInChildren<PressurePlate>(true))
            {
                var target = plate.transform.Find("Visual").GetComponent<SpriteRenderer>();
                var sensor = plate.GetComponent<BoxCollider2D>();
                Set(target, art.PlateUp, Vector3.zero, sensor.size.x / art.PlateUp.bounds.size.x);
                Ensure<PressurePlateArtView>(plate.gameObject).Configure(plate, target, art);
            }
        }

        public static void ApplyToScene(GameObject root)
        {
            var art = Art;
            if (art == null) return;
            var player = root.GetComponentInChildren<PlayerStateMachine>(true);
            if (player != null) ApplyPlayer(player.gameObject);
            foreach (var fragment in root.GetComponentsInChildren<StarFragment>(true)) ApplyToPiece(fragment.gameObject, LevelPiece.Fragment);
            foreach (var checkpoint in root.GetComponentsInChildren<Checkpoint>(true)) ApplyToPiece(checkpoint.gameObject, LevelPiece.Checkpoint);
            foreach (var socket in root.GetComponentsInChildren<StarSocket>(true))
                ApplyToPiece(socket.gameObject, socket.GetComponentInChildren<PressurePlate>(true) != null
                    ? LevelPiece.PlateSocket : LevelPiece.PlatformSocket);
            foreach (var plate in root.GetComponentsInChildren<PressurePlate>(true)) ApplyToPiece(plate.gameObject, LevelPiece.PressurePlate);
            foreach (var stone in root.GetComponentsInChildren<PressureWeight>(true))
                if (stone.Kind == PressureWeightKind.Stone) ApplyToPiece(stone.gameObject, LevelPiece.Stone);
            RemainingArtAuthoring.ApplyScene(root);
            var camera = root.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                var background = Child(root.transform, "Background");
                foreach (var old in background.GetComponentsInChildren<SpriteRenderer>(true))
                    if (old.name == "BackgroundColumn") old.enabled = false;
                var back = Ensure<SpriteRenderer>(Child(background, "DeliveryBackground").gameObject);
                back.sprite = art.Background; back.color = Color.white; back.sortingOrder = -100;
                var front = Ensure<SpriteRenderer>(Child(background, "DeliveryForeground").gameObject);
                front.sprite = art.ForegroundGrass; front.color = Color.white; front.sortingOrder = 40;
                Ensure<CameraBackdrop>(background.gameObject).Configure(camera, back, front);
            }
            var pause = root.GetComponentInChildren<PuzzlePauseMenu>(true);
            if (pause != null) ApplyPause(pause, root.GetComponent<LevelSession>(), art);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        private static RectTransform UI(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void ApplyPause(PuzzlePauseMenu pause, LevelSession session, DeliveredArtSet art)
        {
            var panel = (RectTransform)pause.transform.Find("Panel");
            if (panel == null) return;
            pause.GetComponent<Canvas>().sortingOrder = 2000;
            Stretch(panel);
            panel.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            var scaler = pause.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            var backdrop = Ensure<UnityEngine.UI.RawImage>(UI(panel, "BlurredGame").gameObject);
            Stretch((RectTransform)backdrop.transform); backdrop.raycastTarget = false;
            backdrop.color = new Color(0.65f, 0.69f, 0.8f, 1f);
            backdrop.transform.SetAsFirstSibling();
            var frame = UI(panel, "ArtworkFrame"); Stretch(frame);
            var fit = Ensure<UnityEngine.UI.AspectRatioFitter>(frame.gameObject);
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
            var image = Ensure<UnityEngine.UI.Image>(UI(frame, "Illustration").gameObject);
            Stretch((RectTransform)image.transform); image.sprite = art.PauseProgress[0];
            image.color = Color.white; image.raycastTarget = false; image.preserveAspect = true;
            frame.SetSiblingIndex(1);
            var buttons = new[] { "Resume", "Reset puzzle", "Restart level" };
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = panel.Find(buttons[i]);
                if (button == null) continue;
                var rect = (RectTransform)button;
                rect.anchorMin = rect.anchorMax = new Vector2(0.84f, 0.5f); rect.pivot = Vector2.one * 0.5f;
                rect.sizeDelta = new Vector2(300f, 76f); rect.anchoredPosition = new Vector2(0f, 110f - i * 110f);
                button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.13f, 0.24f, 0.96f);
                var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label != null) { label.fontSize = 27; label.color = new Color(1f, 0.87f, 0.62f); }
                button.SetAsLastSibling();
            }
            var artwork = Ensure<PauseArtwork>(pause.gameObject);
            artwork.Configure(session, art, panel.gameObject, backdrop, image);
            pause.ConfigureArt(artwork); panel.gameObject.SetActive(false);
        }
    }
}
