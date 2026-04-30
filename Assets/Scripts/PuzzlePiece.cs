using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzlePiece : MonoBehaviour
{
    public enum ShapePreset
    {
        AutoByName,
        LShape,
        TShape,
        Square,
        Line3,
        Cross,
        Custom
    }

    [Header("Shape")]
    public ShapePreset shapePreset = ShapePreset.AutoByName;
    public Vector2Int[] customCells;

    [Header("Grid")]
    public float gridSizePixels = 100f;
    public float pixelsPerUnit = 100f;
    public Vector2 gridOffset = Vector2.zero;
    public bool useSpriteAlphaMaskForShape = true;
    public bool strictAlphaMask = true;
    [Range(0f, 1f)] public float alphaThreshold = 0.1f;

    private Vector3 offset;
    private float zCoord;
    private Vector3 _inventoryPosition;
    private bool _isDragging;
    private int _rotationSteps;
    private PuzzleSceneManager _manager;
    private SpriteRenderer _spriteRenderer;
    private Vector2Int[] _bakedSpriteShape;
    private bool _shapeBaked;
    private Vector2 _anchorCellLocalCenterOffset;
    private bool _anchorOffsetBaked;
    private float _anchorOffsetForCellSize = -1f;
    private float _bakedForCellSize = -1f;

    public Vector2Int PlacedAnchorCell { get; private set; }
    public bool IsPlaced { get; private set; }

    void Awake()
    {
        _inventoryPosition = transform.position;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _manager = PuzzleSceneManager.GetOrCreate();
        _manager.RegisterPiece(this);
    }

    void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.UnregisterPiece(this);
        }
    }

    void OnMouseDown()
    {
        if (_manager != null && _manager.InputLocked)
        {
            return;
        }

        if (Mouse.current == null || Camera.main == null)
        {
            return;
        }

        _manager?.DetachPiece(this);
        zCoord = transform.position.z;
        offset = transform.position - GetMouseWorldPos();
        _isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        Vector3 newPos = GetMouseWorldPos() + offset;
        transform.position = new Vector3(newPos.x, newPos.y, zCoord);
    }

    void OnMouseUp()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;

        if (_manager != null)
        {
            _manager.TryPlacePiece(this);
            return;
        }

        float worldGridSize = gridSizePixels / pixelsPerUnit;
        float x = Mathf.Round((transform.position.x - gridOffset.x) / worldGridSize) * worldGridSize + gridOffset.x;
        float y = Mathf.Round((transform.position.y - gridOffset.y) / worldGridSize) * worldGridSize + gridOffset.y;
        transform.position = new Vector3(x, y, zCoord);
    }

    void OnMouseOver()
    {
        if (_manager != null && _manager.InputLocked)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            RotateClockwise();
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RotateClockwise();
        }
    }

    public Vector2Int[] GetLocalShapeCells()
    {
        float currentCellSize = ResolveShapeCellSizeWorld();
        if (_shapeBaked && _bakedForCellSize > 0f && !Mathf.Approximately(_bakedForCellSize, currentCellSize))
        {
            _shapeBaked = false;
            _bakedSpriteShape = null;
            _anchorCellLocalCenterOffset = Vector2.zero;
        }

        if (_anchorOffsetBaked && _anchorOffsetForCellSize > 0f && !Mathf.Approximately(_anchorOffsetForCellSize, currentCellSize))
        {
            _anchorOffsetBaked = false;
            _anchorCellLocalCenterOffset = Vector2.zero;
        }

        if (useSpriteAlphaMaskForShape)
        {
            if (TryBakeShapeFromSprite())
            {
                return _bakedSpriteShape;
            }

            if (strictAlphaMask)
            {
                return System.Array.Empty<Vector2Int>();
            }
        }

        if (shapePreset == ShapePreset.Custom && customCells != null && customCells.Length > 0)
        {
            EnsureAnchorOffsetFromSprite(currentCellSize);
            return customCells;
        }

        ShapePreset resolvedPreset = shapePreset == ShapePreset.AutoByName ? ResolvePresetByName() : shapePreset;
        if (resolvedPreset != ShapePreset.Custom)
        {
            EnsureAnchorOffsetFromSprite(currentCellSize);
            return GetPresetCells(resolvedPreset);
        }

        EnsureAnchorOffsetFromSprite(currentCellSize);
        return new[] { new Vector2Int(0, 0) };
    }

    private static Vector2Int[] GetPresetCells(ShapePreset resolvedPreset)
    {
        switch (resolvedPreset)
        {
            case ShapePreset.LShape:
                return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1) };
            case ShapePreset.TShape:
                return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) };
            case ShapePreset.Square:
                return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
            case ShapePreset.Line3:
                return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            case ShapePreset.Cross:
                return new[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(1, 2)
                };
            default:
                return new[] { new Vector2Int(0, 0) };
        }
    }

    public Vector2Int[] GetRotatedShapeCells()
    {
        Vector2Int[] src = GetLocalShapeCells();
        Vector2Int[] result = new Vector2Int[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            result[i] = RotateCell(src[i], _rotationSteps);
        }

        return result;
    }

    public void RotateClockwise()
    {
        _rotationSteps = (_rotationSteps + 1) % 4;
        transform.Rotate(0f, 0f, 90f, Space.Self);
        if (_manager != null)
        {
            _manager.NotifyPieceTransformed(this);
        }
    }

    public void ResetToInventory()
    {
        transform.position = _inventoryPosition;
        IsPlaced = false;
        _rotationSteps = 0;
        transform.rotation = Quaternion.identity;
    }

    public void MarkPlaced(Vector2Int anchorCell, Vector3 snappedWorldPosition)
    {
        PlacedAnchorCell = anchorCell;
        IsPlaced = true;
        transform.position = new Vector3(snappedWorldPosition.x, snappedWorldPosition.y, zCoord);
    }

    public void MarkDetached()
    {
        IsPlaced = false;
    }

    public Vector3 GetSnappedWorldFromAnchor(Vector3 anchorCellCenterWorld)
    {
        if (_anchorCellLocalCenterOffset == Vector2.zero)
        {
            return new Vector3(anchorCellCenterWorld.x, anchorCellCenterWorld.y, zCoord);
        }

        Vector2 rotatedOffset = RotateVector90(_anchorCellLocalCenterOffset, _rotationSteps);
        return new Vector3(anchorCellCenterWorld.x - rotatedOffset.x, anchorCellCenterWorld.y - rotatedOffset.y, zCoord);
    }

    public Vector3 GetAnchorReferenceWorld()
    {
        if (_anchorCellLocalCenterOffset == Vector2.zero)
        {
            return transform.position;
        }

        Vector2 rotatedOffset = RotateVector90(_anchorCellLocalCenterOffset, _rotationSteps);
        return transform.position + new Vector3(rotatedOffset.x, rotatedOffset.y, 0f);
    }

    private ShapePreset ResolvePresetByName()
    {
        string n = gameObject.name.ToLowerInvariant();
        if (n.Contains("puzzle1")) return ShapePreset.LShape;
        if (n.Contains("puzzle2")) return ShapePreset.TShape;
        if (n.Contains("puzzle3")) return ShapePreset.Square;
        if (n.Contains("puzzle4")) return ShapePreset.Line3;
        if (n.Contains("puzzle5")) return ShapePreset.Cross;
        return ShapePreset.Square;
    }

    private bool TryBakeShapeFromSprite()
    {
        float cellSizeWorld = ResolveShapeCellSizeWorld();

        if (_shapeBaked)
        {
            return _bakedSpriteShape != null && _bakedSpriteShape.Length > 0;
        }

        _shapeBaked = true;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null || _spriteRenderer.sprite == null || _spriteRenderer.sprite.texture == null)
        {
            return false;
        }

        Texture2D texture = _spriteRenderer.sprite.texture;
        if (!texture.isReadable)
        {
            return false;
        }

        Sprite sprite = _spriteRenderer.sprite;
        Rect texRect = sprite.textureRect;
        Vector2 spriteLocalMin = sprite.bounds.min;
        Vector2 spriteLocalSize = sprite.bounds.size;

        int width = Mathf.Max(1, Mathf.RoundToInt(spriteLocalSize.x / cellSizeWorld));
        int height = Mathf.Max(1, Mathf.RoundToInt(spriteLocalSize.y / cellSizeWorld));

        List<Vector2Int> cells = new List<Vector2Int>();
        float pivotCellX = ((0f - spriteLocalMin.x) / cellSizeWorld) - 0.5f;
        float pivotCellY = ((0f - spriteLocalMin.y) / cellSizeWorld) - 0.5f;
        int anchorX = Mathf.RoundToInt(pivotCellX);
        int anchorY = Mathf.RoundToInt(pivotCellY);

        _anchorCellLocalCenterOffset = new Vector2(
            spriteLocalMin.x + (anchorX + 0.5f) * cellSizeWorld,
            spriteLocalMin.y + (anchorY + 0.5f) * cellSizeWorld);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 localCenter = new Vector3(
                    spriteLocalMin.x + (x + 0.5f) * cellSizeWorld,
                    spriteLocalMin.y + (y + 0.5f) * cellSizeWorld,
                    0f);

                if (SampleAlphaAtLocal(texture, texRect, spriteLocalMin, spriteLocalSize, localCenter) >= alphaThreshold)
                {
                    cells.Add(new Vector2Int(x - anchorX, y - anchorY));
                }
            }
        }

        if (cells.Count == 0)
        {
            return false;
        }

        _bakedSpriteShape = cells.ToArray();
        _bakedForCellSize = cellSizeWorld;
        return true;
    }

    private static float SampleAlphaAtLocal(Texture2D texture, Rect texRect, Vector2 spriteLocalMin, Vector2 spriteLocalSize, Vector3 localPos)
    {
        float nx = Mathf.InverseLerp(spriteLocalMin.x, spriteLocalMin.x + spriteLocalSize.x, localPos.x);
        float ny = Mathf.InverseLerp(spriteLocalMin.y, spriteLocalMin.y + spriteLocalSize.y, localPos.y);
        if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
        {
            return 0f;
        }

        int px = Mathf.Clamp(Mathf.FloorToInt(texRect.x + nx * texRect.width), 0, texture.width - 1);
        int py = Mathf.Clamp(Mathf.FloorToInt(texRect.y + ny * texRect.height), 0, texture.height - 1);
        return texture.GetPixel(px, py).a;
    }

    private void EnsureAnchorOffsetFromSprite(float cellSizeWorld)
    {
        if (_anchorOffsetBaked)
        {
            return;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null || _spriteRenderer.sprite == null)
        {
            _anchorCellLocalCenterOffset = Vector2.zero;
            _anchorOffsetBaked = true;
            _anchorOffsetForCellSize = cellSizeWorld;
            return;
        }

        Sprite sprite = _spriteRenderer.sprite;
        Vector2 spriteLocalMin = sprite.bounds.min;
        float pivotCellX = ((0f - spriteLocalMin.x) / cellSizeWorld) - 0.5f;
        float pivotCellY = ((0f - spriteLocalMin.y) / cellSizeWorld) - 0.5f;
        int anchorX = Mathf.RoundToInt(pivotCellX);
        int anchorY = Mathf.RoundToInt(pivotCellY);

        _anchorCellLocalCenterOffset = new Vector2(
            spriteLocalMin.x + (anchorX + 0.5f) * cellSizeWorld,
            spriteLocalMin.y + (anchorY + 0.5f) * cellSizeWorld);

        _anchorOffsetBaked = true;
        _anchorOffsetForCellSize = cellSizeWorld;
    }

    private float ResolveShapeCellSizeWorld()
    {
        if (_manager == null)
        {
            _manager = FindAnyObjectByType<PuzzleSceneManager>();
        }

        if (_manager != null)
        {
            float fieldCellSize = _manager.GetFieldCellSize();
            if (fieldCellSize > 0f)
            {
                return fieldCellSize;
            }
        }

        return Mathf.Max(0.01f, gridSizePixels / Mathf.Max(1f, pixelsPerUnit));
    }

    private static Vector2Int RotateCell(Vector2Int cell, int stepsCW)
    {
        int s = ((stepsCW % 4) + 4) % 4;
        switch (s)
        {
            case 0: return cell;
            case 1: return new Vector2Int(-cell.y, cell.x);
            case 2: return new Vector2Int(-cell.x, -cell.y);
            case 3: return new Vector2Int(cell.y, -cell.x);
            default: return cell;
        }
    }

    private static Vector2 RotateVector90(Vector2 v, int stepsCW)
    {
        int s = ((stepsCW % 4) + 4) % 4;
        switch (s)
        {
            case 0: return v;
            case 1: return new Vector2(-v.y, v.x);
            case 2: return new Vector2(-v.x, -v.y);
            case 3: return new Vector2(v.y, -v.x);
            default: return v;
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        if (Mouse.current == null || Camera.main == null)
        {
            return transform.position;
        }

        Vector3 mousePoint = Mouse.current.position.ReadValue();
        mousePoint.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}