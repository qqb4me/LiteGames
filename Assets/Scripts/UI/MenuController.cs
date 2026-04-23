using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Scene UI References")]
    [SerializeField] Canvas sceneCanvas;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Button playButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button closeSettingsButton;
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;

    [Header("Behavior")]
    [SerializeField] bool autoCreateEventSystem = true;

    [Header("Editor")]
    [SerializeField] bool autoWireInEditor = true;

    void Awake()
    {
        if (autoCreateEventSystem)
        {
            EnsureEventSystem();
        }
    }

    void Start()
    {
        if (!TryInitializeSceneUi())
        {
            Debug.LogError("MenuController: UI references are not configured. Assign references in Inspector or use Auto Wire UI References.", this);
        }
    }

    public void PlayGame()
    {
        GameSession.LoadScene("AlchemistHome");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    bool TryInitializeSceneUi()
    {
        TryResolveReferences();

        if (!HasRequiredReferences())
        {
            return false;
        }

        BindButton(playButton, PlayGame);
        BindButton(settingsButton, OpenSettings);
        BindButton(quitButton, QuitGame);
        BindButton(closeSettingsButton, CloseSettings);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        TheAlchemest.UI.AudioSettings audioSettings = GetComponent<TheAlchemest.UI.AudioSettings>();
        if (audioSettings == null)
        {
            audioSettings = gameObject.AddComponent<TheAlchemest.UI.AudioSettings>();
        }

        audioSettings.Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
        return true;
    }

    void BindButton(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    bool HasRequiredReferences()
    {
        return sceneCanvas != null
            && settingsPanel != null
            && playButton != null
            && settingsButton != null
            && quitButton != null
            && closeSettingsButton != null;
    }

    [ContextMenu("Auto Wire UI References")]
    void AutoWireUiReferences()
    {
        TryResolveReferences();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void TryResolveReferences()
    {
        if (sceneCanvas == null)
        {
            sceneCanvas = FindAnyObjectByType<Canvas>();
        }

        if (sceneCanvas == null)
        {
            return;
        }

        mainMenuPanel ??= FindObjectByName<RectTransform>("MenuPanel")?.gameObject;
        settingsPanel ??= FindObjectByName<RectTransform>("SettingsPanel")?.gameObject;
        playButton ??= FindObjectByName<Button>("PlayButton");
        settingsButton ??= FindObjectByName<Button>("SettingsButton");
        quitButton ??= FindObjectByName<Button>("QuitButton");
        closeSettingsButton ??= FindObjectByName<Button>("CloseButton");
        masterVolumeSlider ??= FindSliderByName("MasterVolumeSlider");
        musicVolumeSlider ??= FindSliderByName("MusicVolumeSlider");
        sfxVolumeSlider ??= FindSliderByName("SfxVolumeSlider");
    }

    T FindObjectByName<T>(string objectName) where T : Component
    {
        T[] components = sceneCanvas.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component != null && component.name == objectName)
            {
                return component;
            }
        }

        return null;
    }

    Slider FindSliderByName(string containerName)
    {
        GameObject sliderContainer = FindObjectByName<RectTransform>(containerName)?.gameObject;
        if (sliderContainer == null)
        {
            return null;
        }

        return sliderContainer.GetComponentInChildren<Slider>(true);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!autoWireInEditor || Application.isPlaying)
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall -= DelayedAutoWire;
        UnityEditor.EditorApplication.delayCall += DelayedAutoWire;
    }

    void DelayedAutoWire()
    {
        if (this == null || Application.isPlaying || !autoWireInEditor)
        {
            return;
        }

        AutoWireUiReferences();
    }
#endif
}
