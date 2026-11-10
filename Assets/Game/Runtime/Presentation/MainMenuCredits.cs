using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HOne.Game
{
    public sealed class MainMenuCredits : MonoBehaviour
    {
        public const string ArtistUrl = "https://space.bilibili.com/18212244/";
        public const string TrackUrl = "https://www.bilibili.com/video/BV1QhA5efEq9/";
        public const string ProgrammerUrl = "https://github.com/OriSnow420";
        public const string ProjectUrl = "https://github.com/Uuuyv-325/H-ONE";
        [SerializeField] private UnityEngine.UI.Button open, close, artist, track, programmer, project;
        [SerializeField] private GameObject panel;
        [SerializeField] private UnityEngine.UI.RawImage backdrop;
        [SerializeField] private CanvasGroup[] backgroundControls;
        [SerializeField] private Material blur;
        [SerializeField] private InputActionReference cancel;
        private RenderTexture blurred;
        private Coroutine capture;
        private GameObject previousSelection;
        public bool IsOpen => capture != null || (panel != null && panel.activeSelf);
        public RenderTexture Backdrop => blurred;

        public void Configure(UnityEngine.UI.Button button, UnityEngine.UI.Button back,
            UnityEngine.UI.Button authorLink, UnityEngine.UI.Button trackLink,
            UnityEngine.UI.Button programmerLink, UnityEngine.UI.Button projectLink,
            GameObject overlay, UnityEngine.UI.RawImage background, CanvasGroup[] controls,
            Material material, InputActionReference cancelAction)
        {
            open = button; close = back; artist = authorLink; track = trackLink;
            programmer = programmerLink; project = projectLink; panel = overlay; backdrop = background;
            backgroundControls = controls; blur = material; cancel = cancelAction;
        }

        private void OnEnable()
        {
            open.onClick.AddListener(Open); close.onClick.AddListener(Close);
            artist.onClick.AddListener(OpenArtist); track.onClick.AddListener(OpenTrack);
            programmer.onClick.AddListener(OpenProgrammer); project.onClick.AddListener(OpenProject);
            if (cancel != null && cancel.action != null) cancel.action.performed += Cancel;
        }
        private void OnDisable()
        {
            open.onClick.RemoveListener(Open); close.onClick.RemoveListener(Close);
            artist.onClick.RemoveListener(OpenArtist); track.onClick.RemoveListener(OpenTrack);
            programmer.onClick.RemoveListener(OpenProgrammer); project.onClick.RemoveListener(OpenProject);
            if (cancel != null && cancel.action != null) cancel.action.performed -= Cancel;
            Close();
        }
        private void OpenArtist() => Application.OpenURL(ArtistUrl);
        private void OpenTrack() => Application.OpenURL(TrackUrl);
        private void OpenProgrammer() => Application.OpenURL(ProgrammerUrl);
        private void OpenProject() => Application.OpenURL(ProjectUrl);
        private void Cancel(InputAction.CallbackContext context) { if (IsOpen) Close(); }

        public void Open()
        {
            if (IsOpen) return;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            open.interactable = false;
            foreach (var group in backgroundControls) group.interactable = false;
            // The menu remains visible for this frame's capture, then the modal covers it.
            capture = StartCoroutine(Capture());
        }
        private IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            var width = Mathf.Max(1, Screen.width); var height = Mathf.Max(1, Screen.height);
            var full = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var small = RenderTexture.GetTemporary(Mathf.Max(1, width / 4), Mathf.Max(1, height / 4), 0);
            try
            {
                blurred = new RenderTexture(small.width, small.height, 0) { filterMode = FilterMode.Bilinear };
                blurred.Create();
                ScreenCapture.CaptureScreenshotIntoRenderTexture(full);
                Graphics.Blit(full, small);
                if (blur != null) Graphics.Blit(small, blurred, blur);
                else Graphics.Blit(small, blurred);
                backdrop.texture = blurred;
                backdrop.uvRect = SystemInfo.graphicsUVStartsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
                panel.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
            }
            finally { RenderTexture.ReleaseTemporary(full); RenderTexture.ReleaseTemporary(small); }
            capture = null;
        }
        public void Close()
        {
            if (capture != null) { StopCoroutine(capture); capture = null; }
            if (panel != null) panel.SetActive(false);
            if (backdrop != null) backdrop.texture = null;
            if (blurred != null) { blurred.Release(); Destroy(blurred); blurred = null; }
            if (open != null) open.interactable = true;
            if (backgroundControls != null)
                foreach (var group in backgroundControls) if (group != null) group.interactable = true;
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }
    }
}
