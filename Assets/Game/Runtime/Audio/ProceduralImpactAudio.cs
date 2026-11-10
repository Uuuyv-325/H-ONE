using System;
using UnityEngine;

namespace HOne.Game
{
    // Owned runtime samples, not a replacement for the final recorded sound delivery.
    // All tuning is deterministic and leaves UnityEngine.Random's gameplay state untouched.
    internal sealed class ProceduralImpactAudio : IDisposable
    {
        private const int SampleRate = 48000;
        private const float D4 = 293.6648f;
        public readonly AudioClip[] Stone = new AudioClip[3];
        public readonly AudioClip Lyre, Drum, DrumWave;
        public readonly AudioClip[] Ceramic = new AudioClip[3];
        public readonly AudioClip[] Collection = new AudioClip[3];
        public int ClipCount { get; private set; }

        public ProceduralImpactAudio()
        {
            for (var variant = 0; variant < Stone.Length; variant++)
            {
                var index = variant;
                var noise = new Noise((uint)(1277 + 983 * index));
                var low = 0f;
                var resonance = 215f + 8f * index;
                Stone[index] = Create("stone_" + index, 0.24f, t =>
                {
                    low += 0.065f * (noise.Next() - low);
                    return 0.48f * low * Decay(t, 26f) +
                           0.18f * Tone(resonance, t) * Decay(t, 31f) +
                           0.065f * Tone(487f + 11f * index, t) * Decay(t, 45f);
                });
            }

            // A D4 pluck with a stable A4 fifth; a fast, clear onset and soft upper partials.
            Lyre = Create("lyre_d_a", 0.70f, t =>
                0.44f * Tone(D4, t) * Decay(t, 7.7f) +
                0.17f * Tone(D4 * 2f, t) * Decay(t, 15f) +
                0.075f * Tone(D4 * 3f, t) * Decay(t, 23f) +
                0.12f * Tone(440f, t) * Decay(t, 10f));

            var drumNoise = new Noise(4109);
            var membrane = 0f;
            Drum = Create("drum_membrane_d", 0.48f, t =>
            {
                membrane += 0.08f * (drumNoise.Next() - membrane);
                // The brief downward settling belongs to the membrane attack, not random pitch.
                var phase = D4 * 0.5f * t + 0.68f * (1f - Decay(t, 43f));
                return 0.49f * Mathf.Sin(2f * Mathf.PI * phase) * Decay(t, 12f) +
                       0.17f * Tone(D4 * 0.797f, t) * Decay(t, 19f) +
                       0.24f * membrane * Decay(t, 41f);
            });
            var waveNoise = new Noise(7213);
            var waveLow = 0f;
            var waveBody = 0f;
            DrumWave = Create("drum_air_wave", 0.52f, t =>
            {
                waveLow += 0.025f * (waveNoise.Next() - waveLow);
                waveBody += 0.13f * (waveLow - waveBody);
                return 0.85f * (waveLow - waveBody) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.52f)) * Decay(t, 4f);
            }, 0.20f);

            for (var variant = 0; variant < Ceramic.Length; variant++)
            {
                var index = variant;
                var noise = new Noise((uint)(9073 + 431 * index));
                var dust = 0f;
                Ceramic[index] = Create("ceramic_" + index, 0.65f, t =>
                {
                    dust += 0.16f * (noise.Next() - dust);
                    var sound = 0.16f * dust * Decay(t, 11f);
                    // Staggered, damped tile resonances, avoiding a glassy high-frequency crash.
                    for (var shard = 0; shard < 4; shard++)
                    {
                        var age = t - shard * (0.023f + index * 0.003f);
                        if (age < 0f) continue;
                        var attack = Mathf.Min(1f, age / 0.003f);
                        var hz = 580f + 193f * shard + 13f * index;
                        sound += attack * Decay(age, 17f + shard * 3f) *
                                 (0.09f * Tone(hz, age) + 0.032f * Tone(hz * 1.71f, age));
                    }
                    return sound;
                }, 0.40f);
            }

            // D Dorian's D5, F5, A5 give fixed, ascending progress cues.
            var notes = new[] { D4 * 2f, 698.4565f, 880f };
            for (var stage = 0; stage < Collection.Length; stage++)
            {
                var hz = notes[stage];
                var final = stage == 2;
                Collection[stage] = Create("fragment_" + (stage + 1), final ? 0.64f : 0.48f, t =>
                {
                    var sound = 0.41f * Tone(hz, t) * Decay(t, 10f) +
                                0.10f * Tone(hz * 2f, t) * Decay(t, 21f);
                    if (final && t >= 0.115f)
                        sound += 0.19f * Tone(D4 * 4f, t - 0.115f) * Decay(t - 0.115f, 12f) *
                                 Mathf.Min(1f, (t - 0.115f) / 0.005f);
                    return sound;
                });
            }
        }

        private AudioClip Create(string cue, float seconds, Func<float, float> sample, float peak = 0.52f)
        {
            var data = new float[Mathf.CeilToInt(seconds * SampleRate)];
            var maximum = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = (float)i / SampleRate;
                var fade = Mathf.Min(1f, t / 0.005f) * Mathf.Min(1f, (data.Length - 1 - i) / (SampleRate * 0.018f));
                data[i] = sample(t) * fade;
                maximum = Mathf.Max(maximum, Mathf.Abs(data[i]));
            }
            // Keep equal cue ceilings with headroom before the SFX bus. Never hard clip.
            var gain = maximum > 0.00001f ? peak / maximum : 0f;
            for (var i = 0; i < data.Length; i++) data[i] *= gain;
            var clip = AudioClip.Create("Procedural_" + cue, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            ClipCount++;
            return clip;
        }

        private static float Tone(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        private static float Decay(float t, float rate) => Mathf.Exp(-t * rate);

        public void Dispose()
        {
            foreach (var clip in Stone) Release(clip);
            Release(Lyre); Release(Drum); Release(DrumWave);
            foreach (var clip in Ceramic) Release(clip);
            foreach (var clip in Collection) Release(clip);
            ClipCount = 0;
        }

        private static void Release(AudioClip clip)
        {
            if (clip == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(clip);
            else UnityEngine.Object.DestroyImmediate(clip);
        }

        private sealed class Noise
        {
            private uint state;
            public Noise(uint seed) { state = seed; }
            public float Next()
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (state & 0x00ffffff) / 8388607.5f - 1f;
            }
        }
    }
}
