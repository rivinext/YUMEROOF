using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class InventoryPlacementBridge : MonoBehaviour
{
    private static InventoryPlacementBridge instance;
    public static InventoryPlacementBridge Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InventoryPlacementBridge>();
                if (instance == null)
                {
                    GameObject go = new GameObject("InventoryPlacementBridge");
                    instance = go.AddComponent<InventoryPlacementBridge>();
                }
            }
            return instance;
        }
    }

    // 配置中の情報
    private string placingItemID;
    private bool isPlacingFromInventory = false;

    // 参照（毎回取得するため削除）
    // private InventoryUI inventoryUI;
    // private FreePlacementSystem placementSystem;

    // イベント
    public event Action<string> OnPlacementComplete;
    public event Action OnPlacementCancelled;

    [Header("Debug")]
    public bool debugMode = true;

    [SerializeField]
    private List<string> disabledScenes = new List<string>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            if (debugMode)
                Debug.Log("[InventoryPlacementBridge] Initialized");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // シーンロード時のイベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode)
            Debug.Log($"[InventoryPlacementBridge] Scene loaded: {scene.name}");
    }

    // 現在のシーンで配置が無効かどうか
    public bool IsPlacementDisabledScene()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        return disabledScenes != null && disabledScenes.Contains(activeScene);
    }

    // インベントリからの配置を開始
    public void StartPlacementFromInventory(InventoryItem item)
    {
        if (IsPlacementDisabledScene())
        {
            string activeScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[InventoryPlacementBridge] Placement disabled in scene: {activeScene}");
            return;
        }

        Debug.Log($"[InventoryPlacementBridge] StartPlacementFromInventory called with item: {item?.itemID}");

        if (item == null || item.quantity <= 0)
        {
            Debug.LogWarning("[InventoryPlacementBridge] Cannot place item with zero quantity");
            return;
        }

        // 現在のシーンから参照を取得
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        FreePlacementSystem placementSystem = FindObjectOfType<FreePlacementSystem>();

        if (debugMode)
        {
            Debug.Log($"[InventoryPlacementBridge] Found InventoryUI: {inventoryUI != null}");
            Debug.Log($"[InventoryPlacementBridge] Found FreePlacementSystem: {placementSystem != null}");
        }

        // 家具データとPrefabを取得
        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        var prefab = FurnitureDataManager.Instance?.GetFurniturePrefab(item.itemID);

        if (furnitureData == null)
        {
            Debug.LogError($"[InventoryPlacementBridge] Furniture data not found for {item.itemID}");

            // ScriptableObjectを直接取得してみる
            var furnitureDataSO = FurnitureDataManager.Instance?.GetFurnitureDataSO(item.itemID);
            if (furnitureDataSO != null)
            {
                Debug.Log($"[InventoryPlacementBridge] Found FurnitureDataSO: {furnitureDataSO.name}");
                prefab = furnitureDataSO.prefab;

                // FurnitureDataに変換
                furnitureData = new FurnitureData
                {
                    itemID = furnitureDataSO.itemID,
                    nameID = furnitureDataSO.nameID,
                    cozy = furnitureDataSO.cozy,
                    nature = furnitureDataSO.nature,
                    category = furnitureDataSO.category,
                    placementRules = furnitureDataSO.placementRules,
                    canStackOn = furnitureDataSO.canStackOn,
                    sellPrice = furnitureDataSO.sellPrice,
                    iconName = furnitureDataSO.iconName,
                    modelName = furnitureDataSO.modelName,
                    recipeID = furnitureDataSO.recipeID,
                    rarity = furnitureDataSO.rarity,
                    interactionType = furnitureDataSO.interactionType,
                    isMovable = furnitureDataSO.isMovable,
                    descriptionID = furnitureDataSO.descriptionID,
                    weatherAttribute = furnitureDataSO.weatherAttribute,
                    recipeMaterialIDs = furnitureDataSO.recipeMaterialIDs,
                    recipeMaterialQuantities = furnitureDataSO.recipeMaterialQuantities,
                    dropMaterialIDs = furnitureDataSO.dropMaterialIDs,
                    dropRates = furnitureDataSO.dropRates
                };
            }
        }

        if (prefab == null)
        {
            Debug.LogError($"[InventoryPlacementBridge] Prefab not found for {item.itemID}");
            return;
        }

        Debug.Log($"[InventoryPlacementBridge] Found prefab: {prefab.name}");

        // 配置情報を保存
        placingItemID = item.itemID;
        isPlacingFromInventory = true;

        // インベントリを一時的に閉じる
        if (inventoryUI != null)
        {
            Debug.Log("[InventoryPlacementBridge] Closing inventory for placement");
            // 配置中フラグを立ててから閉じる
            inventoryUI.SetPlacingItem(true);
            inventoryUI.CloseInventory();
        }
        else
        {
            Debug.LogWarning("[InventoryPlacementBridge] InventoryUI not found in current scene!");
        }

        // 配置システムを開始
        if (placementSystem != null)
        {
            Debug.Log("[InventoryPlacementBridge] Starting FreePlacementSystem");

            // コールバックを設定
            placementSystem.OnPlacementCompleted = OnPlacementCompleteCallback;
            placementSystem.OnPlacementCancelled = OnPlacementCancelCallback;

            // 配置開始
            placementSystem.StartPlacingNewFurniture(prefab, furnitureData);
        }
        else
        {
            Debug.LogError("[InventoryPlacementBridge] FreePlacementSystem not found in current scene!");
            // エラー時はインベントリを復元
            RestoreInventory();
        }
    }

    // 配置完了時のコールバック
    private void OnPlacementCompleteCallback()
    {
        if (isPlacingFromInventory && !string.IsNullOrEmpty(placingItemID))
        {
            // インベントリから数量を減らす
            InventoryManager.Instance?.RemoveFurniture(placingItemID, 1);
            Debug.Log($"[InventoryPlacementBridge] Placed {placingItemID} from inventory");

            // イベント発火
            OnPlacementComplete?.Invoke(placingItemID);
        }

        // 配置システムのコールバックをクリア
        ClearPlacementCallbacks();

        // インベントリを復元
        RestoreInventory();

        // 状態をリセット
        ResetPlacementState();
    }

    // 配置キャンセル時のコールバック
    private void OnPlacementCancelCallback()
    {
        Debug.Log("[InventoryPlacementBridge] Placement cancelled, restoring inventory");

        // 配置システムのコールバックをクリア
        ClearPlacementCallbacks();

        // イベント発火
        OnPlacementCancelled?.Invoke();

        // インベントリを復元（数量は減らさない）
        RestoreInventory();

        // 状態をリセット
        ResetPlacementState();
    }

    // 配置システムのコールバックをクリア
    private void ClearPlacementCallbacks()
    {
        // 現在のシーンから配置システムを取得
        FreePlacementSystem placementSystem = FindObjectOfType<FreePlacementSystem>();
        if (placementSystem != null)
        {
            placementSystem.OnPlacementCompleted = null;
            placementSystem.OnPlacementCancelled = null;
        }
    }

    // インベントリを復元
    private void RestoreInventory()
    {
        // 現在のシーンからInventoryUIを取得
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();

        if (inventoryUI != null)
        {
            // 配置中フラグを解除
            inventoryUI.SetPlacingItem(false);

            // インベントリを開く
            inventoryUI.OpenInventory();

            // Furnitureタブに切り替え
            inventoryUI.SwitchTab(false);  // false = Furnitureタブ

            if (debugMode)
                Debug.Log("[InventoryPlacementBridge] Inventory restored");
        }
        else
        {
            Debug.LogWarning("[InventoryPlacementBridge] Cannot restore inventory - InventoryUI not found");
        }
    }

    // 状態をリセット
    private void ResetPlacementState()
    {
        placingItemID = null;
        isPlacingFromInventory = false;

        if (debugMode)
            Debug.Log("[InventoryPlacementBridge] State reset");
    }

    // 配置中かどうか
    public bool IsPlacing()
    {
        return isPlacingFromInventory;
    }

    // 現在配置中のアイテムID
    public string GetPlacingItemID()
    {
        return placingItemID;
    }

    // デバッグ用：現在の状態を表示
    [ContextMenu("Show Current State")]
    public void ShowCurrentState()
    {
        Debug.Log("=== InventoryPlacementBridge State ===");
        Debug.Log($"Is Placing: {isPlacingFromInventory}");
        Debug.Log($"Placing Item ID: {placingItemID}");

        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        Debug.Log($"Current InventoryUI: {(inventoryUI != null ? "Found" : "Not Found")}");

        FreePlacementSystem placementSystem = FindObjectOfType<FreePlacementSystem>();
        Debug.Log($"Current FreePlacementSystem: {(placementSystem != null ? "Found" : "Not Found")}");

        Debug.Log("=====================================");
    }
}
