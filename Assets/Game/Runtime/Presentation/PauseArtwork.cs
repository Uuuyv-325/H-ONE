using System.Collections;
using UnityEngine;

namespace HOne.Game
{
    public sealed class PauseArtwork : MonoBehaviour
    {
        [SerializeField] private LevelSession session;
        [SerializeField] private DeliveredArtSet art;
        [SerializeField] private GameObject panel;
        [SerializeField] private UnityEngine.UI.RawImage backdrop;
        [SerializeField] private UnityEngine.UI.Image illustration;
        private RenderTexture blurred;
        private Coroutine capture;
        public void Configure(LevelSession owner, DeliveredArtSet delivery, GameObject root,
            UnityEngine.UI.RawImage background, UnityEngine.UI.Image overlay)
        { session = owner; art = delivery; panel = root; backdrop = background; illustration = overlay; }

        public void SetVisible(bool visible)
        {
            if (capture != null) { StopCoroutine(capture); capture = null; }
            panel.SetActive(false);
            Release();
            if (visible && isActiveAndEnabled) capture = StartCoroutine(Capture());
        }

        private IEnumerator Capture()
        {
            // Capture only the frozen game, before the overlay becomes visible.
            yield return new WaitForEndOfFrame();
            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var full = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var small = RenderTexture.GetTemporary(Mathf.Max(1, width / 4), Mathf.Max(1, height / 4), 0);
            blurred = new RenderTexture(small.width, small.height, 0) { filterMode = FilterMode.Bilinear };
            blurred.Create();
            ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
            Graphics.Blit(full, small);
            Graphics.Blit(small, blurred, art.PauseBlur);
            RenderTexture.ReleaseTemporary(full);
            RenderTexture.ReleaseTemporary(small);
            backdrop.texture = blurred;
            backdrop.uvRect = SystemInfo.graphicsUVStartsAtTop
                ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
            if (art.PauseProgress != null && art.PauseProgress.Length > 0)
                illustration.sprite = art.PauseProgress[Mathf.Clamp(session.FragmentCount, 0, art.PauseProgress.Length - 1)];
            panel.SetActive(true);
            capture = null;
        }

        private void Release()
        {
            if (backdrop != null) backdrop.texture = null;
            if (blurred == null) return;
            blurred.Release(); Destroy(blurred); blurred = null;
        }
        private void OnDisable()
        {
            if (capture != null) StopCoroutine(capture);
            capture = null;
            if (panel != null) panel.SetActive(false);
            Release();
        }
    }
}
