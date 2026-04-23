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
    [SerializeField] bool buildRuntimeUiIfMissing = true;

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
            if (buildRuntimeUiIfMissing)
            {
                BuildRuntimeFallbackUi();
                if (TryInitializeSceneUi())
                {
                    return;
                }
            }

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

    void BuildRuntimeFallbackUi()
    {
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            sceneCanvas = existingCanvas;
        }

        if (existingCanvas != null
            && FindObjectByName<RectTransform>("MenuPanel") != null
            && FindObjectByName<RectTransform>("SettingsPanel") != null
            && FindObjectByName<Button>("PlayButton") != null
            && FindObjectByName<Button>("SettingsButton") != null
            && FindObjectByName<Button>("QuitButton") != null
            && FindObjectByName<Button>("CloseButton") != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        sceneCanvas = canvas;

        Transform root = canvasObject.transform;

        CreatePanel(root, "Background", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Color(0.08f, 0.09f, 0.14f, 1f));
        mainMenuPanel = CreatePanel(root, "MenuPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 360), new Color(0.13f, 0.15f, 0.2f, 0.92f));

        CreateLabel(mainMenuPanel.transform, "Title", "THE ALCHEMIST", 56, new Vector2(0.5f, 0.8f), new Vector2(460, 80), new Color(0.96f, 0.91f, 0.78f, 1f));

        playButton = CreateButton(mainMenuPanel.transform, "PlayButton", "PLAY", new Vector2(0.5f, 0.58f), new Vector2(280, 60), new Color(0.22f, 0.55f, 0.3f, 1f));
        settingsButton = CreateButton(mainMenuPanel.transform, "SettingsButton", "SETTINGS", new Vector2(0.5f, 0.42f), new Vector2(280, 60), new Color(0.22f, 0.34f, 0.55f, 1f));
        quitButton = CreateButton(mainMenuPanel.transform, "QuitButton", "QUIT", new Vector2(0.5f, 0.26f), new Vector2(280, 60), new Color(0.52f, 0.2f, 0.2f, 1f));

        settingsPanel = CreatePanel(root, "SettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 500), new Color(0.11f, 0.12f, 0.17f, 0.97f));
        settingsPanel.SetActive(false);

        CreateLabel(settingsPanel.transform, "SettingsTitle", "SETTINGS", 44, new Vector2(0.5f, 0.9f), new Vector2(520, 70), new Color(0.96f, 0.91f, 0.78f, 1f));
        closeSettingsButton = CreateButton(settingsPanel.transform, "CloseButton", "BACK", new Vector2(0.84f, 0.9f), new Vector2(130, 46), new Color(0.48f, 0.22f, 0.22f, 1f));

        masterVolumeSlider = CreateSlider(settingsPanel.transform, "MasterVolumeSlider", "Master Volume", new Vector2(0.5f, 0.62f), new Vector2(460, 26));
        musicVolumeSlider = CreateSlider(settingsPanel.transform, "MusicVolumeSlider", "Music Volume", new Vector2(0.5f, 0.46f), new Vector2(460, 26));
        sfxVolumeSlider = CreateSlider(settingsPanel.transform, "SfxVolumeSlider", "SFX Volume", new Vector2(0.5f, 0.30f), new Vector2(460, 26));
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static void CreateLabel(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;

        Text label = go.GetComponent<Text>();
        label.text = text;
        label.font = GetBuiltinUiFont();
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
    }

    static Button CreateButton(Transform parent, string name, string text, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;

        CreateLabel(go.transform, "Label", text, 28, new Vector2(0.5f, 0.5f), size, Color.white);
        return go.GetComponent<Button>();
    }

    static Slider CreateSlider(Transform parent, string name, string labelText, Vector2 anchor, Vector2 size)
    {
        GameObject holder = new GameObject(name, typeof(RectTransform));
        holder.transform.SetParent(parent, false);
        RectTransform hrt = holder.GetComponent<RectTransform>();
        hrt.anchorMin = anchor;
        hrt.anchorMax = anchor;
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = size;

        CreateLabel(holder.transform, "Label", labelText, 18, new Vector2(0.25f, 1f), new Vector2(220, 26), new Color(0.9f, 0.9f, 0.9f, 1f));

        GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        sliderGo.transform.SetParent(holder.transform, false);

        RectTransform srt = sliderGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0);
        srt.anchorMax = new Vector2(1, 1);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        sliderGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);
        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = new Vector2(0.05f, 0.25f);
        far.anchorMax = new Vector2(0.95f, 0.75f);
        far.offsetMin = Vector2.zero;
        far.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.35f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        RectTransform har = handleArea.GetComponent<RectTransform>();
        har.anchorMin = Vector2.zero;
        har.anchorMax = Vector2.one;
        har.offsetMin = Vector2.zero;
        har.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hndl = handle.GetComponent<RectTransform>();
        hndl.sizeDelta = new Vector2(24, 24);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.98f, 0.96f, 0.92f, 1f);

        slider.fillRect = frt;
        slider.handleRect = hndl;
        slider.targetGraphic = handleImage;
        return slider;
    }

    static Font GetBuiltinUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
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
