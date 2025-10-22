using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles game save/load/delete using json files in Application.persistentDataPath.
/// Collects data from PlayerManager, InventoryManager, FurnitureSaveManager and GameClock.
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    private static SaveGameManager instance;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    private string currentSlot;
    public string CurrentSlotKey => currentSlot;
    private Coroutine autoSaveCoroutine;
    public static SaveGameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SaveGameManager>();
                if (instance == null)
                {
                    var go = new GameObject("SaveGameManager");
                    instance = go.AddComponent<SaveGameManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        var furnitureMgr = FurnitureSaveManager.Instance;
        if (furnitureMgr != null)
            furnitureMgr.OnFurnitureChanged += TriggerSave;

        var inventoryMgr = InventoryManager.Instance;
        if (inventoryMgr != null)
            inventoryMgr.OnInventoryChanged += TriggerSave;

        var environmentMgr = EnvironmentStatsManager.Instance;
        if (environmentMgr != null)
            environmentMgr.OnStatsChanged += TriggerSave;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        var furnitureMgr = FurnitureSaveManager.Instance;
        if (furnitureMgr != null)
            furnitureMgr.OnFurnitureChanged -= TriggerSave;

        var inventoryMgr = InventoryManager.Instance;
        if (inventoryMgr != null)
            inventoryMgr.OnInventoryChanged -= TriggerSave;

        var environmentMgr = EnvironmentStatsManager.Instance;
        if (environmentMgr != null)
            environmentMgr.OnStatsChanged -= TriggerSave;

        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }

    void OnApplicationQuit()
    {
        TriggerSave();
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene.name != "MainMenu")
            TriggerSave();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (instance == this) instance = null;
            Destroy(gameObject);
        }
    }

    void TriggerSave()
    {
        if (!string.IsNullOrEmpty(currentSlot))
            Save(currentSlot);
    }

    void TriggerSave(int cozy, int nature) => TriggerSave();

    public void SaveCurrentSlot() => TriggerSave();

    void EnsureAutoSave()
    {
        if (autoSaveCoroutine == null)
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    System.Collections.IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            if (!string.IsNullOrEmpty(currentSlot))
                Save(currentSlot);
        }
    }

    string GetSlotPath(string slotKey)
    {
        return Path.Combine(Application.persistentDataPath, slotKey + ".json");
    }

    public bool HasSlot(string slotKey) => File.Exists(GetSlotPath(slotKey));

    public void Save(string slotKey)
    {
        if (string.IsNullOrEmpty(slotKey)) return;
        currentSlot = slotKey;
        EnsureAutoSave();

        BaseSaveData baseData;
        bool creative = slotKey.StartsWith("Creative", StringComparison.OrdinalIgnoreCase);
        if (creative)
        {
            var data = new CreativeSaveData();
            FillCommon(data);
            SaveManagers(data);
            baseData = data;
        }
        else
        {
            var data = new StorySaveData();
            FillCommon(data);
            SaveManagers(data);
            baseData = data;
        }

        string json = JsonUtility.ToJson(baseData, true);
        File.WriteAllText(GetSlotPath(slotKey), json);
    }

    public BaseSaveData LoadMetadata(string slotKey)
    {
        string path = GetSlotPath(slotKey);
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            bool creative = slotKey.StartsWith("Creative", StringComparison.OrdinalIgnoreCase);
            return creative ? (BaseSaveData)CreativeSaveData.FromJson(json)
                            : StorySaveData.FromJson(json);
        }
        catch
        {
            return null;
        }
    }

    public void Load(string slotKey)
    {
        string path = GetSlotPath(slotKey);
        currentSlot = slotKey;
        EnsureAutoSave();
        bool creative = slotKey.StartsWith("Creative", StringComparison.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            if (creative)
            {
                var emptyData = new CreativeSaveData();
                ApplyManagers(emptyData);
            }
            else
            {
                var emptyData = new StorySaveData();
                ApplyManagers(emptyData);
            }
            return;
        }

        string json = File.ReadAllText(path);
        if (creative)
        {
            var data = CreativeSaveData.FromJson(json);
            ApplyManagers(data);
        }
        else
        {
            var data = StorySaveData.FromJson(json);
            ApplyManagers(data);
        }
    }

    public void Delete(string slotKey)
    {
        string path = GetSlotPath(slotKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    void FillCommon(BaseSaveData data)
    {
        data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playTime = Time.time;
        data.location = SceneManager.GetActiveScene().name;
        var milestone = FindFirstObjectByType<MilestoneManager>();
        data.chapterName = milestone != null ? milestone.CurrentMilestoneID : "";

        data.screenshotFilename = currentSlot + "_screenshot.png";
        var path = Path.Combine(Application.persistentDataPath, data.screenshotFilename);
        try
        {
            ScreenCapture.CaptureScreenshot(path);
        }
        catch (Exception)
        {
            // ignore screenshot errors
        }
    }

    void SaveManagers(StorySaveData data)
    {
        // Include the player's transform by asking PlayerManager for its save data
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            data.player = player.GetSaveData();

        data.inventory = CollectInventory();
        SaveFurniture(data.furniture);

        var clock = GameClock.Instance;
        if (clock != null)
            data.clock = clock.GetSaveData();

        var money = MoneyManager.Instance;
        data.money = money != null ? money.CurrentMoney : 0;

        var milestone = MilestoneManager.Instance;
        data.milestoneIndex = milestone != null ? milestone.CurrentMilestoneIndex : 0;

        var env = EnvironmentStatsManager.Instance;
        if (env != null)
        {
            data.cozy = env.CozyTotal;
            data.nature = env.NatureTotal;
        }
    }

    void SaveManagers(CreativeSaveData data)
    {
        // Include the player's transform by asking PlayerManager for its save data
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            data.player = player.GetSaveData();

        data.ownedItems = CollectInventory();
        SaveFurniture(data.furniture);

        var clock = GameClock.Instance;
        if (clock != null)
            data.clock = clock.GetSaveData();

        var money = MoneyManager.Instance;
        data.money = money != null ? money.CurrentMoney : 0;

        var milestone = MilestoneManager.Instance;
        data.milestoneIndex = milestone != null ? milestone.CurrentMilestoneIndex : 0;

        var env = EnvironmentStatsManager.Instance;
        if (env != null)
        {
            data.cozy = env.CozyTotal;
            data.nature = env.NatureTotal;
        }
    }

    List<string> CollectInventory()
    {
        List<string> list = new();
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            foreach (var item in inv.GetFurnitureList())
            {
                for (int i = 0; i < item.quantity; i++)
                    list.Add(item.itemID);
            }
            foreach (var item in inv.GetMaterialList())
            {
                for (int i = 0; i < item.quantity; i++)
                    list.Add(item.itemID);
            }
        }
        return list;
    }

    void SaveFurniture(List<SaveSystem.PlacedFurniture> list)
    {
        var mgr = FurnitureSaveManager.Instance;
        if (mgr == null) return;

        var all = mgr.GetAllFurniture();
        foreach (var f in all)
        {
            list.Add(new SaveSystem.PlacedFurniture
            {
                id = f.furnitureID,
                position = f.GetPosition(),
                rotation = f.GetRotation(),
                sceneName = f.sceneName,
                parentUID = f.parentFurnitureID,
                uniqueID = f.uniqueID
            });
        }
    }

    void ApplyManagers(StorySaveData data)
    {
        // Restore player position and rotation through PlayerManager
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            player.ApplySaveData(data.player);

        ApplyInventory(data.inventory);
        ApplyFurniture(data.furniture);
        ApplyTime(data.clock);

        var money = MoneyManager.Instance;
        if (money != null)
            money.SetMoney(data.money);

        var milestone = MilestoneManager.Instance;
        if (milestone != null)
        {
            milestone.SetCurrentMilestoneIndex(data.milestoneIndex, notify: false);
        }

        var env = EnvironmentStatsManager.Instance;
        if (env != null)
            env.SetValues(data.cozy, data.nature);

        if (milestone != null)
        {
            milestone.RequestProgressUpdate();
        }
    }

    void ApplyManagers(CreativeSaveData data)
    {
        // Restore player position and rotation through PlayerManager
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            player.ApplySaveData(data.player);

        ApplyInventory(data.ownedItems);
        ApplyFurniture(data.furniture);
        ApplyTime(data.clock);

        var money = MoneyManager.Instance;
        if (money != null)
            money.SetMoney(data.money);

        var milestone = MilestoneManager.Instance;
        if (milestone != null)
        {
            milestone.SetCurrentMilestoneIndex(data.milestoneIndex, notify: false);
        }

        var env = EnvironmentStatsManager.Instance;
        if (env != null)
            env.SetValues(data.cozy, data.nature);

        if (milestone != null)
        {
            milestone.RequestProgressUpdate();
        }
    }

    void ApplyInventory(List<string> items)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        foreach (var item in inv.GetFurnitureList())
            inv.RemoveFurniture(item.itemID, item.quantity);
        foreach (var item in inv.GetMaterialList())
            inv.RemoveMaterial(item.itemID, item.quantity);

        foreach (var id in items)
        {
            bool added = false;
            var furnitureMgr = FurnitureDataManager.Instance;
            if (furnitureMgr != null && furnitureMgr.GetFurnitureData(id) != null)
            {
                inv.AddFurniture(id, 1);
                added = true;
            }
            else if (inv.GetMaterialData(id) != null)
            {
                inv.AddMaterial(id, 1);
                added = true;
            }

            if (!added)
            {
                Debug.LogWarning($"SaveGameManager.ApplyInventory: Unknown item ID '{id}'");
            }
        }
        inv.ForceInventoryUpdate();
    }

    void ApplyFurniture(List<SaveSystem.PlacedFurniture> list)
    {
        var mgr = FurnitureSaveManager.Instance;
        if (mgr == null) return;

        mgr.ClearAllSaveData();
        var allData = new FurnitureSaveManager.AllFurnitureData();
        foreach (var f in list)
        {
            allData.furnitureList.Add(new FurnitureSaveManager.FurnitureSaveData(
                f.id, f.sceneName, f.position, f.rotation, f.parentUID, f.uniqueID));
        }
        string json = JsonUtility.ToJson(allData);
        mgr.LoadFromData(json);
        var sceneName = SceneManager.GetActiveScene().name;
        mgr.LoadFurnitureForSceneAsync(sceneName);
    }

    void ApplyTime(GameClock.ClockData savedData)
    {
        var clock = FindFirstObjectByType<GameClock>();
        if (clock != null)
            clock.ApplySaveData(savedData);
    }
}
