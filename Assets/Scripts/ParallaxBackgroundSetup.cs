using UnityEngine;

public class ParallaxBackgroundSetup : MonoBehaviour
{
    [Header("Camera Reference")]
    public Camera targetCamera;

    [Header("Display Information")]
    [SerializeField] bool showDebugInfo = true;

    void OnEnable()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void OnDrawGizmos()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null) return;

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        float requiredBgHeight = cameraHeight * 1.5f;
        float requiredBgWidth = cameraWidth * 1.5f;

        if (showDebugInfo)
        {
            Debug.Log($"=== PARALLAX BACKGROUND SETUP ===\n" +
                $"Camera Orthographic Size: {targetCamera.orthographicSize}\n" +
                $"Camera Visible Height: {cameraHeight} units\n" +
                $"Camera Visible Width: {cameraWidth} units\n" +
                $"Camera Aspect Ratio: {targetCamera.aspect}\n" +
                $"\n--- RECOMMENDED BACKGROUND SIZE ---\n" +
                $"Background Height: {requiredBgHeight} units (minimum)\n" +
                $"Background Width: {requiredBgWidth} units (minimum)\n" +
                $"Aspect Ratio: {requiredBgWidth / requiredBgHeight:F2}\n" +
                $"\nМаксимальный прыжок должен быть не более {cameraHeight * 0.25f} units");
        }

        Gizmos.color = Color.cyan;
        Vector3 camCenter = targetCamera.transform.position;
        Gizmos.DrawWireCube(camCenter, new Vector3(cameraWidth, cameraHeight, 1f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(camCenter, new Vector3(requiredBgWidth, requiredBgHeight, 1f));
    }

#if UNITY_EDITOR
    [ContextMenu("Log Size Requirements")]
    void LogRequirements()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Camera not found!");
            return;
        }

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        float requiredBgHeight = cameraHeight * 1.5f;
        float requiredBgWidth = cameraWidth * 1.5f;

        Debug.Log(
            $"╔════════════════════════════════════════════════════════╗\n" +
            $"║          ПАРАЛЛАКС ФОНА - ТРЕБОВАНИЯ К РАЗМЕРУ        ║\n" +
            $"╠════════════════════════════════════════════════════════╣\n" +
            $"║ Размер камеры (Orthographic Size): {targetCamera.orthographicSize,6}\n" +
            $"║ Видимая высота камеры:             {cameraHeight,6} units\n" +
            $"║ Видимая ширина камеры:             {cameraWidth,6} units\n" +
            $"║ Соотношение сторон (aspect):       {targetCamera.aspect,6:F2}\n" +
            $"╠════════════════════════════════════════════════════════╣\n" +
            $"║ ТРЕБУЕМЫХ МИНИМАЛЬНЫЙ РАЗМЕР ФОНА:                    ║\n" +
            $"║ Высота фона:  {requiredBgHeight,6} units × ширина {requiredBgWidth,6} units\n" +
            $"║ Масштаб: примерно 1.5x от видимой области            ║\n" +
            $"╚════════════════════════════════════════════════════════╝"
        );
    }
#endif
}
