using UnityEngine;

[DisallowMultipleComponent]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform leftPoint;
    [SerializeField] Transform rightPoint;
    [SerializeField] float speed = 2.5f;

    [Header("Visual")]
    [SerializeField] bool autoFlipSprite = true;
    [SerializeField] bool useSpriteRendererFlipX = true;

    [Header("Damage")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] int contactDamage = 1;
    [SerializeField] float contactCooldown = 0.2f;

    float lastDamageTime;
    bool movingToRight = true;
    SpriteRenderer cachedSpriteRenderer;

    void Awake()
    {
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        if (leftPoint != null && rightPoint != null)
        {
            movingToRight = transform.position.x <= rightPoint.position.x;
            UpdateVisualDirection();
        }
    }

    void Reset()
    {
        speed = 2.5f;
        contactDamage = 1;
        contactCooldown = 0.2f;
        playerTag = "Player";
    }

    void Update()
    {
        if (leftPoint == null || rightPoint == null)
        {
            return;
        }

        Vector3 target = movingToRight ? rightPoint.position : leftPoint.position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= 0.02f)
        {
            movingToRight = !movingToRight;
            UpdateVisualDirection();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void TryDamage(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        PlayerLives playerLives = target.GetComponent<PlayerLives>();
        if (playerLives == null)
        {
            playerLives = target.GetComponentInParent<PlayerLives>();
        }

        if (playerLives == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && !playerLives.gameObject.CompareTag(playerTag))
        {
            return;
        }

        if (Time.time < lastDamageTime + contactCooldown)
        {
            return;
        }

        playerLives.TakeDamage(contactDamage, transform.position);
        lastDamageTime = Time.time;
    }

    void UpdateVisualDirection()
    {
        if (!autoFlipSprite)
        {
            return;
        }

        if (cachedSpriteRenderer == null)
        {
            cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (cachedSpriteRenderer == null)
            {
                return;
            }
        }

        bool faceRight = movingToRight;

        if (useSpriteRendererFlipX)
        {
            cachedSpriteRenderer.flipX = !faceRight;
            return;
        }

        Vector3 localScale = cachedSpriteRenderer.transform.localScale;
        float absX = Mathf.Abs(localScale.x);
        localScale.x = faceRight ? absX : -absX;
        cachedSpriteRenderer.transform.localScale = localScale;
    }

    void OnDrawGizmosSelected()
    {
        if (leftPoint == null || rightPoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(leftPoint.position, rightPoint.position);
        Gizmos.DrawSphere(leftPoint.position, 0.12f);
        Gizmos.DrawSphere(rightPoint.position, 0.12f);
    }
}