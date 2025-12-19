using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
      public void ForceInventoryUpdate()
    {
      OnInventoryChanged?.Invoke();
    }

    private static InventoryManager instance;
    public static InventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<InventoryManager>();
            }
            return instance;
        }
    }

    // データ辞書
    private Dictionary<string, InventoryItem> furnitureInventory = new Dictionary<string, InventoryItem>();
    private Dictionary<string, InventoryItem> materialInventory = new Dictionary<string, InventoryItem>();
    private Dictionary<string, MaterialData> materialDatabase = new Dictionary<string, MaterialData>();

    public InventoryItem GetFurnitureItem(string id) =>
        furnitureInventory.TryGetValue(id, out var item) ? item : null;

    // イベント
    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action<InventoryItem> OnItemUpdated;
    public event Action OnInventoryChanged;

    private int bulkUpdateDepth = 0;
    private bool inventoryChangedDuringBulk = false;

    [Header("CSV File Paths")]
    public string materialsCSVPath = "Data/YUME_ROOF_Materials";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMaterialData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            instance = null;
            Destroy(gameObject);
        }
    }

    // ========== Context Menu Actions（Inspector右クリックメニュー） ==========


    [ContextMenu("Log Inventory Status")]
    void LogInventoryStatus()
    {
        Debug.Log("=== Inventory Status ===");
        Debug.Log($"Furniture Items: {furnitureInventory.Count}");
        foreach (var item in furnitureInventory)
        {
            Debug.Log($"  - {item.Key}: {item.Value.quantity}x");
        }

        Debug.Log($"Material Items: {materialInventory.Count}");
        foreach (var item in materialInventory)
        {
            Debug.Log($"  - {item.Key}: {item.Value.quantity}x");
        }
    }

    public int GetTotalFurnitureCount()
    {
        return furnitureInventory.Values.Sum(item => item.quantity);
    }

    // ========== 既存のメソッド（以下は変更なし） ==========

    void LoadMaterialData()
    {
        materialDatabase.Clear();

        var dataManager = FurnitureDataManager.Instance;
        var materialSOs = Resources.LoadAll<MaterialDataSO>("Data/ScriptableObjects/Materials");

        if (materialSOs == null || materialSOs.Length == 0)
        {
            Debug.LogWarning("No MaterialDataSO assets found in Resources/ Data/ScriptableObjects/Materials");
            return;
        }

        foreach (var so in materialSOs)
        {
            if (so == null || string.IsNullOrEmpty(so.materialID))
                continue;

            // FurnitureDataManagerにある辞書を優先して利用する
            var data = dataManager?.GetMaterialData(so.materialID);

            // 辞書にまだ存在しない場合はScriptableObjectから生成
            if (data == null)
            {
                data = new MaterialData
                {
                    materialID = so.materialID,
                    nameID = so.nameID,
                    materialName = so.materialName,
                    category = so.category,
                    maxStack = so.maxStack,
                    sellPrice = so.sellPrice,
                    iconName = so.iconName,
                    rarity = so.rarity,
                    descriptionID = so.descriptionID,
                    weatherAttribute = so.weatherAttribute,
                    sourceItems = so.sourceItems,
                    dropRates = so.dropRates
                };
            }

            materialDatabase[so.materialID] = data;
        }

        Debug.Log($"Loaded {materialDatabase.Count} materials into materialDatabase.");
    }

    // å®¶å…·ã‚’è¿½åŠ
    public bool AddFurniture(string furnitureID, int quantity = 1)
    {
        if (string.IsNullOrEmpty(furnitureID)) return false;

        if (furnitureInventory.ContainsKey(furnitureID))
        {
            furnitureInventory[furnitureID].AddQuantity(quantity);
            NotifyItemUpdated(furnitureInventory[furnitureID]);
        }
        else
        {
            var newItem = new InventoryItem(InventoryItem.ItemType.Furniture, furnitureID, quantity);
            furnitureInventory[furnitureID] = newItem;
            NotifyItemAdded(newItem);
        }

        NotifyInventoryChanged();
        return true;
    }

    // ç´ æã‚’è¿½åŠ
    public bool AddMaterial(string materialID, int quantity = 1)
    {
        if (string.IsNullOrEmpty(materialID)) return false;

        if (materialInventory.ContainsKey(materialID))
        {
            materialInventory[materialID].AddQuantity(quantity);
            NotifyItemUpdated(materialInventory[materialID]);
        }
        else
        {
            var newItem = new InventoryItem(InventoryItem.ItemType.Material, materialID, quantity);
            materialInventory[materialID] = newItem;
            NotifyItemAdded(newItem);
        }

        NotifyInventoryChanged();
        return true;
    }

    // 家具を削除（修正版：数量0でも保持）
    public bool RemoveFurniture(string furnitureID, int quantity = 1)
    {
        if (!furnitureInventory.ContainsKey(furnitureID)) return false;

        var item = furnitureInventory[furnitureID];

        // 数量が足りない場合は削除しない
        if (item.quantity < quantity) return false;

        // 数量を減らす
        item.quantity -= quantity;

        // 数量が0以下でもアイテムは削除しない（レシピとして保持）
        // ただし、数量は0に固定
        if (item.quantity < 0)
        {
            item.quantity = 0;
        }

        NotifyItemUpdated(item);
        NotifyInventoryChanged();
        return true;
    }

    // ç´ æã‚’å‰Šé™¤
    public bool RemoveMaterial(string materialID, int quantity = 1)
    {
        if (!materialInventory.ContainsKey(materialID)) return false;

        var item = materialInventory[materialID];
        if (item.RemoveQuantity(quantity))
        {
            if (item.quantity <= 0)
            {
                materialInventory.Remove(materialID);
                NotifyItemRemoved(item);
            }
            else
            {
                NotifyItemUpdated(item);
            }
            NotifyInventoryChanged();
            return true;
        }
        return false;
    }

    public void BeginBulkUpdate()
    {
        bulkUpdateDepth++;
    }

    public void EndBulkUpdate()
    {
        if (bulkUpdateDepth == 0)
        {
            return;
        }

        bulkUpdateDepth--;

        if (bulkUpdateDepth == 0 && inventoryChangedDuringBulk)
        {
            inventoryChangedDuringBulk = false;
            OnInventoryChanged?.Invoke();
        }
    }

    private void NotifyItemAdded(InventoryItem item)
    {
        if (bulkUpdateDepth == 0)
        {
            OnItemAdded?.Invoke(item);
        }
    }

    private void NotifyItemRemoved(InventoryItem item)
    {
        if (bulkUpdateDepth == 0)
        {
            OnItemRemoved?.Invoke(item);
        }
    }

    private void NotifyItemUpdated(InventoryItem item)
    {
        if (bulkUpdateDepth == 0)
        {
            OnItemUpdated?.Invoke(item);
        }
    }

    private void NotifyInventoryChanged()
    {
        if (bulkUpdateDepth > 0)
        {
            inventoryChangedDuringBulk = true;
            return;
        }

        OnInventoryChanged?.Invoke();
    }

    // ã‚¢ã‚¤ãƒ†ãƒ ã®æ‰€æŒæ•°ã‚’å–å¾—
    public int GetItemCount(InventoryItem.ItemType type, string itemID)
    {
        if (type == InventoryItem.ItemType.Furniture)
        {
            return furnitureInventory.ContainsKey(itemID) ? furnitureInventory[itemID].quantity : 0;
        }
        else
        {
            return materialInventory.ContainsKey(itemID) ? materialInventory[itemID].quantity : 0;
        }
    }

    // 家具リストを取得（ソート・フィルター機能付き）
    public List<InventoryItem> GetFurnitureList(string sortType = "name", bool showOnlyCraftable = false, bool showOnlyFavorites = false, bool ascending = true)
    {
        // すべての家具を取得（数量0も含む）
        var list = furnitureInventory.Values.ToList();

        // フィルタリング
        if (showOnlyCraftable)
        {
            list = list.Where(item => item.canCraft).ToList();
        }

        if (showOnlyFavorites)
        {
            list = list.Where(item => item.isFavorite).ToList();
        }

        // ソート（昇順・降順対応）
        list = SortItems(list, sortType, ascending);

        return list;
    }

    // 素材リストを取得（ソート機能付き）
    public List<InventoryItem> GetMaterialList(string sortType = "name", bool showOnlyFavorites = false, bool ascending = true)
    {
        var list = materialInventory.Values.ToList();

        // フィルタリング
        if (showOnlyFavorites)
        {
            list = list.Where(item => item.isFavorite).ToList();
        }

        // ソート（昇順・降順対応）
        list = SortItems(list, sortType, ascending);

        return list;
    }

    // アイテムをソート（昇順・降順対応版）
    private List<InventoryItem> SortItems(List<InventoryItem> items, string sortType, bool ascending = true)
    {
        switch (sortType.ToLower())
        {
            case "rarity":
                // レアリティでソート
                if (ascending)
                {
                    // 昇順：Common → Uncommon → Rare
                    items = items.OrderBy(item => GetItemRarity(item))
                                .ThenBy(item => GetItemName(item))
                                .ToList();
                }
                else
                {
                    // 降順：Rare → Uncommon → Common
                    items = items.OrderByDescending(item => GetItemRarity(item))
                                .ThenBy(item => GetItemName(item))
                                .ToList();
                }
                break;

            case "name":
                // 名前でソート
                if (ascending)
                {
                    // 昇順：A → Z
                    items = items.OrderBy(item => GetItemName(item)).ToList();
                }
                else
                {
                    // 降順：Z → A
                    items = items.OrderByDescending(item => GetItemName(item)).ToList();
                }
                break;

            case "craftable":
                // クラフト可能を優先、その後名前順
                items = items.OrderByDescending(item => item.canCraft)
                            .ThenBy(item => GetItemName(item))
                            .ToList();
                break;

            case "favorite":
                // お気に入りを優先、その後名前順
                items = items.OrderByDescending(item => item.isFavorite)
                            .ThenBy(item => GetItemName(item))
                            .ToList();
                break;

            case "quantity":
                // 数量順
                if (ascending)
                {
                    items = items.OrderBy(item => item.quantity)
                                .ThenBy(item => GetItemName(item))
                                .ToList();
                }
                else
                {
                    items = items.OrderByDescending(item => item.quantity)
                                .ThenBy(item => GetItemName(item))
                                .ToList();
                }
                break;

            default:
                // デフォルトは名前順（A-Z）
                items = items.OrderBy(item => GetItemName(item)).ToList();
                break;
        }

        return items;
    }

    // ã‚¢ã‚¤ãƒ†ãƒ ã®åå‰ã‚’å–å¾—
    public string GetItemName(InventoryItem item)
    {
        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
            return data?.nameID ?? item.itemID;
        }
        else
        {
            return materialDatabase.ContainsKey(item.itemID) ?
                   materialDatabase[item.itemID].materialName : item.itemID;
        }
    }

    // ã‚¢ã‚¤ãƒ†ãƒ ã®ãƒ¬ã‚¢ãƒªãƒ†ã‚£ã‚’å–å¾—
    public Rarity GetItemRarity(InventoryItem item)
    {
        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
            return data?.rarity ?? Rarity.Common;
        }
        else
        {
            return materialDatabase.ContainsKey(item.itemID) ?
                   materialDatabase[item.itemID].rarity : Rarity.Common;
        }
    }

    // ç´ æãƒ‡ãƒ¼ã‚¿ã‚’å–å¾—
    public MaterialData GetMaterialData(string materialID)
    {
        if (string.IsNullOrEmpty(materialID)) return null;

        if (materialDatabase.TryGetValue(materialID, out var data))
            return data;

        // 未登録の場合はFurnitureDataManager経由で取得して辞書に追加する
        var dataManager = FurnitureDataManager.Instance;
        data = dataManager?.GetMaterialData(materialID);
        if (data != null)
        {
            materialDatabase[materialID] = data;
            return data;
        }

        return null;
    }

    // お気に入り切り替え
    public void ToggleFavorite(InventoryItem.ItemType type, string itemID)
    {
        InventoryItem item = null;
        if (type == InventoryItem.ItemType.Furniture)
        {
            if (furnitureInventory.ContainsKey(itemID))
            {
                item = furnitureInventory[itemID];
            }
        }
        else
        {
            if (materialInventory.ContainsKey(itemID))
            {
                item = materialInventory[itemID];
            }
        }

        if (item != null)
        {
            // 注意：ItemCardから既にトグルされた状態で来るので、
            // ここでは再度トグルしない（既に変更済み）
            // item.ToggleFavorite(); // この行は削除

            Debug.Log($"InventoryManager - Item: {itemID}, Favorite: {item.isFavorite}");

            OnItemUpdated?.Invoke(item);
            OnInventoryChanged?.Invoke();
        }
    }

    // ã‚¯ãƒ©ãƒ•ãƒˆå¯èƒ½ã‹ãƒã‚§ãƒƒã‚¯ï¼ˆãƒ¬ã‚·ãƒ”ã‚·ã‚¹ãƒ†ãƒ ã¨é€£æºäºˆå®šï¼‰
    public void UpdateCraftableStatus()
    {
        // TODO: ãƒ¬ã‚·ãƒ”ã‚·ã‚¹ãƒ†ãƒ ã¨é€£æºã—ã¦ã€å„å®¶å…·ã®ã‚¯ãƒ©ãƒ•ãƒˆå¯èƒ½çŠ¶æ…‹ã‚’æ›´æ–°
        foreach (var item in furnitureInventory.Values)
        {
            // ä»®å®Ÿè£…ï¼šãƒ©ãƒ³ãƒ€ãƒ ã«ã‚¯ãƒ©ãƒ•ãƒˆå¯èƒ½ã‚’è¨­å®š
            item.canCraft = UnityEngine.Random.Range(0, 2) == 0;
        }
    }
}
