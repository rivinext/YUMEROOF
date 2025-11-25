using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

/// <summary>
/// Handles displaying purchase and sell tabs for the shop.
/// Purchase items are refreshed every day based on milestone unlock data.
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    public event Action ShopOpened;
    public event Action ShopClosed;

    [Header("UI References")]
    public GameObject shopRoot;           // Root panel for the shop UI
    public GameObject purchaseTab;        // Purchase tab object
    public GameObject sellTab;            // Sell tab object
    [SerializeField] private UISlidePanel purchaseTabSlidePanel; // Slide panel controller for purchase tab
    [SerializeField] private UISlidePanel sellTabSlidePanel;     // Slide panel controller for sell tab
    public Transform purchaseContent;     // Parent for purchase item cards
    public Transform sellContent;         // Parent for sell item cards
    public GameObject purchaseItemCardPrefab; // Prefab for purchase tab items
    public GameObject sellItemCardPrefab;     // Prefab for sell tab items
    public Button sellButton;             // Button to confirm selling
    public Button purchaseButton;         // Button to confirm purchasing
    public ToggleGroup tabToggleGroup;    // Toggle group for purchase/sell tabs
    public Toggle purchaseTabToggle;      // Toggle to show purchase tab
    public Toggle sellTabToggle;          // Toggle to show sell tab
    public Button closeButton;            // Button to close the shop UI

    [Header("Description Panels")]
    public GameObject purchaseDescriptionArea;
    public GameObject sellDescriptionArea;

    [Header("Sell Tab Filters")]
    public Button sellRarityUpButton;
    public Button sellRarityDownButton;
    public Toggle sellFavoriteToggle;
    public InputField sellSearchField;

    [Header("CSV Paths")]
    public string itemCSVPath = "Data/YUME_ROOF - Item";
    public string unlockCSVPath = "Data/YUME_ROOF - WhenUnlock";

    [Header("Shop Settings")]
    public string currentMilestoneID = "Milestone_00"; // Current milestone

    [Header("Audio")]
    [SerializeField] private AudioClip purchaseSfx;
    [SerializeField] private AudioClip sellSfx;
    [SerializeField] private AudioSource transactionAudioSource;
    [SerializeField] private bool autoCreateAudioSource = true;
    [SerializeField, Range(0f, 1f)] private float transactionSfxVolume = 1f;

    private readonly Dictionary<string, ShopItem> allItems = new();
    private readonly List<ShopItem> dailyPurchaseItems = new();
    private int generatedDay = -1;
    private GameClock clock;
    private bool isOpen;
    private bool inputOwnedExternally;
    private InventoryItem selectedForSale;
    private ShopItem selectedForPurchase;
    private UISlidePanel currentTabSlidePanel;
    private readonly Dictionary<string, InventoryItemCardPurchase> activePurchaseCards = new();
    private readonly Stack<InventoryItemCardPurchase> purchaseCardPool = new();
    private readonly Dictionary<string, InventoryItemCardSell> activeSellCards = new();
    private readonly Stack<InventoryItemCardSell> sellCardPool = new();
    private bool inventoryEventsSubscribed;

    // Sell tab filter state
    private string sellSortType = "name";
    private bool sellSortAscending = true;
    private bool sellShowOnlyFavorites = false;
    private string sellSearchQuery = "";

    public bool IsOpen => isOpen;
    private float currentSfxVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        clock = FindFirstObjectByType<GameClock>();
        if (clock != null)
        {
            clock.OnDayChanged += OnDayChanged;
        }
        LoadItemData();
        LoadUnlockData();
        if (purchaseTabSlidePanel == null && purchaseTab != null)
        {
            purchaseTabSlidePanel = purchaseTab.GetComponent<UISlidePanel>();
        }

        if (sellTabSlidePanel == null && sellTab != null)
        {
            sellTabSlidePanel = sellTab.GetComponent<UISlidePanel>();
        }

        if (purchaseTabSlidePanel != null)
        {
            purchaseTabSlidePanel.ConfigureCloseBehaviors(false, false);
        }

        if (sellTabSlidePanel != null)
        {
            sellTabSlidePanel.ConfigureCloseBehaviors(false, false);
        }

        if (shopRoot != null)
        {
            shopRoot.SetActive(false);
        }

        SetupTransactionAudioSource();
    }

    void Start()
    {
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(SellSelectedItem);
            sellButton.interactable = false;
        }
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(PurchaseSelectedItem);
            purchaseButton.interactable = false;
        }
        if (purchaseTabToggle != null)
        {
            purchaseTabToggle.onValueChanged.AddListener(HandlePurchaseToggleChanged);
        }
        if (sellTabToggle != null)
        {
            sellTabToggle.onValueChanged.AddListener(HandleSellToggleChanged);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }

        SetupSellTabFilters();
    }

    void OnEnable()
    {
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
        SubscribeToInventoryEvents();
    }

    void OnDisable()
    {
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        UnsubscribeFromInventoryEvents();
    }

    void OnDestroy()
    {
        if (clock != null)
        {
            clock.OnDayChanged -= OnDayChanged;
        }

        // No slide panel event subscriptions to clean up.
    }

    void OnDayChanged(int day)
    {
        dailyPurchaseItems.Clear();
        generatedDay = -1;
    }

    #region CSV Loading
    void LoadItemData()
    {
        TextAsset csv = Resources.Load<TextAsset>(itemCSVPath);
        if (csv == null) return;

        string[] lines = csv.text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCSVLine(lines[i]);
            if (values.Length < 8) continue;
            string id = values[0];
            int sell = ParseInt(values[7]);
            int buy = values.Length > 24 ? ParseInt(values[24]) : sell;
            if (!allItems.ContainsKey(id))
            {
                allItems.Add(id, new ShopItem { itemID = id, sellPrice = sell, buyPrice = buy });
            }
        }
    }

    void LoadUnlockData()
    {
        TextAsset csv = Resources.Load<TextAsset>(unlockCSVPath);
        if (csv == null) return;

        string[] lines = csv.text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length < 2) continue;
            string itemId = values[0];
            string milestone = values[1];
            if (allItems.TryGetValue(itemId, out var item))
            {
                item.milestoneID = milestone;
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Open the shop UI and populate tabs.
    /// </summary>
    public void OpenShop()
    {
        if (OpenShopInternal(false))
        {
            ShowSellTab();
        }
    }

    public bool OpenBuyPanel(bool externalInputOwner = false)
    {
        if (!OpenShopInternal(externalInputOwner))
        {
            return false;
        }

        ShowPurchaseTab();
        return true;
    }

    public bool OpenSellPanel(bool externalInputOwner = false)
    {
        if (!OpenShopInternal(externalInputOwner))
        {
            return false;
        }

        ShowSellTab();
        return true;
    }

    /// <summary>
    /// Close the shop UI.
    /// </summary>
    public void CloseShop()
    {
        if (!isOpen)
            return;

        isOpen = false;
        ClearDescriptionPanels();
        bool shouldReleaseInput = !inputOwnedExternally;
        inputOwnedExternally = false;
        if (shouldReleaseInput)
        {
            PlayerController.SetGlobalInputEnabled(true);
        }

        if (shopRoot != null)
        {
            shopRoot.SetActive(false);
        }

        NotifyShopClosed();
    }

    /// <summary>
    /// Display purchase tab and hide sell tab.
    /// </summary>
    public void ShowPurchaseTab()
    {
        SetActiveTabToggle(purchaseTabToggle);
        SwitchTab(purchaseTabSlidePanel, sellTabSlidePanel, purchaseTab, sellTab, () =>
        {
            PopulatePurchaseTab();
            UpdatePurchaseButtonState();
            UpdatePurchaseDescription(selectedForPurchase);
        });
    }

    /// <summary>
    /// Display sell tab and hide purchase tab.
    /// </summary>
    public void ShowSellTab()
    {
        SetActiveTabToggle(sellTabToggle);
        SwitchTab(sellTabSlidePanel, purchaseTabSlidePanel, sellTab, purchaseTab, () =>
        {
            PopulateSellTab();
            UpdateDescriptionPanel(selectedForSale);
        });
    }
    #endregion

    bool OpenShopInternal(bool externalInputOwner)
    {
        if (shopRoot == null && purchaseTab == null && sellTab == null &&
            purchaseTabSlidePanel == null && sellTabSlidePanel == null)
        {
            Debug.LogWarning("[ShopUIManager] Cannot open shop because UI references are missing.");
            return false;
        }

        PlayerController.SetGlobalInputEnabled(false);
        inputOwnedExternally = externalInputOwner;
        selectedForSale = null;
        selectedForPurchase = null;
        if (sellButton != null) sellButton.interactable = false;
        if (purchaseButton != null) purchaseButton.interactable = false;
        ClearDescriptionPanels();
        ResetTabToggles();
        isOpen = true;

        if (shopRoot != null)
        {
            shopRoot.SetActive(true);
        }

        ShopOpened?.Invoke();
        return true;
    }

    void NotifyShopClosed()
    {
        ShopClosed?.Invoke();
    }

    void SwitchTab(UISlidePanel targetSlidePanel, UISlidePanel otherSlidePanel, GameObject targetTab, GameObject otherTab, Action onShown)
    {
        if (targetSlidePanel == null || otherSlidePanel == null)
        {
            if (targetTab != null) targetTab.SetActive(true);
            if (otherTab != null) otherTab.SetActive(false);
            currentTabSlidePanel = targetSlidePanel;
            onShown?.Invoke();
            return;
        }

        if (currentTabSlidePanel == targetSlidePanel && currentTabSlidePanel != null && currentTabSlidePanel.IsOpen)
        {
            onShown?.Invoke();
            return;
        }

        if (currentTabSlidePanel != null && currentTabSlidePanel != targetSlidePanel && currentTabSlidePanel.IsOpen)
        {
            currentTabSlidePanel.SlideOut();
        }

        currentTabSlidePanel = targetSlidePanel;
        currentTabSlidePanel.SlideIn();

        onShown?.Invoke();
    }

    private void HandlePurchaseToggleChanged(bool isOn)
    {
        if (isOn)
        {
            ShowPurchaseTab();
        }
    }

    private void HandleSellToggleChanged(bool isOn)
    {
        if (isOn)
        {
            ShowSellTab();
        }
    }


    private void SetActiveTabToggle(Toggle targetToggle)
    {
        if (targetToggle == null)
            return;

        if (purchaseTabToggle != null && purchaseTabToggle != targetToggle)
        {
            purchaseTabToggle.SetIsOnWithoutNotify(false);
        }
        if (sellTabToggle != null && sellTabToggle != targetToggle)
        {
            sellTabToggle.SetIsOnWithoutNotify(false);
        }

        targetToggle.SetIsOnWithoutNotify(true);
    }

    private void ResetTabToggles()
    {
        if (tabToggleGroup != null)
        {
            bool previousAllowSwitchOff = tabToggleGroup.allowSwitchOff;
            tabToggleGroup.allowSwitchOff = true;
            tabToggleGroup.SetAllTogglesOff();
            tabToggleGroup.allowSwitchOff = previousAllowSwitchOff;
        }
        else
        {
            if (purchaseTabToggle != null)
            {
                purchaseTabToggle.SetIsOnWithoutNotify(false);
            }
            if (sellTabToggle != null)
            {
                sellTabToggle.SetIsOnWithoutNotify(false);
            }
        }
    }

    void EnsureDailyItems()
    {
        int day = clock != null ? clock.currentDay : 0;
        if (generatedDay == day) return;

        List<ShopItem> candidates = new();
        foreach (var item in allItems.Values)
        {
            if (!string.IsNullOrEmpty(item.milestoneID) && item.milestoneID == currentMilestoneID)
            {
                candidates.Add(item);
            }
        }

        dailyPurchaseItems.Clear();
        System.Random rand = new System.Random();
        for (int i = 0; i < 3 && candidates.Count > 0; i++)
        {
            int index = rand.Next(candidates.Count);
            dailyPurchaseItems.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        generatedDay = day;
    }

    void PopulatePurchaseTab()
    {
        EnsureDailyItems();
        if (purchaseContent == null || purchaseItemCardPrefab == null) return;

        var requiredIds = new HashSet<string>(dailyPurchaseItems.Select(i => i.itemID));
        var keysToRelease = activePurchaseCards.Keys.Where(id => !requiredIds.Contains(id)).ToList();
        foreach (var key in keysToRelease)
        {
            ReleasePurchaseCard(key);
        }

        if (selectedForPurchase != null && !requiredIds.Contains(selectedForPurchase.itemID))
        {
            selectedForPurchase = null;
        }

        for (int i = 0; i < dailyPurchaseItems.Count; i++)
        {
            var item = dailyPurchaseItems[i];
            var card = GetOrCreatePurchaseCard(item);
            BindPurchaseCard(card, item);
            card.transform.SetSiblingIndex(i);
        }

        UpdatePurchaseDescription(selectedForPurchase);
    }

    void PopulateSellTab()
    {
        if (sellContent == null || sellItemCardPrefab == null) return;
        SubscribeToInventoryEvents();

        var sellItems = BuildSellItemList();
        var requiredKeys = new HashSet<string>(sellItems.Select(GetSellCardKey));

        var currentKeys = activeSellCards.Keys.ToList();
        foreach (var key in currentKeys)
        {
            if (!requiredKeys.Contains(key))
            {
                ReleaseSellCard(key);
            }
        }

        for (int i = 0; i < sellItems.Count; i++)
        {
            var item = sellItems[i];
            var key = GetSellCardKey(item);
            var card = GetOrCreateSellCard(key);
            BindSellCard(card, item);
            card.transform.SetSiblingIndex(i);
        }

        if (selectedForSale != null && !requiredKeys.Contains(GetSellCardKey(selectedForSale)))
        {
            selectedForSale = null;
            UpdateDescriptionPanel(null);
        }
        else
        {
            UpdateDescriptionPanel(selectedForSale);
        }

        UpdateSellButtonState();
    }

    List<InventoryItem> BuildSellItemList()
    {
        var items = new List<InventoryItem>();
        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            return items;
        }

        var furniture = inventory.GetFurnitureList(sellSortType, false, sellShowOnlyFavorites, sellSortAscending);
        if (furniture != null)
        {
            foreach (var item in furniture)
            {
                if (!ShouldDisplayItem(item))
                {
                    continue;
                }

                items.Add(item);
            }
        }

        return items;
    }

    bool ShouldDisplayItem(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (item.itemType == InventoryItem.ItemType.Material)
        {
            return false;
        }

        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            if (sellShowOnlyFavorites && !item.isFavorite)
            {
                return false;
            }

            return MatchesSellSearch(item);
        }

        return false;
    }

    bool MatchesSellSearch(InventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sellSearchQuery))
        {
            return true;
        }

        var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        if (data == null)
        {
            return false;
        }

        string locName = LocalizationSettings.StringDatabase.GetLocalizedString("ItemNames", data.nameID);
        if (string.IsNullOrEmpty(locName))
        {
            return false;
        }

        return locName.IndexOf(sellSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    string GetSellCardKey(InventoryItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        return $"{item.itemType}:{item.itemID}";
    }

    InventoryItemCardSell GetOrCreateSellCard(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (activeSellCards.TryGetValue(key, out var existingCard) && existingCard != null)
        {
            existingCard.gameObject.SetActive(true);
            return existingCard;
        }

        InventoryItemCardSell card = null;
        if (sellCardPool.Count > 0)
        {
            card = sellCardPool.Pop();
        }
        else
        {
            var go = Instantiate(sellItemCardPrefab, sellContent);
            go.transform.localScale = Vector3.one;
            card = go.GetComponent<InventoryItemCardSell>();
        }

        if (card == null)
        {
            return null;
        }

        card.transform.SetParent(sellContent, false);
        card.gameObject.SetActive(true);
        activeSellCards[key] = card;
        return card;
    }

    void ReleaseSellCard(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!activeSellCards.TryGetValue(key, out var card) || card == null)
        {
            return;
        }

        activeSellCards.Remove(key);
        card.OnItemClicked -= SelectItemForSale;
        card.OnFavoriteToggled -= HandleSellCardFavoriteToggled;
        card.SetSelected(false);
        card.gameObject.SetActive(false);
        sellCardPool.Push(card);

        if (selectedForSale != null && GetSellCardKey(selectedForSale) == key)
        {
            selectedForSale = null;
            UpdateDescriptionPanel(null);
            UpdateSellButtonState();
        }
    }

    void BindSellCard(InventoryItemCardSell card, InventoryItem item)
    {
        if (card == null || item == null)
        {
            return;
        }

        card.OnItemClicked -= SelectItemForSale;
        card.OnItemClicked += SelectItemForSale;
        card.OnFavoriteToggled -= HandleSellCardFavoriteToggled;
        card.OnFavoriteToggled += HandleSellCardFavoriteToggled;

        bool isMaterial = item.itemType == InventoryItem.ItemType.Material;
        card.SetItem(item, isMaterial);
        bool isSelected = selectedForSale != null && card.currentItem == selectedForSale;
        card.SetSelected(isSelected);
    }

    void UpdateSellCardDisplay(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!ShouldDisplayItem(item))
        {
            ReleaseSellCard(GetSellCardKey(item));
            return;
        }

        var card = GetOrCreateSellCard(GetSellCardKey(item));
        BindSellCard(card, item);
    }

    void UpdateSellButtonState()
    {
        if (sellButton == null)
        {
            return;
        }

        bool canSell = selectedForSale != null && selectedForSale.quantity > 0;
        sellButton.interactable = canSell;
    }

    void HandleSellCardFavoriteToggled(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        UpdateSellCardDisplay(item);
    }

    void SubscribeToInventoryEvents()
    {
        if (inventoryEventsSubscribed)
        {
            return;
        }

        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            return;
        }

        inventory.OnItemAdded -= HandleInventoryItemAdded;
        inventory.OnItemAdded += HandleInventoryItemAdded;
        inventory.OnItemRemoved -= HandleInventoryItemRemoved;
        inventory.OnItemRemoved += HandleInventoryItemRemoved;
        inventory.OnItemUpdated -= HandleInventoryItemUpdated;
        inventory.OnItemUpdated += HandleInventoryItemUpdated;
        inventoryEventsSubscribed = true;
    }

    void UnsubscribeFromInventoryEvents()
    {
        if (!inventoryEventsSubscribed)
        {
            return;
        }

        var inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            inventoryEventsSubscribed = false;
            return;
        }

        inventory.OnItemAdded -= HandleInventoryItemAdded;
        inventory.OnItemRemoved -= HandleInventoryItemRemoved;
        inventory.OnItemUpdated -= HandleInventoryItemUpdated;
        inventoryEventsSubscribed = false;
    }

    void HandleInventoryItemAdded(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!ShouldDisplayItem(item))
        {
            return;
        }

        PopulateSellTab();
    }

    void HandleInventoryItemRemoved(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        ReleaseSellCard(GetSellCardKey(item));
    }

    void HandleInventoryItemUpdated(InventoryItem item)
    {
        if (item == null)
        {
            return;
        }

        UpdateSellCardDisplay(item);

        if (selectedForSale == item)
        {
            UpdateSellButtonState();
            UpdateDescriptionPanel(item);
        }
    }

    void SetupSellTabFilters()
    {
        if (sellRarityUpButton != null)
        {
            sellRarityUpButton.onClick.RemoveAllListeners();
            sellRarityUpButton.onClick.AddListener(() =>
            {
                sellSortType = "rarity";
                sellSortAscending = true;
                PopulateSellTab();
            });
        }

        if (sellRarityDownButton != null)
        {
            sellRarityDownButton.onClick.RemoveAllListeners();
            sellRarityDownButton.onClick.AddListener(() =>
            {
                sellSortType = "rarity";
                sellSortAscending = false;
                PopulateSellTab();
            });
        }

        if (sellFavoriteToggle != null)
        {
            sellFavoriteToggle.onValueChanged.RemoveAllListeners();
            sellFavoriteToggle.onValueChanged.AddListener(value =>
            {
                sellShowOnlyFavorites = value;
                PopulateSellTab();
            });
        }

        if (sellSearchField != null)
        {
            sellSearchField.onValueChanged.RemoveAllListeners();
            sellSearchField.onValueChanged.AddListener(value =>
            {
                sellSearchQuery = value;
                PopulateSellTab();
            });
        }
    }

    void SelectItemForSale(InventoryItem item)
    {
        selectedForSale = item;
        UpdateSellButtonState();

        foreach (var card in activeSellCards.Values)
        {
            bool isSelected = card != null && card.currentItem == item;
            card?.SetSelected(isSelected);
        }

        UpdateDescriptionPanel(item);
    }

    void SelectItemForPurchase(ShopItem item)
    {
        selectedForPurchase = item;
        UpdatePurchaseButtonState();

        foreach (var kvp in activePurchaseCards)
        {
            bool isSelected = item != null && kvp.Key == item.itemID;
            kvp.Value.SetSelected(isSelected);
        }

        UpdatePurchaseDescription(item);
    }

    void PurchaseSelectedItem()
    {
        if (selectedForPurchase == null) return;
        int price = GetBuyPrice(selectedForPurchase);
        bool purchaseSucceeded = false;
        if (MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney >= price)
        {
            // Reduce player's money and add item to inventory
            MoneyManager.Instance.AddMoney(-price);
            InventoryManager.Instance?.AddFurniture(selectedForPurchase.itemID, 1);
            Debug.Log($"Purchased {selectedForPurchase.itemID} for {price}");
            purchaseSucceeded = true;
            UpdatePurchaseCardDisplay(selectedForPurchase.itemID);
        }
        else
        {
            Debug.Log("Not enough money to purchase " + selectedForPurchase.itemID);
        }

        if (purchaseSucceeded)
        {
            PlayTransactionSfx(purchaseSfx);
        }

        UpdatePurchaseButtonState();
        UpdatePurchaseDescription(selectedForPurchase);
    }

    void SellSelectedItem()
    {
        if (selectedForSale == null) return;

        var item = selectedForSale;
        int price = 0;
        bool sellSucceeded = false;

        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            InventoryManager.Instance?.RemoveFurniture(item.itemID, 1);
            var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
            if (data != null) price = data.sellPrice;
        }
        else
        {
            InventoryManager.Instance?.RemoveMaterial(item.itemID, 1);
            var data = InventoryManager.Instance?.GetMaterialData(item.itemID);
            if (data != null) price = data.sellPrice;
        }

        Debug.Log($"Sold {item.itemID} for {price}");
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(price);
            sellSucceeded = true;
        }

        if (sellSucceeded)
        {
            PlayTransactionSfx(sellSfx);
        }

        UpdateSellButtonState();
        UpdatePurchaseButtonState();
        UpdateDescriptionPanel(selectedForSale);
    }

    InventoryItemCardPurchase GetOrCreatePurchaseCard(ShopItem item)
    {
        if (item == null) return null;

        if (activePurchaseCards.TryGetValue(item.itemID, out var existingCard) && existingCard != null)
        {
            existingCard.gameObject.SetActive(true);
            return existingCard;
        }

        InventoryItemCardPurchase card;
        if (purchaseCardPool.Count > 0)
        {
            card = purchaseCardPool.Pop();
        }
        else
        {
            var go = Instantiate(purchaseItemCardPrefab, purchaseContent);
            go.transform.localScale = Vector3.one;
            card = go.GetComponent<InventoryItemCardPurchase>();
        }

        if (card == null)
        {
            return null;
        }

        card.transform.SetParent(purchaseContent, false);
        card.gameObject.SetActive(true);
        activePurchaseCards[item.itemID] = card;
        return card;
    }

    void BindPurchaseCard(InventoryItemCardPurchase card, ShopItem item)
    {
        if (card == null || item == null)
        {
            return;
        }

        card.OnItemClicked -= HandlePurchaseCardClicked;
        card.OnItemClicked += HandlePurchaseCardClicked;

        int owned = InventoryManager.Instance?.GetItemCount(InventoryItem.ItemType.Furniture, item.itemID) ?? 0;
        var invItem = new InventoryItem(InventoryItem.ItemType.Furniture, item.itemID, owned);
        card.SetItem(invItem);
        bool isSelected = selectedForPurchase != null && selectedForPurchase.itemID == item.itemID;
        card.SetSelected(isSelected);
    }

    void ReleasePurchaseCard(string itemId)
    {
        if (!activePurchaseCards.TryGetValue(itemId, out var card) || card == null)
        {
            return;
        }

        activePurchaseCards.Remove(itemId);
        card.SetSelected(false);
        card.gameObject.SetActive(false);
        purchaseCardPool.Push(card);
    }

    void HandlePurchaseCardClicked(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return;
        }

        var shopItem = dailyPurchaseItems.FirstOrDefault(i => i.itemID == inventoryItem.itemID);
        if (shopItem != null)
        {
            SelectItemForPurchase(shopItem);
        }
    }

    void UpdatePurchaseCardDisplay(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        if (!activePurchaseCards.TryGetValue(itemId, out var card) || card == null)
        {
            return;
        }

        int owned = InventoryManager.Instance?.GetItemCount(InventoryItem.ItemType.Furniture, itemId) ?? 0;
        var invItem = new InventoryItem(InventoryItem.ItemType.Furniture, itemId, owned);
        card.SetItem(invItem);
        bool isSelected = selectedForPurchase != null && selectedForPurchase.itemID == itemId;
        card.SetSelected(isSelected);
    }

    string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuote = false;
        string current = "";
        foreach (char c in line)
        {
            if (c == '\"')
            {
                inQuote = !inQuote;
            }
            else if (c == ',' && !inQuote)
            {
                result.Add(current);
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }
        result.Add(current);
        return result.ToArray();
    }

    int ParseInt(string s)
    {
        int.TryParse(s, out int v);
        return v;
    }

    [Serializable]
    public class ShopItem
    {
        public string itemID;
        public string milestoneID;
        public int buyPrice;
        public int sellPrice;
    }

    void UpdatePurchaseButtonState()
    {
        if (purchaseButton == null)
        {
            return;
        }

        bool canPurchase = false;
        if (selectedForPurchase != null && MoneyManager.Instance != null)
        {
            int price = GetBuyPrice(selectedForPurchase);
            canPurchase = MoneyManager.Instance.CurrentMoney >= price;
        }

        purchaseButton.interactable = canPurchase;
    }

    int GetBuyPrice(ShopItem item)
    {
        if (item == null)
        {
            return 0;
        }

        var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
        if (data != null && data.buyPrice > 0)
        {
            return data.buyPrice;
        }

        return item.buyPrice;
    }

    void SetupTransactionAudioSource()
    {
        if (transactionAudioSource == null)
        {
            transactionAudioSource = GetComponent<AudioSource>();
            if (transactionAudioSource == null && autoCreateAudioSource)
            {
                transactionAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (transactionAudioSource != null)
        {
            transactionAudioSource.playOnAwake = false;
            transactionAudioSource.loop = false;
            transactionAudioSource.spatialBlend = 0f;
        }
    }

    void HandleSfxVolumeChanged(float value)
    {
        currentSfxVolume = Mathf.Clamp01(value);
    }

    void PlayTransactionSfx(AudioClip clip)
    {
        if (clip == null || transactionAudioSource == null)
        {
            return;
        }

        float volume = transactionSfxVolume * currentSfxVolume;
        if (volume <= 0f)
        {
            return;
        }

        transactionAudioSource.PlayOneShot(clip, volume);
    }

    void UpdatePurchaseDescription(ShopItem item)
    {
        var furniturePanel = GetPurchaseDescriptionPanel();

        if (item == null)
        {
            if (furniturePanel != null) furniturePanel.ClearDescription();
            return;
        }

        InventoryItem invItem = InventoryManager.Instance?.GetFurnitureItem(item.itemID);

        if (invItem == null)
        {
            int owned = InventoryManager.Instance?.GetItemCount(InventoryItem.ItemType.Furniture, item.itemID) ?? 0;
            invItem = new InventoryItem(InventoryItem.ItemType.Furniture, item.itemID, owned);
        }

        if (furniturePanel != null)
        {
            furniturePanel.ShowFurnitureDetail(invItem);
        }
    }

    void UpdateDescriptionPanel(InventoryItem item)
    {
        var furniturePanel = GetSellDescriptionPanel();

        if (item == null)
        {
            if (furniturePanel != null) furniturePanel.ClearDescription();
            return;
        }

        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            if (furniturePanel != null)
            {
                furniturePanel.ShowFurnitureDetail(item);
            }
        }
        else
        {
            if (furniturePanel != null)
            {
                furniturePanel.ClearDescription();
            }
        }
    }

    void ClearDescriptionPanels()
    {
        var purchasePanel = GetPurchaseDescriptionPanel();
        if (purchasePanel != null)
        {
            purchasePanel.ClearDescription();
        }

        var sellPanel = GetSellDescriptionPanel();
        if (sellPanel != null)
        {
            sellPanel.ClearDescription();
        }
    }

    FurnitureDescriptionPanel GetPurchaseDescriptionPanel()
    {
        FurnitureDescriptionPanel descPanel = null;

        if (purchaseDescriptionArea != null)
        {
            descPanel = purchaseDescriptionArea.GetComponent<FurnitureDescriptionPanel>();
            if (descPanel == null)
            {
                descPanel = purchaseDescriptionArea.GetComponentInChildren<FurnitureDescriptionPanel>();
            }
        }

        if (descPanel == null)
        {
            descPanel = FindFirstObjectByType<FurnitureDescriptionPanel>();
        }

        return descPanel;
    }

    FurnitureDescriptionPanel GetSellDescriptionPanel()
    {
        FurnitureDescriptionPanel descPanel = null;

        if (sellDescriptionArea != null)
        {
            descPanel = sellDescriptionArea.GetComponent<FurnitureDescriptionPanel>();
            if (descPanel == null)
            {
                descPanel = sellDescriptionArea.GetComponentInChildren<FurnitureDescriptionPanel>();
            }
        }

        if (descPanel == null)
        {
            descPanel = FindFirstObjectByType<FurnitureDescriptionPanel>();
        }

        return descPanel;
    }
}
