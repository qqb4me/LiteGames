using UnityEngine;

public class MoveOnApproach : MonoBehaviour
{
    public Vector2 moveDirection = new Vector2(5f, 0f);
    public float moveSpeed = 10f;
    public float destroyDelay = 2f;

    private bool isMoving = false;

    void Start()
    {
        isMoving = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isMoving = true;
            Destroy(gameObject, destroyDelay);
        }
    }

    void Update()
    {
        if (isMoving)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        }
    }
}