using UnityEngine;
using TMPro;

public class GameInventoryManager : MonoBehaviour
{
    public int totalIngredients = 5;
    private int _collectedCount = 0;

    [Header("UI Settings")]
    public TextMeshProUGUI counterText;

    void Start() => UpdateUI();

    public void AddIngredient()
    {
        _collectedCount++;
        UpdateUI();

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
        if (counterText != null)
            counterText.text = "Всё готово для варки зелья";
        
        Debug.Log("Все предметы у игрока. Всё готово для варки зелья.");
    }

    public bool AreAllIngredientsCollected()
    {
        return _collectedCount >= totalIngredients;
    }
}
