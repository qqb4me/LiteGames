using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    [SerializeField] AudioMixer mixer;

    const string MasterKey = "audio_master";
    const string MusicKey = "audio_music";
    const string SfxKey = "audio_sfx";

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        LoadAndApply();
    }

    public void SetMasterVolume(float linear)
    {
        SetVolume("Master", MasterKey, linear);
    }

    public void SetMusicVolume(float linear)
    {
        SetVolume("Music", MusicKey, linear);
    }

    public void SetSfxVolume(float linear)
    {
        SetVolume("SFX", SfxKey, linear);
    }

    void SetVolume(string exposedName, string prefKey, float linear)
    {
        var db = LinearToDb(linear);
        if (mixer != null) mixer.SetFloat(exposedName, db);
        PlayerPrefs.SetFloat(prefKey, linear);
    }

    static float LinearToDb(float linear)
    {
        var v = Mathf.Clamp01(linear);
        return v <= 0f ? -80f : 20f * Mathf.Log10(v);
    }

    void LoadAndApply()
    {
        var master = PlayerPrefs.GetFloat(MasterKey, 1f);
        var music = PlayerPrefs.GetFloat(MusicKey, 1f);
        var sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        if (mixer != null)
        {
            mixer.SetFloat("Master", LinearToDb(master));
            mixer.SetFloat("Music", LinearToDb(music));
            mixer.SetFloat("SFX", LinearToDb(sfx));
        }
    }

    public float GetMaster() => PlayerPrefs.GetFloat(MasterKey, 1f);
    public float GetMusic() => PlayerPrefs.GetFloat(MusicKey, 1f);
    public float GetSfx() => PlayerPrefs.GetFloat(SfxKey, 1f);
}
