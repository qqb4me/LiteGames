using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLives : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField] int maxLives = 3;
    [SerializeField] float invulnerabilityDuration = 0.7f;

    [Header("Hit Feedback")]
    [SerializeField] bool enableKnockback = true;
    [SerializeField] float knockbackHorizontalSpeed = 6f;
    [SerializeField] float knockbackVerticalSpeed = 3.5f;

    [Header("UI: Text (optional)")]
    [SerializeField] Text livesText;
    [SerializeField] string livesTextFormat = "Lives: {0}/{1}";

    [Header("UI: Icons (optional)")]
    [SerializeField] Transform livesIconsContainer;
    [SerializeField] Image lifeIconPrefab;
    [SerializeField] Sprite aliveLifeIcon;
    [SerializeField] Sprite lostLifeIcon;

    [Header("Death UI")]
    [SerializeField] bool useDeathMenu = false;
    [SerializeField] DeathMenuUI deathMenu;

    [Header("Auto Respawn")]
    [SerializeField] float autoRespawnDelay = 0f;

    [Header("Respawn")]
    [SerializeField] Transform defaultRespawnPoint;

    [Header("Debug")]
    [SerializeField] bool logStateChanges;

    readonly List<Image> spawnedLifeIcons = new List<Image>();

    int currentLives;
    Vector3 currentRespawnPoint;
    bool hasRespawnPoint;
    bool isDead;
    bool isInvulnerable;
    Coroutine invulnerabilityRoutine;
    Coroutine autoRespawnRoutine;
    Rigidbody2D cachedRigidbody;

    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody2D>();

        if (maxLives < 1)
        {
            maxLives = 1;
        }

        currentLives = maxLives;

        if (defaultRespawnPoint != null)
        {
            SetRespawnPoint(defaultRespawnPoint.position);
        }
        else
        {
            SetRespawnPoint(transform.position);
        }

        if (deathMenu == null)
        {
            deathMenu = FindAnyObjectByType<DeathMenuUI>();
        }

        if (deathMenu != null)
        {
            deathMenu.Configure(this);
            deathMenu.Hide();
        }

        BuildLivesIcons();
        UpdateLivesUi();
    }

    public void TakeDamage(int damageAmount)
    {
        TakeDamage(damageAmount, transform.position);
    }

    public void TakeDamage(int damageAmount, Vector2 damageSourcePosition)
    {
        if (damageAmount <= 0 || isDead || isInvulnerable)
        {
            return;
        }

        ApplyKnockback(damageSourcePosition);

        currentLives = Mathf.Max(0, currentLives - damageAmount);
        UpdateLivesUi();

        if (logStateChanges)
        {
            Debug.Log($"PlayerLives: damage {damageAmount}, lives {currentLives}/{maxLives}", this);
        }

        if (currentLives <= 0)
        {
            HandleDeath();
            return;
        }

        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityCoroutine());
    }

    void ApplyKnockback(Vector2 damageSourcePosition)
    {
        if (!enableKnockback || cachedRigidbody == null)
        {
            return;
        }

        float direction = Mathf.Sign(((Vector2)transform.position - damageSourcePosition).x);
        if (Mathf.Approximately(direction, 0f))
        {
            if (Mathf.Abs(cachedRigidbody.linearVelocity.x) > 0.01f)
            {
                direction = Mathf.Sign(cachedRigidbody.linearVelocity.x);
            }
            else
            {
                direction = 1f;
            }
        }

        Vector2 velocity = cachedRigidbody.linearVelocity;
        velocity.x = direction * knockbackHorizontalSpeed;
        velocity.y = Mathf.Max(velocity.y, knockbackVerticalSpeed);
        cachedRigidbody.linearVelocity = velocity;
    }

    public void AddLife(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentLives = Mathf.Clamp(currentLives + amount, 0, maxLives);
        UpdateLivesUi();

        if (logStateChanges)
        {
            Debug.Log($"PlayerLives: add life {amount}, lives {currentLives}/{maxLives}", this);
        }
    }

    public void Respawn()
    {
        if (!isDead)
        {
            return;
        }

        if (autoRespawnRoutine != null)
        {
            StopCoroutine(autoRespawnRoutine);
            autoRespawnRoutine = null;
        }

        Vector3 spawn = hasRespawnPoint ? currentRespawnPoint : transform.position;
        transform.position = spawn;

        currentLives = maxLives;
        isDead = false;
        UpdateLivesUi();

        if (deathMenu != null)
        {
            deathMenu.Hide();
        }

        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityCoroutine());

        if (logStateChanges)
        {
            Debug.Log("PlayerLives: respawned.", this);
        }
    }

    public void SetRespawnPoint(Vector3 respawnPoint)
    {
        currentRespawnPoint = respawnPoint;
        hasRespawnPoint = true;

        if (logStateChanges)
        {
            Debug.Log($"PlayerLives: respawn point set to {respawnPoint}", this);
        }
    }

    void HandleDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (useDeathMenu && deathMenu != null)
        {
            deathMenu.Show();
            return;
        }

        if (autoRespawnDelay <= 0f)
        {
            Respawn();
            return;
        }

        if (autoRespawnRoutine != null)
        {
            StopCoroutine(autoRespawnRoutine);
        }

        autoRespawnRoutine = StartCoroutine(AutoRespawnAfterDelay());
    }

    IEnumerator AutoRespawnAfterDelay()
    {
        yield return new WaitForSeconds(autoRespawnDelay);
        autoRespawnRoutine = null;
        Respawn();
    }

    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    void BuildLivesIcons()
    {
        spawnedLifeIcons.Clear();

        if (livesIconsContainer == null || lifeIconPrefab == null)
        {
            return;
        }

        for (int i = livesIconsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(livesIconsContainer.GetChild(i).gameObject);
        }

        for (int i = 0; i < maxLives; i++)
        {
            Image icon = Instantiate(lifeIconPrefab, livesIconsContainer);
            icon.gameObject.SetActive(true);
            spawnedLifeIcons.Add(icon);
        }
    }

    void UpdateLivesUi()
    {
        if (livesText != null)
        {
            livesText.text = string.Format(livesTextFormat, currentLives, maxLives);
        }

        if (spawnedLifeIcons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spawnedLifeIcons.Count; i++)
        {
            Image icon = spawnedLifeIcons[i];
            if (icon == null)
            {
                continue;
            }

            bool isAlive = i < currentLives;
            icon.enabled = isAlive || lostLifeIcon != null;

            if (isAlive && aliveLifeIcon != null)
            {
                icon.sprite = aliveLifeIcon;
            }
            else if (!isAlive && lostLifeIcon != null)
            {
                icon.sprite = lostLifeIcon;
            }
        }
    }
}