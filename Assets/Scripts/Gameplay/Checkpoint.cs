using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    static Checkpoint activeCheckpoint;

    [Header("Trigger")]
    [SerializeField] string playerTag = "Player";

    [Header("Respawn")]
    [SerializeField] Transform respawnPoint;

    [Header("Visual State")]
    [SerializeField] GameObject inactiveVisual;
    [SerializeField] GameObject activeVisual;

    bool isActivated;

    void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }

        SetActivated(activeCheckpoint == this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        PlayerLives playerLives = other.GetComponent<PlayerLives>();
        if (playerLives == null)
        {
            playerLives = other.GetComponentInParent<PlayerLives>();
        }

        if (playerLives == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && !playerLives.gameObject.CompareTag(playerTag))
        {
            return;
        }

        ActivateFor(playerLives);
    }

    void ActivateFor(PlayerLives playerLives)
    {
        if (playerLives == null)
        {
            return;
        }

        if (activeCheckpoint == this && isActivated)
        {
            return;
        }

        if (activeCheckpoint != null && activeCheckpoint != this)
        {
            activeCheckpoint.SetActivated(false);
        }

        activeCheckpoint = this;
        SetActivated(true);

        Vector3 point = respawnPoint != null ? respawnPoint.position : transform.position;
        playerLives.SetRespawnPoint(point);
    }

    void SetActivated(bool value)
    {
        isActivated = value;

        if (inactiveVisual != null)
        {
            inactiveVisual.SetActive(!value);
        }

        if (activeVisual != null)
        {
            activeVisual.SetActive(value);
        }
    }
}