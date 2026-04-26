using UnityEngine;

public class MoveOnDistance : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;
    public Vector2 moveDirection = new Vector2(5f, 0f);
    public float moveSpeed = 10f;
    public float destroyDelay = 2f;

    private bool isMoving = false;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void Update()
    {
        if (!isMoving && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= activationDistance)
            {
                isMoving = true;
                Destroy(gameObject, destroyDelay);
            }
        }

        if (isMoving)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        }
    }
}