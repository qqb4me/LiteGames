using UnityEngine;
using System.Collections;

public class ProximityReveal2D : MonoBehaviour
{
    [Header("Настройки игрока")]
    public Transform playerTransform;

    [Header("Настройки видимости")]
    public float revealDistance = 5f;
    public float revealDelay = 1.5f;
    [Range(0f, 1f)]
    public float initialAlpha = 0.2f;

    [Header("Анимация проявления")]
    public bool smoothReveal = true;
    public float smoothRevealDuration = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isRevealed = false;
    private bool isRevealing = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            enabled = false;
            return;
        }

        originalColor = spriteRenderer.color;
        SetTransparency(initialAlpha);

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (!isRevealed && distance <= revealDistance)
        {
            if (!isRevealing)
                StartCoroutine(RevealCoroutine());
        }
        else if (isRevealed && distance > revealDistance)
        {
            if (!isRevealing)
                StartCoroutine(HideCoroutine());
        }
    }

    IEnumerator RevealCoroutine()
    {
        if (isRevealing) yield break;

        isRevealing = true;
        yield return new WaitForSeconds(revealDelay);

        if (smoothReveal)
        {
            float elapsed = 0f;
            float startAlpha = spriteRenderer.color.a;
            float targetAlpha = 1f;

            while (elapsed < smoothRevealDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / smoothRevealDuration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetTransparency(newAlpha);
                yield return null;
            }
        }

        SetTransparency(1f);
        isRevealed = true;
        isRevealing = false;
    }

    IEnumerator HideCoroutine()
    {
        if (isRevealing) yield break;

        isRevealing = true;

        if (smoothReveal)
        {
            float elapsed = 0f;
            float startAlpha = spriteRenderer.color.a;
            float targetAlpha = initialAlpha;

            while (elapsed < smoothRevealDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / smoothRevealDuration;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetTransparency(newAlpha);
                yield return null;
            }
        }

        SetTransparency(initialAlpha);
        isRevealed = false;
        isRevealing = false;
    }

    void SetTransparency(float alpha)
    {
        if (spriteRenderer == null) return;
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, revealDistance);
    }
}