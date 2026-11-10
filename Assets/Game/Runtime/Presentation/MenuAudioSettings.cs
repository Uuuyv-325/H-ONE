using UnityEngine;
using UnityEngine.Audio;

namespace HOne.Game
{
    public sealed class MenuAudioSettings : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private UnityEngine.UI.Slider music, sfx;
        private bool dirty;
        public void Configure(AudioMixer output, UnityEngine.UI.Slider musicSlider, UnityEngine.UI.Slider sfxSlider)
        { mixer = output; music = musicSlider; sfx = sfxSlider; }
        private void Start()
        {
            music.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVol", 0.8f));
            sfx.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVol", 0.8f));
            SetBus("MusicVol", music.value); SetBus("SFXVol", sfx.value);
            music.onValueChanged.AddListener(MusicChanged); sfx.onValueChanged.AddListener(SfxChanged);
        }
        private void SetBus(string bus, float value) => mixer.SetFloat(bus, value <= 0.001f ? -80f : 20f * Mathf.Log10(value));
        private void MusicChanged(float value) { SetBus("MusicVol", value); PlayerPrefs.SetFloat("MusicVol", value); dirty = true; }
        private void SfxChanged(float value) { SetBus("SFXVol", value); PlayerPrefs.SetFloat("SFXVol", value); dirty = true; }
        private void OnDisable() { if (dirty) { PlayerPrefs.Save(); dirty = false; } }
        private void OnDestroy()
        {
            if (music != null) music.onValueChanged.RemoveListener(MusicChanged);
            if (sfx != null) sfx.onValueChanged.RemoveListener(SfxChanged);
        }
    }
}
