using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles game save/load/delete using json files in Application.persistentDataPath.
/// Collects data from PlayerManager, InventoryManager, FurnitureSaveManager and GameClock.
/// </summary>
public class SaveGameManager : MonoBehaviour, IIndependentMaterialColorSaveAccessor
{
    private static SaveGameManager instance;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    private string currentSlot;
    private bool applied_0_1_6_seed;
    private bool storyOpeningShown;
    public string CurrentSlotKey => currentSlot;
    public bool Applied_0_1_6_Seed
    {
        get => applied_0_1_6_seed;
        set => applied_0_1_6_seed = value;
    }
    public bool StoryOpeningShown
    {
        get => storyOpeningShown;
        set => storyOpeningShown = value;
    }
    public event Action<string> OnSlotKeyChanged;
    private Coroutine autoSaveCoroutine;
    private readonly Dictionary<string, IndependentMaterialColorSaveData> independentMaterialColorStore = new();
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

    public void SetCurrentSlotKey(string slotKey, bool ensureAutoSave = false)
    {
        bool changed = currentSlot != slotKey;
        currentSlot = slotKey;

        if (ensureAutoSave && !string.IsNullOrEmpty(slotKey))
        {
            EnsureAutoSave();
        }

        if (changed && !string.IsNullOrEmpty(slotKey))
        {
            IndependentMaterialColorController.SetSaveContextForAllControllers(slotKey, this);
            OnSlotKeyChanged?.Invoke(slotKey);
            MaterialHuePresetManager.EnsureAllManagersInitialized();
        }
    }

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

    string GetSaveDirectory()
    {
#if UNITY_EDITOR
        string versionFolder = "editor";
#else
#if DEMO_VERSION
        string versionFolder = "demo";
#else
        string versionFolder = "release";
#endif
#endif
        string directory = Path.Combine(Application.persistentDataPath, versionFolder);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        return directory;
    }

    string GetSlotPath(string slotKey)
    {
        return Path.Combine(GetSaveDirectory(), slotKey + ".json");
    }

    public bool HasSlot(string slotKey) => File.Exists(GetSlotPath(slotKey));

