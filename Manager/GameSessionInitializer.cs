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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private DevItemInjector devItemInjectorPrefab;
#endif

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
            DevItemInjector.ResetInjected();
            Destroy(gameObject);
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
#endif

        MilestoneManager.CreateIfNeeded(milestoneManagerPrefab);
        ApplyAudioSettingsToScene();

        if (!initialized && !string.IsNullOrEmpty(slotKey))
        {
            StartCoroutine(DelayedLoad());
        }
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

        SaveGameManager.Instance.Load(slotKey);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        FindFirstObjectByType<DevItemInjector>(FindObjectsInactive.Include)?.Inject();
        InventoryManager.Instance.ForceInventoryUpdate();
#endif
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
