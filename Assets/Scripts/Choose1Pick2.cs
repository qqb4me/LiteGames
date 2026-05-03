using UnityEngine;
using UnityEngine.InputSystem;

public class DragObject2D : MonoBehaviour
{
    [Header("Объект, который перемещается:")]
    public GameObject targetObject;

    private bool isDragging = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Проверяем момент нажатия
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // Если попали по этому объекту — начинаем тащить
            if (hit.collider != null && hit.transform == transform)
            {
                isDragging = true;
            }
        }

        // 2. Пока кнопка ЗАЖАТА и мы в режиме перетаскивания
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            if (targetObject != null)
            {
                Vector3 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = targetObject.transform.position.z; // Сохраняем Z
                targetObject.transform.position = mousePos;
            }
        }

        // 3. Если кнопку ОТПУСТИЛИ — прекращаем тащить
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}