    public void Save(string slotKey)
    {
        if (string.IsNullOrEmpty(slotKey)) return;
        SetCurrentSlotKey(slotKey, ensureAutoSave: true);

        BaseSaveData baseData;
        bool creative = slotKey.StartsWith("Creative", StringComparison.OrdinalIgnoreCase);
        if (creative)
        {
            var data = new CreativeSaveData();
            FillCommon(data);
            SaveManagers(data);
            data.applied_0_1_6_seed = applied_0_1_6_seed;
            EnsureIndependentMaterialColorSlotCached(slotKey);
            data.independentMaterialColors = GetCachedIndependentMaterialColors(slotKey);
            data.independentMaterialColorSlots = GetCachedIndependentMaterialColorSlots();
            baseData = data;
        }
        else
        {
            var data = new StorySaveData();
            FillCommon(data);
            SaveManagers(data);
            data.applied_0_1_6_seed = applied_0_1_6_seed;
            EnsureIndependentMaterialColorSlotCached(slotKey);
            data.independentMaterialColors = GetCachedIndependentMaterialColors(slotKey);
            data.independentMaterialColorSlots = GetCachedIndependentMaterialColorSlots();
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

    public bool Load(string slotKey)
    {
        string path = GetSlotPath(slotKey);
        SetCurrentSlotKey(slotKey, ensureAutoSave: true);
        bool creative = slotKey.StartsWith("Creative", StringComparison.OrdinalIgnoreCase);
        bool createdNewSave = false;

        if (!File.Exists(path))
        {
            createdNewSave = true;
            MaterialHuePresetManager.ClearSavedPresetsForSlot(slotKey);
            if (creative)
            {
                var emptyData = new CreativeSaveData();
                applied_0_1_6_seed = emptyData.applied_0_1_6_seed;
                storyOpeningShown = false;
                CacheIndependentMaterialColors(slotKey, emptyData.independentMaterialColors, emptyData.independentMaterialColorSlots);
                ApplyManagers(emptyData);
            }
            else
            {
                var emptyData = new StorySaveData();
                applied_0_1_6_seed = emptyData.applied_0_1_6_seed;
                storyOpeningShown = emptyData.storyOpeningShown;
                CacheIndependentMaterialColors(slotKey, emptyData.independentMaterialColors, emptyData.independentMaterialColorSlots);
                ApplyManagers(emptyData);
            }
            IndependentMaterialColorController.SetSaveContextForAllControllers(slotKey, this);
            return createdNewSave;
        }

        string json = File.ReadAllText(path);
        if (creative)
        {
            var data = CreativeSaveData.FromJson(json);
            applied_0_1_6_seed = data != null && data.applied_0_1_6_seed;
            storyOpeningShown = false;
            CacheIndependentMaterialColors(slotKey, data.independentMaterialColors, data.independentMaterialColorSlots);
            ApplyManagers(data);
        }
        else
        {
            var data = StorySaveData.FromJson(json);
            applied_0_1_6_seed = data != null && data.applied_0_1_6_seed;
            storyOpeningShown = data != null && data.storyOpeningShown;
            CacheIndependentMaterialColors(slotKey, data.independentMaterialColors, data.independentMaterialColorSlots);
            ApplyManagers(data);
        }

        IndependentMaterialColorController.SetSaveContextForAllControllers(slotKey, this);

        return createdNewSave;
    }

    public void Delete(string slotKey)
    {
        string path = GetSlotPath(slotKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        MaterialHuePresetManager.ClearSavedPresetsForSlot(slotKey);
    }

    void FillCommon(BaseSaveData data)
    {
        data.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playTime = Time.time;
        data.location = SceneManager.GetActiveScene().name;
        var milestone = FindFirstObjectByType<MilestoneManager>();
        data.chapterName = milestone != null ? milestone.CurrentMilestoneID : "";
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

        data.storyOpeningShown = storyOpeningShown;

        var huePresetManagers = FindObjectsOfType<MaterialHuePresetManager>(includeInactive: true);
        if (huePresetManagers != null && huePresetManagers.Length > 0)
        {
            data.materialHue = new MaterialHueSaveData();
            foreach (var huePresetManager in huePresetManagers)
            {
                if (huePresetManager == null)
                {
                    continue;
                }

                var managerSaveData = huePresetManager.GetSaveData();
                if (managerSaveData != null)
                {
                    data.materialHue.managers.Add(managerSaveData);
                }
            }
        }

        SaveWardrobeSelections(data.wardrobeSelections, out data.hasWardrobeSelections);
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
        data.unlimitedMoney = money != null && money.UnlimitedMoney;

        var milestone = MilestoneManager.Instance;
        data.milestoneIndex = milestone != null ? milestone.CurrentMilestoneIndex : 0;

        var env = EnvironmentStatsManager.Instance;
        if (env != null)
        {
            data.cozy = env.CozyTotal;
            data.nature = env.NatureTotal;
        }

        var huePresetManagers = FindObjectsOfType<MaterialHuePresetManager>(includeInactive: true);
        if (huePresetManagers != null && huePresetManagers.Length > 0)
        {
            data.materialHue = new MaterialHueSaveData();
            foreach (var huePresetManager in huePresetManagers)
            {
                if (huePresetManager == null)
                {
                    continue;
                }

                var managerSaveData = huePresetManager.GetSaveData();
                if (managerSaveData != null)
                {
                    data.materialHue.managers.Add(managerSaveData);
                }
            }
        }

        SaveWardrobeSelections(data.wardrobeSelections, out data.hasWardrobeSelections);
    }

    List<InventoryEntry> CollectInventory()
    {
        List<InventoryEntry> list = new();
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            foreach (var item in inv.GetFurnitureList())
            {
                list.Add(new InventoryEntry
                {
                    itemID = item.itemID,
                    quantity = item.quantity,
                    isMaterial = false
                });
            }
            foreach (var item in inv.GetMaterialList())
            {
                list.Add(new InventoryEntry
                {
                    itemID = item.itemID,
                    quantity = item.quantity,
                    isMaterial = true
                });
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
                layer = f.layer,
                parentUID = f.parentFurnitureID,
                uniqueID = f.uniqueID,
                wallParentId = f.wallParentId,
                wallParentName = f.wallParentName,
                wallParentPath = f.wallParentPath
            });
        }
    }

    void ApplyManagers(StorySaveData data)
    {
        // Restore player position and rotation through PlayerManager
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            player.ApplySaveData(data.player);

        ApplyInventory(data.inventory, data.legacyInventory);
        ApplyFurniture(data.furniture);
        ApplyTime(data.clock);

        var money = MoneyManager.Instance;
        if (money != null)
        {
            money.SetMoney(data.money);
        }

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

        ApplyHuePresets(data.materialHue);
        ApplyWardrobeSelections(data.wardrobeSelections, data.hasWardrobeSelections);
    }

    void ApplyManagers(CreativeSaveData data)
    {
        // Restore player position and rotation through PlayerManager
        var player = FindFirstObjectByType<PlayerManager>();
        if (player != null)
            player.ApplySaveData(data.player);

        ApplyInventory(data.ownedItems, data.legacyOwnedItems);
        ApplyFurniture(data.furniture);
        ApplyTime(data.clock);

        var money = MoneyManager.Instance;
        if (money != null)
        {
            money.SetMoney(data.money);
            money.UnlimitedMoney = data.unlimitedMoney;
        }

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

        ApplyHuePresets(data.materialHue);
        ApplyWardrobeSelections(data.wardrobeSelections, data.hasWardrobeSelections);
    }

    void ApplyHuePresets(MaterialHueSaveData data)
    {
        MaterialHuePresetManager.ApplySaveDataToAllManagers(data);
    }

    void ApplyInventory(List<InventoryEntry> items, List<string> legacyItems)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        inv.BeginBulkUpdate();

        inv.ClearAllInventory();

        if (items != null && items.Count > 0)
        {
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemID))
                {
                    continue;
                }

