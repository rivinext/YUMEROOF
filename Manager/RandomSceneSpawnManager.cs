using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Spawns a random prefab in the scene based on weighted selection.
/// Prevents repeated spawns across multiple scene initialization paths.
/// </summary>
public class RandomSceneSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public struct Candidate
    {
        public GameObject prefab;
        public float weight;
    }

    [SerializeField, Min(0f)] private float noneWeight = 1f;
    [SerializeField] private Candidate[] candidates;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Collider randomAreaCollider;

    private bool hasSpawned;

#if UNITY_EDITOR
    private static readonly GUIStyle SpawnLabelStyle = new GUIStyle
    {
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = Color.cyan },
        fontStyle = FontStyle.Bold
    };
#endif

    /// <summary>
    /// Performs a weighted random selection and spawns the selected prefab once.
    /// </summary>
    public void SpawnOnce()
    {
        if (hasSpawned)
            return;

        hasSpawned = true;

        Candidate? selection = SelectCandidate();
        if (!selection.HasValue || selection.Value.prefab == null)
            return;

        var pose = GetSpawnPose();
        Instantiate(selection.Value.prefab, pose.position, pose.rotation);
    }

    private Candidate? SelectCandidate()
    {
        float totalWeight = Mathf.Max(0f, noneWeight);

        if (candidates != null)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.prefab != null && candidate.weight > 0f)
                {
                    totalWeight += candidate.weight;
                }
            }
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        if (roll < noneWeight)
            return null;

        roll -= noneWeight;

        if (candidates != null)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.prefab == null || candidate.weight <= 0f)
                    continue;

                if (roll < candidate.weight)
                    return candidate;

                roll -= candidate.weight;
            }
        }

        return null;
    }

    private (Vector3 position, Quaternion rotation) GetSpawnPose()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (point != null)
                return (point.position, point.rotation);
        }

        if (randomAreaCollider != null)
        {
            var bounds = randomAreaCollider.bounds;
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));
            randomPoint = randomAreaCollider.ClosestPoint(randomPoint);
            return (randomPoint, transform.rotation);
        }

        return (transform.position, transform.rotation);
    }

    private void OnDrawGizmos()
    {
        DrawSpawnGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawSpawnGizmos(true);
    }

    private void DrawSpawnGizmos(bool isSelected)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        Gizmos.color = isSelected ? new Color(0.2f, 1f, 0.8f, 0.6f) : new Color(0.2f, 0.7f, 1f, 0.4f);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var point = spawnPoints[i];
            if (point == null)
                continue;

            Gizmos.DrawSphere(point.position, 0.2f);
            Gizmos.DrawLine(transform.position, point.position);

#if UNITY_EDITOR
            if (isSelected)
            {
                Handles.Label(point.position + Vector3.up * 0.15f, $"Spawn {i + 1}", SpawnLabelStyle);
            }
#endif
        }
    }
}
