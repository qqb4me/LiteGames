using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DoorPortal : MonoBehaviour
{
    [Tooltip("Scene name to load when the player interacts with the door.")]
    public string targetSceneName = "AlchemistHome";

    [Tooltip("Optional spawn point id in the destination scene.")]
    public string targetSpawnPointId = "default";

    [Tooltip("Interaction prompt shown in the inspector and for future UI hookup.")]
    public string interactionPrompt = "Press E";

    [Tooltip("If true, the door can be used only when the player is inside the trigger.")]
    public bool requirePlayerInRange = true;

    Collider2D playerInRange;

    void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = other;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRange == other)
        {
            playerInRange = null;
        }
    }

    void Update()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            return;
        }

        if (requirePlayerInRange && playerInRange == null)
        {
            return;
        }

        bool pressed = false;

        if (Keyboard.current != null)
        {
            pressed |= Keyboard.current.eKey.wasPressedThisFrame;
        }

        if (!pressed)
        {
            return;
        }

        GameSession.Instance.SetPendingSpawnPoint(targetSpawnPointId);
        GameSession.LoadScene(targetSceneName);
    }
}