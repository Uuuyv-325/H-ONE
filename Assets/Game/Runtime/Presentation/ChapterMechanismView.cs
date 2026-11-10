using System;
using UnityEngine;
using UnityEngine.Audio;

namespace HOne.Game
{
    public sealed class ChapterMechanismView : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private StarSwitch[] switches = Array.Empty<StarSwitch>();
        [SerializeField] private FlutePort[] ports = Array.Empty<FlutePort>();
        [SerializeField] private AudioMixerGroup bus;
        private GameplayFeedbackMesh mesh;
        private AudioSource source;
        private AudioClip up, down, chime;
        private Action[] portHandlers;
        public void Configure(PlayerStateMachine owner, AudioMixerGroup output, StarSwitch[] targets, FlutePort[] mouths)
        { player = owner; bus = output; switches = targets; ports = mouths; }
        private void Start()
        {
            mesh = new GameplayFeedbackMesh(transform, "SwitchAndFluteSymbols", null, 24);
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.outputAudioMixerGroup = bus;
            up = Tone("flute_up_sample", 587.33f, 783.99f); down = Tone("flute_down_sample", 783.99f, 587.33f);
            chime = Tone("switch_sample", 1174.66f, 1567.98f);
            portHandlers = new Action[ports.Length];
            for (var i = 0; i < ports.Length; i++)
            {
                var port = ports[i]; portHandlers[i] = () => Play(port.Raise ? up : down);
                port.Activated += portHandlers[i];
            }
            foreach (var target in switches) target.Activated += Chime;
            player.Session.PuzzleReset += Stop;
            player.Session.ProgressReset += Stop;
        }
        private void Play(AudioClip cue) { if (bus != null && !player.Paused) source.PlayOneShot(cue, .35f); }
        private void Chime() => Play(chime);
        private void Stop() { if (source != null) source.Stop(); }
        private void LateUpdate()
        {
            if (mesh == null) return;
            if (player.Paused) Stop();
            mesh.Begin();
            for (var i = 0; i < switches.Length; i++)
            {
                var point = (Vector2)switches[i].transform.position;
                var tint = switches[i].Value ? Color.yellow : new Color(.77f,.84f,.95f);
                if (i == 0) mesh.Diamond(point, .38f, .06f, tint);
                else mesh.Arc(point, .38f, 0, 360, .06f, tint);
                if (switches[i].Value) mesh.Star(point, .25f, tint);
                else mesh.Star(point, .12f, tint);
            }
            foreach (var port in ports)
            {
                var p = (Vector2)port.transform.position;
                var tint = new Color(.9f,.74f,.45f);
                for (var i = 0; i < 4; i++)
                    mesh.Line(p + new Vector2(-.3f + i * .18f,-.35f), p + new Vector2(-.3f + i * .18f,.2f + i * .06f), .085f, tint);
                mesh.Arrow(p + Vector2.right * .65f, port.Raise ? Vector2.up : Vector2.down, .28f, .055f, Color.white);
            }
            mesh.Upload();
        }
        private static AudioClip Tone(string name, float first, float second)
        {
            const int rate = 48000;
            var samples = new float[rate / 2];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                var frequency = t < .22f ? first : second;
                var local = t < .22f ? t : t - .22f;
                var envelope = Mathf.Min(1f, local / .025f) * Mathf.Clamp01((.22f - local) / .07f);
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * local) * envelope * .24f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDisable() => Stop();
        private void OnDestroy()
        {
            if (player != null) { player.Session.PuzzleReset -= Stop; player.Session.ProgressReset -= Stop; }
            for (var i = 0; portHandlers != null && i < ports.Length; i++) if (ports[i] != null) ports[i].Activated -= portHandlers[i];
            foreach (var target in switches) if (target != null) target.Activated -= Chime;
            mesh?.Dispose();
            if (up != null) Destroy(up); if (down != null) Destroy(down); if (chime != null) Destroy(chime);
        }
    }
}
