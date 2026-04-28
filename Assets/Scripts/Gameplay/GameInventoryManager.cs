using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class GameInventoryManager : MonoBehaviour
{
    public int totalIngredients = 5;
    private int _collectedCount = 0;
    private bool _canTransition = false;

    [Header("UI Settings")]
    public TextMeshProUGUI counterText;

    [Header("Scene Settings")]
    public string puzzleSceneName = "PuzzleScene";

    void Start() => UpdateUI();

    void Update()
    {
        // Если всё собрано, игрок может нажать Enter (или другую кнопку), чтобы уйти на уровень с пазлом
        if (_canTransition && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(puzzleSceneName);
        }
    }

    public void AddIngredient()
    {
        _collectedCount++;
        UpdateUI();

        if (_collectedCount >= totalIngredients)
        {
            _canTransition = true;
            OnAllCollected();
        }
    }

    void UpdateUI()
    {
        if (counterText != null)
            counterText.text = _collectedCount + " / " + totalIngredients;
    }

    void OnAllCollected()
    {
        if (counterText != null)
            counterText.text = "Все собрано! Нажми Enter";
        
        Debug.Log("Все предметы у игрока. Можно переходить к головоломке.");
    }

    public bool AreAllIngredientsCollected()
    {
        return _collectedCount >= totalIngredients;
    }
}
