using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[DefaultExecutionOrder(-1000)]
public sealed class GameSession : MonoBehaviour
{
    static GameSession instance;

    readonly Dictionary<string, string> serializedStates = new Dictionary<string, string>();
    string pendingSpawnPointId;

    public static GameSession Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<GameSession>();
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject(nameof(GameSession));
            instance = root.AddComponent<GameSession>();
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        _ = Instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveState<T>(string key, T state)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        serializedStates[key] = JsonUtility.ToJson(state);
    }

    public bool TryLoadState<T>(string key, out T state)
    {
        state = default;

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (!serializedStates.TryGetValue(key, out string json) || string.IsNullOrEmpty(json))
        {
            return false;
        }

        state = JsonUtility.FromJson<T>(json);
        return true;
    }

    public bool HasState(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && serializedStates.ContainsKey(key);
    }

    public void ClearState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        serializedStates.Remove(key);
    }

    public void ClearAllStates()
    {
        serializedStates.Clear();
    }

    public void SetPendingSpawnPoint(string spawnPointId)
    {
        pendingSpawnPointId = string.IsNullOrWhiteSpace(spawnPointId) ? null : spawnPointId.Trim();
    }

    public bool TryConsumePendingSpawnPoint(out string spawnPointId)
    {
        spawnPointId = pendingSpawnPointId;

        if (string.IsNullOrWhiteSpace(spawnPointId))
        {
            return false;
        }

        pendingSpawnPointId = null;
        return true;
    }

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("GameSession.LoadScene: sceneName is null or empty.");
            return;
        }

        if (!IsSceneInBuildSettings(sceneName))
        {
            Debug.LogError($"GameSession.LoadScene: scene '{sceneName}' is not present in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void LoadScene(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"GameSession.LoadScene: sceneBuildIndex {sceneBuildIndex} is out of range.");
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

    static bool IsSceneInBuildSettings(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}