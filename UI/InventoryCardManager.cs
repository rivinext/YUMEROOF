using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// インベントリカードの管理を担当（修正版）
/// アイテム選択時にInventoryUIに通知する機能を追加
/// </summary>
public class InventoryCardManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject furnitureCardPrefab;

    // カードのキャッシュ
    private List<InventoryItemCard> activeFurnitureCards = new List<InventoryItemCard>();
    private Queue<InventoryItemCard> furnitureCardPool = new Queue<InventoryItemCard>();

    // 選択管理
    private InventoryItem selectedFurnitureItem;
    private InventoryItemCard selectedFurnitureCard;

    // UI参照
    private GameObject furnitureContent;

    // デバッグモード
#if UNITY_EDITOR
    [SerializeField]
    private bool debugMode = false;
#else
    private const bool debugMode = false;
#endif

    public void Initialize(GameObject content)
    {
        furnitureContent = content;
        if (debugMode) Debug.Log("[CardManager] Initialized");
    }

    // 家具カードを更新
    public void RefreshFurnitureCards(List<InventoryItem> items)
    {
        if (debugMode) Debug.Log($"[CardManager] RefreshFurnitureCards: {items.Count} items");

        if (selectedFurnitureItem != null && !items.Contains(selectedFurnitureItem))
        {
            selectedFurnitureItem = null;
            selectedFurnitureCard = null;
        }

        // 既存のカードをプールに戻す
        foreach (var card in activeFurnitureCards)
        {
            card.SetSelected(false);
            card.gameObject.SetActive(false);
            furnitureCardPool.Enqueue(card);
        }
        activeFurnitureCards.Clear();

        // アイテムごとにカードを生成
        foreach (var item in items)
        {
            var card = GetOrCreateFurnitureCard();
            if (card == null) continue;

            card.SetSelected(false);
            card.transform.SetParent(furnitureContent.transform, false);
            card.transform.SetAsLastSibling();
            card.SetItem(item, false);

            // イベント設定（重要：毎回設定し直す）
            card.OnItemClicked -= OnFurnitureCardClicked;
            card.OnItemClicked += OnFurnitureCardClicked;
            card.OnItemDragged -= OnFurnitureCardDragged;
            card.OnItemDragged += OnFurnitureCardDragged;
            card.OnFavoriteToggled -= OnFurnitureFavoriteToggled;
            card.OnFavoriteToggled += OnFurnitureFavoriteToggled;

            activeFurnitureCards.Add(card);
        }

        // 選択状態を復元
        RestoreSelection();

        if (debugMode) Debug.Log($"[CardManager] Cards created: {activeFurnitureCards.Count}");
    }

    // カードを取得または作成
    InventoryItemCard GetOrCreateFurnitureCard()
    {
        InventoryItemCard card;

        if (furnitureCardPool.Count > 0)
        {
            card = furnitureCardPool.Dequeue();
            card.gameObject.SetActive(true);
        }
        else
        {
            if (furnitureCardPrefab != null)
            {
                GameObject cardObj = Instantiate(furnitureCardPrefab);
                card = cardObj.GetComponent<InventoryItemCard>();
                if (card == null)
                {
                    card = cardObj.AddComponent<InventoryItemCard>();
                }
            }
            else
            {
                Debug.LogError("[CardManager] Furniture Card Prefab is not assigned!");
                return null;
            }
        }

        return card;
    }

    // 家具クリック時の処理（修正版）
    void OnFurnitureCardClicked(InventoryItem item)
    {
        if (debugMode) Debug.Log($"[CardManager] Card clicked: {item?.itemID ?? "null"}");

        // クリックされたカードを取得
        var clickedCard = GetCardForItem(item);

        if (clickedCard == selectedFurnitureCard)
        {
            // 同じカードをクリックした場合は選択解除
            selectedFurnitureCard.SetSelected(false);
            selectedFurnitureCard = null;
            selectedFurnitureItem = null;

            // 選択解除をInventoryUIに通知
            NotifyItemSelection(null);
        }
        else
        {
            // 別のカードをクリックした場合は選択変更
            SelectFurnitureItem(item);
        }

        // クラフトボタンの状態を更新
        UpdateCraftButtonAfterSelection();
    }

    // 家具選択（修正版）
    public void SelectFurnitureItem(InventoryItem item)
    {
        if (debugMode) Debug.Log($"[CardManager] Selecting item: {item?.itemID ?? "null"}");

        selectedFurnitureItem = item;

        // 前の選択を解除
        if (selectedFurnitureCard != null)
        {
            selectedFurnitureCard.SetSelected(false);
        }

        // 新しいカードを選択
        selectedFurnitureCard = GetCardForItem(item);
        if (selectedFurnitureCard != null)
        {
            selectedFurnitureCard.SetSelected(true);
        }

        // 説明パネルを更新
        UpdateDescriptionPanel(item);

        // InventoryUIに選択を通知（重要！）
        NotifyItemSelection(item);

        // クラフトボタンの状態を更新
        UpdateCraftButtonAfterSelection();
    }

    // 説明パネルを更新
    void UpdateDescriptionPanel(InventoryItem item)
    {
        var descPanel = FindFirstObjectByType<FurnitureDescriptionPanel>();
        if (descPanel != null)
        {
            if (item != null)
            {
                descPanel.ShowFurnitureDetail(item);
                if (debugMode) Debug.Log($"[CardManager] Updated description panel for: {item.itemID}");
            }
            else
            {
                descPanel.ClearDescription();
                if (debugMode) Debug.Log("[CardManager] Cleared description panel");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("[CardManager] FurnitureDescriptionPanel not found!");
        }
    }

    // InventoryUIに選択を通知
    void NotifyItemSelection(InventoryItem item)
    {
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.OnFurnitureItemSelected(item);
            if (debugMode) Debug.Log($"[CardManager] Notified InventoryUI of selection: {item?.itemID ?? "null"}");
        }
        else if (debugMode)
        {
            Debug.LogWarning("[CardManager] Could not find InventoryUI!");
        }
    }

    // クラフトボタンの状態を更新
    void UpdateCraftButtonAfterSelection()
    {
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.UpdateCraftButtonState();
            if (debugMode) Debug.Log("[CardManager] Triggered craft button update");
        }
    }

    // アイテムに対応するカードを取得
    InventoryItemCard GetCardForItem(InventoryItem item)
    {
        foreach (var card in activeFurnitureCards)
        {
            if (card.currentItem == item)
                return card;
        }
        return null;
    }

    // 選択状態を復元
    void RestoreSelection()
    {
        if (selectedFurnitureItem != null)
        {
            var card = GetCardForItem(selectedFurnitureItem);
            if (card != null)
            {
                selectedFurnitureCard = card;
                card.SetSelected(true);

                // 復元時も通知
                NotifyItemSelection(selectedFurnitureItem);
            }
        }
    }

    // ドラッグ処理
    void OnFurnitureCardDragged(InventoryItem item)
    {
        if (debugMode) Debug.Log($"[CardManager] Card dragged: {item?.itemID ?? "null"}");
        // InventoryItemCard側で処理
    }

    // お気に入り切り替え
    void OnFurnitureFavoriteToggled(InventoryItem item)
    {
        if (debugMode) Debug.Log($"[CardManager] Favorite toggled - Item: {item.itemID}, Favorite: {item.isFavorite}");

        // InventoryManagerに保存
        InventoryManager.Instance?.ToggleFavorite(item.itemType, item.itemID);

        // InventoryUIに通知して表示を更新
        var inventoryUI = GetComponentInParent<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshInventoryDisplay();
        }
    }

    // 全カードのクラフト状態を更新
    public void UpdateAllCraftableStatus()
    {
        foreach (var card in activeFurnitureCards)
        {
            if (card.currentItem != null)
            {
                card.UpdateVisualState();
            }
        }

        if (debugMode) Debug.Log("[CardManager] Updated all craftable status");
    }

    // 選択中のアイテムを取得
    public InventoryItem GetSelectedItem()
    {
        return selectedFurnitureItem;
    }

    // クリーンアップ
    public void Cleanup()
    {
        foreach (var card in activeFurnitureCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        activeFurnitureCards.Clear();

        while (furnitureCardPool.Count > 0)
        {
            var card = furnitureCardPool.Dequeue();
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        furnitureCardPool.Clear();
    }

    public void DeselectAll()
    {
        if (selectedFurnitureCard != null)
        {
            selectedFurnitureCard.SetSelected(false);
            selectedFurnitureCard = null;
        }
        selectedFurnitureItem = null;

        // 選択解除を通知
        NotifyItemSelection(null);
    }
}
