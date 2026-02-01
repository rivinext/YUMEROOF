using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Listens for day change events and spawns material drops around
/// placed furniture based on each furniture's drop settings.
/// </summary>
public class FurnitureDropManager : MonoBehaviour
{
    public static FurnitureDropManager Instance { get; private set; }

    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private float spawnYOffset = 1f;
    [SerializeField] private GameObject dropPrefab;
    private GameClock clock;
    private Coroutine clockRetryCoroutine;
    private int lastProcessedDay = -1;
    private bool isWaitingForFurnitureLoad = false;
    private int pendingSleepDay = -1;
    private FurnitureSaveManager furnitureSaveManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (dropPrefab == null)
        {
            Debug.LogError("[FurnitureDropManager] Drop prefab is not assigned in the inspector.");
        }
    }

    void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        FindClockAndSubscribe();
    }

    void OnDisable()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopClockRetryCoroutine();
        UnsubscribeFromClock();
        UnsubscribeFromFurnitureLoad();
    }

    private void HandleSleepAdvancedDay(int day)
    {
        if (day <= lastProcessedDay)
        {
            return;
        }

        var saveManager = FurnitureSaveManager.Instance;
        var dataManager = FurnitureDataManager.Instance;
        bool isReady = saveManager != null && dataManager != null && !saveManager.IsFurnitureLoading;

        if (!isReady)
        {
            Debug.LogWarning($"[FurnitureDropManager] Furniture systems not ready. SaveManager: {(saveManager != null ? "Ready" : "Missing")}, DataManager: {(dataManager != null ? "Ready" : "Missing")}, IsLoading: {(saveManager != null && saveManager.IsFurnitureLoading ? "Yes" : "No")}, TargetDay: {day}");
            if (saveManager != null)
            {
                ScheduleSleepAdvancedDayAfterFurnitureLoad(day, saveManager);
            }
            return;
        }

        ExecuteSleepAdvancedDay(day, saveManager, dataManager);
    }

    private void ScheduleSleepAdvancedDayAfterFurnitureLoad(int day, FurnitureSaveManager saveManager)
    {
        if (isWaitingForFurnitureLoad)
        {
            if (pendingSleepDay < day)
            {
                pendingSleepDay = day;
            }
            return;
        }

        isWaitingForFurnitureLoad = true;
        pendingSleepDay = day;
        furnitureSaveManager = saveManager;
        furnitureSaveManager.OnFurnitureLoadCompleted += HandleFurnitureLoadCompleted;
        Debug.Log($"[FurnitureDropManager] Furniture load in progress. Waiting to process day {day}.");
    }

    private void HandleFurnitureLoadCompleted()
    {
        if (furnitureSaveManager != null)
        {
            furnitureSaveManager.OnFurnitureLoadCompleted -= HandleFurnitureLoadCompleted;
        }

        if (!isWaitingForFurnitureLoad)
        {
            return;
        }

        isWaitingForFurnitureLoad = false;
        int dayToProcess = pendingSleepDay;
        pendingSleepDay = -1;

        var saveManager = FurnitureSaveManager.Instance;
        var dataManager = FurnitureDataManager.Instance;
        if (saveManager == null || dataManager == null)
        {
            Debug.LogWarning($"[FurnitureDropManager] Furniture systems not ready after load completion. SaveManager: {(saveManager != null ? "Ready" : "Missing")}, DataManager: {(dataManager != null ? "Ready" : "Missing")}, TargetDay: {dayToProcess}");
            return;
        }

        if (saveManager.IsFurnitureLoading)
        {
            Debug.LogWarning($"[FurnitureDropManager] FurnitureSaveManager still loading after completion callback. TargetDay: {dayToProcess}");
            return;
        }

        if (dayToProcess > lastProcessedDay)
        {
            ExecuteSleepAdvancedDay(dayToProcess, saveManager, dataManager);
        }
    }

    private void ExecuteSleepAdvancedDay(int day, FurnitureSaveManager saveManager, FurnitureDataManager dataManager)
    {
        lastProcessedDay = day;
        var allFurniture = saveManager.GetAllFurniture();
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[FurnitureDropManager] HandleSleepAdvancedDay start. Furniture count: {allFurniture.Count}, Scene: {currentScene}, TargetDay: {day}");
        if (dropPrefab == null)
        {
            Debug.LogWarning("[FurnitureDropManager] Drop prefab is null. Aborting spawn.");
            return;
        }

        int masterFoundCount = 0;

        foreach (var saveData in allFurniture)
        {
            var fData = dataManager.GetFurnitureData(saveData.furnitureID);
            bool hasMaster = fData != null;
            if (hasMaster)
            {
                masterFoundCount++;
            }

            if (fData == null || fData.dropMaterialIDs == null || fData.dropRates == null) continue;

            int len = Mathf.Min(fData.dropMaterialIDs.Length, fData.dropRates.Length);
            for (int i = 0; i < len; i++)
            {
                string materialID = fData.dropMaterialIDs[i];
                float rate = fData.dropRates[i];
                if (string.IsNullOrEmpty(materialID) || materialID == "None" || rate <= 0f) continue;

                float randomValue = Random.value;
                Debug.Log($"[FurnitureDropManager] Random value {randomValue}, rate {rate} for material {materialID}");

                if (randomValue <= rate)
                {
                    Vector3 spawnPos = (saveData.sceneName == currentScene)
                        ? GetSpawnPositionCurrentScene(saveData)
                        : GetSpawnPositionFromSave(saveData);

                    if (saveData.sceneName == currentScene)
                    {
                        SpawnDropImmediate(materialID, spawnPos);
                    }
                    else
                    {
                        DropMaterialSaveManager.Instance?.RegisterDrop(saveData.sceneName, materialID, spawnPos, null);
                    }
                    Debug.Log($"[FurnitureDropManager] Queued spawn of {materialID} at {spawnPos} for scene {saveData.sceneName}");
                }
            }
        }

        Debug.Log($"[FurnitureDropManager] HandleSleepAdvancedDay summary. Furniture count: {allFurniture.Count}, Master data found: {masterFoundCount}, TargetDay: {day}");
    }

    private Vector3 GetSpawnPositionCurrentScene(FurnitureSaveManager.FurnitureSaveData data)
    {
        GameObject obj = GameObject.Find($"{data.furnitureID}_UID_{data.uniqueID}");
        float baseY = data.posY;
        Vector3 basePos = data.GetPosition();
        if (obj != null)
        {
            basePos = obj.transform.position;
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds combinedBounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    combinedBounds.Encapsulate(colliders[i].bounds);
                }

                baseY = combinedBounds.min.y;
                return GetPointAroundBounds(combinedBounds, baseY + spawnYOffset);
            }
            else
            {
                baseY = obj.transform.position.y;
            }
        }
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(basePos.x + circle.x, baseY + spawnYOffset, basePos.z + circle.y);
    }

    private Vector3 GetSpawnPositionFromSave(FurnitureSaveManager.FurnitureSaveData data)
    {
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(data.posX + circle.x, data.posY + spawnYOffset, data.posZ + circle.y);
    }

    private Vector3 GetPointAroundBounds(Bounds bounds, float y)
    {
        float minX = bounds.min.x - spawnRadius;
        float maxX = bounds.max.x + spawnRadius;
        float minZ = bounds.min.z - spawnRadius;
        float maxZ = bounds.max.z + spawnRadius;

        float width = Mathf.Max(0.001f, maxX - minX);
        float depth = Mathf.Max(0.001f, maxZ - minZ);
        float perimeter = 2f * (width + depth);
        float sample = Random.value * perimeter;

        float x, z;
        if (sample < width)
        {
            x = minX + sample;
            z = minZ;
        }
        else if (sample < width + depth)
        {
            x = maxX;
            z = minZ + (sample - width);
        }
        else if (sample < (2f * width) + depth)
        {
            x = maxX - (sample - (width + depth));
            z = maxZ;
        }
        else
        {
            x = minX;
            z = maxZ - (sample - ((2f * width) + depth));
        }

        return new Vector3(x, y, z);
    }

    private void SpawnDropImmediate(string materialID, Vector3 position)
    {
        GameObject dropObj = Instantiate(dropPrefab, position, Quaternion.identity);
        var dropComp = dropObj.GetComponent<DropMaterial>();
        if (dropComp != null)
        {
            dropComp.MaterialID = materialID;
        }
        DropMaterialSaveManager.Instance?.RegisterDrop(
            SceneManager.GetActiveScene().name,
            materialID,
            position,
            null);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Destroy(gameObject);
            return;
        }
        FindClockAndSubscribe();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            StopClockRetryCoroutine();
            UnsubscribeFromClock();
            UnsubscribeFromFurnitureLoad();
            Instance = null;
        }
    }

    private void UnsubscribeFromFurnitureLoad()
    {
        if (furnitureSaveManager != null)
        {
            furnitureSaveManager.OnFurnitureLoadCompleted -= HandleFurnitureLoadCompleted;
            furnitureSaveManager = null;
        }
        isWaitingForFurnitureLoad = false;
        pendingSleepDay = -1;
    }

    private void FindClockAndSubscribe()
    {
        GameClock newClock = GameClock.Instance ?? FindFirstObjectByType<GameClock>();
        if (newClock == clock) return;

        if (clock != null)
        {
            clock.OnSleepAdvancedDay -= HandleSleepAdvancedDay;
        }
        clock = newClock;
        if (clock != null)
        {
            Debug.Log($"[FurnitureDropManager] GameClock found (source: {(GameClock.Instance != null ? "Instance" : "FindFirstObjectByType")}). Subscribing to OnSleepAdvancedDay.");
            clock.OnSleepAdvancedDay += HandleSleepAdvancedDay;
            StopClockRetryCoroutine();
        }
        else
        {
            Debug.LogWarning("[FurnitureDropManager] GameClock not found. Waiting for GameClock.Instance to become available.");
            StartClockRetryCoroutine();
        }
    }

    private void UnsubscribeFromClock()
    {
        if (clock != null)
        {
            clock.OnSleepAdvancedDay -= HandleSleepAdvancedDay;
            clock = null;
            Debug.Log("[FurnitureDropManager] Unsubscribed from GameClock.OnSleepAdvancedDay.");
        }
    }

    private void StartClockRetryCoroutine()
    {
        if (!isActiveAndEnabled || clockRetryCoroutine != null)
        {
            return;
        }

        clockRetryCoroutine = StartCoroutine(WaitForClockAndSubscribe());
    }

    private void StopClockRetryCoroutine()
    {
        if (clockRetryCoroutine != null)
        {
            StopCoroutine(clockRetryCoroutine);
            clockRetryCoroutine = null;
        }
    }

    private IEnumerator WaitForClockAndSubscribe()
    {
        while (clock == null && isActiveAndEnabled)
        {
            GameClock newClock = GameClock.Instance ?? FindFirstObjectByType<GameClock>();
            if (newClock != null)
            {
                clock = newClock;
                Debug.Log($"[FurnitureDropManager] GameClock found during retry (source: {(GameClock.Instance != null ? "Instance" : "FindFirstObjectByType")}). Subscribing to OnSleepAdvancedDay.");
                clock.OnSleepAdvancedDay += HandleSleepAdvancedDay;
                clockRetryCoroutine = null;
                yield break;
            }

            yield return null;
        }

        clockRetryCoroutine = null;
    }
}