                if (item.isMaterial)
                {
                    inv.AddMaterial(item.itemID, item.quantity);
                }
                else
                {
                    inv.AddFurniture(item.itemID, item.quantity);
                }
            }
        }
        else if (legacyItems != null && legacyItems.Count > 0)
        {
            ApplyLegacyInventory(inv, legacyItems);
        }

        inv.EndBulkUpdate();
    }

    void ApplyLegacyInventory(InventoryManager inv, List<string> items)
    {
        var counts = new Dictionary<string, int>();
        foreach (var id in items)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            string trimmed = id.Trim();
            counts[trimmed] = counts.TryGetValue(trimmed, out var count) ? count + 1 : 1;
        }

        foreach (var kvp in counts)
        {
            bool added = false;
            var materialData = inv.GetMaterialData(kvp.Key);
            if (materialData != null)
            {
                inv.AddMaterial(kvp.Key, kvp.Value);
                added = true;
            }
            else
            {
                var furnitureMgr = FurnitureDataManager.Instance;
                if (furnitureMgr != null && furnitureMgr.GetFurnitureData(kvp.Key) != null)
                {
                    inv.AddFurniture(kvp.Key, kvp.Value);
                    added = true;
                }
            }

            if (!added)
            {
                Debug.LogWarning($"SaveGameManager.ApplyInventory: Unknown item ID '{kvp.Key}'");
            }
        }
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
                f.id,
                f.sceneName,
                f.position,
                f.rotation,
                f.layer,
                f.parentUID,
                f.uniqueID,
                f.wallParentId,
                f.wallParentName,
                f.wallParentPath));
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

    void SaveWardrobeSelections(List<WardrobeSelectionEntry> target, out bool hasSelections)
    {
        hasSelections = false;
        if (target == null)
        {
            return;
        }

        target.Clear();

        var wardrobe = FindFirstObjectByType<WardrobeUIController>();
        if (wardrobe == null)
        {
            return;
        }

        wardrobe.CollectSelectionEntries(target);
        hasSelections = target.Count > 0;
    }

    void ApplyWardrobeSelections(List<WardrobeSelectionEntry> entries, bool hasSelections)
    {
        if (!hasSelections || entries == null)
        {
            return;
        }

        var wardrobe = FindFirstObjectByType<WardrobeUIController>();
        if (wardrobe != null)
        {
            wardrobe.ApplySelectionEntries(entries);
        }
    }

    public void SaveColor(string slotId, string key, HSVColor color)
    {
        if (string.IsNullOrEmpty(slotId) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var data = GetOrCreateIndependentMaterialColors(slotId);
        data.SetColor(key, color);
    }

    public bool TryGetColor(string slotId, string key, out HSVColor color)
    {
        color = default;

        if (string.IsNullOrEmpty(slotId) || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (independentMaterialColorStore.TryGetValue(slotId, out var data) && data != null)
        {
            return data.TryGetColor(key, out color);
        }

        return false;
    }

    private IndependentMaterialColorSaveData GetOrCreateIndependentMaterialColors(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return new IndependentMaterialColorSaveData();
        }

        if (!independentMaterialColorStore.TryGetValue(slotId, out var data) || data == null)
        {
            data = new IndependentMaterialColorSaveData();
            independentMaterialColorStore[slotId] = data;
        }

        return data;
    }

    private void CacheIndependentMaterialColors(string slotId, IndependentMaterialColorSaveData data, List<IndependentMaterialColorSlotEntry> slots)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return;
        }

        if (slots != null)
        {
            foreach (var slotEntry in slots)
            {
                if (slotEntry == null || string.IsNullOrWhiteSpace(slotEntry.slotKey))
                {
                    continue;
                }

                independentMaterialColorStore[slotEntry.slotKey.Trim()] = slotEntry.colors != null
                    ? new IndependentMaterialColorSaveData(slotEntry.colors)
                    : new IndependentMaterialColorSaveData();
            }
        }

        independentMaterialColorStore[slotId] = data != null
            ? new IndependentMaterialColorSaveData(data)
            : new IndependentMaterialColorSaveData();
    }

    private IndependentMaterialColorSaveData GetCachedIndependentMaterialColors(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return new IndependentMaterialColorSaveData();
        }

        if (independentMaterialColorStore.TryGetValue(slotId, out var data) && data != null)
        {
            return new IndependentMaterialColorSaveData(data);
        }

        return new IndependentMaterialColorSaveData();
    }

    private List<IndependentMaterialColorSlotEntry> GetCachedIndependentMaterialColorSlots()
    {
        List<IndependentMaterialColorSlotEntry> slots = new();
        foreach (var kvp in independentMaterialColorStore)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            slots.Add(new IndependentMaterialColorSlotEntry
            {
                slotKey = kvp.Key.Trim(),
                colors = kvp.Value != null ? new IndependentMaterialColorSaveData(kvp.Value) : new IndependentMaterialColorSaveData()
            });
        }

        return slots;
    }

    private void EnsureIndependentMaterialColorSlotCached(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            return;
        }

        if (!independentMaterialColorStore.ContainsKey(slotId))
        {
            independentMaterialColorStore[slotId] = new IndependentMaterialColorSaveData();
        }
    }
}
