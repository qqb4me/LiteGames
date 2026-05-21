using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheAlchemest.Audio
{
    [DefaultExecutionOrder(-800)]
    public class AudioSettings : MonoBehaviour
    {
        public static AudioSettings Instance { get; private set; }
        const string PrefMaster = "masterVolume";
        const string PrefMusic = "musicVolume";
        const string PrefSfx = "sfxVolume";

        float masterVolume = 1f;
        float musicVolume = 1f;
        float sfxVolume = 1f;

        AudioSource musicSource;
        Slider masterSlider;
        Slider musicSlider;
        Slider sfxSlider;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
            musicVolume = PlayerPrefs.GetFloat(PrefMusic, 1f);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfx, 1f);

            ApplyVolumes();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Initialize(Slider master, Slider music, Slider sfx)
        {
            masterSlider = master;
            musicSlider = music;
            sfxSlider = sfx;

            if (masterSlider != null)
            {
                masterSlider.value = masterVolume;
                masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
                masterSlider.onValueChanged.AddListener(OnMasterChanged);
            }

            if (musicSlider != null)
            {
                musicSlider.value = musicVolume;
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = sfxVolume;
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            ApplyVolumes();
        }

        void OnMasterChanged(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefMaster, masterVolume);
            ApplyVolumes();
        }

        void OnMusicChanged(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefMusic, musicVolume);
            ApplyVolumes();
        }

        void OnSfxChanged(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            AudioListener.volume = masterVolume;
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(HandleSceneMusic(scene));
        }

        IEnumerator HandleSceneMusic(Scene scene)
        {
            
            AudioClip clip = null;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Transform t = root.transform.Find("SceneMusic");
                if (t != null)
                {
                    var src = t.GetComponent<AudioSource>();
                    if (src != null && src.clip != null)
                    {
                        clip = src.clip;
                        
                        src.enabled = false;
                        break;
                    }
                }
            }

            
            if (clip == null)
            {
                var res = Resources.Load<AudioClip>($"Music/{scene.name}");
                if (res != null)
                {
                    clip = res;
                }
            }

            
            if (clip == null)
            {
                yield break;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                yield break;
            }

            
            yield return StartCoroutine(CrossfadeTo(clip, 0.5f));
        }

        IEnumerator CrossfadeTo(AudioClip newClip, float duration)
        {
            float startVol = musicSource.volume;
            float t = 0f;
            if (musicSource.isPlaying)
            {
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                    yield return null;
                }
                musicSource.Stop();
            }

            musicSource.clip = newClip;
            musicSource.Play();

            t = 0f;
            float target = musicVolume * masterVolume;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, target, t / duration);
                yield return null;
            }
            musicSource.volume = target;
        }

        
        public void PlaySfx(AudioClip clip, float spatialBlend = 0f)
        {
            if (clip == null) return;
            GameObject go = new GameObject("sfx");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = spatialBlend;
            src.volume = sfxVolume * masterVolume;
            src.Play();
            Destroy(go, clip.length + 0.1f);
        }
    }
}
