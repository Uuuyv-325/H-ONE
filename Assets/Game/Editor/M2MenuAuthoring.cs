using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace HOne.Game.Editor
{
    // Shared authored uGUI for the M2 entry and its pause overlay. No runtime UI generation.
    public static class M2MenuAuthoring
    {
        private static readonly Color Ink = new Color(0.035f, 0.065f, 0.12f, 0.97f);
        private static readonly Color Gold = new Color(1f, 0.86f, 0.56f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/MenuSans.ttf");

        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        public static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(parent, name, min, max);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = color;
            return rect;
        }
        public static UnityEngine.UI.Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max, int size,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = Rect(parent, name, min, max);
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Font; text.text = value; text.fontSize = size; text.color = Gold; text.alignment = alignment;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        public static UnityEngine.UI.Button Button(Transform parent, string name, string value, Vector2 min, Vector2 max)
        {
            var rect = Panel(parent, name, min, max, new Color(0.12f, 0.20f, 0.29f));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.2f, 1.08f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            Label(rect, "Label", value, Vector2.zero, Vector2.one, 30);
            return button;
        }
        public static Transform Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster)); go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = order;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }
        public static void EventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false); go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        public static void CreateMainMenu(Transform parent)
        {
            var canvas = Canvas(parent, "MainMenuCanvas", 100);
            var backdrop = Rect(canvas, "TitleArtwork", Vector2.zero, Vector2.one).gameObject.AddComponent<UnityEngine.UI.Image>();
            backdrop.sprite = DeliveredArtAuthoring.Art.Title; backdrop.preserveAspect = true; backdrop.raycastTarget = false;
            // Underlay fills the margins at non-16:9 resolutions.
            var underlay = Panel(canvas, "Underlay", Vector2.zero, Vector2.one, Ink); underlay.SetAsFirstSibling();
            var home = Panel(canvas, "Home", new Vector2(0.65f, 0.08f), new Vector2(0.96f, 0.92f), Ink);
            Label(home, "Title", GameFlow.GameTitle, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.94f), 48);
            var play = Button(home, "Start", "进入试场", new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.59f));
            var settings = Button(home, "Settings", "操作与声音", new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.44f));
            var quit = Button(home, "Quit", "退出游戏", new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.29f));
            var controls = Panel(canvas, "Settings", new Vector2(0.59f, 0.08f), new Vector2(0.97f, 0.92f), Ink);
            Label(controls, "Heading", "操作与声音", new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.96f), 40);
            Label(controls, "Controls", "A / D    移动\nSpace    跳跃\n按住 E + 鼠标    蓄力瞄准\n松开 E    发射\n鼠标右键    取消蓄力\nR    召回星影\nEsc    暂停 / 继续",
                new Vector2(0.10f, 0.39f), new Vector2(0.95f, 0.82f), 27, TextAnchor.MiddleLeft);
            AudioSliders(controls, 0.22f, canvas.gameObject);
            var back = Button(controls, "Back", "返回", new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.16f));
            canvas.gameObject.AddComponent<MainMenuController>().Configure(play, settings, quit, back, home.gameObject, controls.gameObject);
            controls.gameObject.SetActive(false);
            EventSystem(parent);
            MainMenuPresentationAuthoring.Apply(canvas);
        }
        private static UnityEngine.UI.Slider Slider(Transform parent, string name, float y)
        {
            Label(parent, name + "Label", name == "Music" ? "音乐" : "音效", new Vector2(0.08f, y), new Vector2(0.3f, y + 0.055f), 26, TextAnchor.MiddleLeft);
            var rect = Rect(parent, name, new Vector2(0.34f, y), new Vector2(0.90f, y + 0.055f));
            var track = Panel(rect, "Track", new Vector2(0f, 0.4f), new Vector2(1f, 0.6f), new Color(0.32f, 0.39f, 0.46f));
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var handle = Panel(rect, "Handle", Vector2.zero, Vector2.one, Gold);
            handle.sizeDelta = new Vector2(24, 0);
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.8f;
            return slider;
        }
        private static void AudioSliders(Transform parent, float y, GameObject owner)
        {
            var music = Slider(parent, "Music", y + 0.09f); var sfx = Slider(parent, "SFX", y);
            owner.AddComponent<MenuAudioSettings>().Configure(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Game/Audio/Mixers/Core.mixer"), music, sfx);
        }
        public static void UpgradePause(PuzzlePauseMenu pause)
        {
            var panel = pause.transform.Find("Panel");
            // Keep the existing capture/progress-art hierarchy and its callbacks.
            var controls = Panel(panel, "MenuControls", new Vector2(0.65f, 0f), Vector2.one, new Color(0.035f, 0.065f, 0.12f, 1f));
            Label(controls, "Heading", "暂停", new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), 44);
            var names = new[] { "Resume", "Reset puzzle", "Restart level" };
            var labels = new[] { "继续游戏", "重置当前谜题", "重新开始试场" };
            for (var i = 0; i < names.Length; i++)
            {
                var existing = panel.Find(names[i]); existing.SetParent(controls, false);
                var rect = (RectTransform)existing;
                rect.anchorMin = new Vector2(0.10f, 0.72f - i * 0.115f);
                rect.anchorMax = new Vector2(0.90f, 0.81f - i * 0.115f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var label = existing.GetComponentInChildren<UnityEngine.UI.Text>();
                label.font = Font; label.text = labels[i]; label.fontSize = 30;
            }
            var menu = Button(controls, "ReturnToMenu", "返回主界面", new Vector2(0.10f, 0.375f), new Vector2(0.90f, 0.465f));
            pause.ConfigureNavigation(menu);
            AudioSliders(controls, 0.20f, pause.gameObject);
            Label(controls, "Hint", "Esc 继续\n返回主界面将结束本次进度", new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.16f), 24);
            EditorUtility.SetDirty(pause);
        }
    }
}
