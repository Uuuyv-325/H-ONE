using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HOne.Game
{
    // A temporary overlay survives the scene load, then releases all input and timing overrides.
    public sealed class SceneTransition : MonoBehaviour
    {
        public const float FlashDuration = .48f;
        public const float FadeOutDuration = .65f;
        public const float FadeInDuration = .65f;
        private static SceneTransition current;
        private CanvasGroup veil;
        private CanvasGroup flashing;
        private MenuMusic music;
        private readonly List<PlayerInputReader> inputs = new List<PlayerInputReader>();
        private readonly List<(EventSystem system, bool navigation)> events = new List<(EventSystem, bool)>();
        public static bool IsActive => current != null;
        public float Opacity => veil != null ? veil.alpha : 0f;

        public static void Load(string path, UnityEngine.UI.Button button = null)
        {
            if (IsActive) return;
            var root = new GameObject("SceneTransition", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup), typeof(SceneTransition));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            current = root.GetComponent<SceneTransition>();
            current.veil = root.GetComponent<CanvasGroup>();
            current.veil.alpha = 0f;
            var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            black.transform.SetParent(root.transform, false);
            var rect = (RectTransform)black.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            black.GetComponent<UnityEngine.UI.Image>().color = Color.black;
            current.StartCoroutine(current.Run(path, button));
        }

        private void BlockInput()
        {
            foreach (var input in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
                if (input.enabled) { inputs.Add(input); input.enabled = false; }
            if (EventSystem.current != null && !events.Exists(e => e.system == EventSystem.current))
            {
                events.Add((EventSystem.current, EventSystem.current.sendNavigationEvents));
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.sendNavigationEvents = false;
            }
        }

        private IEnumerator Run(string path, UnityEngine.UI.Button button)
        {
            music = FindFirstObjectByType<MenuMusic>();
            Time.timeScale = 0f;
            BlockInput();
            try
            {
                if (button != null)
                {
                    flashing = button.GetComponent<CanvasGroup>();
                    if (flashing == null) flashing = button.gameObject.AddComponent<CanvasGroup>();
                    for (var i = 0; i < 3; i++)
                    {
                        flashing.alpha = .15f;
                        yield return new WaitForSecondsRealtime(FlashDuration / 6f);
                        flashing.alpha = 1f;
                        yield return new WaitForSecondsRealtime(FlashDuration / 6f);
                    }
                }
                yield return Fade(0f, 1f, FadeOutDuration, true);
                // Present a fully black frame before activation can replace either scene's UI/camera.
                yield return new WaitForEndOfFrame();
                AsyncOperation loading = null;
                try { loading = SceneManager.LoadSceneAsync(path); }
                catch (System.Exception error) { Debug.LogException(error); }
                if (loading != null)
                {
                    yield return loading;
                    Time.timeScale = 0f;
                    BlockInput();
                    yield return new WaitForEndOfFrame();
                }
                yield return Fade(1f, 0f, FadeInDuration, false);
            }
            finally
            {
                Release();
                Destroy(gameObject);
            }
        }

        private IEnumerator Fade(float from, float to, float duration, bool fadeMusic)
        {
            // The first post-load delta can include the entire load; start this fade's own clock.
            var started = Time.realtimeSinceStartupAsDouble;
            for (var elapsed = 0f; elapsed < duration; elapsed = (float)(Time.realtimeSinceStartupAsDouble - started))
            {
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                veil.alpha = Mathf.Lerp(from, to, t);
                if (fadeMusic && music != null) music.SetTransitionGain(1f - t);
                yield return null;
            }
            veil.alpha = to;
            if (fadeMusic && music != null) music.SetTransitionGain(0f);
        }

        private void Release()
        {
            if (current != this) return;
            if (flashing != null) flashing.alpha = 1f;
            if (music != null) music.SetTransitionGain(1f);
            foreach (var input in inputs) if (input != null) { input.enabled = true; input.DiscardUntilRelease(); }
            foreach (var entry in events) if (entry.system != null) entry.system.sendNavigationEvents = entry.navigation;
            Time.timeScale = 1f;
            current = null;
        }
        private void OnDestroy() => Release();
    }
}
