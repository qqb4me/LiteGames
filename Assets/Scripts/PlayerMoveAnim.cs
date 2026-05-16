using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMoveAnim : MonoBehaviour
{
    public float speed = 5f;
    private Animator animator;
    private float horizontalInput;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        horizontalInput = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontalInput = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontalInput = 1;

        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }

        Vector2 movement = new Vector2(horizontalInput, 0);
        transform.Translate(movement * speed * Time.deltaTime);

        float speedX = Mathf.Abs(horizontalInput);
        animator.SetFloat("Speed", speedX);
    }
}