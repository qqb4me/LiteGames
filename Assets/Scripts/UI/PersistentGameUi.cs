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

        [Header("Scenes")]
        [SerializeField] string mainMenuSceneName = "MainMenu";

        [Header("UI Source")]
        [SerializeField] bool preferSceneUi = true;

        [Header("Scene UI Object Names")]
        [SerializeField] string pauseButtonObjectName = "PauseButton";
        [SerializeField] string pausePanelObjectName = "PausePanel";
        [SerializeField] string pauseSettingsPanelObjectName = "PauseSettingsPanel";
        [SerializeField] string resumeButtonObjectName = "ResumeButton";
        [SerializeField] string openSettingsButtonObjectName = "SettingsButton";
        [SerializeField] string closeSettingsButtonObjectName = "BackButton";
        [SerializeField] string exitToMainMenuButtonObjectName = "MainMenuButton";
        [SerializeField] string masterVolumeSliderObjectName = "MasterVolumeSlider";
        [SerializeField] string musicVolumeSliderObjectName = "MusicVolumeSlider";
        [SerializeField] string sfxVolumeSliderObjectName = "SfxVolumeSlider";

        Canvas runtimeCanvas;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
            // Standard singleton behaviour: keep the first created instance and destroy duplicates.
            if (instance != null && instance != this)
            {
                Debug.Log($"PersistentGameUi: duplicate Awake on '{gameObject.name}' (id={gameObject.GetInstanceID()}) - destroying this, keeping existing instance id={instance.gameObject.GetInstanceID()}");
                // Keep scene canvas objects intact; remove only duplicate manager component.
                Destroy(this);
                return;
            }

            instance = this;
            Debug.Log($"PersistentGameUi: Awake on '{gameObject.name}' (id={gameObject.GetInstanceID()}) - set as singleton instance");
            DontDestroyOnLoad(gameObject);

            // Ensure there is a single EventSystem early.
            EnsureEventSystem();

            Scene activeScene = SceneManager.GetActiveScene();
            // Build runtime UI only for gameplay scenes; MainMenu should not create a persistent canvas.
            if (IsGameplayScene(activeScene) && !TryInitializeSceneUi(activeScene))
            {
                BuildRuntimeUi();
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            RefreshForScene(SceneManager.GetActiveScene());
            LogUiDiagnostics();
        }

        void LogUiDiagnostics()
        {
            var allInstances = FindObjectsOfType<PersistentGameUi>(true);
            Debug.Log($"PersistentGameUi instances found: {allInstances.Length}");
            for (int i = 0; i < allInstances.Length; i++)
            {
                var it = allInstances[i];
                Debug.Log($"  Instance[{i}] name='{it.gameObject.name}' id={it.gameObject.GetInstanceID()} scene={(it.gameObject.scene.IsValid()?it.gameObject.scene.name:"<no-scene>")}");
            }

            var pauseButtons = FindObjectsOfType<Button>(true);
            int found = 0;
            for (int i = 0; i < pauseButtons.Length; i++)
            {
                if (pauseButtons[i] != null && pauseButtons[i].name == pauseButtonObjectName)
                {
                    var b = pauseButtons[i];
                    Debug.Log($"Found PauseButton[{found}] objName='{b.name}' id={b.gameObject.GetInstanceID()} root={b.transform.root.name} scene={(b.gameObject.scene.IsValid()?b.gameObject.scene.name:"<no-scene>")} interactable={b.interactable} raycast={(b.GetComponent<Image>()?b.GetComponent<Image>().raycastTarget:false)}");
                    found++;
                }
            }
            Debug.Log($"Total Buttons named '{pauseButtonObjectName}': {found}");

            var eventSystems = FindObjectsOfType<EventSystem>(true);
            Debug.Log($"EventSystem count: {eventSystems.Length}");
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

            bool escPressed = false;
            if (Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            else
            {
                escPressed = Input.GetKeyDown(KeyCode.Escape);
            }

            if (escPressed)
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

            if (gameplayScene && !TryInitializeSceneUi(scene))
            {
                BuildRuntimeUi();
            }

            // Rebind UI elements on every scene refresh to handle scene unload/load cycles
            // (some child GameObjects can be recreated by scene load and references become invalid).
            if (gameplayScene)
            {
                RebindUiElements();
            }

            if (sceneCanvas != null)
            {
                sceneCanvas.enabled = gameplayScene;
            }

            if (gameplayScene && sceneCanvas != null && runtimeCanvas != null && runtimeCanvas != sceneCanvas)
            {
                Destroy(runtimeCanvas.gameObject);
                runtimeCanvas = null;
            }

            if (runtimeCanvas != null && runtimeCanvas != sceneCanvas)
            {
                runtimeCanvas.enabled = gameplayScene && sceneCanvas == runtimeCanvas;
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

        void RebindUiElements()
        {
            // Prefer sceneCanvas when available (scene-provided UI), otherwise runtimeCanvas
            Canvas target = sceneCanvas != null ? sceneCanvas : runtimeCanvas;
            if (target == null)
            {
                // Nothing to bind against
                return;
            }

            // Find fresh references under the target canvas
            pauseButton = FindObjectByName<Button>(target.transform, pauseButtonObjectName) ?? pauseButton;
            pausePanel = FindObjectByName<RectTransform>(target.transform, pausePanelObjectName)?.gameObject ?? pausePanel;
            settingsPanel = FindObjectByName<RectTransform>(target.transform, pauseSettingsPanelObjectName)?.gameObject ?? settingsPanel;

            if (pauseButton != null)
            {
                BindButton(pauseButton, PauseGame);
                pauseButton.gameObject.SetActive(true);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                // Rebind settings children
                masterVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, masterVolumeSliderObjectName) ?? masterVolumeSlider;
                musicVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, musicVolumeSliderObjectName) ?? musicVolumeSlider;
                sfxVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, sfxVolumeSliderObjectName) ?? sfxVolumeSlider;

                closeSettingsButton = FindObjectByName<Button>(settingsPanel.transform, closeSettingsButtonObjectName) ?? closeSettingsButton;
                if (closeSettingsButton == null)
                {
                    var btns = settingsPanel.GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        var txt = b.GetComponentInChildren<UnityEngine.UI.Text>(true);
                        if (txt != null && !string.IsNullOrWhiteSpace(txt.text) && string.Equals(txt.text.Trim(), "BACK", System.StringComparison.OrdinalIgnoreCase))
                        {
                            closeSettingsButton = b;
                            break;
                        }
                    }
                }

                BindButton(closeSettingsButton, CloseSettings);
            }

            // Rebind pause panel buttons
            if (pausePanel != null)
            {
                resumeButton = FindObjectByName<Button>(pausePanel.transform, resumeButtonObjectName) ?? resumeButton;
                openSettingsButton = FindObjectByName<Button>(pausePanel.transform, openSettingsButtonObjectName) ?? openSettingsButton;
                exitToMainMenuButton = FindObjectByName<Button>(pausePanel.transform, exitToMainMenuButtonObjectName) ?? exitToMainMenuButton;

                BindButton(resumeButton, ResumeGame);
                BindButton(openSettingsButton, OpenSettings);
                BindButton(exitToMainMenuButton, ExitToMainMenu);
            }

            // Ensure AudioSettings component has latest slider refs
            AudioSettings audioSettingsComp = gameObject.GetComponent<AudioSettings>();
            if (audioSettingsComp == null)
            {
                audioSettingsComp = gameObject.AddComponent<AudioSettings>();
            }

            if (masterVolumeSlider != null && musicVolumeSlider != null && sfxVolumeSlider != null)
            {
                audioSettingsComp.Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
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
            Debug.Log($"PauseGame called on instance '{gameObject.name}' (id={gameObject.GetInstanceID()})");
            if (pausePanel == null)
            {
                Debug.LogWarning("PauseGame: pausePanel reference is null");
            }
            else
            {
                Debug.Log($"PauseGame: pausePanel activeBefore={pausePanel.activeSelf}");
            }
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(false);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                Debug.Log($"PauseGame: pausePanel activeAfter={pausePanel.activeSelf}");
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
            // Find all EventSystems in the scene (including inactive)
            EventSystem[] systems = FindObjectsOfType<EventSystem>(true);

            if (systems != null && systems.Length > 0)
            {
                // Keep the first valid EventSystem, remove extras
                EventSystem keeper = systems[0];
                if (keeper.GetComponent<InputSystemUIInputModule>() == null)
                {
                    keeper.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                keeper.enabled = true;

                if (systems.Length > 1)
                {
                    Debug.LogWarning($"Multiple EventSystem instances found ({systems.Length}). Keeping '{keeper.gameObject.name}' and destroying others.");
                    for (int i = 1; i < systems.Length; i++)
                    {
                        var s = systems[i];
                        if (s != null && s.gameObject != null)
                        {
                            Destroy(s.gameObject);
                        }
                    }
                }

                DontDestroyOnLoad(keeper.gameObject);
                return;
            }

            // No EventSystem found — create one
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(eventSystemObject);
        }

        void BuildRuntimeUi()
        {
            // If a PersistentGameCanvas object already exists in the scene (for example from a previous
            // DontDestroyOnLoad instance), reuse it instead of creating a duplicate.
            GameObject existing = GameObject.Find("PersistentGameCanvas");
            if (existing != null)
            {
                Canvas existingCanvas = existing.GetComponent<Canvas>();
                if (existingCanvas != null)
                {
                    runtimeCanvas = existingCanvas;
                    sceneCanvas = runtimeCanvas;
                    bool isSceneObject = existing.scene.IsValid() && existing.scene == SceneManager.GetActiveScene();
                    if (!isSceneObject)
                    {
                        DontDestroyOnLoad(existing);
                    }

                    Debug.Log(isSceneObject
                        ? "BuildRuntimeUi: reusing existing scene PersistentGameCanvas"
                        : "BuildRuntimeUi: reusing existing PersistentGameCanvas");

                    // Try to find pause UI elements inside the existing canvas and bind them
                    pauseButton = FindObjectByName<Button>(runtimeCanvas.transform, pauseButtonObjectName);
                    pausePanel = FindObjectByName<RectTransform>(runtimeCanvas.transform, pausePanelObjectName)?.gameObject;
                    settingsPanel = FindObjectByName<RectTransform>(runtimeCanvas.transform, pauseSettingsPanelObjectName)?.gameObject;

                    if (pauseButton != null)
                    {
                        BindButton(pauseButton, PauseGame);
                    }

                    if (pausePanel != null)
                    {
                        pausePanel.SetActive(false);
                    }

                    if (settingsPanel != null)
                    {
                        settingsPanel.SetActive(false);
                    }

                    // Try to find other buttons inside the pause panel
                    if (pausePanel != null)
                    {
                        resumeButton = FindObjectByName<Button>(pausePanel.transform, resumeButtonObjectName);
                        openSettingsButton = FindObjectByName<Button>(pausePanel.transform, openSettingsButtonObjectName);
                        exitToMainMenuButton = FindObjectByName<Button>(pausePanel.transform, exitToMainMenuButtonObjectName);

                        BindButton(resumeButton, ResumeGame);
                        BindButton(openSettingsButton, OpenSettings);
                        BindButton(exitToMainMenuButton, ExitToMainMenu);
                    }

                    // Find sliders in settings panel and initialize AudioSettings if present
                    if (settingsPanel != null)
                    {
                        masterVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, masterVolumeSliderObjectName);
                        musicVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, musicVolumeSliderObjectName);
                        sfxVolumeSlider = FindObjectByName<Slider>(settingsPanel.transform, sfxVolumeSliderObjectName);
                        // Try find close settings button inside settings panel and bind it
                        closeSettingsButton = FindObjectByName<Button>(settingsPanel.transform, closeSettingsButtonObjectName);
                        if (closeSettingsButton == null)
                        {
                            var btns = settingsPanel.GetComponentsInChildren<Button>(true);
                            foreach (var b in btns)
                            {
                                var txt = b.GetComponentInChildren<UnityEngine.UI.Text>(true);
                                if (txt != null && !string.IsNullOrWhiteSpace(txt.text) && string.Equals(txt.text.Trim(), "BACK", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    closeSettingsButton = b;
                                    Debug.Log($"BuildRuntimeUi: found closeSettingsButton by label on '{b.gameObject.name}'");
                                    break;
                                }
                            }
                        }
                        BindButton(closeSettingsButton, CloseSettings);
                    }

                    AudioSettings audioSettingsComp = gameObject.GetComponent<AudioSettings>();
                    if (audioSettingsComp == null)
                    {
                        audioSettingsComp = gameObject.AddComponent<AudioSettings>();
                    }

                    if (masterVolumeSlider != null && musicVolumeSlider != null && sfxVolumeSlider != null)
                    {
                        audioSettingsComp.Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
                    }

                    return;
                }
            }

            if (runtimeCanvas != null)
            {
                sceneCanvas = runtimeCanvas;
                return;
            }

            GameObject canvasObject = new GameObject("PersistentGameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);

            runtimeCanvas = canvasObject.GetComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = 500;

            sceneCanvas = runtimeCanvas;

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

        bool TryInitializeSceneUi(Scene scene)
        {
            if (!preferSceneUi || !scene.IsValid() || !scene.isLoaded || string.Equals(scene.name, mainMenuSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Canvas targetCanvas = FindCanvasInScene(scene);
            if (targetCanvas == null)
            {
                return false;
            }

            Button scenePauseButton = FindObjectByName<Button>(targetCanvas.transform, pauseButtonObjectName);
            GameObject scenePausePanel = FindObjectByName<RectTransform>(targetCanvas.transform, pausePanelObjectName)?.gameObject;
            GameObject sceneSettingsPanel = FindObjectByName<RectTransform>(targetCanvas.transform, pauseSettingsPanelObjectName)?.gameObject;
            // If exact-named pause button not found, try to locate a Button under this canvas with label text "PAUSE"
            if (scenePauseButton == null)
            {
                var buttons = targetCanvas.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    var txt = b.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (txt != null && !string.IsNullOrWhiteSpace(txt.text) && string.Equals(txt.text.Trim(), "PAUSE", System.StringComparison.OrdinalIgnoreCase))
                    {
                        scenePauseButton = b;
                        Debug.Log($"TryInitializeSceneUi: found pause button by label on '{b.gameObject.name}' (id={b.gameObject.GetInstanceID()})");
                        break;
                    }
                }
            }

            if (scenePauseButton == null || scenePausePanel == null || sceneSettingsPanel == null)
            {
                if (scenePauseButton == null) Debug.LogWarning($"PauseButton not found with name '{pauseButtonObjectName}'");
                if (scenePausePanel == null) Debug.LogWarning($"PausePanel not found with name '{pausePanelObjectName}'");
                if (sceneSettingsPanel == null) Debug.LogWarning($"SettingsPanel not found with name '{pauseSettingsPanelObjectName}'");
                return false;
            }

            Button sceneResumeButton = FindObjectByName<Button>(scenePausePanel.transform, resumeButtonObjectName);
            Button sceneOpenSettingsButton = FindObjectByName<Button>(scenePausePanel.transform, openSettingsButtonObjectName);
            Button sceneCloseSettingsButton = FindObjectByName<Button>(sceneSettingsPanel.transform, closeSettingsButtonObjectName);
            Button sceneExitToMainMenuButton = FindObjectByName<Button>(scenePausePanel.transform, exitToMainMenuButtonObjectName);

            if (sceneResumeButton == null || sceneOpenSettingsButton == null || sceneCloseSettingsButton == null || sceneExitToMainMenuButton == null)
            {
                return false;
            }

            pauseButton = scenePauseButton;
            pausePanel = scenePausePanel;
            settingsPanel = sceneSettingsPanel;
            resumeButton = sceneResumeButton;
            openSettingsButton = sceneOpenSettingsButton;
            closeSettingsButton = sceneCloseSettingsButton;
            exitToMainMenuButton = sceneExitToMainMenuButton;

            masterVolumeSlider = FindObjectByName<Slider>(sceneSettingsPanel.transform, masterVolumeSliderObjectName);
            musicVolumeSlider = FindObjectByName<Slider>(sceneSettingsPanel.transform, musicVolumeSliderObjectName);
            sfxVolumeSlider = FindObjectByName<Slider>(sceneSettingsPanel.transform, sfxVolumeSliderObjectName);

            sceneCanvas = targetCanvas;

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

            if (masterVolumeSlider != null && musicVolumeSlider != null && sfxVolumeSlider != null)
            {
                audioSettings.Initialize(masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider);
            }
            else
            {
                if (masterVolumeSlider == null) Debug.LogWarning($"MasterVolumeSlider not found with name '{masterVolumeSliderObjectName}'");
                if (musicVolumeSlider == null) Debug.LogWarning($"MusicVolumeSlider not found with name '{musicVolumeSliderObjectName}'");
                if (sfxVolumeSlider == null) Debug.LogWarning($"SfxVolumeSlider not found with name '{sfxVolumeSliderObjectName}'");
            }

            return true;
        }

        static Canvas FindCanvasInScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Canvas candidate = root.GetComponentInChildren<Canvas>(true);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        static T FindObjectByName<T>(Transform root, string objectName) where T : Component
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component != null && component.name == objectName)
                {
                    return component;
                }
            }

            return null;
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                Debug.LogWarning($"BindButton: button is null");
                return;
            }

            string rootName = button.transform.root != null ? button.transform.root.name : "<no-root>";
            string sceneName = button.gameObject.scene.IsValid() ? button.gameObject.scene.name : "<no-scene>";
            Debug.Log($"BindButton: binding '{button.name}' (root={rootName}, scene={sceneName}, id={button.gameObject.GetInstanceID()})");
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
