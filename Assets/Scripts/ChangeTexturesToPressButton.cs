using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleKeyPressTexture : MonoBehaviour
{
    public Key key = Key.E;
    public int pressesNeeded = 5;
    public Sprite newSprite;
    public Sprite originalSprite;

    private int pressCount = 0;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
        {
            pressCount++;
            Debug.Log($"Нажатий: {pressCount}/{pressesNeeded}");

            if (pressCount >= pressesNeeded)
            {
                if (spriteRenderer != null && newSprite != null)
                {
                    spriteRenderer.sprite = newSprite;
                    Debug.Log("Спрайт изменен!");
                }
            }
        }
    }
}