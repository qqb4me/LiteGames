using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private float rotationAngle = 90f;
    [SerializeField] private float rotationDuration = 0.2f;

    private Quaternion targetRotation;
    private bool isRotating = false;
    private float rotationProgress = 0f;
    private Quaternion startRotation;

    private GameObject selectedBlock = null;
    private bool isMoving = false;
    private Vector3 offset;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleBlockSelection();
        HandleBlockMovement();

        if (selectedBlock == null) return;
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateLeft();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateRight();
        }
    }

    void HandleBlockSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                if (selectedBlock == hitObject)
                {
                    selectedBlock = null;
                    isMoving = false;
                }
                else
                {
                    selectedBlock = hitObject;
                    CalculateOffset();
                    isMoving = true;
                }
            }
            else
            {
                selectedBlock = null;
                isMoving = false;
            }
        }
    }

    void HandleBlockMovement()
    {
        if (selectedBlock != null && isMoving)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, selectedBlock.transform.position);
            float distance;

            if (plane.Raycast(ray, out distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance);
                selectedBlock.transform.position = targetPosition + offset;
            }
        }
    }

    void CalculateOffset()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, selectedBlock.transform.position);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            offset = selectedBlock.transform.position - hitPoint;
        }
    }

    void RotateLeft()
    {
        startRotation = selectedBlock.transform.rotation;
        targetRotation = selectedBlock.transform.rotation * Quaternion.Euler(0, -rotationAngle, 0);
        StartCoroutine(RotateCoroutine());
    }

    void RotateRight()
    {
        startRotation = selectedBlock.transform.rotation;
        targetRotation = selectedBlock.transform.rotation * Quaternion.Euler(0, rotationAngle, 0);
        StartCoroutine(RotateCoroutine());
    }

    System.Collections.IEnumerator RotateCoroutine()
    {
        isRotating = true;
        rotationProgress = 0f;

        while (rotationProgress < 1f)
        {
            rotationProgress += Time.deltaTime / rotationDuration;
            selectedBlock.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationProgress);
            yield return null;
        }

        selectedBlock.transform.rotation = targetRotation;
        isRotating = false;
    }
}