using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    class LayerRuntime
    {
        public Transform template;
        public readonly List<Transform> tiles = new List<Transform>();
        public float tileWidth;
        public float parallax;

        public float startCenterX;
        public float currentCenterX;
        public float startY;
        public float startZ;
        public bool initialized;
    }

    [Header("Camera Reference")]
    public Camera mainCamera;

    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxStrength = 0.25f;
    [Range(0f, 0.99f)]
    public float closestLayerParallax = 0.85f;
    public bool useLayerDepth = true;
    public bool moveOnY = false;

    [Header("Layer Filter")]
    public bool includeOnlySpriteRenderers = true;
    public bool excludeColliderObjects = true;

    [Header("Strip Tiling")]
    public bool enableLooping = true;
    public bool autoCreateTileCopies = true;
    [Min(3)] public int minimumTilesPerLayer = 5;
    [Min(1)] public int extraTilesBeyondView = 2;
    [Min(0f)] public float preloadPadding = 1f;

    [Header("Closest Layer Alignment")]
    public bool alignClosestLayerBottom = false;
    public bool alignClosestLayerBottomOnlyOnStart = true;
    [Tooltip("-1 means the last layer.")]
    public int closestLayerIndex = -1;

    readonly List<LayerRuntime> layers = new List<LayerRuntime>();
    Vector3 cameraStartPos;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("ParallaxBackground: Main camera not found!", this);
            enabled = false;
            return;
        }

        cameraStartPos = mainCamera.transform.position;

        BuildLayers();
        AssignParallaxFactors();

        if (alignClosestLayerBottomOnlyOnStart)
        {
            AlignClosestLayerBottom();
        }

        if (enableLooping && autoCreateTileCopies)
        {
            EnsureTileCopies();
        }

        InitializeTileLayout();
        LogInfo();
    }

    void LateUpdate()
    {
        if (mainCamera == null || layers.Count == 0)
        {
            return;
        }

        Vector3 cameraOffset = mainCamera.transform.position - cameraStartPos;
        float camLeft = mainCamera.transform.position.x - GetCameraHalfWidth() - preloadPadding;
        float camRight = mainCamera.transform.position.x + GetCameraHalfWidth() + preloadPadding;

        for (int i = 0; i < layers.Count; i++)
        {
            LayerRuntime layer = layers[i];
            if (layer == null || layer.tiles.Count == 0)
            {
                continue;
            }

            float targetCenterX = layer.startCenterX + cameraOffset.x * layer.parallax;
            float targetY = layer.startY + (moveOnY ? cameraOffset.y * layer.parallax : 0f);

            float deltaX = targetCenterX - layer.currentCenterX;
            layer.currentCenterX = targetCenterX;

            for (int t = 0; t < layer.tiles.Count; t++)
            {
                Transform tile = layer.tiles[t];
                if (tile == null)
                {
                    continue;
                }

                Vector3 p = tile.position;
                p.x += deltaX;
                p.y = targetY;
                p.z = layer.startZ;
                tile.position = p;
            }

            if (enableLooping)
            {
                RecycleTiles(layer, camLeft, camRight);
            }
        }

        if (alignClosestLayerBottom && !alignClosestLayerBottomOnlyOnStart)
        {
            AlignClosestLayerBottom();
        }
    }

    void BuildLayers()
    {
        layers.Clear();

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!IsEligibleLayer(child))
            {
                continue;
            }

            if (TryCreateRuntime(child, out LayerRuntime runtime))
            {
                layers.Add(runtime);
            }
        }

        if (layers.Count == 0 && IsEligibleLayer(transform) && TryCreateRuntime(transform, out LayerRuntime selfLayer))
        {
            layers.Add(selfLayer);
            Debug.LogWarning("ParallaxBackground: Using object with script as single layer (no eligible child layers).", this);
        }
    }

    bool TryCreateRuntime(Transform template, out LayerRuntime runtime)
    {
        runtime = null;

        if (!TryGetWidth(template, out float width) || width <= 0.0001f)
        {
            return false;
        }

        runtime = new LayerRuntime
        {
            template = template,
            tileWidth = width,
            startCenterX = template.position.x,
            currentCenterX = template.position.x,
            startY = template.position.y,
            startZ = template.position.z,
            initialized = false
        };

        runtime.tiles.Add(template);
        return true;
    }

    void AssignParallaxFactors()
    {
        if (layers.Count == 0)
        {
            return;
        }

        float far = Mathf.Clamp(parallaxStrength, 0f, 0.99f);
        float close = Mathf.Clamp(closestLayerParallax, 0f, 0.99f);

        for (int i = 0; i < layers.Count; i++)
        {
            if (!useLayerDepth)
            {
                layers[i].parallax = far;
                continue;
            }

            float t = layers.Count == 1 ? 1f : (float)i / (layers.Count - 1);
            layers[i].parallax = Mathf.Lerp(far, close, t);
        }
    }

    void EnsureTileCopies()
    {
        float cameraWidth = GetCameraHalfWidth() * 2f;

        for (int i = 0; i < layers.Count; i++)
        {
            LayerRuntime layer = layers[i];
            if (layer == null || layer.template == null)
            {
                continue;
            }

            int desired = Mathf.CeilToInt(cameraWidth / layer.tileWidth) + extraTilesBeyondView * 2;
            desired = Mathf.Max(desired, minimumTilesPerLayer);
            if (desired % 2 == 0)
            {
                desired++;
            }

            while (layer.tiles.Count < desired)
            {
                Transform copy = Instantiate(layer.template.gameObject, layer.template.parent).transform;
                copy.name = layer.template.name + "_LoopCopy_" + layer.tiles.Count;

                ParallaxBackground nested = copy.GetComponent<ParallaxBackground>();
                if (nested != null)
                {
                    Destroy(nested);
                }

                layer.tiles.Add(copy);
            }
        }
    }

    void InitializeTileLayout()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            LayerRuntime layer = layers[i];
            if (layer == null || layer.tiles.Count == 0)
            {
                continue;
            }

            int count = layer.tiles.Count;
            float leftX = layer.startCenterX - (count - 1) * 0.5f * layer.tileWidth;

            for (int t = 0; t < count; t++)
            {
                Transform tile = layer.tiles[t];
                if (tile == null)
                {
                    continue;
                }

                tile.position = new Vector3(leftX + t * layer.tileWidth, layer.startY, layer.startZ);
            }

            layer.currentCenterX = layer.startCenterX;
            layer.initialized = true;
        }
    }

    void RecycleTiles(LayerRuntime layer, float camLeft, float camRight)
    {
        if (layer.tiles.Count <= 1)
        {
            return;
        }

        float half = layer.tileWidth * 0.5f;

        for (int guard = 0; guard < 64; guard++)
        {
            Transform left = GetLeftMost(layer.tiles);
            Transform right = GetRightMost(layer.tiles);
            if (left == null || right == null)
            {
                return;
            }

            if (left.position.x + half < camLeft)
            {
                left.position = new Vector3(right.position.x + layer.tileWidth, left.position.y, left.position.z);
                continue;
            }

            if (right.position.x - half > camRight)
            {
                right.position = new Vector3(left.position.x - layer.tileWidth, right.position.y, right.position.z);
                continue;
            }

            break;
        }
    }

    static Transform GetLeftMost(List<Transform> tiles)
    {
        Transform result = null;
        float min = float.MaxValue;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform t = tiles[i];
            if (t == null)
            {
                continue;
            }

            if (t.position.x < min)
            {
                min = t.position.x;
                result = t;
            }
        }

        return result;
    }

    static Transform GetRightMost(List<Transform> tiles)
    {
        Transform result = null;
        float max = float.MinValue;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform t = tiles[i];
            if (t == null)
            {
                continue;
            }

            if (t.position.x > max)
            {
                max = t.position.x;
                result = t;
            }
        }

        return result;
    }

    bool IsEligibleLayer(Transform tr)
    {
        if (tr == null)
        {
            return false;
        }

        if (excludeColliderObjects && tr.GetComponentInChildren<Collider2D>() != null)
        {
            return false;
        }

        if (includeOnlySpriteRenderers && tr.GetComponentInChildren<SpriteRenderer>() == null)
        {
            return false;
        }

        return true;
    }

    static bool TryGetWidth(Transform tr, out float width)
    {
        width = 0f;
        SpriteRenderer[] renderers = tr.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        width = b.size.x;
        return width > 0.0001f;
    }

    float GetCameraHalfWidth()
    {
        return mainCamera.orthographicSize * mainCamera.aspect;
    }

    void AlignClosestLayerBottom()
    {
        if (layers.Count == 0)
        {
            return;
        }

        int idx = closestLayerIndex < 0 ? layers.Count - 1 : closestLayerIndex;
        if (idx < 0 || idx >= layers.Count)
        {
            return;
        }

        LayerRuntime closest = layers[idx];
        if (closest == null || closest.template == null)
        {
            return;
        }

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < layers.Count; i++)
        {
            if (i == idx)
            {
                continue;
            }

            LayerRuntime l = layers[i];
            if (l == null || l.template == null)
            {
                continue;
            }

            sum += GetBottomY(l.template);
            count++;
        }

        if (count == 0)
        {
            return;
        }

        float targetBottom = sum / count;
        float currentBottom = GetBottomY(closest.template);
        float dy = targetBottom - currentBottom;
        if (Mathf.Abs(dy) < 0.0001f)
        {
            return;
        }

        Vector3 p = closest.template.position;
        p.y += dy;
        closest.template.position = p;
        closest.startY = p.y;
    }

    static float GetBottomY(Transform tr)
    {
        SpriteRenderer[] renderers = tr.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return tr.position.y;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        return b.min.y;
    }

    void LogInfo()
    {
        Debug.Log(
            "Parallax ready:\n" +
            $"- layers: {layers.Count}\n" +
            $"- looping: {enableLooping}\n" +
            $"- auto copies: {autoCreateTileCopies}\n" +
            $"- min tiles/layer: {minimumTilesPerLayer}\n" +
            $"- preload padding: {preloadPadding}",
            this);
    }
}
