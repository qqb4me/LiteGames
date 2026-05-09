using UnityEngine;
using TMPro;

public class GameInventoryManager : MonoBehaviour
{
    public int totalIngredients = 5;
    private int _collectedCount = 0;
    public event System.Action<int> OnCollectedChanged;

    public int CollectedCount => _collectedCount;

    [Header("UI Settings")]
    public bool showAllCollectedMessage = true;
    public string allCollectedMessage = "Всё готово для варки зелья";

    [Header("UI Settings")]
    public TextMeshProUGUI counterText;

    void Start() => UpdateUI();

    public void AddIngredient()
    {
        _collectedCount++;
        UpdateUI();

        OnCollectedChanged?.Invoke(_collectedCount);

        if (_collectedCount >= totalIngredients)
        {
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
        if (counterText != null && showAllCollectedMessage)
            counterText.text = allCollectedMessage;

        Debug.Log("Все предметы у игрока. " + (showAllCollectedMessage ? allCollectedMessage : "(сообщение отключено)."));
    }

    public bool AreAllIngredientsCollected()
    {
        return _collectedCount >= totalIngredients;
    }
}
