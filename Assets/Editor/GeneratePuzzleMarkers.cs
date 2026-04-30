using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor utility: Tools -> Puzzle -> Generate Pole Markers From PuzzleField
// Finds first PuzzleField in scene and creates pole markers at each cell center.
// If a prototype pole GameObject exists (named starting with "pole" and has SpriteRenderer), its sprite and render settings are copied.

public static class GeneratePuzzleMarkers
{
    [MenuItem("Tools/Puzzle/Generate Pole Markers From PuzzleField")]
    public static void GenerateMarkersFromField()
    {
        PuzzleField field = Object.FindObjectOfType<PuzzleField>();
        if (field == null)
        {
            Debug.LogError("PuzzleField not found in scene. Add a PuzzleField component to an empty GameObject.");
            return;
        }

        // Find prototype pole sprite if present
        Sprite prototypeSprite = null;
        SpriteRenderer prototypeRenderer = null;
        GameObject[] all = Object.FindObjectsOfType<GameObject>();
        foreach (var go in all)
        {
            if (go.name.ToLowerInvariant().StartsWith("pole") && go.GetComponent<SpriteRenderer>() != null)
            {
                prototypeRenderer = go.GetComponent<SpriteRenderer>();
                prototypeSprite = prototypeRenderer.sprite;
                break;
            }
        }

        // Parent container
        GameObject container = GameObject.Find("PuzzlePoles");
        if (container == null)
        {
            container = new GameObject("PuzzlePoles");
            Undo.RegisterCreatedObjectUndo(container, "Create PuzzlePoles container");
        }

        // Clear existing children under container
        List<GameObject> toRemove = new List<GameObject>();
        for (int i = 0; i < container.transform.childCount; i++)
        {
            toRemove.Add(container.transform.GetChild(i).gameObject);
        }
        if (toRemove.Count > 0)
        {
            if (EditorUtility.DisplayDialog("Clear existing pole markers?","Existing children under PuzzlePoles will be deleted before generating new markers.","Delete","Cancel"))
            {
                foreach (var c in toRemove)
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }
            else
            {
                Debug.Log("Generate cancelled by user.");
                return;
            }
        }

        Vector2Int size = field.GetFieldSize();
        int created = 0;
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 world = field.GridCellToWorld(cell);

                GameObject pole = new GameObject($"pole_{created}");
                Undo.RegisterCreatedObjectUndo(pole, "Create pole marker");
                pole.transform.position = world;
                pole.transform.SetParent(container.transform, true);

                if (prototypeSprite != null)
                {
                    var sr = pole.AddComponent<SpriteRenderer>();
                    sr.sprite = prototypeSprite;
                    // copy some settings
                    sr.sortingLayerID = prototypeRenderer.sortingLayerID;
                    sr.sortingOrder = prototypeRenderer.sortingOrder;
                    sr.material = prototypeRenderer.sharedMaterial;
                }

                created++;
            }
        }

        Debug.Log($"Generated {created} pole markers under GameObject 'PuzzlePoles'.");
        Selection.activeGameObject = container;
    }

    [MenuItem("Tools/Puzzle/Clear Pole Markers")] 
    public static void ClearPoleMarkers()
    {
        GameObject container = GameObject.Find("PuzzlePoles");
        if (container == null)
        {
            Debug.Log("No PuzzlePoles container found.");
            return;
        }

        if (!EditorUtility.DisplayDialog("Clear pole markers?","Delete all children under PuzzlePoles?","Delete","Cancel"))
            return;

        List<GameObject> toRemove = new List<GameObject>();
        for (int i = 0; i < container.transform.childCount; i++)
            toRemove.Add(container.transform.GetChild(i).gameObject);

        foreach (var c in toRemove)
            Undo.DestroyObjectImmediate(c);

        Debug.Log("Cleared pole markers.");
    }
}
