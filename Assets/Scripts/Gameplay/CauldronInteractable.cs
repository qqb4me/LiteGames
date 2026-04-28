using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CauldronInteractable : MonoBehaviour
{
    private GameInventoryManager _inventoryManager;
    private bool _isPlayerInside = false;
    private bool _isUnlocked = false;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "PuzzleScene";

    void Start()
    {
        _inventoryManager = FindObjectOfType<GameInventoryManager>();
    }

    void Update()
    {
        if (_inventoryManager != null)
        {
            _isUnlocked = _inventoryManager.AreAllIngredientsCollected();
        }

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
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"Сцена '{targetSceneName}' не добавлена в Build Settings. Добавь её в File -> Build Profiles или подставь сцену-заглушку.");
            return;
        }

        Debug.Log("Переход к головоломке через котелок!");
        SceneManager.LoadScene(targetSceneName);
    }
}
