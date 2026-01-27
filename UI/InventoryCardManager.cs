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
    private readonly Dictionary<string, int> displayedQuantities = new Dictionary<string, int>();
    private readonly Dictionary<string, bool> displayedFavorites = new Dictionary<string, bool>();

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
            ReleaseFurnitureCard(card);
        }
        activeFurnitureCards.Clear();

        // アイテムごとにカードを生成
        foreach (var item in items)
        {
            var card = GetOrCreateFurnitureCard();
            if (card == null) continue;

            card.transform.SetParent(furnitureContent.transform, false);
            card.transform.SetAsLastSibling();
            card.SetItem(item, false);

            ConfigureFurnitureCard(card);
            TrackDisplayedItem(item);

            activeFurnitureCards.Add(card);
        }

        // 選択状態を復元
        RestoreSelection();

        if (debugMode) Debug.Log($"[CardManager] Cards created: {activeFurnitureCards.Count}");
    }

    public InventoryItemCard AcquireFurnitureCard()
    {
        var card = GetOrCreateFurnitureCard();
        if (card == null)
        {
            return null;
        }

        ConfigureFurnitureCard(card);
        return card;
    }

    public void RegisterActiveCard(InventoryItemCard card)
    {
        if (card == null)
        {
            return;
        }

        if (!activeFurnitureCards.Contains(card))
        {
            activeFurnitureCards.Add(card);
        }
    }

    public void ReleaseVirtualizedCard(InventoryItemCard card)
    {
        if (card == null)
        {
            return;
        }

        activeFurnitureCards.Remove(card);

        if (card == selectedFurnitureCard)
        {
            selectedFurnitureCard = null;
        }

        ReleaseFurnitureCard(card);
    }

    public void BindVirtualizedCard(InventoryItemCard card, InventoryItem item)
    {
        if (card == null)
        {
            return;
        }

        card.SetItem(item, false);
        TrackDisplayedItem(item);

        if (selectedFurnitureItem == item)
        {
            if (selectedFurnitureCard != null && selectedFurnitureCard != card)
            {
                selectedFurnitureCard.SetSelected(false);
            }
            selectedFurnitureCard = card;
            card.SetSelected(true);
        }
        else
        {
            card.SetSelected(false);
        }
    }

    public void SyncFurnitureCards(List<InventoryItem> items)
    {
        if (debugMode) Debug.Log($"[CardManager] SyncFurnitureCards: {items.Count} items");

        var desiredIds = new HashSet<string>(items.Select(item => item.itemID));
        var existingCards = activeFurnitureCards
            .Where(card => card != null && card.currentItem != null)
            .ToDictionary(card => card.currentItem.itemID, card => card);

        if (selectedFurnitureItem != null && !desiredIds.Contains(selectedFurnitureItem.itemID))
        {
            DeselectAll();
        }

        for (int i = activeFurnitureCards.Count - 1; i >= 0; i--)
        {
            var card = activeFurnitureCards[i];
            if (card == null || card.currentItem == null || !desiredIds.Contains(card.currentItem.itemID))
            {
                ReleaseFurnitureCard(card);
                activeFurnitureCards.RemoveAt(i);
            }
        }

        var nextActiveCards = new List<InventoryItemCard>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!existingCards.TryGetValue(item.itemID, out var card) || card == null)
            {
                card = GetOrCreateFurnitureCard();
                if (card == null) continue;
                ConfigureFurnitureCard(card);
                card.SetItem(item, false);
            }
            else
            {
                if (ShouldUpdateCard(item))
                {
                    card.SetItem(item, false);
                }
            }

            card.transform.SetParent(furnitureContent.transform, false);
            card.transform.SetSiblingIndex(i);
            nextActiveCards.Add(card);
            TrackDisplayedItem(item);
        }

        activeFurnitureCards = nextActiveCards;
        RestoreSelection();
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

    void ConfigureFurnitureCard(InventoryItemCard card)
    {
        card.SetSelected(false);
        card.OnItemClicked -= OnFurnitureCardClicked;
        card.OnItemClicked += OnFurnitureCardClicked;
        card.OnItemDragged -= OnFurnitureCardDragged;
        card.OnItemDragged += OnFurnitureCardDragged;
        card.OnFavoriteToggled -= OnFurnitureFavoriteToggled;
        card.OnFavoriteToggled += OnFurnitureFavoriteToggled;
    }

    void ReleaseFurnitureCard(InventoryItemCard card)
    {
        if (card == null)
        {
            return;
        }

        card.SetSelected(false);
        card.gameObject.SetActive(false);
        furnitureCardPool.Enqueue(card);
        if (card.currentItem != null)
        {
            displayedQuantities.Remove(card.currentItem.itemID);
            displayedFavorites.Remove(card.currentItem.itemID);
        }
    }

    bool ShouldUpdateCard(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (!displayedQuantities.TryGetValue(item.itemID, out var lastQuantity) || lastQuantity != item.quantity)
        {
            return true;
        }

        if (!displayedFavorites.TryGetValue(item.itemID, out var lastFavorite) || lastFavorite != item.isFavorite)
        {
            return true;
        }

        return false;
    }

    void TrackDisplayedItem(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        displayedQuantities[item.itemID] = item.quantity;
        displayedFavorites[item.itemID] = item.isFavorite;
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
