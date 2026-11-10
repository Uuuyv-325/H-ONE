using UnityEngine;
using UnityEngine.Audio;

namespace HOne.Game
{
    // Owned by the menu scene: page changes keep the song, scene changes release it.
    public sealed class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField, Range(0f, 1f)] private float level = .42f;
        private bool started;
        private float age;
        private float transitionGain = 1f;
        public AudioSource Source => source;
        public void SetTransitionGain(float value)
        {
            transitionGain = Mathf.Clamp01(value);
            ApplyVolume();
        }

        public void Configure(AudioSource output, AudioClip clip, AudioMixerGroup bus)
        {
            source = output;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = bus;
            source.clip = clip;
            source.volume = 0f;
        }

        private void Start() { started = true; Play(); }
        private void OnEnable() { if (started) Play(); }
        private void Play()
        {
            age = 0f;
            transitionGain = 1f;
            if (source == null || source.clip == null || source.outputAudioMixerGroup == null) return;
            source.volume = 0f;
            source.Play();
        }
        private void Update()
        {
            if (source == null || !source.isPlaying) return;
            age += Time.unscaledDeltaTime;
            ApplyVolume();
        }
        private void ApplyVolume()
        {
            if (source == null || source.clip == null) return;
            // Soften the original full-song loop boundary without altering the delivered file.
            var edge = Mathf.Min(source.time / .6f, (source.clip.length - source.time) / .8f);
            source.volume = level * Mathf.Clamp01(Mathf.Min(age / .8f, edge)) * transitionGain;
        }
        private void OnDisable() { if (source != null) source.Stop(); }
    }
}
