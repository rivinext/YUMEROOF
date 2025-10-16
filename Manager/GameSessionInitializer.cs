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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private DevItemInjector devItemInjectorPrefab;
    [SerializeField] private bool debugLogging = true;
#endif

    private const string LogPrefix = "[GameSessionInitializer]";

    void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} Awake. ExistingInstance={(Instance != null && Instance != this)}");
        }
#endif
        if (Instance != null && Instance != this)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.LogWarning($"{LogPrefix} Duplicate initializer detected. Destroying this instance.");
            }
#endif
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} Instance registered and marked DontDestroyOnLoad.");
        }
#endif
        EnsurePersistentManagers();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} OnDestroy. Static instance cleared and sceneLoaded unsubscribed.");
            }
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} OnSceneLoaded => {scene.name} (mode={mode}). initialized={initialized}, slotKey={slotKey}.");
        }
#endif
        if (scene.name == "MainMenu")
        {
            DevItemInjector.ResetInjected();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Returned to MainMenu. Destroying initializer and resetting injector state.");
            }
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} DelayedLoad started. Awaiting one frame before initialization.");
        }
#endif
        yield return null;

        var clock = FindObjectOfType<GameClock>();
        if (clock == null)
        {
            clock = new GameObject("GameClock").AddComponent<GameClock>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Created GameClock as it was missing.");
            }
#endif
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else if (debugLogging)
        {
            Debug.Log($"{LogPrefix} Found existing GameClock on '{clock.gameObject.name}'.");
        }
#endif

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
            {
                Instantiate(furnitureDataManagerPrefab);
            }
            else
            {
                new GameObject("FurnitureDataManager").AddComponent<FurnitureDataManager>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugLogging)
                {
                    Debug.LogWarning($"{LogPrefix} FurnitureDataManager prefab missing. Created new GameObject instance.");
                }
#endif
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} FurnitureDataManager instantiated during DelayedLoad.");
            }
#endif
            createdManager = true;
        }
        if (FurnitureSaveManager.Instance == null)
        {
            if (furnitureSaveManagerPrefab != null)
            {
                Instantiate(furnitureSaveManagerPrefab);
            }
            else
            {
                new GameObject("FurnitureSaveManager").AddComponent<FurnitureSaveManager>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugLogging)
                {
                    Debug.LogWarning($"{LogPrefix} FurnitureSaveManager prefab missing. Created new GameObject instance.");
                }
#endif
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} FurnitureSaveManager instantiated during DelayedLoad.");
            }
#endif
            createdManager = true;
        }
        if (InventoryManager.Instance == null)
        {
            if (inventoryManagerPrefab != null)
            {
                Instantiate(inventoryManagerPrefab);
            }
            else
            {
                new GameObject("InventoryManager").AddComponent<InventoryManager>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugLogging)
                {
                    Debug.LogWarning($"{LogPrefix} InventoryManager prefab missing. Created new GameObject instance.");
                }
#endif
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} InventoryManager instantiated during DelayedLoad.");
            }
#endif
            createdManager = true;
        }

        if (createdManager)
        {
            // Wait a frame to ensure managers initialize before loading
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Managers were instantiated during DelayedLoad. Waiting an extra frame before loading save.");
            }
#endif
            yield return null;
        }

        SaveGameManager.Instance.Load(slotKey);
        initialized = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} Save loaded for slot '{slotKey}'. Initialization complete.");
        }
#endif
        slotKey = null;
    }

    private void EnsurePersistentManagers()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} Ensuring persistent managers.");
        }
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EnsureDevItemInjector();
#endif
    }

    private T EnsureManager<T>(ref GameObject prefabField, Func<T> instanceGetter, string resourcePath)
        where T : Component
    {
        var current = instanceGetter();
        if (current != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Found existing manager {typeof(T).Name} on '{current.gameObject.name}'.");
            }
#endif
            return current;
        }

        GameObject source = prefabField;
        if (source == null && !string.IsNullOrEmpty(resourcePath))
        {
            source = Resources.Load<GameObject>(resourcePath);
            if (source != null)
            {
                prefabField = source;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugLogging)
                {
                    Debug.Log($"{LogPrefix} Loaded prefab for {typeof(T).Name} from Resources '{resourcePath}'.");
                }
#endif
            }
        }

        GameObject instance;
        if (source != null)
        {
            instance = Instantiate(source);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Instantiated manager {typeof(T).Name} from prefab '{source.name}'.");
            }
#endif
        }
        else
        {
            instance = new GameObject(typeof(T).Name);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.LogWarning($"{LogPrefix} No prefab/resource for {typeof(T).Name}. Created empty GameObject.");
            }
#endif
        }

        var component = instance.GetComponent<T>();
        if (component == null)
        {
            component = instance.AddComponent<T>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Added missing component {typeof(T).Name} to '{instance.name}'.");
            }
#endif
        }

        return component;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void EnsureDevItemInjector()
    {
        var existing = FindObjectOfType<DevItemInjector>(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} EnsureDevItemInjector. Existing={(existing != null ? existing.name : "none")}, prefab={(devItemInjectorPrefab != null ? devItemInjectorPrefab.name : "null")}.");
        }
#endif
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            existing.gameObject.SetActive(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Reusing existing DevItemInjector '{existing.name}'.");
            }
#endif
            return;
        }

        DevItemInjector injector = null;
        if (devItemInjectorPrefab != null)
        {
            injector = Instantiate(devItemInjectorPrefab);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.Log($"{LogPrefix} Instantiated DevItemInjector from prefab '{devItemInjectorPrefab.name}'.");
            }
#endif
        }
        else
        {
            var loaded = Resources.Load<DevItemInjector>("DevItemInjector");
            if (loaded != null)
            {
                injector = Instantiate(loaded);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugLogging)
                {
                    Debug.Log($"{LogPrefix} Instantiated DevItemInjector from Resources '{loaded.name}'.");
                }
#endif
            }
        }

        if (injector == null)
        {
            injector = new GameObject("DevItemInjector").AddComponent<DevItemInjector>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugLogging)
            {
                Debug.LogWarning($"{LogPrefix} No prefab or resource found. Created fallback DevItemInjector.");
            }
#endif
        }

        DontDestroyOnLoad(injector.gameObject);
        injector.gameObject.SetActive(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugLogging)
        {
            Debug.Log($"{LogPrefix} DevItemInjector '{injector.name}' marked DontDestroyOnLoad and activated.");
        }
#endif
    }
#endif
}
