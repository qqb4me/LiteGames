using UnityEngine;
using UnityEngine.InputSystem;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private float rotationAngle = 90f;
    [SerializeField] private float rotationDuration = 0.2f;

    private PuzzlePiece _piece;

    void Start()
    {
        _piece = GetComponent<PuzzlePiece>();
    }

    void Update()
    {
        if (_piece == null)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
        {
            _piece.RotateClockwise();
        }
    }
}