using UnityEngine;
using UnityEngine.InputSystem;

public class Ingredient : MonoBehaviour
{
    private bool isPlayerInside = false;

    private void Update()
    {
        
        if (isPlayerInside && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collect();
        }
    }

    private void Collect()
    {
        GameInventoryManager manager = FindAnyObjectByType<GameInventoryManager>();
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
