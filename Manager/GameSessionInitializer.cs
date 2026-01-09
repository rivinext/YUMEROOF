using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Initializes a game session by loading save data for the selected slot.
/// Ensures it persists across scene loads and cleans up when returning to the MainMenu.
/// </summary>
public class GameSessionInitializer : MonoBehaviour
{
    public static GameSessionInitializer Instance { get; private set; }

    private string slotKey;
    private bool initialized;
    [SerializeField] private GameObject inventoryManagerPrefab;
    [SerializeField] private GameObject furnitureDataManagerPrefab;
    [SerializeField] private GameObject furnitureSaveManagerPrefab;
    [SerializeField] private GameObject milestoneManagerPrefab;
    [SerializeField] private DevItemInjector devItemInjectorPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    /// <summary>
    /// Creates the initializer if needed and stores the slot key to load after the next scene.
    /// </summary>
    public static void CreateIfNeeded(string key)
    {
        if (Instance == null)
        {
            var go = new GameObject("GameSessionInitializer");
            var initializer = go.AddComponent<GameSessionInitializer>();
            initializer.slotKey = key;
        }
        else
        {
            Instance.slotKey = key;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Destroy(gameObject);
            return;
        }

        if (!DevItemInjector.BuildDisablesInjection)
        {
            var injector = FindFirstObjectByType<DevItemInjector>(FindObjectsInactive.Include);
            if (injector == null)
            {
                var prefab = devItemInjectorPrefab != null ? devItemInjectorPrefab : Resources.Load<DevItemInjector>("DevItemInjector");
                if (prefab != null)
                    injector = Instantiate(prefab);
            }
            else
            {
                injector.gameObject.SetActive(true);
            }
        }

        MilestoneManager.CreateIfNeeded(milestoneManagerPrefab);
        ApplyAudioSettingsToScene();

        if (!initialized && !string.IsNullOrEmpty(slotKey))
        {
            StartCoroutine(DelayedLoad());
        }

        FindFirstObjectByType<RandomSceneSpawnManager>(FindObjectsInactive.Include)?.SpawnOnce();
    }

    private System.Collections.IEnumerator DelayedLoad()
    {
        yield return null;

        var clock = FindFirstObjectByType<GameClock>();
        if (clock == null)
        {
            clock = new GameObject("GameClock").AddComponent<GameClock>();
        }

        if (inventoryManagerPrefab == null)
            inventoryManagerPrefab = Resources.Load<GameObject>("InventoryManager");
        if (furnitureDataManagerPrefab == null)
            furnitureDataManagerPrefab = Resources.Load<GameObject>("FurnitureDataManager");
        if (furnitureSaveManagerPrefab == null)
            furnitureSaveManagerPrefab = Resources.Load<GameObject>("FurnitureSaveManager");

        bool createdManager = false;
        if (FurnitureDataManager.Instance == null)
        {
            if (furnitureDataManagerPrefab != null)
                Instantiate(furnitureDataManagerPrefab);
            else
                new GameObject("FurnitureDataManager").AddComponent<FurnitureDataManager>();
            createdManager = true;
        }
        if (FurnitureSaveManager.Instance == null)
        {
            if (furnitureSaveManagerPrefab != null)
                Instantiate(furnitureSaveManagerPrefab);
            else
                new GameObject("FurnitureSaveManager").AddComponent<FurnitureSaveManager>();
            createdManager = true;
        }
        if (InventoryManager.Instance == null)
        {
            if (inventoryManagerPrefab != null)
                Instantiate(inventoryManagerPrefab);
            else
                new GameObject("InventoryManager").AddComponent<InventoryManager>();
            createdManager = true;
        }

        if (createdManager)
        {
            // Wait a frame to ensure managers initialize before loading
            yield return null;
        }

        bool createdNewSave = SaveGameManager.Instance.Load(slotKey);
        bool isStorySlot = slotKey.StartsWith("Story", StringComparison.OrdinalIgnoreCase);
        StoryOpeningSequenceState.SetNewStorySession(isStorySlot && createdNewSave);
        if (!DevItemInjector.BuildDisablesInjection)
        {
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                var furnitureList = inventoryManager.GetFurnitureList();
                var materialList = inventoryManager.GetMaterialList();
                bool hasNoFurniture = furnitureList == null || furnitureList.Count == 0;
                bool hasNoMaterials = materialList == null || materialList.Count == 0;

                if (hasNoFurniture && hasNoMaterials && !SaveGameManager.Instance.Applied_0_1_6_Seed)
                {
                    FindFirstObjectByType<DevItemInjector>(FindObjectsInactive.Include)?.Inject();
                    inventoryManager.ForceInventoryUpdate();
                    SaveGameManager.Instance.Applied_0_1_6_Seed = true;

                    if (!string.IsNullOrEmpty(SaveGameManager.Instance.CurrentSlotKey))
                    {
                        SaveGameManager.Instance.Save(SaveGameManager.Instance.CurrentSlotKey);
                    }
                }
            }
        }
        initialized = true;
        slotKey = null;
    }

    private void ApplyAudioSettingsToScene()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.ApplyVolumesToListeners();
    }
}
