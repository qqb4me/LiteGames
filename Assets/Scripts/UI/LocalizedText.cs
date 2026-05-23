using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    public string ruText;
    public string enText;
    public string trText;

    private TextMeshProUGUI textComponent;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText(LocalizationManager.Instance.CurrentLanguage);
        }
    }

    void UpdateText(string lang)
    {
        if (textComponent == null) return;

        switch (lang)
        {
            case "ru":
                textComponent.text = ruText;
                break;
            case "en":
                textComponent.text = enText;
                break;
            case "tr":
                textComponent.text = trText;
                break;
            default:
                textComponent.text = enText;
                break;
        }
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }
}