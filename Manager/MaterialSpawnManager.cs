using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns material drop objects within a defined area each day.
/// Handles clearing previously spawned drops and registering them with
/// <see cref="DropMaterialSaveManager"/> so that they persist correctly.
/// Listens to <see cref="GameClock.OnSleepAdvancedDay"/> to respawn materials when
/// the player advances the day by sleeping.
/// </summary>
public class MaterialSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnMaterialInfo
    {
        public string materialID;
        public GameObject prefab;
    }

    [SerializeField] private Collider spawnArea;
    [SerializeField] private SpawnMaterialInfo[] materialTable;
    [SerializeField] private int minCount = 1;
    [SerializeField] private int maxCount = 3;

    private readonly List<GameObject> activeDrops = new();
    private GameClock clock;

    void Start()
    {
        SpawnMaterials();
    }

    void OnEnable()
    {
        clock = FindFirstObjectByType<GameClock>();
        if (clock != null)
        {
            clock.OnSleepAdvancedDay += HandleSleepAdvancedDay;
        }
    }

    void OnDisable()
    {
        if (clock != null)
        {
            clock.OnSleepAdvancedDay -= HandleSleepAdvancedDay;
            clock = null;
        }
    }

    private void HandleSleepAdvancedDay(int day)
    {
        ClearMaterials();
        SpawnMaterials();
    }

    /// <summary>
    /// Spawns a random number of materials within the spawn area.
    /// </summary>
    public void SpawnMaterials()
    {
        if (spawnArea == null || materialTable == null || materialTable.Length == 0)
        {
            return;
        }

        int count = Random.Range(minCount, maxCount + 1);
        Bounds bounds = spawnArea.bounds;

        for (int i = 0; i < count; i++)
        {
            SpawnMaterialInfo info = materialTable[Random.Range(0, materialTable.Length)];
            Vector3 pos = RandomPointInBounds(bounds);
            GameObject obj = Instantiate(info.prefab, pos, Quaternion.identity);
            var drop = obj.GetComponent<DropMaterial>();
            if (drop != null)
            {
                drop.MaterialID = info.materialID;
            }
            DropMaterialSaveManager.Instance?.RegisterDrop(
                SceneManager.GetActiveScene().name,
                info.materialID,
                pos,
                null);
            activeDrops.Add(obj);
        }
    }

    /// <summary>
    /// Destroys all spawned materials and removes them from the save manager.
    /// </summary>
    public void ClearMaterials()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (var obj in activeDrops)
        {
            if (obj == null) continue;
            var drop = obj.GetComponent<DropMaterial>();
            if (drop != null)
            {
                DropMaterialSaveManager.Instance?.RemoveDrop(
                    sceneName,
                    drop.MaterialID,
                    obj.transform.position,
                    null);
            }
            Destroy(obj);
        }
        activeDrops.Clear();
    }

    private static Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z));
    }
}
