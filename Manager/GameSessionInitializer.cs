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
    [SerializeField] private GameObject environmentStatsManagerPrefab;
    [SerializeField] private GameObject moneyManagerPrefab;
    [SerializeField] private GameObject dropMaterialSaveManagerPrefab;
    [SerializeField] private GameObject furnitureDropManagerPrefab;
    [SerializeField] private GameObject hintSystemPrefab;
    [SerializeField] private GameObject sceneTransitionManagerPrefab;
    [SerializeField] private GameObject slideTransitionManagerPrefab;
    [SerializeField] private DevItemInjector devItemInjectorPrefab;
    [SerializeField] private DevItemInjectorSettings devItemInjectorSettings;
    private bool devItemInjectorSettingsWarningLogged;

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
        EnsurePersistentManagers();
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
            var existing = FindObjectOfType<GameSessionInitializer>();
            GameSessionInitializer initializer = existing;

            if (initializer == null)
            {
                var prefab = Resources.Load<GameObject>("GameSessionInitializer");
                if (prefab != null)
                {
                    var go = Instantiate(prefab);
                    initializer = go.GetComponent<GameSessionInitializer>();
                    if (initializer == null)
                    {
                        initializer = go.AddComponent<GameSessionInitializer>();
                    }
                }
                else
                {
                    var go = new GameObject("GameSessionInitializer");
                    initializer = go.AddComponent<GameSessionInitializer>();
                }
            }

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
            CleanupInitializerSpawnedDevInjector();
            DevItemInjector.ResetInjected();
            Destroy(gameObject);
            return;
        }

        EnsurePersistentManagers();
        MilestoneManager.CreateIfNeeded(milestoneManagerPrefab);

        if (!initialized && !string.IsNullOrEmpty(slotKey))
        {
            StartCoroutine(DelayedLoad());
        }
    }

    private System.Collections.IEnumerator DelayedLoad()
    {
        yield return null;

        var clock = FindObjectOfType<GameClock>();
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
        initialized = true;
        slotKey = null;
    }

    private void EnsurePersistentManagers()
    {
        EnsureManager(ref inventoryManagerPrefab, () => FindObjectOfType<InventoryManager>(), "InventoryManager");
        EnsureManager(ref furnitureDataManagerPrefab, () => FindObjectOfType<FurnitureDataManager>(), "FurnitureDataManager");
        EnsureManager(ref furnitureSaveManagerPrefab, () => FindObjectOfType<FurnitureSaveManager>(), "FurnitureSaveManager");
        EnsureManager(ref milestoneManagerPrefab, () => FindObjectOfType<MilestoneManager>(), "MilestoneManager");
        EnsureManager(ref environmentStatsManagerPrefab, () => FindObjectOfType<EnvironmentStatsManager>(), "EnvironmentStatsManager");
        EnsureManager(ref moneyManagerPrefab, () => FindObjectOfType<MoneyManager>(), "MoneyManager");
        EnsureManager(ref dropMaterialSaveManagerPrefab, () => FindObjectOfType<DropMaterialSaveManager>(), "DropMaterialSaveManager");
        EnsureManager(ref furnitureDropManagerPrefab, () => FindObjectOfType<FurnitureDropManager>(), "FurnitureDropManager");
        EnsureManager(ref hintSystemPrefab, () => FindObjectOfType<HintSystem>(), "HintSystem");

        var sceneTransition = EnsureManager(ref sceneTransitionManagerPrefab, () => FindObjectOfType<SceneTransitionManager>(), "SceneTransitionManager");
        if (sceneTransition != null)
        {
            sceneTransition.gameObject.SetActive(true);
        }

        var slideTransition = EnsureManager(ref slideTransitionManagerPrefab, () => FindObjectOfType<SlideTransitionManager>(), "SlideTransitionManager");
        if (slideTransition != null)
        {
            slideTransition.EnsurePanelAssigned();
        }

        EnsureDevItemInjector();
    }

    private T EnsureManager<T>(ref GameObject prefabField, Func<T> instanceGetter, string resourcePath)
        where T : Component
    {
        var current = instanceGetter();
        if (current != null)
        {
            return current;
        }

        GameObject source = prefabField;
        if (source == null && !string.IsNullOrEmpty(resourcePath))
        {
            source = Resources.Load<GameObject>(resourcePath);
            if (source != null)
            {
                prefabField = source;
            }
        }

        GameObject instance;
        if (source != null)
        {
            instance = Instantiate(source);
        }
        else
        {
            instance = new GameObject(typeof(T).Name);
        }

        var component = instance.GetComponent<T>();
        if (component == null)
        {
            component = instance.AddComponent<T>();
        }

        return component;
    }

    private void EnsureDevItemInjector()
    {
        var injectors = Resources.FindObjectsOfTypeAll<DevItemInjector>();
        DevItemInjector fallback = null;

        foreach (var injector in injectors)
        {
            if (injector == null)
            {
                continue;
            }

            if (!injector.gameObject.scene.IsValid())
            {
                continue;
            }

            if (injector.SpawnedByInitializer)
            {
                if (fallback == null)
                {
                    fallback = injector;
                }

                continue;
            }

            ApplyDevItemInjectorSettings(injector);
            ActivateDevItemInjector(injector);

            if (fallback != null && fallback != injector)
            {
                Destroy(fallback.gameObject);
            }

            return;
        }

        var configured = InstantiateConfiguredDevItemInjector();
        if (configured != null)
        {
            configured.SpawnedByInitializer = true;
            ApplyDevItemInjectorSettings(configured);
            ActivateDevItemInjector(configured);

            if (fallback != null && fallback != configured)
            {
                Destroy(fallback.gameObject);
            }

            return;
        }

        if (fallback != null)
        {
            fallback.SpawnedByInitializer = true;
            ApplyDevItemInjectorSettings(fallback);
            ActivateDevItemInjector(fallback);
            return;
        }

        var createdFallback = new GameObject("DevItemInjector").AddComponent<DevItemInjector>();
        createdFallback.SpawnedByInitializer = true;
        ApplyDevItemInjectorSettings(createdFallback);
        ActivateDevItemInjector(createdFallback);
    }

    private DevItemInjector InstantiateConfiguredDevItemInjector()
    {
        var template = devItemInjectorPrefab;
        if (template == null)
        {
            template = Resources.Load<DevItemInjector>("DevItemInjector");
        }

        if (template == null)
        {
            return null;
        }

        return Instantiate(template);
    }

    private void ApplyDevItemInjectorSettings(DevItemInjector injector)
    {
        if (injector == null)
        {
            return;
        }

        var settings = ResolveDevItemInjectorSettings();
        if (settings == null)
        {
            if (!devItemInjectorSettingsWarningLogged)
            {
                Debug.LogWarning("[GameSessionInitializer] DevItemInjectorSettings asset is not assigned or found. Using DevItemInjector defaults.");
                devItemInjectorSettingsWarningLogged = true;
            }

            return;
        }

        injector.ConfigureFromSettings(settings);
    }

    private DevItemInjectorSettings ResolveDevItemInjectorSettings()
    {
        if (devItemInjectorSettings != null)
        {
            return devItemInjectorSettings;
        }

        var loaded = Resources.Load<DevItemInjectorSettings>("DevItemInjectorSettings");
        if (loaded != null)
        {
            devItemInjectorSettings = loaded;
        }

        return devItemInjectorSettings;
    }

    private static void ActivateDevItemInjector(DevItemInjector injector)
    {
        if (injector == null)
        {
            return;
        }

        DontDestroyOnLoad(injector.gameObject);
        injector.gameObject.SetActive(true);
    }

    private void CleanupInitializerSpawnedDevInjector()
    {
        var injectors = Resources.FindObjectsOfTypeAll<DevItemInjector>();
        foreach (var injector in injectors)
        {
            if (injector != null && injector.SpawnedByInitializer)
            {
                Destroy(injector.gameObject);
            }
        }
    }
}
