using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Tooltip("Assign all possible spawn points in the scene")]
    public Transform[] spawnPoints;
    private List<int> usedIndices = new List<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 GetRandomSpawnPosition()
    {
        int spawnIndex = GetRandomUnusedIndex();
        if (spawnIndex == -1)
        {
            Debug.LogWarning("No available spawn points! Player will spawn at origin.");
            return Vector3.zero;
        }
        usedIndices.Add(spawnIndex);
        return spawnPoints[spawnIndex].position;
    }

    public Quaternion GetRandomSpawnRotation()
    {
        int spawnIndex = usedIndices.Count > 0 ? usedIndices[usedIndices.Count - 1] : 0;
        return spawnPoints[spawnIndex].rotation;
    }

    private int GetRandomUnusedIndex()
    {
        var available = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!usedIndices.Contains(i)) available.Add(i);
        }
        if (available.Count == 0) return -1;
        return available[Random.Range(0, available.Count)];
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var t in spawnPoints)
            {
                if (t != null)
                    Gizmos.DrawSphere(t.position, 0.5f);
            }
        }
    }
}

