using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator Parameters")]
    [SerializeField] string isMovingParameter = "isMoving";
    [SerializeField] string isGroundedParameter = "isGrounded";
    [SerializeField] float moveSpeedThreshold = 0.1f;
    [SerializeField] string speedParameter = "Speed";
    
    [Header("Sprite Alignment")]
    [SerializeField] bool alignFrames = true;
    [SerializeField] float alignSpeed = 10f; // units per second
    [SerializeField] float alignYOffset = 0f; // additive offset if needed
    [SerializeField] bool autoComputeAlignment = true;
    
    Animator animator;
    Rigidbody2D rb;
    PlayerMovement movement;

    int isMovingHash;
    int isGroundedHash;
    int speedHash;
    bool hasIsMovingParameter;
    bool hasIsGroundedParameter;
    bool hasSpeedParameter;

    SpriteRenderer spriteRenderer;
    float currentAlignY = 0f;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        if (animator == null)
        {
            Debug.LogError($"PlayerAnimationController: Animator was not found on {name} or its children.", this);
            enabled = false;
            return;
        }

        isMovingHash = Animator.StringToHash(isMovingParameter);
        isGroundedHash = Animator.StringToHash(isGroundedParameter);
        speedHash = Animator.StringToHash(speedParameter);

        // find sprite renderer on animator child
        spriteRenderer = animator.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = animator.GetComponentInChildren<SpriteRenderer>(true);

        // auto-compute alignment offset based on Collider2D bottom and sprite bottom
        if (alignFrames && autoComputeAlignment && spriteRenderer != null)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
                col = GetComponentInChildren<Collider2D>(true);

            if (col != null && spriteRenderer.sprite != null)
            {
                float colliderBottomWorld = col.bounds.min.y;
                Vector3 spriteBottomWorldPt = spriteRenderer.transform.TransformPoint(spriteRenderer.sprite.bounds.min);
                float spriteBottomWorld = spriteBottomWorldPt.y;

                float deltaWorld = colliderBottomWorld - spriteBottomWorld;
                // convert world delta to local delta relative to sprite's parent
                Transform parentOfSprite = spriteRenderer.transform.parent != null ? spriteRenderer.transform.parent : transform;
                Vector3 worldDelta = new Vector3(0f, deltaWorld, 0f);
                Vector3 localDelta = parentOfSprite.InverseTransformVector(worldDelta);

                float targetLocalY = spriteRenderer.transform.localPosition.y + localDelta.y;
                // alignYOffset used in LateUpdate: targetLocalY = -spriteBottom + alignYOffset
                float spriteBottomLocal = spriteRenderer.sprite.bounds.min.y;
                alignYOffset = targetLocalY + spriteBottomLocal;

                // choose a reasonable alignSpeed to avoid instant snapping; larger sprites may need larger speed
                alignSpeed = Mathf.Max(8f, Mathf.Abs(deltaWorld) * 30f);
                Debug.Log($"[PlayerAnimationController] Auto computed alignYOffset={alignYOffset:F3}, alignSpeed={alignSpeed:F1}");
            }
        }

        CacheParameterAvailability();
    }

    void Update()
    {
        if (animator == null || rb == null)
        {
            return;
        }

        float rawSpeed = Mathf.Abs(rb.linearVelocity.x);
        float normalizedSpeed = GetNormalizedMoveSpeed(rawSpeed);
        bool isMoving = rawSpeed > moveSpeedThreshold;
        bool isGrounded = ComputeIsGrounded();

        if (hasSpeedParameter)
        {
            animator.SetFloat(speedHash, normalizedSpeed);
        }

        if (hasIsMovingParameter)
        {
            animator.SetBool(isMovingHash, isMoving);
        }

        if (hasIsGroundedParameter)
        {
            animator.SetBool(isGroundedHash, isGrounded);
        }
    }

    bool ComputeIsGrounded()
    {
        // If moving upward or just started jumping, consider airborne
        if (rb.linearVelocity.y > 0.01f)
        {
            return false;
        }

        // Check if touching ground using PlayerMovement's ground check
        if (movement != null && movement.groundCheck != null)
        {
            return Physics2D.OverlapCircle(movement.groundCheck.position, movement.groundCheckRadius, movement.groundLayer);
        }

        return false;
    }

    float GetNormalizedMoveSpeed(float rawSpeed)
    {
        float maxSpeed = movement != null ? movement.moveSpeed : 0f;
        if (maxSpeed <= 0.0001f)
        {
            return rawSpeed > moveSpeedThreshold ? 1f : 0f;
        }

        float normalized = rawSpeed / maxSpeed;
        return normalized > moveSpeedThreshold ? Mathf.Clamp01(normalized) : 0f;
    }

    void CacheParameterAvailability()
    {
        hasIsMovingParameter = false;
        hasIsGroundedParameter = false;
        hasSpeedParameter = false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter.name == isMovingParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsMovingParameter = true;
            }

            if (parameter.name == isGroundedParameter && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasIsGroundedParameter = true;
            }

            if (parameter.name == speedParameter && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
            }
        }

        if (!hasIsMovingParameter)
        {
            Debug.LogWarning($"PlayerAnimationController: Bool parameter '{isMovingParameter}' not found in Animator on {name}.", this);
        }

        if (!hasIsGroundedParameter)
        {
            Debug.LogWarning($"PlayerAnimationController: Bool parameter '{isGroundedParameter}' not found in Animator on {name}.", this);
        }
        
        // initialize alignment
        if (alignFrames && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            currentAlignY = spriteRenderer.transform.localPosition.y;
        }
    }

    void LateUpdate()
    {
        if (!alignFrames || spriteRenderer == null || spriteRenderer.sprite == null) return;

        float spriteBottom = spriteRenderer.sprite.bounds.min.y;
        float targetLocalY = -spriteBottom + alignYOffset;

        // smooth approach
        float maxDelta = alignSpeed * Time.deltaTime;
        float newY = Mathf.MoveTowards(spriteRenderer.transform.localPosition.y, targetLocalY, maxDelta);
        Vector3 lp = spriteRenderer.transform.localPosition;
        lp.y = newY;
        spriteRenderer.transform.localPosition = lp;
    }
}
