using UnityEngine;
using YG;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public string CurrentLanguage { get; private set; } = "ru";

    public event System.Action<string> OnLanguageChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CurrentLanguage = YG2.lang;
        Debug.Log($"Язык Яндекс Игр: {CurrentLanguage}");
        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

    public string GetText(string ru, string en, string tr)
    {
        switch (CurrentLanguage)
        {
            case "ru": return ru;
            case "en": return en;
            case "tr": return tr;
            default: return en;
        }
    }
}