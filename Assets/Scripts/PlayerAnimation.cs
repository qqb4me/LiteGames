using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private string moveXParam = "moveX";
    [SerializeField] private string moveYParam = "moveY";
    [SerializeField] private float transitionSpeed = 5f;

    private float currentMoveX = 0f;
    private float currentMoveY = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        float targetX = 0f;
        if (Keyboard.current.dKey.isPressed) targetX = 1f;
        else if (Keyboard.current.aKey.isPressed) targetX = -1f;

        float targetY = 0f;
        if (Keyboard.current.spaceKey.isPressed) targetY = 1f;

        currentMoveX = Mathf.MoveTowards(currentMoveX, targetX, transitionSpeed * Time.deltaTime);
        currentMoveY = Mathf.MoveTowards(currentMoveY, targetY, transitionSpeed * Time.deltaTime);

        animator.SetFloat(moveXParam, currentMoveX);
        animator.SetFloat(moveYParam, currentMoveY);
    }
}