using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float speed = 10f;
    public float accel = 50f;
    public float decel = 50f;
    public int jumps = 2;
    public float jForce = 15f;
    public LayerMask gLayer;
    public Transform gCheck;
    public float gRad = 0.3f;
    public float fallM = 2.5f;
    public float lowJM = 2f;
    public SpriteRenderer sr;

    Rigidbody2D rb;
    float dirX;
    bool isGnd;
    int jCount;

    InputAction moveAct;
    InputAction jumpAct;
    Collider2D[] results = new Collider2D[5];

    void OnEnable()
    {
        var map = new InputActionMap("P");
        moveAct = map.AddAction("M", binding: "<Keyboard>/d");
        moveAct.AddBinding("<Keyboard>/a").WithProcessor("Scale(factor=-1)");
        jumpAct = map.AddAction("J", binding: "<Keyboard>/space");
        moveAct.Enable();
        jumpAct.Enable();
    }

    void OnDisable()
    {
        moveAct.Disable();
        jumpAct.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        dirX = moveAct.ReadValue<float>();

        if (dirX > 0 && sr != null) sr.flipX = false;
        else if (dirX < 0 && sr != null) sr.flipX = true;

        int numColliders = Physics2D.OverlapCircleNonAlloc(gCheck.position, gRad, results, gLayer);
        isGnd = false;

        for (int i = 0; i < numColliders; i++)
        {
            if (results[i].gameObject != gameObject)
            {
                isGnd = true;
                break;
            }
        }

        if (isGnd && rb.linearVelocity.y <= 0.05f)
        {
            jCount = jumps;
        }

        if (jumpAct.WasPressedThisFrame() && jCount > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jForce);
            jCount--;
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallM - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpAct.IsPressed())
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJM - 1) * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        float targetX = dirX * speed;
        float rate = (Mathf.Abs(targetX) > 0.01f) ? accel : decel;
        float moveX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
    }

    void OnDrawGizmos()
    {
        if (gCheck != null)
        {
            Gizmos.color = isGnd ? Color.green : Color.red;
            Gizmos.DrawWireSphere(gCheck.position, gRad);
        }
    }
}