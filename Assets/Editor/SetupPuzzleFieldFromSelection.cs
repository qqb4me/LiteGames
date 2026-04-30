using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper: Tools -> Puzzle -> Setup Selected Field From Sprite
/// Select the white field sprite in Hierarchy (the visual grid), then run this.
/// It will create/add `PuzzleField` on the selected GameObject, compute origin/size/cellSize
/// from the SpriteRenderer bounds and the first PuzzlePiece's grid settings.
/// Optionally removes `PuzzlePiece` from the selected object and can generate pole markers.
/// </summary>
public static class SetupPuzzleFieldFromSelection
{
    [MenuItem("Tools/Puzzle/Setup Selected Field From Sprite")]
    public static void SetupFromSelection()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Select object", "Select the visual grid GameObject (white field sprite) in Hierarchy.", "OK");
            return;
        }

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            EditorUtility.DisplayDialog("No SpriteRenderer", "Selected object has no SpriteRenderer. Select the visual grid sprite.", "OK");
            return;
        }

        // Determine cellSize from first PuzzlePiece if present
        float cellSize = 1f;
        PuzzlePiece sample = Object.FindObjectOfType<PuzzlePiece>();
        if (sample != null)
        {
            cellSize = Mathf.Max(0.01f, sample.gridSizePixels / Mathf.Max(1f, sample.pixelsPerUnit));
        }

        Bounds bounds = sr.bounds; // world-space bounds
        Vector3 min = bounds.min;
        Vector3 size = bounds.size;

        int width = Mathf.RoundToInt(size.x / cellSize);
        int height = Mathf.RoundToInt(size.y / cellSize);

        // Ensure sensible defaults
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        // Add or reuse PuzzleField component on selected GameObject
        PuzzleField field = go.GetComponent<PuzzleField>();
        if (field == null)
        {
            Undo.AddComponent<PuzzleField>(go);
            field = go.GetComponent<PuzzleField>();
        }

        Undo.RecordObject(field, "Setup PuzzleField");
        field.fieldOrigin = new Vector2(min.x, min.y);
        field.fieldWidth = width;
        field.fieldHeight = height;
        field.cellSize = cellSize;
        EditorUtility.SetDirty(field);

        // Offer to remove PuzzlePiece component from the visual field if present
        PuzzlePiece pieceComp = go.GetComponent<PuzzlePiece>();
        if (pieceComp != null)
        {
            bool remove = EditorUtility.DisplayDialog("Remove PuzzlePiece?", "Selected visual field has a PuzzlePiece component. Remove it?", "Remove", "Keep");
            if (remove)
            {
                Undo.DestroyObjectImmediate(pieceComp);
            }
        }

        EditorUtility.DisplayDialog("Setup complete",
            $"PuzzleField configured. Origin={field.fieldOrigin}, Size={field.fieldWidth}x{field.fieldHeight}, CellSize={field.cellSize}",
            "OK");

        // Offer to immediately generate pole markers
        if (EditorUtility.DisplayDialog("Generate markers now?", "Generate pole markers from this PuzzleField now?", "Yes", "No"))
        {
            GeneratePuzzleMarkers.GenerateMarkersFromField();
        }
    }
}
