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
    public GameObject furnitureDescriptionArea;
    public GameObject materialDescriptionArea;

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

    private readonly Dictionary<string, ShopItem> allItems = new();
    private readonly List<ShopItem> dailyPurchaseItems = new();
    private int generatedDay = -1;
    private GameClock clock;
    private bool isOpen;
    private bool inputOwnedExternally;
    private InventoryItem selectedForSale;
    private ShopItem selectedForPurchase;
    private UISlidePanel currentTabSlidePanel;
    private UISlidePanel pendingTabSlidePanel;
    private Action pendingTabShownCallback;
    private bool tabTransitionInProgress;

    // Sell tab filter state
    private string sellSortType = "name";
    private bool sellSortAscending = true;
    private bool sellShowOnlyFavorites = false;
    private string sellSearchQuery = "";

    public bool IsOpen => isOpen;

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
            purchaseTabSlidePanel.OnSlideOutComplete += HandlePurchaseTabSlideOutComplete;
        }

        if (sellTabSlidePanel != null)
        {
            sellTabSlidePanel.OnSlideOutComplete += HandleSellTabSlideOutComplete;
        }

        if (shopRoot != null)
        {
            shopRoot.SetActive(false);
        }

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

    void OnDestroy()
    {
        if (clock != null)
        {
            clock.OnDayChanged -= OnDayChanged;
        }

        if (purchaseTabSlidePanel != null)
        {
            purchaseTabSlidePanel.OnSlideOutComplete -= HandlePurchaseTabSlideOutComplete;
        }

        if (sellTabSlidePanel != null)
        {
            sellTabSlidePanel.OnSlideOutComplete -= HandleSellTabSlideOutComplete;
        }
    }

    void OnDayChanged(int day)
    {
        dailyPurchaseItems.Clear();
        generatedDay = -1;
    }

    void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                CloseShop();
            }
        }
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
        OpenShopInternal(false);
        ShowSellTab();
    }

    public void OpenBuyPanel(bool externalInputOwner = false)
    {
        OpenShopInternal(externalInputOwner);
        ShowPurchaseTab();
    }

    public void OpenSellPanel(bool externalInputOwner = false)
    {
        OpenShopInternal(externalInputOwner);
        ShowSellTab();
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

    void OpenShopInternal(bool externalInputOwner)
    {
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
            pendingTabSlidePanel = null;
            pendingTabShownCallback = null;
            tabTransitionInProgress = false;
            onShown?.Invoke();
            return;
        }

        if (currentTabSlidePanel == targetSlidePanel && currentTabSlidePanel != null && currentTabSlidePanel.IsOpen)
        {
            onShown?.Invoke();
            return;
        }

        pendingTabSlidePanel = targetSlidePanel;
        pendingTabShownCallback = onShown;

        if (currentTabSlidePanel != null && (currentTabSlidePanel.IsOpen || tabTransitionInProgress))
        {
            if (!tabTransitionInProgress)
            {
                tabTransitionInProgress = true;
                currentTabSlidePanel.SlideOut();
            }
            return;
        }

        ActivatePendingTab();
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

    void ActivatePendingTab()
    {
        tabTransitionInProgress = false;

        if (pendingTabSlidePanel == null)
        {
            pendingTabShownCallback?.Invoke();
            pendingTabShownCallback = null;
            return;
        }

        currentTabSlidePanel = pendingTabSlidePanel;
        pendingTabSlidePanel = null;
        currentTabSlidePanel.SlideIn();

        pendingTabShownCallback?.Invoke();
        pendingTabShownCallback = null;
    }

    void HandlePurchaseTabSlideOutComplete()
    {
        HandleTabSlideOutComplete(purchaseTabSlidePanel);
    }

    void HandleSellTabSlideOutComplete()
    {
        HandleTabSlideOutComplete(sellTabSlidePanel);
    }

    void HandleTabSlideOutComplete(UISlidePanel panel)
    {
        if (currentTabSlidePanel == panel)
        {
            currentTabSlidePanel = null;
        }

        ActivatePendingTab();
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

        foreach (Transform child in purchaseContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in dailyPurchaseItems)
        {
            var go = Instantiate(purchaseItemCardPrefab, purchaseContent);
            go.transform.localScale = Vector3.one;
            var card = go.GetComponent<InventoryItemCardPurchase>();
            if (card != null)
            {
                int owned = InventoryManager.Instance?.GetItemCount(InventoryItem.ItemType.Furniture, item.itemID) ?? 0;
                var invItem = new InventoryItem(InventoryItem.ItemType.Furniture, item.itemID, owned);
                card.SetItem(invItem);
                card.OnItemClicked += _ => SelectItemForPurchase(item);
                card.SetSelected(selectedForPurchase != null && selectedForPurchase.itemID == item.itemID);
            }
        }

        UpdatePurchaseDescription(selectedForPurchase);
    }

    void PopulateSellTab()
    {
        if (sellContent == null || sellItemCardPrefab == null) return;

        foreach (Transform child in sellContent)
        {
            Destroy(child.gameObject);
        }

        // Furniture with filters
        var furniture = InventoryManager.Instance?.GetFurnitureList(sellSortType, false, sellShowOnlyFavorites, sellSortAscending);
        if (furniture != null)
        {
            if (!string.IsNullOrEmpty(sellSearchQuery))
            {
                furniture = furniture.Where(item =>
                {
                    var data = FurnitureDataManager.Instance?.GetFurnitureData(item.itemID);
                    if (data == null) return false;
                    string locName = LocalizationSettings.StringDatabase.GetLocalizedString("ItemNames", data.nameID);
                    return !string.IsNullOrEmpty(locName) && locName.ToLower().Contains(sellSearchQuery.ToLower());
                }).ToList();
            }

            foreach (var item in furniture)
            {
                var go = Instantiate(sellItemCardPrefab, sellContent);
                go.transform.localScale = Vector3.one;
                var card = go.GetComponent<InventoryItemCardSell>();
                if (card != null)
                {
                    card.SetItem(item, false);
                    card.OnItemClicked += SelectItemForSale;
                    card.OnFavoriteToggled += _ => PopulateSellTab();
                    card.SetSelected(selectedForSale != null && card.currentItem == selectedForSale);
                }
            }
        }

        // Materials (filter out any without data)
        var materials = InventoryManager.Instance?.GetMaterialList();
        if (materials != null)
        {
            foreach (var item in materials)
            {
                // Dev materials may not have data; skip any without valid material data
                var data = InventoryManager.Instance?.GetMaterialData(item.itemID);
                if (data == null)
                {
                    continue;
                }

                var go = Instantiate(sellItemCardPrefab, sellContent);
                go.transform.localScale = Vector3.one;
                var card = go.GetComponent<InventoryItemCardSell>();
                if (card != null)
                {
                    card.SetItem(item, true);
                    card.OnItemClicked += SelectItemForSale;
                    card.OnFavoriteToggled += _ => PopulateSellTab();
                    card.SetSelected(selectedForSale != null && card.currentItem == selectedForSale);
                }
            }
        }

        UpdateDescriptionPanel(selectedForSale);
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
        if (sellButton != null) sellButton.interactable = true;

        foreach (Transform child in sellContent)
        {
            var card = child.GetComponent<InventoryItemCard>();
            if (card != null)
            {
                bool isSelected = card.currentItem == item;
                card.SetSelected(isSelected);
            }
        }

        UpdateDescriptionPanel(item);
    }

    void SelectItemForPurchase(ShopItem item)
    {
        selectedForPurchase = item;
        UpdatePurchaseButtonState();

        foreach (Transform child in purchaseContent)
        {
            var card = child.GetComponent<InventoryItemCardPurchase>();
            if (card != null)
            {
                bool isSelected = card.currentItem != null && card.currentItem.itemID == item.itemID;
                card.SetSelected(isSelected);
            }
        }

        UpdatePurchaseDescription(item);
    }

    void PurchaseSelectedItem()
    {
        if (selectedForPurchase == null) return;
        int price = GetBuyPrice(selectedForPurchase);
        if (MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney >= price)
        {
            // Reduce player's money and add item to inventory
            MoneyManager.Instance.AddMoney(-price);
            InventoryManager.Instance?.AddFurniture(selectedForPurchase.itemID, 1);
            Debug.Log($"Purchased {selectedForPurchase.itemID} for {price}");

            // Refresh UI but keep the item selected
            PopulatePurchaseTab();
        }
        else
        {
            Debug.Log("Not enough money to purchase " + selectedForPurchase.itemID);
        }

        UpdatePurchaseButtonState();
        UpdatePurchaseDescription(selectedForPurchase);
    }

    void SellSelectedItem()
    {
        if (selectedForSale == null) return;

        var item = selectedForSale;
        int price = 0;

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
        MoneyManager.Instance?.AddMoney(price);

        // Refresh UI while keeping selection
        PopulateSellTab();
        if (sellButton != null)
        {
            sellButton.interactable = selectedForSale.quantity > 0;
        }

        UpdatePurchaseButtonState();
        UpdateDescriptionPanel(selectedForSale);
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

    void UpdatePurchaseDescription(ShopItem item)
    {
        if (item == null)
        {
            ClearDescriptionPanels();
            return;
        }

        InventoryItem invItem = InventoryManager.Instance?.GetFurnitureItem(item.itemID);

        if (invItem == null)
        {
            int owned = InventoryManager.Instance?.GetItemCount(InventoryItem.ItemType.Furniture, item.itemID) ?? 0;
            invItem = new InventoryItem(InventoryItem.ItemType.Furniture, item.itemID, owned);
        }

        UpdateDescriptionPanel(invItem);
    }

    void UpdateDescriptionPanel(InventoryItem item)
    {
        var furniturePanel = GetFurnitureDescriptionPanel();
        var materialPanel = GetMaterialDescriptionPanel();

        if (item == null)
        {
            if (furniturePanel != null) furniturePanel.ClearDescription();
            if (materialPanel != null) materialPanel.ClearDescription();
            return;
        }

        if (item.itemType == InventoryItem.ItemType.Furniture)
        {
            if (furniturePanel != null)
            {
                furniturePanel.ShowFurnitureDetail(item);
            }
            if (materialPanel != null)
            {
                materialPanel.ClearDescription();
            }
        }
        else if (item.itemType == InventoryItem.ItemType.Material)
        {
            if (materialPanel != null)
            {
                materialPanel.ShowMaterialDetail(item);
            }
            if (furniturePanel != null)
            {
                furniturePanel.ClearDescription();
            }
        }
    }

    void ClearDescriptionPanels()
    {
        UpdateDescriptionPanel(null);
    }

    FurnitureDescriptionPanel GetFurnitureDescriptionPanel()
    {
        FurnitureDescriptionPanel descPanel = null;

        if (furnitureDescriptionArea != null)
        {
            descPanel = furnitureDescriptionArea.GetComponent<FurnitureDescriptionPanel>();
            if (descPanel == null)
            {
                descPanel = furnitureDescriptionArea.GetComponentInChildren<FurnitureDescriptionPanel>();
            }
        }

        if (descPanel == null)
        {
            descPanel = FindFirstObjectByType<FurnitureDescriptionPanel>();
        }

        return descPanel;
    }

    MaterialDescriptionPanel GetMaterialDescriptionPanel()
    {
        MaterialDescriptionPanel descPanel = null;

        if (materialDescriptionArea != null)
        {
            descPanel = materialDescriptionArea.GetComponent<MaterialDescriptionPanel>();
            if (descPanel == null)
            {
                descPanel = materialDescriptionArea.GetComponentInChildren<MaterialDescriptionPanel>();
            }
        }

        if (descPanel == null)
        {
            descPanel = FindFirstObjectByType<MaterialDescriptionPanel>();
        }

        return descPanel;
    }
}
