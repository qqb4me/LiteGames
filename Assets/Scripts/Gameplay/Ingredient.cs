using UnityEngine;
using UnityEngine.InputSystem;

public class Ingredient : MonoBehaviour
{
    private bool isPlayerInside = false;

    private void Update()
    {
        // Если игрок в зоне триггера и нажал нужную кнопку
        if (isPlayerInside && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collect();
        }
    }

    private void Collect()
    {
        GameInventoryManager manager = FindObjectOfType<GameInventoryManager>();
        if (manager != null)
        {
            manager.AddIngredient();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            // Тут можно добавить появление подсказки "Нажми E", если захочешь
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}
