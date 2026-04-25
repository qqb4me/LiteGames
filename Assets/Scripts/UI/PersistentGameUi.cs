using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheAlchemest.UI
{
    [DefaultExecutionOrder(-900)]
    public class PersistentGameUi : MonoBehaviour
    {
        static PersistentGameUi instance;

        [SerializeField] string mainMenuSceneName = "MainMenu";
        Canvas sceneCanvas;
        Button pauseButton;
        GameObject pausePanel;
        GameObject settingsPanel;
        Button resumeButton;
        Button openSettingsButton;
        Button closeSettingsButton;
        Button exitToMainMenuButton;
        Slider masterVolumeSlider;
        Slider musicVolumeSlider;
        Slider sfxVolumeSlider;

        string resumableSceneName;
        bool hasResumableScene;
        bool isPaused;

        public static bool HasResumableSession => instance != null && instance.hasResumableScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            EnsureInstance();
        }

        public static bool TryResumeFromMainMenu()
        {
            if (instance == null || !instance.hasResumableScene || string.IsNullOrWhiteSpace(instance.resumableSceneName))
            {
                return false;
            }

            string sceneToLoad = instance.resumableSceneName;
            instance.hasResumableScene = false;
            instance.resumableSceneName = null;
            instance.ForceResumeState();
            GameSession.LoadScene(sceneToLoad);
            return true;
        }

        static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            instance = FindAnyObjectByType<PersistentGameUi>();
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject(nameof(PersistentGameUi));
            instance = root.AddComponent<PersistentGameUi>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
            BuildRuntimeUi();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            RefreshForScene(SceneManager.GetActiveScene());
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ForceResumeState();
        }

        void Update()
        {
            if (!IsGameplayScene(SceneManager.GetActiveScene()))
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEventSystem();
            RefreshForScene(scene);
        }

        void RefreshForScene(Scene scene)
        {
            bool gameplayScene = IsGameplayScene(scene);

            if (sceneCanvas != null)
            {
                sceneCanvas.enabled = gameplayScene;
            }

            if (!gameplayScene)
            {
                ForceResumeState();
                return;
            }

            isPaused = false;

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(true);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        bool IsGameplayScene(Scene scene)
        {
            return scene.IsValid()
                && scene.isLoaded
                && !string.Equals(scene.name, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);
        }

        void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        void ResumeGame()
        {
            ForceResumeState();

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(true);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
        }

        void ExitToMainMenu()
        {
            Scene current = SceneManager.GetActiveScene();
            if (IsGameplayScene(current))
            {
                resumableSceneName = current.name;
                hasResumableScene = true;
            }

            ForceResumeState();
            GameSession.LoadScene(mainMenuSceneName);
        }

        void ForceResumeState()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                if (!eventSystem.enabled)
                {
                    eventSystem.enabled = true;
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        void BuildRuntimeUi()
        {
            if (sceneCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("PersistentGameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);

            sceneCanvas = canvasObject.GetComponent<Canvas>();
            sceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sceneCanvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;

            GameObject hudPanel = CreatePanel(root, "HUDPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-100f, -70f), new Vector2(180f, 90f), new Color(0f, 0f, 0f, 0f));
            pauseButton = CreateButton(hudPanel.transform, "PauseButton", "PAUSE", new Vector2(0.5f, 0.5f), new Vector2(160f, 64f), new Color(0.2f, 0.28f, 0.45f, 0.94f));

            pausePanel = CreatePanel(root, "PausePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 420f), new Color(0.08f, 0.1f, 0.15f, 0.95f));
            CreateLabel(pausePanel.transform, "PauseTitle", "PAUSED", 54, new Vector2(0.5f, 0.84f), new Vector2(440f, 72f), new Color(0.95f, 0.9f, 0.78f, 1f));

            resumeButton = CreateButton(pausePanel.transform, "ResumeButton", "CONTINUE", new Vector2(0.5f, 0.58f), new Vector2(300f, 60f), new Color(0.21f, 0.55f, 0.32f, 1f));
            openSettingsButton = CreateButton(pausePanel.transform, "SettingsButton", "SOUND", new Vector2(0.5f, 0.42f), new Vector2(300f, 60f), new Color(0.22f, 0.36f, 0.58f, 1f));
            exitToMainMenuButton = CreateButton(pausePanel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0.5f, 0.26f), new Vector2(300f, 60f), new Color(0.5f, 0.25f, 0.2f, 1f));

            settingsPanel = CreatePanel(root, "PauseSettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f), new Color(0.1f, 0.12f, 0.18f, 0.97f));
            CreateLabel(settingsPanel.transform, "SettingsTitle", "SOUND SETTINGS", 42, new Vector2(0.5f, 0.88f), new Vector2(560f, 70f), new Color(0.95f, 0.9f, 0.78f, 1f));
            closeSettingsButton = CreateButton(settingsPanel.transform, "BackButton", "BACK", new Vector2(0.84f, 0.88f), new Vector2(130f, 48f), new Color(0.45f, 0.22f, 0.22f, 1f));

            masterVolumeSlider = CreateSlider(settingsPanel.transform, "MasterVolumeSlider", "Master Volume", new Vector2(0.5f, 0.62f), new Vector2(480f, 26f));
            musicVolumeSlider = CreateSlider(settingsPanel.transform, "MusicVolumeSlider", "Music Volume", new Vector2(0.5f, 0.46f), new Vector2(480f, 26f));
            sfxVolumeSlider = CreateSlider(settingsPanel.transform, "SfxVolumeSlider", "SFX Volume", new Vector2(0.5f, 0.30f), new Vector2(480f, 26f));

            pausePanel.SetActive(false);
            settingsPanel.SetActive(false);

            BindButton(pauseButton, PauseGame);
            BindButton(resumeButton, ResumeGame);
            BindButton(openSettingsButton, OpenSettings);
            BindButton(closeSettingsButton, CloseSettings);
            BindButton(exitToMainMenuButton, ExitToMainMenu);

            AudioSettings audioSettings = gameObject.GetComponent<AudioSettings>();
            if (audioSettings == null)
            {
                audioSettings = gameObject.AddComponent<AudioSettings>();
            }

            audioSettings.Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
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

            RectTransform holderRect = holder.GetComponent<RectTransform>();
            holderRect.anchorMin = anchor;
            holderRect.anchorMax = anchor;
            holderRect.anchoredPosition = Vector2.zero;
            holderRect.sizeDelta = size;

            CreateLabel(holder.transform, "Label", labelText, 18, new Vector2(0.25f, 1f), new Vector2(220f, 26f), new Color(0.9f, 0.9f, 0.9f, 1f));

            GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(holder.transform, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            sliderObject.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.05f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(0.95f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.35f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.98f, 0.96f, 0.92f, 1f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
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
    }
}
