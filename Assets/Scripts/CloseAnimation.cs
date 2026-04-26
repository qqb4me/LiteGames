using UnityEngine;

public class ActivateAnimationOnDistance : MonoBehaviour
{
    public Transform player;
    public float activationDistance = 3f;

    private Animator animator;
    private bool isActivated = false;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    void Update()
    {
        if (isActivated) return;

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= activationDistance)
        {
            isActivated = true;

            if (animator != null)
            {
                animator.enabled = true;
            }
        }
    }
}