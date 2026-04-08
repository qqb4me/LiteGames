using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Jump")]
    public float jumpForce = 15f;
    public int extraJumps = 0; // allow double jump when >0
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    Rigidbody2D rb;
    float horizontalInput;
    bool facingRight = true;

    // state
    int jumpsLeft;
    float coyoteTimeCounter;
    float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = extraJumps;
    }

    void Update()
    {
        // Read input via New Input System (keyboard + gamepad). If both present, gamepad stick takes priority.
        float kbDir = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kbDir -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kbDir += 1f;
        }

        float padDir = 0f;
        if (Gamepad.current != null)
        {
            padDir = Gamepad.current.leftStick.ReadValue().x;
        }

        // prefer gamepad stick when it's active, otherwise keyboard
        horizontalInput = Mathf.Abs(padDir) > 0.05f ? Mathf.Clamp(padDir, -1f, 1f) : Mathf.Clamp(kbDir, -1f, 1f);

        // jump buffering using new input
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // ground check
        bool isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpsLeft = extraJumps;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // jump when buffered and allowed
        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f || jumpsLeft > 0)
            {
                DoJump();
                jumpBufferCounter = 0f;
            }
        }

        // flip sprite
        if (horizontalInput > 0.1f && !facingRight) Flip();
        else if (horizontalInput < -0.1f && facingRight) Flip();
    }

    void FixedUpdate()
    {
        float targetVel = horizontalInput * moveSpeed;
        float accel = Mathf.Abs(targetVel) > 0.01f ? acceleration : deceleration;
        float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetVel, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);
    }

    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (coyoteTimeCounter <= 0f) jumpsLeft--;
        coyoteTimeCounter = 0f;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x = -s.x;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}