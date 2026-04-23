using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [Tooltip("Name of the scene to load.")]
    public string sceneName;

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.", this);
            return;
        }

        GameSession.LoadScene(sceneName);
    }
}