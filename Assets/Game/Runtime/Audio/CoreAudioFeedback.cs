using UnityEngine;
using UnityEngine.Audio;

namespace HOne.Game
{
    // Technical P0 feedback. Gameplay never waits for or reads playback state.
    public sealed class CoreAudioFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine player;
        [SerializeField] private AudioMixerGroup bus;
        [SerializeField] private AudioClip chargeStart, chargeLoop, launch, wall, fail, reform, collect;
        private AudioSource loop, oneShot;
        private bool paused;

        public void Configure(PlayerStateMachine owner, AudioMixerGroup output, AudioClip start, AudioClip sustain,
            AudioClip shot, AudioClip hit, AudioClip failure, AudioClip reappear, AudioClip fragment)
        {
            player = owner; bus = output; chargeStart = start; chargeLoop = sustain;
            launch = shot; wall = hit; fail = failure; reform = reappear; collect = fragment;
        }

        private void Awake()
        {
            loop = gameObject.AddComponent<AudioSource>(); loop.playOnAwake = false; loop.loop = true;
            oneShot = gameObject.AddComponent<AudioSource>(); oneShot.playOnAwake = false;
            loop.outputAudioMixerGroup = oneShot.outputAudioMixerGroup = bus;
            loop.spatialBlend = oneShot.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (player == null) return;
            player.StateChanged += StateChanged;
            player.StarImpact += Impact;
            player.Reformed += Reformed;
            player.Session.FragmentCollected += Collected;
        }
        private void OnDisable()
        {
            if (loop != null) loop.Stop();
            if (oneShot != null) oneShot.Stop();
            if (player == null) return;
            player.StateChanged -= StateChanged;
            player.StarImpact -= Impact;
            player.Reformed -= Reformed;
            player.Session.FragmentCollected -= Collected;
        }

        private void Play(AudioClip clip) { if (clip != null) oneShot.PlayOneShot(clip); }
        private void Update()
        {
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
                if (chargeLoop != null) loop.Play();
            }
            else if (state == PlayerState.StarFlight) Play(launch);
            else if (state == PlayerState.Reforming) Play(fail);
        }
        private void Impact(StarContact contact) => Play(wall);
        private void Reformed() => Play(reform);
        private void Collected(string id) => Play(collect);
    }
}
