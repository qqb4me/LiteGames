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

    [Header("Stability")]
    [SerializeField] bool snapToPixelGrid = false;
    [SerializeField] bool followTargetCenter = true;

    [Header("Optional Bounds")]
    public bool useBounds = false;
    public Vector2 minPosition;
    public Vector2 maxPosition;

    Vector3 velocity = Vector3.zero;
    Camera cachedCamera;
    Transform cachedTarget;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        cachedCamera = GetComponent<Camera>();
        RestoreState();
        ResolveTarget();
        cachedTarget = target;
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

        if (target != cachedTarget)
        {
            cachedTarget = target;
            velocity = Vector3.zero;
        }

        if (target == null) return;

        Vector3 followPoint = GetFollowPoint(target);
        Vector3 targetPos = followPoint + offset;
        float followFactor = 1f - Mathf.Exp(-Mathf.Max(0.0001f, smoothTime) * Time.deltaTime * 10f);
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, followFactor);

        if (useBounds)
        {
            newPos.x = Mathf.Clamp(newPos.x, minPosition.x, maxPosition.x);
            newPos.y = Mathf.Clamp(newPos.y, minPosition.y, maxPosition.y);
        }

        if (snapToPixelGrid && cachedCamera != null && cachedCamera.orthographic && cachedCamera.pixelHeight > 0)
        {
            float unitsPerPixel = (cachedCamera.orthographicSize * 2f) / cachedCamera.pixelHeight;
            newPos.x = Mathf.Round(newPos.x / unitsPerPixel) * unitsPerPixel;
            newPos.y = Mathf.Round(newPos.y / unitsPerPixel) * unitsPerPixel;
        }

        transform.position = new Vector3(newPos.x, newPos.y, offset.z);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveTarget();
        cachedTarget = target;
        velocity = Vector3.zero;
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

    Vector3 GetFollowPoint(Transform followTarget)
    {
        if (!followTargetCenter || followTarget == null)
        {
            return followTarget != null ? followTarget.position : Vector3.zero;
        }

        Collider2D collider = followTarget.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            return collider.bounds.center;
        }

        SpriteRenderer spriteRenderer = followTarget.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.center;
        }

        return followTarget.position;
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
