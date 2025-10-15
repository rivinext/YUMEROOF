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
    [SerializeField] private GameObject playerPrefab;
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
            DevItemInjector.ResetInjected();
            Destroy(gameObject);
            return;
        }

        EnsurePersistentManagers();
        MilestoneManager.CreateIfNeeded(milestoneManagerPrefab);
        EnsurePlayer(scene);

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        FindObjectOfType<DevItemInjector>(true)?.Inject();
        InventoryManager.Instance.ForceInventoryUpdate();
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        EnsureDevItemInjector();
#endif
    }

    private void EnsurePlayer(Scene scene)
    {
        if (scene.name == "MainMenu" || scene == gameObject.scene)
        {
            return;
        }

        GameObject playerObject = null;

        var persistence = FindRuntimeObject<PlayerPersistence>();
        if (persistence != null)
        {
            playerObject = persistence.gameObject;
        }
        else
        {
            var manager = FindRuntimeObject<PlayerManager>();
            if (manager != null)
            {
                playerObject = manager.gameObject;
            }
            else
            {
                var controller = FindRuntimeObject<PlayerController>();
                if (controller != null)
                {
                    playerObject = controller.gameObject;
                }
            }
        }

        if (playerObject == null)
        {
            var source = playerPrefab;
            if (source == null)
            {
                source = Resources.Load<GameObject>("Player");
                if (source != null)
                {
                    playerPrefab = source;
                }
            }

            if (source != null)
            {
                playerObject = Instantiate(source);
            }
            else
            {
                playerObject = new GameObject("Player");
                Debug.LogWarning("Player prefab was not assigned. Created a placeholder Player GameObject at runtime.");
            }
        }

        if (playerObject == null)
        {
            return;
        }

        var root = playerObject.transform.root.gameObject;

        if (root.GetComponent<PlayerManager>() == null)
        {
            root.AddComponent<PlayerManager>();
        }

        if (root.GetComponent<PlayerController>() == null)
        {
            root.AddComponent<PlayerController>();
        }

        if (root.GetComponent<PlayerPersistence>() == null)
        {
            root.AddComponent<PlayerPersistence>();
        }

        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        if (root.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(root);
        }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static T FindRuntimeObject<T>() where T : Component
    {
        var active = FindObjectOfType<T>();
        if (active != null)
        {
            return active;
        }

        foreach (var candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate == null)
                continue;

            var go = candidate.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded)
                continue;

            if (candidate.hideFlags != HideFlags.None && candidate.hideFlags != HideFlags.HideInHierarchy)
                continue;

            return candidate;
        }

        return null;
    }
#else
    private static T FindRuntimeObject<T>() where T : Component
    {
        return FindObjectOfType<T>();
    }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void EnsureDevItemInjector()
    {
        var existing = FindObjectOfType<DevItemInjector>(true);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        DevItemInjector injector = null;
        if (devItemInjectorPrefab != null)
        {
            injector = Instantiate(devItemInjectorPrefab);
        }
        else
        {
            var loaded = Resources.Load<DevItemInjector>("DevItemInjector");
            if (loaded != null)
            {
                injector = Instantiate(loaded);
            }
        }

        if (injector == null)
        {
            injector = new GameObject("DevItemInjector").AddComponent<DevItemInjector>();
        }

        injector.gameObject.SetActive(true);
    }
#endif
}
