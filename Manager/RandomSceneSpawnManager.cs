using UnityEngine;

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
        var rotation = pose.rotation * selection.Value.prefab.transform.rotation;
        Instantiate(selection.Value.prefab, pose.position, rotation);
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
}
