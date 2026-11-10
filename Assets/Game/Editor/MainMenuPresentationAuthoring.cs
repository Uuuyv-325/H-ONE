using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace HOne.Game.Editor
{
    // Add the credits and music to the authored menu; keep its existing pages intact.
    public static class MainMenuPresentationAuthoring
    {
        public const string MusicPath = "Assets/Game/Audio/Music/larse_before_departure.mp3";
        public static void Apply(Transform canvas)
        {
            ApplyHomeLayout(canvas);
            if (canvas.GetComponent<MainMenuCredits>() != null) return;
            var home = canvas.Find("Home").gameObject;
            var settings = canvas.Find("Settings").gameObject;
            var groups = new[] { home.AddComponent<CanvasGroup>(), settings.AddComponent<CanvasGroup>() };
            var open = M2MenuAuthoring.Button(canvas, "Credits", "Credits", Vector2.zero, Vector2.one);
            var corner = (RectTransform)open.transform;
            corner.anchorMin = corner.anchorMax = new Vector2(1f, 0f); corner.pivot = new Vector2(1f, 0f);
            corner.sizeDelta = new Vector2(152f, 48f); corner.anchoredPosition = new Vector2(-32f, 28f);
            open.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 22;
            FeedbackMenuStyle.ApplyTextButton(open);

            var overlay = M2MenuAuthoring.Rect(canvas, "CreditsPanel", Vector2.zero, Vector2.one);
            var background = M2MenuAuthoring.Rect(overlay, "BlurredMenu", Vector2.zero, Vector2.one)
                .gameObject.AddComponent<UnityEngine.UI.RawImage>();
            background.raycastTarget = false;
            M2MenuAuthoring.Panel(overlay, "Shade", Vector2.zero, Vector2.one, new Color(.02f, .04f, .08f, .78f));
            var content = M2MenuAuthoring.Rect(overlay, "List", new Vector2(.16f, .04f), new Vector2(.84f, .96f));
            M2MenuAuthoring.Label(content, "Heading", "制作名单", new Vector2(0f, .87f), new Vector2(1f, .98f), 46);
            M2MenuAuthoring.Label(content, "Music", "音乐\n《启程之前》 · Larse", new Vector2(0f, .70f), new Vector2(1f, .85f), 31);
            var artist = Link(content, "MusicAuthor", "Bilibili 主页", new Vector2(.20f, .635f), new Vector2(.49f, .69f));
            var track = Link(content, "MusicTrack", "作品 · BV1QhA5efEq9", new Vector2(.51f, .635f), new Vector2(.80f, .69f));
            M2MenuAuthoring.Label(content, "Sound", "音效 · 阿锰", new Vector2(0f, .53f), new Vector2(1f, .60f), 32);
            M2MenuAuthoring.Label(content, "Art", "美术 · 不眠巷", new Vector2(0f, .44f), new Vector2(1f, .51f), 32);
            var programmer = Link(content, "Programmer", "程序 · OriSnow420 · GitHub", new Vector2(.16f, .35f), new Vector2(.84f, .42f));
            programmer.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 32;
            M2MenuAuthoring.Label(content, "Design", "策划 · 游里", new Vector2(0f, .26f), new Vector2(1f, .33f), 32);
            var project = Link(content, "Project", "项目 GitHub · Uuuyv-325/H-ONE", new Vector2(.12f, .15f), new Vector2(.88f, .22f));
            var close = M2MenuAuthoring.Button(content, "Back", "返回", new Vector2(.39f, .035f), new Vector2(.61f, .10f));
            var module = canvas.root.GetComponentInChildren<InputSystemUIInputModule>(true);
            canvas.gameObject.AddComponent<MainMenuCredits>().Configure(open, close, artist, track, programmer, project,
                overlay.gameObject, background, groups, DeliveredArtAuthoring.Art.PauseBlur, module.cancel);
            overlay.gameObject.SetActive(false);

            var music = new GameObject("MenuMusic", typeof(AudioSource), typeof(MenuMusic));
            music.transform.SetParent(canvas.parent, false);
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Game/Audio/Mixers/Core.mixer");
            music.GetComponent<MenuMusic>().Configure(music.GetComponent<AudioSource>(),
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath), mixer.FindMatchingGroups("Music")[0]);
        }
        public static void ApplyHomeLayout(Transform canvas)
        {
            var home = (RectTransform)canvas.Find("Home");
            home.anchorMin = new Vector2(.79f, .08f);
            home.anchorMax = new Vector2(.99f, .92f);
            home.offsetMin = home.offsetMax = Vector2.zero;
            var background = home.GetComponent<UnityEngine.UI.Image>();
            if (background != null) { background.color = Color.clear; background.raycastTarget = false; }
            foreach (var button in home.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                FeedbackMenuStyle.ApplyTextButton(button);
            var title = home.Find("Title").GetComponent<UnityEngine.UI.Text>();
            title.text = GameFlow.GameTitle;
            title.fontSize = 40;
            foreach (var name in new[] { "Subtitle", "Scope" })
            {
                var legacy = home.Find(name);
                if (legacy == null) continue;
                legacy.GetComponent<UnityEngine.UI.Text>().text = string.Empty;
                legacy.gameObject.SetActive(false);
            }
            var credits = canvas.Find("Credits");
            if (credits != null) FeedbackMenuStyle.ApplyTextButton(credits.GetComponent<UnityEngine.UI.Button>());
        }
        private static UnityEngine.UI.Button Link(Transform parent, string name, string text, Vector2 min, Vector2 max)
        {
            var button = M2MenuAuthoring.Button(parent, name, text, min, max);
            button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 25;
            return button;
        }
    }
}
