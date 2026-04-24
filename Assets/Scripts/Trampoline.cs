using UnityEngine;

public class Trampoline : MonoBehaviour
{
    public float bounceForce = 15f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRigidbody = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, bounceForce);
                playerRigidbody.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
            }
        }
    }
}
