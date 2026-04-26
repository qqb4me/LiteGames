using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DeathMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject deathPanel;
    [SerializeField] Button respawnButton;
    [SerializeField] Button exitToMenuButton;

    [Header("Scene")]
    [SerializeField] string mainMenuSceneName = "MainMenu";

    PlayerLives playerLives;
    bool isVisible;

    void Awake()
    {
        BindButtons();
        Hide();
    }

    public void Configure(PlayerLives targetPlayerLives)
    {
        playerLives = targetPlayerLives;
    }

    public void Show()
    {
        if (isVisible)
        {
            return;
        }

        isVisible = true;

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void Hide()
    {
        isVisible = false;

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    void BindButtons()
    {
        if (respawnButton != null)
        {
            respawnButton.onClick.RemoveListener(OnRespawnPressed);
            respawnButton.onClick.AddListener(OnRespawnPressed);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveListener(OnExitToMenuPressed);
            exitToMenuButton.onClick.AddListener(OnExitToMenuPressed);
        }
    }

    void OnRespawnPressed()
    {
        if (playerLives == null)
        {
            playerLives = FindAnyObjectByType<PlayerLives>();
        }

        if (playerLives == null)
        {
            return;
        }

        Hide();
        playerLives.Respawn();
    }

    void OnExitToMenuPressed()
    {
        Hide();
        GameSession.LoadScene(mainMenuSceneName);
    }
}