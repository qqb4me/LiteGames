using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1f, -10f);
    public float smoothTime = 0.12f;

    [Header("Optional Bounds")]
    public bool useBounds = false;
    public Vector2 minPosition;
    public Vector2 maxPosition;

    Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
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

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + offset);
        }
    }
}
