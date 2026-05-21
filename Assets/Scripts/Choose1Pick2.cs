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
        
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            
            if (hit.collider != null && hit.transform == transform)
            {
                isDragging = true;
            }
        }

        
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            if (targetObject != null)
            {
                Vector3 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mousePos.z = targetObject.transform.position.z; 
                targetObject.transform.position = mousePos;
            }
        }

        
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}