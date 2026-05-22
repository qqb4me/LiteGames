using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SimpleKeyPressSceneLoader : MonoBehaviour
{
    public Key key = Key.E;
    public int pressesNeeded = 5;
    public string sceneName = "AlchemistHomeAfterHeal";

    private int pressCount = 0;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
        {
            pressCount++;
            Debug.Log($"Нажатий: {pressCount}/{pressesNeeded}");

            if (pressCount >= pressesNeeded)
            {
                Debug.Log($"Загрузка сцены: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}