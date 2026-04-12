using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class CloudMovement : MonoBehaviour
{
    public string playerTag = "Player";
    public float lowerDistance = 1.5f;
    public float lowerSpeed = 1f;
    public float raiseSpeed = 1.5f;
    public bool raiseWhenLeft = true;
    [Tooltip("Высота зоны проверки над платформой")]
    public float topCheckHeight = 0.08f;
    [Tooltip("Внутренний отступ по краям платформы для проверки")]
    public float topCheckInset = 0.05f;

    Rigidbody2D rb;
    BoxCollider2D bc;
    Vector2 startPos;
    Vector2 targetPos;
    Vector2 prevPos;
    Rigidbody2D standingPlayer;

    void Awake()
    {
        bc = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        bc.isTrigger = false;

        startPos = (Vector2)transform.position;
        targetPos = startPos - new Vector2(0f, lowerDistance);
        prevPos = startPos;
    }

    void FixedUpdate()
    {
        standingPlayer = FindPlayerStandingOnTop();
        bool hasPlayerOnTop = standingPlayer != null;

        Vector2 desired = hasPlayerOnTop ? targetPos : (raiseWhenLeft ? startPos : (Vector2)transform.position);
        float speed = hasPlayerOnTop ? lowerSpeed : raiseSpeed;

        Vector2 next = Vector2.MoveTowards(rb.position, desired, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        Vector2 delta = next - prevPos;
        if (standingPlayer != null)
        {
            // Do not override jump ascent; carry only while player is not moving upward.
            if (standingPlayer.linearVelocity.y <= 0.01f)
            {
                Vector2 projectedPlayerPos = standingPlayer.position + standingPlayer.linearVelocity * Time.fixedDeltaTime;
                projectedPlayerPos.y += delta.y;
                standingPlayer.MovePosition(projectedPlayerPos);
            }
        }

        prevPos = next;
        transform.rotation = Quaternion.identity;
    }

    Rigidbody2D GetRootRigidbody(Collider2D col)
    {
        if (col == null) return null;
        if (col.attachedRigidbody != null) return col.attachedRigidbody;
        return col.GetComponentInParent<Rigidbody2D>();
    }

    bool IsPlayerRoot(Rigidbody2D rootRb)
    {
        return rootRb != null && rootRb.gameObject.CompareTag(playerTag);
    }

    Rigidbody2D FindPlayerStandingOnTop()
    {
        Bounds b = bc.bounds;
        float width = Mathf.Max(0.01f, b.size.x - topCheckInset * 2f);
        float height = Mathf.Max(0.01f, topCheckHeight);
        Vector2 center = new Vector2(b.center.x, b.max.y + height * 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(width, height), 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit == bc) continue;

            Rigidbody2D root = GetRootRigidbody(hit);
            if (IsPlayerRoot(root))
            {
                return root;
            }
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (bc == null) bc = GetComponent<BoxCollider2D>();
        if (bc == null) return;

        Bounds b = bc.bounds;
        float width = Mathf.Max(0.01f, b.size.x - topCheckInset * 2f);
        float height = Mathf.Max(0.01f, topCheckHeight);
        Vector3 center = new Vector3(b.center.x, b.max.y + height * 0.5f, transform.position.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0f));

        Gizmos.color = Color.cyan;
        Vector3 basePos = Application.isPlaying ? (Vector3)startPos : transform.position;
        Gizmos.DrawLine(basePos, basePos - new Vector3(0f, lowerDistance, 0f));
    }
}