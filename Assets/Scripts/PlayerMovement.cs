using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Serializable]
    public class SavedState
    {
        public Vector2 position;
        public Vector2 velocity;
        public bool facingRight;

        public float moveSpeed;
        public float acceleration;
        public float deceleration;
        public float jumpForce;
        public int extraJumps;
        public float coyoteTime;
        public float jumpBufferTime;
        public float groundCheckRadius;

        public int jumpsLeft;
        public float coyoteTimeCounter;
        public float jumpBufferCounter;
    }

    const string StateKey = "player_movement";

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Jump")]
    public float jumpForce = 15f;
    public int extraJumps = 0;
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

    void Start()
    {
        RestoreState();
        ApplyPendingSpawnPoint();
    }

    void OnDisable()
    {
        SaveState();
    }

    void OnDestroy()
    {
        SaveState();
    }

    void Update()
    {
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

        horizontalInput = Mathf.Abs(padDir) > 0.05f ? Mathf.Clamp(padDir, -1f, 1f) : Mathf.Clamp(kbDir, -1f, 1f);

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

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

        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f || jumpsLeft > 0)
            {
                DoJump();
                jumpBufferCounter = 0f;
            }
        }

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

    void SaveState()
    {
        if (!GameSession.HasInstance)
        {
            return;
        }

        SavedState state = new SavedState
        {
            position = rb != null ? rb.position : (Vector2)transform.position,
            velocity = rb != null ? rb.linearVelocity : Vector2.zero,
            facingRight = facingRight,
            moveSpeed = moveSpeed,
            acceleration = acceleration,
            deceleration = deceleration,
            jumpForce = jumpForce,
            extraJumps = extraJumps,
            coyoteTime = coyoteTime,
            jumpBufferTime = jumpBufferTime,
            groundCheckRadius = groundCheckRadius,
            jumpsLeft = jumpsLeft,
            coyoteTimeCounter = coyoteTimeCounter,
            jumpBufferCounter = jumpBufferCounter
        };

        GameSession.Instance.SaveState(StateKey, state);
    }

    void RestoreState()
    {
        if (!GameSession.HasInstance)
        {
            return;
        }

        if (!GameSession.Instance.TryLoadState(StateKey, out SavedState state))
        {
            return;
        }

        moveSpeed = state.moveSpeed;
        acceleration = state.acceleration;
        deceleration = state.deceleration;
        jumpForce = state.jumpForce;
        extraJumps = state.extraJumps;
        coyoteTime = state.coyoteTime;
        jumpBufferTime = state.jumpBufferTime;
        groundCheckRadius = state.groundCheckRadius;

        facingRight = state.facingRight;
        jumpsLeft = state.jumpsLeft;
        coyoteTimeCounter = state.coyoteTimeCounter;
        jumpBufferCounter = state.jumpBufferCounter;

        if (rb != null)
        {
            rb.position = state.position;
            rb.linearVelocity = state.velocity;
        }

        transform.position = state.position;

        float scaleX = Mathf.Abs(transform.localScale.x);
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? scaleX : -scaleX;
        transform.localScale = scale;
    }

    void ApplyPendingSpawnPoint()
    {
        if (!GameSession.HasInstance)
        {
            return;
        }

        if (!GameSession.Instance.TryConsumePendingSpawnPoint(out string spawnPointId))
        {
            return;
        }

        if (!SceneSpawnPoint.TryGetSpawnPosition(spawnPointId, out Vector3 spawnPosition))
        {
            return;
        }

        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
        }

        transform.position = spawnPosition;
    }
}