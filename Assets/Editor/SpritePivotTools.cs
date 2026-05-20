using UnityEngine;
using UnityEditor;
using System.IO;

public static class SpritePivotTools
{
    [MenuItem("Tools/Sprites/Set Pivot To Bottom Center (Selected)")]
    public static void SetPivotSelected()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            EditorUtility.DisplayDialog("Set Pivot", "Select textures or a folder in Project view.", "OK");
            return;
        }

        foreach (var o in objs)
        {
            var path = AssetDatabase.GetAssetPath(o);
            ProcessPath(path);
        }

        AssetDatabase.Refresh();
        Debug.Log("Sprite pivot alignment complete.");
    }

    static void ProcessPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
            foreach (var f in files)
                ProcessAssetPath(f.Replace('\\', '/'));
        }
        else
        {
            ProcessAssetPath(path);
        }
    }

    static void ProcessAssetPath(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        try
        {
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                importer.spritePivot = new Vector2(0.5f, 0f);
            }
            else if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                var metas = importer.spritesheet;
                for (int i = 0; i < metas.Length; i++)
                {
                    var m = metas[i];
                    m.pivot = new Vector2(m.rect.width * 0.5f, 0f);
                    metas[i] = m;
                }
                importer.spritesheet = metas;
            }

            importer.SaveAndReimport();
            Debug.Log($"Set pivot bottom center for: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to process {path}: {e.Message}");
        }
    }
}
