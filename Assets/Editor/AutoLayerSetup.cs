using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class AutoLayerSetup
{
    static AutoLayerSetup()
    {
        EditorApplication.delayCall += ApplyToOpenScenes;
    }

    [MenuItem("Tools/The Alchemest/Apply Layer Setup")]
    static void ApplyLayerSetupMenu()
    {
        ApplyToOpenScenes();
    }

    static void ApplyToOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.delayCall -= ApplyToOpenScenes;

        bool changed = false;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                changed |= ApplyToHierarchy(root.transform);
            }
        }

        if (!changed)
        {
            return;
        }

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    static bool ApplyToHierarchy(Transform transform)
    {
        bool changed = false;
        GameObject gameObject = transform.gameObject;

        if (gameObject.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
        {
            string desiredSortingLayer = ResolveSortingLayerName(gameObject.name);
            if (!string.IsNullOrEmpty(desiredSortingLayer) && spriteRenderer.sortingLayerName != desiredSortingLayer)
            {
                spriteRenderer.sortingLayerName = desiredSortingLayer;
                changed = true;
            }

            int desiredSortingOrder = ResolveSortingOrder(gameObject.name);
            if (spriteRenderer.sortingOrder != desiredSortingOrder)
            {
                spriteRenderer.sortingOrder = desiredSortingOrder;
                changed = true;
            }
        }

        if (gameObject.TryGetComponent<Light2D>(out Light2D light2D))
        {
            int[] allSortingLayerIds = SortingLayer.layers.Select(layer => layer.id).ToArray();
            if (!light2D.targetSortingLayers.SequenceEqual(allSortingLayerIds))
            {
                light2D.targetSortingLayers = allSortingLayerIds;
                changed = true;
            }
        }

        if (gameObject.TryGetComponent<Camera>(out Camera camera) && camera.cullingMask != ~0)
        {
            camera.cullingMask = ~0;
            changed = true;
        }

        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
        {
            changed |= ApplyToHierarchy(transform.GetChild(childIndex));
        }

        return changed;
    }

    static string ResolveSortingLayerName(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();

        if (lowerName.Contains("player"))
        {
            return "Player";
        }

        if (lowerName.Contains("door"))
        {
            return "Default";
        }

        if (lowerName.Contains("platform") || lowerName.Contains("cloud") || lowerName.Contains("mushroom"))
        {
            return "Background";
        }

        return null;
    }

    static int ResolveSortingOrder(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();

        if (lowerName.Contains("door"))
        {
            return -1;
        }

        return 0;
    }
}