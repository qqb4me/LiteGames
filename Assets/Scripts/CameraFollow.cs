using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [System.Serializable]
    public class SavedState
    {
        public Vector3 position;
        public Vector3 offset;
        public float smoothTime;
        public bool useBounds;
        public Vector2 minPosition;
        public Vector2 maxPosition;
    }

    const string StateKey = "camera_follow";

    public Transform target;
    public Vector3 offset = new Vector3(0f, 1f, -10f);
    public float smoothTime = 0.12f;

    [Header("Optional Bounds")]
    public bool useBounds = false;
    public Vector2 minPosition;
    public Vector2 maxPosition;

    Vector3 velocity = Vector3.zero;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        RestoreState();
        ResolveTarget();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SaveState();
    }

    void OnDestroy()
    {
        SaveState();
    }

    void LateUpdate()
    {
        ResolveTarget();

        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        if (useBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, minPosition.x, maxPosition.x);
            newPos.y = Mathf.Clamp(newPos.y, minPosition.y, maxPosition.y);
        }

        transform.position = new Vector3(newPos.x, newPos.y, offset.z);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveTarget();
        RestoreState();
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + offset);
        }
    }

    void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    void SaveState()
    {
        if (!GameSession.HasInstance)
        {
            return;
        }

        SavedState state = new SavedState
        {
            position = transform.position,
            offset = offset,
            smoothTime = smoothTime,
            useBounds = useBounds,
            minPosition = minPosition,
            maxPosition = maxPosition
        };

        GameSession.Instance.SaveState(StateKey, state);
    }

    void RestoreState()
    {
        if (!GameSession.HasInstance)
        {
            return;
        }

        if (!GameSession.Instance.TryLoadState(StateKey, out SavedState state))
        {
            return;
        }

        transform.position = state.position;
        offset = state.offset;
        smoothTime = state.smoothTime;
        useBounds = state.useBounds;
        minPosition = state.minPosition;
        maxPosition = state.maxPosition;
    }
}
