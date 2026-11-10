using UnityEngine;

namespace HOne.Game
{
    public sealed class InstrumentArtView : MonoBehaviour
    {
        [SerializeField] private StarInstrument instrument;
        [SerializeField] private SpriteRenderer drawing;
        [SerializeField] private DeliveredArtSet art;
        [SerializeField] private LineRenderer wave;
        private float hitTime = float.NegativeInfinity;
        private LevelSession boundSession;
        private StarInstrument boundInstrument;
        private Vector3 restPosition, restScale;
        private Quaternion restRotation;
        private Color restColor;
        private float waveStartWidth, waveEndWidth;
        private bool baselineCaptured;
        public void Configure(StarInstrument source, SpriteRenderer view, DeliveredArtSet delivery, LineRenderer ring)
        {
            Unbind();
            if (Application.isPlaying) ResetVisual();
            instrument = source; drawing = view; art = delivery; wave = ring;
            baselineCaptured = false;
            CaptureBaseline();
            if (isActiveAndEnabled) Bind();
        }
        private void CaptureBaseline()
        {
            if (baselineCaptured || drawing == null) return;
            restPosition = drawing.transform.localPosition;
            restRotation = drawing.transform.localRotation;
            restScale = drawing.transform.localScale;
            restColor = drawing.color;
            if (wave != null) { waveStartWidth = wave.startWidth; waveEndWidth = wave.endWidth; }
            baselineCaptured = true;
        }
        private void OnEnable() { baselineCaptured = false; CaptureBaseline(); Bind(); }
        private void OnDisable() { Unbind(); if (Application.isPlaying) ResetVisual(); }
        private void Bind()
        {
            if (instrument == null || boundInstrument == instrument) return;
            boundInstrument = instrument;
            boundInstrument.Struck += Hit;
            BindSession();
        }
        private void BindSession()
        {
            if (boundSession != null) { boundSession.ProgressReset -= ResetVisual; boundSession.PuzzleReset -= ResetVisual; }
            boundSession = instrument != null ? instrument.Session : null;
            if (boundSession != null) { boundSession.ProgressReset += ResetVisual; boundSession.PuzzleReset += ResetVisual; }
        }
        private void Unbind()
        {
            if (boundInstrument != null) boundInstrument.Struck -= Hit;
            if (boundSession != null) { boundSession.ProgressReset -= ResetVisual; boundSession.PuzzleReset -= ResetVisual; }
            boundSession = null;
            boundInstrument = null;
        }
        private void ResetVisual()
        {
            hitTime = float.NegativeInfinity;
            if (drawing != null && baselineCaptured)
            {
                drawing.transform.localPosition = restPosition;
                drawing.transform.localRotation = restRotation;
                drawing.transform.localScale = restScale;
                drawing.color = restColor;
                if (instrument != null && instrument.Kind == StarInstrumentKind.Drum && art != null && art.DrumIdle != null)
                    drawing.sprite = art.DrumIdle;
            }
            if (wave != null)
            {
                wave.enabled = false;
                if (baselineCaptured) { wave.startWidth = waveStartWidth; wave.endWidth = waveEndWidth; }
            }
        }
        private void Hit() => hitTime = Time.time;
        private void LateUpdate()
        {
            if (instrument == null || drawing == null) return;
            if (instrument.Session != boundSession) BindSession();
            var elapsed = Time.time - hitTime;
            if (instrument.Kind == StarInstrumentKind.Drum)
            {
                if (art != null)
                {
                    var playing = elapsed >= 0f && art.DrumHit != null && elapsed * 20f < art.DrumHit.Length;
                    drawing.sprite = playing ? art.DrumHit[Mathf.FloorToInt(elapsed * 20f)] : art.DrumIdle;
                }
            }
            else
            {
                // Bend the visual around its authored anchor; the solid instrument plane stays still.
                var envelope = elapsed >= 0f && elapsed < .36f ? Mathf.Pow(1f - elapsed / .36f, 2f) : 0f;
                var vibration = envelope > 0f ? Mathf.Sin(elapsed * 110f) * envelope : 0f;
                drawing.transform.localPosition = restPosition + Vector3.right * (.018f * vibration);
                drawing.transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, 1.4f * vibration);
                var idleColor = instrument.Value ? Color.Lerp(restColor, new Color(1f, 1f, .78f, restColor.a), .6f) : restColor;
                drawing.color = Color.Lerp(idleColor, new Color(1f, .88f, .5f, restColor.a), envelope * .65f);
            }
            if (wave == null) return;
            wave.enabled = elapsed >= 0f && elapsed < .4f;
            if (!wave.enabled) return;
            var progress = elapsed / .4f;
            wave.startColor = wave.endColor = new Color(1f,.8f,.35f,(1f-progress)*(1f-progress));
            wave.startWidth = wave.endWidth = Mathf.Lerp(.075f, .02f, progress);
            var radius = instrument.WaveRadius * elapsed/.4f;
            if (wave.positionCount != 48) wave.positionCount = 48;
            for (var i=0;i<48;i++)
            {
                var angle=i*Mathf.PI*2f/48;
                wave.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0));
            }
        }
    }
}
