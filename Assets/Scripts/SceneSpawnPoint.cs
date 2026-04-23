using System;
using UnityEngine;

public class SceneSpawnPoint : MonoBehaviour
{
    [Tooltip("Identifier used by doors or transitions to select this spawn point.")]
    public string spawnId = "default";

    [Tooltip("Used when no explicit spawn id is provided.")]
    public bool isDefaultSpawn = true;

    public static bool TryGetSpawnPosition(string spawnId, out Vector3 position)
    {
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>();

        if (!string.IsNullOrWhiteSpace(spawnId))
        {
            foreach (SceneSpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && string.Equals(spawnPoint.spawnId, spawnId, StringComparison.OrdinalIgnoreCase))
                {
                    position = spawnPoint.transform.position;
                    return true;
                }
            }
        }

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && spawnPoint.isDefaultSpawn)
            {
                position = spawnPoint.transform.position;
                return true;
            }
        }

        if (spawnPoints.Length > 0 && spawnPoints[0] != null)
        {
            position = spawnPoints[0].transform.position;
            return true;
        }

        position = default;
        return false;
    }
}