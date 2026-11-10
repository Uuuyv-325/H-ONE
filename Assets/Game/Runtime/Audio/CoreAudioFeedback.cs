using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace HOne.Game
{
    // Event-driven feedback. Gameplay never waits for or reads playback state.
    public sealed class CoreAudioFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private AudioMixerGroup bus;
        [SerializeField] private AudioClip chargeStart, chargeLoop, launch, wall, fail, reform, collect;
        [SerializeField] private ChapterAudioSet delivery;
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.85f;
        private AudioSource loop, oneShot;
        private ProceduralImpactAudio samples;
        private PlayerStateMachine boundPlayer;
        private LevelSession boundSession;
        private readonly HashSet<int> struckInstruments = new HashSet<int>();
        private int stoneVariant, ceramicVariant, currentShotId = -1;
        private float lastFractureAt = float.NegativeInfinity;
        private bool paused;

        public int GeneratedClipCount => samples?.ClipCount ?? 0;
        public int ImpactPlaybackCount { get; private set; }
        public int FracturePlaybackCount { get; private set; }
        public int CollectionPlaybackCount { get; private set; }
        public int LastCollectionStage { get; private set; }
        public StarImpactKind LastImpactKind { get; private set; }
        public bool SourcesPaused => paused;
        public ChapterAudioSet Delivery => delivery;
        public AudioClip LastPlayedClip { get; private set; }
        public AudioClip LastImpactClip { get; private set; }

        public void Configure(PlayerStateMachine owner, AudioMixerGroup output, AudioClip start, AudioClip sustain,
            AudioClip shot, AudioClip hit, AudioClip failure, AudioClip reappear, AudioClip fragment)
        {
            StopPlayback();
            player = owner; bus = output; chargeStart = start; chargeLoop = sustain;
            launch = shot; wall = hit; fail = failure; reform = reappear; collect = fragment;
            RouteSources();
            if (isActiveAndEnabled) Bind();
        }

        private void Awake()
        {
            loop = gameObject.AddComponent<AudioSource>(); loop.playOnAwake = false; loop.loop = true;
            oneShot = gameObject.AddComponent<AudioSource>(); oneShot.playOnAwake = false;
            loop.spatialBlend = oneShot.spatialBlend = 0f;
            loop.priority = 100; oneShot.priority = 80;
            RouteSources();
            samples = new ProceduralImpactAudio();
            if (delivery == null) delivery = ChapterAudioSet.LoadDefault();
        }

        private void RouteSources()
        {
            if (loop != null) loop.outputAudioMixerGroup = bus;
            if (oneShot != null) oneShot.outputAudioMixerGroup = bus;
        }

        private void OnEnable() => Bind();

        private void Bind()
        {
            if (boundPlayer == player && boundSession == (player != null ? player.Session : null)) return;
            Unbind();
            StopPlayback();
            if (player == null) return;
            boundPlayer = player;
            boundSession = player.Session;
            boundPlayer.StateChanged += StateChanged;
            boundPlayer.StarImpact += Impact;
            boundPlayer.Reformed += Reformed;
            if (boundSession != null)
            {
                boundSession.FragmentCollected += Collected;
                boundSession.WallBroken += WallBroken;
                boundSession.ProgressReset += ResetFeedback;
                boundSession.PuzzleReset += ResetFeedback;
            }
            paused = boundPlayer.Paused;
        }

        private void OnDisable()
        {
            StopPlayback();
            Unbind();
        }

        private void Unbind()
        {
            if (boundPlayer != null)
            {
                boundPlayer.StateChanged -= StateChanged;
                boundPlayer.StarImpact -= Impact;
                boundPlayer.Reformed -= Reformed;
            }
            if (boundSession != null)
            {
                boundSession.FragmentCollected -= Collected;
                boundSession.WallBroken -= WallBroken;
                boundSession.ProgressReset -= ResetFeedback;
                boundSession.PuzzleReset -= ResetFeedback;
            }
            boundPlayer = null; boundSession = null;
        }

        private bool Play(AudioClip clip, float volume = 1f)
        {
            // A missing bus must stay silent rather than bypass the player's SFX volume.
            if (clip == null || oneShot == null || bus == null || !isActiveAndEnabled ||
                player == null || player.Paused) return false;
            oneShot.PlayOneShot(clip, volume);
            LastPlayedClip = clip;
            return true;
        }

        private void Update()
        {
            if (player == null) return;
            if (paused == player.Paused) return;
            paused = player.Paused;
            if (paused) { loop.Pause(); oneShot.Pause(); }
            else { loop.UnPause(); oneShot.UnPause(); }
        }
        private void StateChanged(PlayerState state)
        {
            loop.Stop();
            if (state == PlayerState.Charging)
            {
                Play(chargeStart); loop.clip = chargeLoop;
                if (chargeLoop != null && bus != null && !player.Paused) loop.Play();
            }
            else if (state == PlayerState.StarFlight)
            {
                struckInstruments.Clear();
                currentShotId = -1;
                Play(delivery != null && delivery.Launch != null ? delivery.Launch : launch);
            }
            else if (state == PlayerState.Reforming && player.ReformReason == ReformReason.Failure) Play(fail);
        }

        private void Impact(StarContact contact)
        {
            if (samples == null || contact.Collider == null) return;
            LastImpactKind = contact.Kind;
            var strength = impactVolume * Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(contact.Charge));
            switch (contact.Kind)
            {
                case StarImpactKind.Stone:
                    if (Play(delivery != null && delivery.Stone != null ? delivery.Stone : samples.Stone[stoneVariant], strength))
                    {
                        stoneVariant = (stoneVariant + 1) % samples.Stone.Length;
                        LastImpactClip = LastPlayedClip;
                        ImpactPlaybackCount++;
                    }
                    break;
                case StarImpactKind.Lyre:
                case StarImpactKind.Drum:
                    if (contact.ShotId != currentShotId)
                    {
                        currentShotId = contact.ShotId;
                        struckInstruments.Clear();
                    }
                    var instrument = contact.Collider.GetComponentInParent<StarInstrument>();
                    var target = instrument != null ? instrument.GetInstanceID() : contact.Collider.GetInstanceID();
                    if (!struckInstruments.Add(target)) return;
                    var delivered = delivery != null ?
                        (contact.Kind == StarImpactKind.Lyre ? delivery.Lyre : delivery.Drum) : null;
                    if (Play(delivered != null ? delivered :
                        (contact.Kind == StarImpactKind.Lyre ? samples.Lyre : samples.Drum), strength))
                    {
                        ImpactPlaybackCount++;
                        LastImpactClip = LastPlayedClip;
                        if (contact.Kind == StarImpactKind.Drum) Play(samples.DrumWave, strength * 0.55f);
                    }
                    break;
                // WallBroken owns ceramic sound, including walls reached by the drum wave.
                // ShadowFeedback owns success/unavailable/recall, so sockets are silent here.
            }
        }

        private void WallBroken(BrittleWall broken)
        {
            if (samples == null || broken == null || !broken.Broken || player == null || player.Paused) return;
            if (Time.time - lastFractureAt < 0.075f) return;
            if (!Play(delivery != null && delivery.Ceramic != null ? delivery.Ceramic : samples.Ceramic[ceramicVariant],
                impactVolume * 0.82f)) return;
            lastFractureAt = Time.time;
            ceramicVariant = (ceramicVariant + 1) % samples.Ceramic.Length;
            FracturePlaybackCount++;
        }

        private void Reformed() => Play(reform);

        private void Collected(string id)
        {
            if (boundSession == null) return;
            LastCollectionStage = Mathf.Clamp(boundSession.FragmentCount, 1, 3);
            if (Play(samples != null ? samples.Collection[LastCollectionStage - 1] : collect))
                CollectionPlaybackCount++;
        }

        private void StopPlayback()
        {
            if (loop != null) { loop.Stop(); loop.clip = null; }
            if (oneShot != null) oneShot.Stop();
        }

        private void ResetFeedback()
        {
            StopPlayback();
            struckInstruments.Clear();
            currentShotId = -1;
            lastFractureAt = float.NegativeInfinity;
            LastCollectionStage = boundSession != null ? Mathf.Clamp(boundSession.FragmentCount, 0, 3) : 0;
        }

        private void OnDestroy()
        {
            StopPlayback();
            samples?.Dispose();
            samples = null;
            if (loop != null) Destroy(loop);
            if (oneShot != null) Destroy(oneShot);
        }
    }
}
