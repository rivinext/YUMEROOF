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
                instance = FindFirstObjectByType<InventoryPlacementBridge>();
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

    [SerializeField]
    private List<string> disabledScenes = new List<string>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
            return;
        }

        if (item == null || item.quantity <= 0)
        {
            return;
        }

        // 現在のシーンから参照を取得
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        FreePlacementSystem placementSystem = FindFirstObjectByType<FreePlacementSystem>();

        // 家具データとPrefabを取得
        var furnitureData = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        var prefab = FurnitureDataManager.Instance?.GetFurniturePrefab(item.itemID);

        if (furnitureData == null)
        {
            // ScriptableObjectを直接取得してみる
            var furnitureDataSO = FurnitureDataManager.Instance?.GetFurnitureDataSO(item.itemID);
            if (furnitureDataSO != null)
            {
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
            return;
        }

        // 配置情報を保存
        placingItemID = item.itemID;
        isPlacingFromInventory = true;

        // インベントリを一時的に閉じる
        if (inventoryUI != null)
        {
            // 配置中フラグを立ててから閉じる
            inventoryUI.SetPlacingItem(true);
            inventoryUI.CloseInventory();
        }

        // 配置システムを開始
        if (placementSystem != null)
        {
            // コールバックを設定
            placementSystem.OnPlacementCompleted = OnPlacementCompleteCallback;
            placementSystem.OnPlacementCancelled = OnPlacementCancelCallback;

            // 配置開始
            placementSystem.StartPlacingNewFurniture(prefab, furnitureData);
        }
        else
        {
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
        FreePlacementSystem placementSystem = FindFirstObjectByType<FreePlacementSystem>();
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
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        bool autoReopenEnabled = inventoryUI != null && inventoryUI.AutoReopenEnabled;

        if (inventoryUI != null)
        {
            // 配置中フラグを解除
            inventoryUI.SetPlacingItem(false);

            if (autoReopenEnabled)
            {
                // インベントリを開く
                inventoryUI.OpenInventory();

                // Furnitureタブに切り替え
                inventoryUI.SwitchTab(false);  // false = Furnitureタブ
            }
        }
    }

    // 状態をリセット
    private void ResetPlacementState()
    {
        placingItemID = null;
        isPlacingFromInventory = false;
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

}
