using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public float gridSizePixels = 100f;
    public float pixelsPerUnit = 100f;
    public Vector2 gridOffset = Vector2.zero;

    private Vector3 offset;
    private float zCoord;

    void OnMouseDown()
    {
        zCoord = transform.position.z;
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseDrag()
    {
        Vector3 newPos = GetMouseWorldPos() + offset;
        transform.position = new Vector3(newPos.x, newPos.y, zCoord);
    }

    void OnMouseUp()
    {
        float worldGridSize = gridSizePixels / pixelsPerUnit;

        float x = Mathf.Round((transform.position.x - gridOffset.x) / worldGridSize) * worldGridSize + gridOffset.x;
        float y = Mathf.Round((transform.position.y - gridOffset.y) / worldGridSize) * worldGridSize + gridOffset.y;

        transform.position = new Vector3(x, y, zCoord);
    }
private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        mousePoint.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}