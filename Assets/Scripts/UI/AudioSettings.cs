using UnityEngine;
using UnityEngine.UI;

namespace TheAlchemest.UI
{
    public class AudioSettings : MonoBehaviour
    {
        [SerializeField] Slider masterVolumeSlider;
        [SerializeField] Slider musicVolumeSlider;
        [SerializeField] Slider sfxVolumeSlider;

        float masterVolume = 1f;
        float musicVolume = 1f;
        float sfxVolume = 1f;
        bool isInitialized;

        void Start()
        {
            Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
        }

        public void Initialize(Slider masterSlider, Slider musicSlider, Slider sfxSlider)
        {
            if (isInitialized && masterVolumeSlider == masterSlider && musicVolumeSlider == musicSlider && sfxVolumeSlider == sfxSlider)
            {
                return;
            }

            isInitialized = true;
            masterVolumeSlider = masterSlider;
            musicVolumeSlider = musicSlider;
            sfxVolumeSlider = sfxSlider;

            BindSlider(masterVolumeSlider, OnMasterVolumeChanged, masterVolume);
            BindSlider(musicVolumeSlider, OnMusicVolumeChanged, musicVolume);
            BindSlider(sfxVolumeSlider, OnSfxVolumeChanged, sfxVolume);
        }

        static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> handler, float value)
        {
            if (slider == null)
            {
                return;
            }

            slider.onValueChanged.RemoveListener(handler);
            slider.value = value;
            slider.onValueChanged.AddListener(handler);
        }

        public void OnMasterVolumeChanged(float value)
        {
            masterVolume = value;
            Debug.Log($"Master Volume: {masterVolume}");
        }

        public void OnMusicVolumeChanged(float value)
        {
            musicVolume = value;
            Debug.Log($"Music Volume: {musicVolume}");
        }

        public void OnSfxVolumeChanged(float value)
        {
            sfxVolume = value;
            Debug.Log($"SFX Volume: {sfxVolume}");
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSfxVolume() => sfxVolume;
    }
}
