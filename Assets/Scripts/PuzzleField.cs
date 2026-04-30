using System.Collections.Generic;
using UnityEngine;

public class PuzzleField : MonoBehaviour
{
    [Header("Sprite source")]
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer gridSourceRenderer;
    public bool useSpriteAlphaMask = true;
    public bool autoBakeOnStart = true;
    public bool drawGridInSceneView = true;
    public bool drawSpriteBounds = true;
    public Color gridColor = new Color(0f, 1f, 0f, 0.8f);

    [Header("Grid")]
    public float cellSize = 1f;
    [Range(0f, 1f)] public float alphaThreshold = 0.1f;

    [Header("Calibration")]
    public Vector2 gridOriginOffset = Vector2.zero;

    [Header("Fallback / legacy values")]
    public int fieldWidth = 5;
    public int fieldHeight = 3;
    public Vector2 fieldOrigin = Vector2.zero;

    [SerializeField] private bool _baked;
    private readonly HashSet<Vector2Int> _targetCells = new HashSet<Vector2Int>();
    private Bounds _worldBounds;

    public IReadOnlyCollection<Vector2Int> TargetCells => _targetCells;
    public Bounds WorldBounds => _worldBounds;

    private void Awake()
    {
        EnsureRenderer();
        if (autoBakeOnStart)
        {
            Bake();
        }
    }

    private void OnValidate()
    {
        EnsureRenderer();
        if (!Application.isPlaying)
        {
            Bake();
        }
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (gridSourceRenderer == null)
        {
            gridSourceRenderer = spriteRenderer;
        }
    }

    public void Bake()
    {
        _targetCells.Clear();
        EnsureRenderer();

        SpriteRenderer source = GetSourceRenderer();
        if (source != null && source.sprite != null && useSpriteAlphaMask)
        {
            BakeFromSprite(source);
        }
        else
        {
            BakeFallbackRectangle();
        }

        _baked = true;
    }

    public bool IsTargetCell(Vector2Int cell)
    {
        if (!_baked)
        {
            Bake();
        }

        return _targetCells.Contains(cell);
    }

    public Vector2Int WorldToGridCell(Vector3 worldPos)
    {
        if (!_baked)
        {
            Bake();
        }

        Vector3 origin = GetOriginWorld();
        // Grid cell indices are defined by cell area [n, n+1), while centers are at n+0.5.
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - origin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridCellToWorld(Vector2Int gridCell)
    {
        if (!_baked)
        {
            Bake();
        }

        Vector3 origin = GetOriginWorld();
        float x = origin.x + gridCell.x * cellSize + cellSize * 0.5f;
        float y = origin.y + gridCell.y * cellSize + cellSize * 0.5f;
        return new Vector3(x, y, 0f);
    }

    public Vector2Int GetFieldSize()
    {
        return new Vector2Int(fieldWidth, fieldHeight);
    }

    public Vector2 GetFieldOrigin()
    {
        return fieldOrigin;
    }

    private SpriteRenderer GetSourceRenderer()
    {
        return gridSourceRenderer != null ? gridSourceRenderer : spriteRenderer;
    }

    private void BakeFromSprite(SpriteRenderer sourceRenderer)
    {
        Sprite sprite = sourceRenderer.sprite;
        Texture2D texture = sprite.texture;

        if (texture == null)
        {
            BakeFallbackRectangle();
            return;
        }

        _worldBounds = sourceRenderer.bounds;
        if (cellSize <= 0f)
        {
            cellSize = Mathf.Max(0.01f, sprite.bounds.size.x / Mathf.Max(1, fieldWidth));
        }

        int width = Mathf.Max(1, Mathf.RoundToInt(_worldBounds.size.x / cellSize));
        int height = Mathf.Max(1, Mathf.RoundToInt(_worldBounds.size.y / cellSize));

        fieldWidth = width;
        fieldHeight = height;
        Vector3 origin = GetOriginWorld();
        fieldOrigin = new Vector2(origin.x, origin.y);

        if (!texture.isReadable)
        {
            Debug.LogWarning($"[{name}] Sprite texture is not readable. Using full rectangle instead of alpha mask.");
            BakeFallbackRectangle();
            return;
        }

        Rect texRect = sprite.textureRect;
        Vector2 spriteLocalMin = sprite.bounds.min;
        Vector2 spriteLocalSize = sprite.bounds.size;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 worldCenter = GridCellToWorld(cell);
                Vector3 local = sourceRenderer.transform.InverseTransformPoint(worldCenter);

                float nx = Mathf.InverseLerp(spriteLocalMin.x, spriteLocalMin.x + spriteLocalSize.x, local.x);
                float ny = Mathf.InverseLerp(spriteLocalMin.y, spriteLocalMin.y + spriteLocalSize.y, local.y);

                if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
                {
                    continue;
                }

                int px = Mathf.Clamp(Mathf.FloorToInt(texRect.x + nx * texRect.width), 0, texture.width - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt(texRect.y + ny * texRect.height), 0, texture.height - 1);

                if (texture.GetPixel(px, py).a >= alphaThreshold)
                {
                    _targetCells.Add(cell);
                }
            }
        }

        if (_targetCells.Count == 0)
        {
            BakeFallbackRectangle();
        }
    }

    private void BakeFallbackRectangle()
    {
        Vector3 origin = GetOriginWorld();
        _worldBounds = new Bounds(
            origin + new Vector3(fieldWidth * cellSize * 0.5f, fieldHeight * cellSize * 0.5f, 0f),
            new Vector3(fieldWidth * cellSize, fieldHeight * cellSize, 0f));

        _targetCells.Clear();
        for (int y = 0; y < fieldHeight; y++)
        {
            for (int x = 0; x < fieldWidth; x++)
            {
                _targetCells.Add(new Vector2Int(x, y));
            }
        }
    }

    private Vector3 GetOriginWorld()
    {
        SpriteRenderer source = GetSourceRenderer();
        if (source != null && source.sprite != null)
        {
            return source.bounds.min + new Vector3(gridOriginOffset.x, gridOriginOffset.y, 0f);
        }

        return new Vector3(fieldOrigin.x, fieldOrigin.y, 0f) + new Vector3(gridOriginOffset.x, gridOriginOffset.y, 0f);
    }

    private void OnDrawGizmos()
    {
        if (!drawGridInSceneView)
        {
            return;
        }

        if (!_baked)
        {
            Bake();
        }

        SpriteRenderer source = GetSourceRenderer();
        if (drawSpriteBounds && source != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
            Gizmos.DrawWireCube(source.bounds.center, source.bounds.size);
        }

        Gizmos.color = new Color(0f, 1f, 0f, 0.9f);
        foreach (Vector2Int cell in _targetCells)
        {
            Vector3 center = GridCellToWorld(cell);
            Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
        }

        Vector3 origin = GetOriginWorld();
        Gizmos.color = new Color(1f, 0f, 0f, 0.9f);
        Gizmos.DrawLine(origin + new Vector3(-0.1f, 0f, 0f), origin + new Vector3(0.1f, 0f, 0f));
        Gizmos.DrawLine(origin + new Vector3(0f, -0.1f, 0f), origin + new Vector3(0f, 0.1f, 0f));
    }
}
