using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveAnim : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    Animator animator;
    float horizontalInput;

    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        // Deprecated: movement and animation are now driven by PlayerMovement and PlayerAnimationController.
        // This script is kept only to avoid breaking existing scene references.
    }
}