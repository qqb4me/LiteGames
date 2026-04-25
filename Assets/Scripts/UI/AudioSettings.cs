using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheAlchemest.UI
{
    public class AudioSettings : MonoBehaviour
    {
        const string MasterVolumeKey = "audio.master";
        const string MusicVolumeKey = "audio.music";
        const string SfxVolumeKey = "audio.sfx";

        [SerializeField] Slider masterVolumeSlider;
        [SerializeField] Slider musicVolumeSlider;
        [SerializeField] Slider sfxVolumeSlider;

        static float masterVolume = 1f;
        static float musicVolume = 1f;
        static float sfxVolume = 1f;
        static bool cacheLoaded;
        static readonly List<AudioSettings> instances = new List<AudioSettings>();
        bool isInitialized;

        void OnEnable()
        {
            LoadCacheIfNeeded();
            if (!instances.Contains(this))
            {
                instances.Add(this);
            }

            ApplyCurrentValuesToSliders();
        }

        void OnDisable()
        {
            instances.Remove(this);
        }

        void Start()
        {
            Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
        }

        public void Initialize(Slider masterSlider, Slider musicSlider, Slider sfxSlider)
        {
            LoadCacheIfNeeded();

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
            ApplyCurrentValuesToSliders();
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
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            BroadcastValuesChanged();
            Debug.Log($"Master Volume: {masterVolume}");
        }

        public void OnMusicVolumeChanged(float value)
        {
            musicVolume = value;
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            BroadcastValuesChanged();
            Debug.Log($"Music Volume: {musicVolume}");
        }

        public void OnSfxVolumeChanged(float value)
        {
            sfxVolume = value;
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            BroadcastValuesChanged();
            Debug.Log($"SFX Volume: {sfxVolume}");
        }

        static void BroadcastValuesChanged()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                {
                    instances[i].ApplyCurrentValuesToSliders();
                }
            }
        }

        void ApplyCurrentValuesToSliders()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(masterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(musicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(sfxVolume);
            }
        }

        static void LoadCacheIfNeeded()
        {
            if (cacheLoaded)
            {
                return;
            }

            cacheLoaded = true;
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSfxVolume() => sfxVolume;
    }
}
