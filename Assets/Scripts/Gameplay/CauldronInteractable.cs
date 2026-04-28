using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CauldronInteractable : MonoBehaviour
{
    private GameInventoryManager _inventoryManager;
    private bool _isPlayerInside = false;
    private bool _isUnlocked = false;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "PuzzleScene";

    void Start()
    {
        _inventoryManager = FindObjectOfType<GameInventoryManager>();
    }

    void Update()
    {
        // Проверяем, разблокирован ли котелок (все ингредиенты собраны)
        if (_inventoryManager != null)
        {
            _isUnlocked = _inventoryManager.AreAllIngredientsCollected();
        }

        // Если игрок рядом, котелок разблокирован и нажата кнопка E
        if (_isPlayerInside && _isUnlocked && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Interact();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = false;
        }
    }

    void Interact()
    {
        Debug.Log("Переход к головоломке через котелок!");
        SceneManager.LoadScene(targetSceneName);
    }
}
