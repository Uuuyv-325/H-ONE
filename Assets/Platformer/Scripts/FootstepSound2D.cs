using UnityEngine;

namespace HOne.Platformer
{
    [RequireComponent(typeof(AudioSource), typeof(DistanceFootstepEmitter2D))]
    public sealed class FootstepSound2D : MonoBehaviour
    {
        private DistanceFootstepEmitter2D steps;
        private AudioSource source;
        private AudioClip clip;

        private void Awake()
        {
            steps = GetComponent<DistanceFootstepEmitter2D>();
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.35f;
            clip = CreateFootstepClip();
        }

        private void OnEnable()
        {
            steps.Step += PlayStep;
        }

        private void OnDisable()
        {
            if (steps != null)
                steps.Step -= PlayStep;
        }

        private void OnDestroy()
        {
            if (clip != null)
                Destroy(clip);
        }

        private void PlayStep(Vector2 position)
        {
            source.PlayOneShot(clip);
        }

        private static AudioClip CreateFootstepClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = 2205;
            var samples = new float[sampleCount];
            var seed = 137u;
            var filteredNoise = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Pow(1f - i / (float)sampleCount, 3f);
                seed = seed * 1664525u + 1013904223u;
                var noise = ((seed >> 16) / 32767.5f) - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.2f);
                samples[i] = envelope * (0.32f * filteredNoise + 0.18f * Mathf.Sin(2f * Mathf.PI * 85f * time));
            }

            var result = AudioClip.Create("Generated Footstep", sampleCount, 1, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }
    }
}
