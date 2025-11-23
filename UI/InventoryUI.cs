using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.Events;


public class InventoryUI : MonoBehaviour
{
    [Header("=== DEBUG MODE ===")]
    public bool debugMode = false;
    [Header("UI References")]
    public GameObject inventoryPanel;
    public GameObject inventoryWindow;
    public Button openButton;


    [Serializable]
    private class InventoryTabBinding
    {
        public InventoryTabType type;
        public Toggle toggle;
        public GameObject content;
    }

    public enum InventoryTabType
    {
        Material,
        Furniture
    }

    [Header("Tab Container")]
    public GameObject tabContainer;
    public GameObject tabContentRoot;
    public ToggleGroup tabToggleGroup;
    [SerializeField] private List<InventoryTabBinding> tabs = new List<InventoryTabBinding>();

    [Header("Tab Visuals")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f, 0.5f);


    [Header("Material Tab Elements")]
    public GameObject materialContent;
    public GameObject materialDescriptionArea;
    public InputField materialSearchField;


    [Header("Furniture Tab Elements")]
    public GameObject furnitureContent;
    public GameObject furnitureDescriptionArea;
    public InputField furnitureSearchField;


    [Header("Prefabs")]
    public GameObject materialIconPrefab;
    public GameObject furnitureCardPrefab;


    [Header("Material Item Slots")]
    public Transform[] materialSlots;


    [Header("Sort Buttons - Material")]
    public Button rarityUpButtonMat;
    public Button rarityDownButtonMat;


    [Header("Sort Buttons - Furniture")]
    public Button rarityUpButtonFurn;
    public Button rarityDownButtonFurn;


    [Header("Filters")]
    public Toggle craftableToggle;
    public Toggle favoriteToggle;
    public Button craftButton;


    [Header("Auto Reopen")]
    public Toggle autoReopenToggle;


    [Header("Panel Animation")]
    [SerializeField] private float closedPositionX = 0f;
    [SerializeField] private float openPositionX = 0f;
    [SerializeField] private float anchoredY = 0f;
    [SerializeField] private float slideDuration = 1f;
    [SerializeField]
    private AnimationCurve slideInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(0.6f, 1.15f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f));
    [SerializeField]
    private AnimationCurve slideOutCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0.5f),
        new Keyframe(0.5f, 0.85f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f));
    private InventoryPanelAnimator panelAnimator;


    // マネージャー
    private InventoryCardManager cardManager;
    private InventoryMaterialManager materialManager;


    // 状態管理
    private bool isOpen = false;
    private bool isMaterialTab = true;
    private string currentSortType = "none";
    private bool sortAscending = true;
    private bool showOnlyCraftable = false;
    private bool showOnlyFavorites = false;
    private bool isPlacingItem = false;
    private string searchQuery = "";
    private InventoryItem selectedFurnitureItem;
    private bool autoReopenEnabled = false;
    private const string AutoReopenPrefKey = "InventoryUI.AutoReopenEnabled";
    private readonly Dictionary<Toggle, UnityAction<bool>> tabToggleListeners = new Dictionary<Toggle, UnityAction<bool>>();

    // シーン上の操作系（家具の再配置など）を制御するための参照
    private SelectionManager cachedSelectionManager;
    private ObjectManipulator cachedObjectManipulator;
    private bool selectionManagerWasEnabled;
    private bool objectManipulatorWasEnabled;
    private bool selectionManagerStateCached;
    private bool objectManipulatorStateCached;

    public bool IsOpen => isOpen;
    public bool AutoReopenEnabled => autoReopenEnabled;

    void Awake()
    {
        UIPanelExclusionManager.Instance?.Register(this);
        LoadAutoReopenPreference();
    }

    void Start()
    {
        if (debugMode) Debug.Log("=== InventoryUI Starting ===");

        InitializeManagers();
        SetupBaseUI();
        SetupTabs();
        SetupSortButtons();
        SetupSearchFields();
        SetupFilters();
        SetupCraftButton();
        SetupAutoReopenControl();
        RegisterEvents();
        InitializeTabState();

        CacheSceneInteractionComponents();
        UpdateAutoReopenVisual();
    }

    void InitializeTabState()
    {
        InventoryTabType? initialTab = null;

        foreach (var tab in tabs)
        {
            if (tab == null || tab.toggle == null)
                continue;

            if (tab.toggle.isOn)
            {
                initialTab = tab.type;
                break;
            }
        }

        if (!initialTab.HasValue && tabs.Count > 0)
        {
            initialTab = tabs[0].type;
        }

        if (initialTab.HasValue)
        {
            SwitchTab(initialTab.Value);
        }
    }

    void InitializeManagers()
    {
        cardManager = gameObject.AddComponent<InventoryCardManager>();
        cardManager.furnitureCardPrefab = furnitureCardPrefab;
        cardManager.Initialize(furnitureContent);

        materialManager = gameObject.AddComponent<InventoryMaterialManager>();
        materialManager.materialIconPrefab = materialIconPrefab;
        materialManager.Initialize(materialContent, materialSlots);
        if (materialDescriptionArea != null)
            materialManager.materialDescPanel = materialDescriptionArea.GetComponent<MaterialDescriptionPanel>();
    }

    void SetupBaseUI()
    {
        // インベントリ本体は常にアクティブにしておく
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        // タブコンテナの初期位置をオフスクリーンへ移動
        var animator = EnsurePanelAnimator();
        animator?.SnapToInitialPosition();

        // ボタン設定
        if (openButton != null)
        {
            openButton.onClick.AddListener(ToggleInventory);

            CanvasGroup openButtonCanvasGroup = openButton.GetComponent<CanvasGroup>();
            if (openButtonCanvasGroup == null)
            {
                openButtonCanvasGroup = openButton.gameObject.AddComponent<CanvasGroup>();
            }

            openButtonCanvasGroup.ignoreParentGroups = true;
            openButtonCanvasGroup.blocksRaycasts = true;
            openButtonCanvasGroup.interactable = true;
        }
    }

    void SetupTabs()
    {
        foreach (var binding in tabs)
        {
            if (binding == null || binding.toggle == null)
                continue;

            if (tabToggleGroup != null)
            {
                binding.toggle.group = tabToggleGroup;
            }

            if (tabToggleListeners.TryGetValue(binding.toggle, out var existingListener))
            {
                binding.toggle.onValueChanged.RemoveListener(existingListener);
            }

            var targetType = binding.type;
            UnityAction<bool> listener = isOn =>
            {
                if (isOn)
                {
                    SwitchTab(targetType);
                }
            };

            tabToggleListeners[binding.toggle] = listener;
            binding.toggle.onValueChanged.AddListener(listener);
        }
    }

    private CanvasGroup EnsureTabCanvasGroup()
    {
        GameObject canvasGroupOwner = tabContentRoot != null ? tabContentRoot : tabContainer;
        if (canvasGroupOwner == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = canvasGroupOwner.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvasGroupOwner.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        return canvasGroup;
    }

    private InventoryPanelAnimator EnsurePanelAnimator()
    {
        if (panelAnimator == null)
        {
            panelAnimator = GetComponent<InventoryPanelAnimator>();
            if (panelAnimator == null)
            {
                panelAnimator = gameObject.AddComponent<InventoryPanelAnimator>();
            }
        }

        RectTransform rectTransform = null;
        if (tabContainer != null)
        {
            rectTransform = tabContainer.GetComponent<RectTransform>();
        }

        CanvasGroup canvasGroup = EnsureTabCanvasGroup();
        panelAnimator.Initialize(rectTransform, canvasGroup, closedPositionX, openPositionX, anchoredY, slideDuration, slideInCurve, slideOutCurve);

        return panelAnimator;
    }

    private void UpdateTabToggleVisuals(InventoryTabType targetType)
    {
        foreach (var binding in tabs)
        {
            if (binding == null || binding.toggle == null)
                continue;

            bool isActive = binding.type == targetType;
            binding.toggle.SetIsOnWithoutNotify(isActive);

            if (binding.toggle.graphic != null)
            {
                binding.toggle.graphic.color = isActive ? activeTabColor : inactiveTabColor;
            }
        }
    }

    void ConfigureRarityButton(Button button, bool ascending, string debugLabel)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (debugMode) Debug.Log(debugLabel);
            currentSortType = "rarity";
            sortAscending = ascending;
            RefreshInventoryDisplay();
        });
    }

    void SetupSortButtons()
    {
        ConfigureRarityButton(rarityUpButtonMat, true, "Material Rarity Up clicked");
        ConfigureRarityButton(rarityDownButtonMat, false, "Material Rarity Down clicked");
        ConfigureRarityButton(rarityUpButtonFurn, true, "Furniture Rarity Up clicked");
        ConfigureRarityButton(rarityDownButtonFurn, false, "Furniture Rarity Down clicked");
    }

    void ConfigureSearchField(InputField field, string debugLabel)
    {
        if (field == null) return;

        field.onValueChanged.RemoveAllListeners();
        field.onValueChanged.AddListener(value =>
        {
            if (debugMode) Debug.Log($"{debugLabel}: {value}");
            searchQuery = value;
            RefreshInventoryDisplay();
        });

        var placeholder = field.placeholder?.GetComponent<Text>();
        if (placeholder != null) placeholder.text = "Search...";
    }

    void SetupSearchFields()
    {
        ConfigureSearchField(furnitureSearchField, "Furniture search");
        ConfigureSearchField(materialSearchField, "Material search");
    }

    void SetupFilters()
    {
        ConfigureFilterToggle(craftableToggle, value => showOnlyCraftable = value, "Craftable toggle");
        ConfigureFilterToggle(favoriteToggle, value => showOnlyFavorites = value, "Favorite toggle");
    }

    void ConfigureFilterToggle(Toggle toggle, Action<bool> apply, string debugLabel)
    {
        if (toggle == null || apply == null) return;

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(value =>
        {
            if (debugMode) Debug.Log($"[FILTER] {debugLabel} changed to: {value}");
            apply(value);
            RefreshInventoryDisplay();
        });
    }

    void SetupCraftButton()
    {
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() =>
            {
                if (debugMode) Debug.Log("=== CRAFT BUTTON CLICKED ===");
                CraftSelectedItem();
            });

            craftButton.interactable = false;

            if (debugMode) Debug.Log("Craft button setup complete");
        }
    }

    void SetupAutoReopenControl()
    {
        if (autoReopenToggle != null)
        {
            autoReopenToggle.onValueChanged.RemoveAllListeners();
            autoReopenToggle.SetIsOnWithoutNotify(autoReopenEnabled);
            autoReopenToggle.onValueChanged.AddListener(_ => ToggleAutoReopen());
        }

    }

    public void ToggleAutoReopen()
    {
        autoReopenEnabled = !autoReopenEnabled;
        UpdateAutoReopenVisual();
        SaveAutoReopenPreference();
    }

    void UpdateAutoReopenVisual()
    {
        if (autoReopenToggle != null)
        {
            autoReopenToggle.SetIsOnWithoutNotify(autoReopenEnabled);
        }
    }

    void LoadAutoReopenPreference()
    {
        autoReopenEnabled = PlayerPrefs.GetInt(AutoReopenPrefKey, autoReopenEnabled ? 1 : 0) == 1;
    }

    void SaveAutoReopenPreference()
    {
        PlayerPrefs.SetInt(AutoReopenPrefKey, autoReopenEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    void RegisterEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventoryDisplay;
        }
    }

    // 家具カード選択時に呼び出される（InventoryCardManagerから通知）
    public void OnFurnitureItemSelected(InventoryItem item)
    {
        if (debugMode) Debug.Log($"[SELECT] Furniture item selected: {item?.itemID ?? "null"}");

        selectedFurnitureItem = item;

        UpdateFurnitureDescription(item);
        UpdateCraftButtonState();
    }

    void UpdateFurnitureDescription(InventoryItem item)
    {
        if (item == null) return;

        var descPanel = GetFurnitureDescriptionPanel();

        if (descPanel != null)
        {
            descPanel.ShowFurnitureDetail(item);
            if (debugMode) Debug.Log($"Updated FurnitureDescriptionPanel for: {item.itemID}");
        }
        else if (debugMode)
        {
            Debug.LogError("FurnitureDescriptionPanel not found!");
        }
    }

    void CraftSelectedItem()
    {
        if (debugMode) Debug.Log("=== CraftSelectedItem START ===");

        var descPanel = GetFurnitureDescriptionPanel();
        if (descPanel == null)
        {
            Debug.LogError("[CRAFT] FurnitureDescriptionPanel not found!");
            return;
        }

        var currentItem = descPanel.GetCurrentItem();
        var currentFurnitureDataSO = descPanel.GetCurrentFurnitureDataSO();

        if (debugMode)
        {
            Debug.Log($"[CRAFT] Current Item: {currentItem?.itemID ?? "null"}");
            Debug.Log($"[CRAFT] Current DataSO: {currentFurnitureDataSO?.nameID ?? "null"}");
        }

        if (currentItem == null || currentFurnitureDataSO == null)
        {
            Debug.LogWarning("[CRAFT] No item selected for crafting");
            return;
        }

        if (!descPanel.CanCraftCurrentItem())
        {
            Debug.LogWarning($"[CRAFT] Cannot craft {currentItem.itemID} - insufficient materials");
            return;
        }

        bool craftSuccess = PerformCraft(currentFurnitureDataSO);

        if (craftSuccess)
        {
            InventoryManager.Instance.AddFurniture(currentItem.itemID, 1);
            if (debugMode) Debug.Log($"[CRAFT] Successfully crafted {currentFurnitureDataSO.nameID}");

            InventoryManager.Instance.ForceInventoryUpdate();
            RefreshInventoryDisplay();

            descPanel.ShowFurnitureDetail(currentItem);
            UpdateCraftButtonState();
        }

        if (debugMode) Debug.Log("=== CraftSelectedItem END ===");
    }

    bool PerformCraft(FurnitureDataSO furnitureDataSO)
    {
        bool craftSuccess = true;
        List<(string materialID, int quantity)> consumedMaterials = new List<(string, int)>();

        for (int i = 0; i < furnitureDataSO.recipeMaterialIDs.Length; i++)
        {
            string materialID = furnitureDataSO.recipeMaterialIDs[i];
            int requiredQuantity = furnitureDataSO.recipeMaterialQuantities[i];

            if (!string.IsNullOrEmpty(materialID) && materialID != "None" && requiredQuantity > 0)
            {
                consumedMaterials.Add((materialID, requiredQuantity));

                if (!InventoryManager.Instance.RemoveMaterial(materialID, requiredQuantity))
                {
                    craftSuccess = false;

                    foreach (var consumed in consumedMaterials)
                    {
                        if (consumed.materialID != materialID)
                        {
                            InventoryManager.Instance.AddMaterial(consumed.materialID, consumed.quantity);
                        }
                    }

                    Debug.LogError($"[CRAFT] Failed to consume material: {materialID} x{requiredQuantity}");
                    break;
                }
            }
        }

        return craftSuccess;
    }

    FurnitureDescriptionPanel GetFurnitureDescriptionPanel()
    {
        FurnitureDescriptionPanel descPanel = null;

        if (furnitureDescriptionArea != null)
        {
            descPanel = furnitureDescriptionArea.GetComponent<FurnitureDescriptionPanel>();
            if (descPanel == null)
                descPanel = furnitureDescriptionArea.GetComponentInChildren<FurnitureDescriptionPanel>();
        }

        if (descPanel == null)
        {
            descPanel = FindFirstObjectByType<FurnitureDescriptionPanel>();
        }

        return descPanel;
    }

    public void UpdateCraftButtonState()
    {
        if (craftButton == null) return;

        var descPanel = GetFurnitureDescriptionPanel();
        if (descPanel == null)
        {
            craftButton.interactable = false;
            if (debugMode) Debug.Log("[CRAFT] Button disabled - no description panel");
            return;
        }

        var currentItem = descPanel.GetCurrentItem();
        var currentFurnitureDataSO = descPanel.GetCurrentFurnitureDataSO();

        bool shouldEnable = !isMaterialTab &&
                           currentItem != null &&
                           currentItem.itemType == InventoryItem.ItemType.Furniture &&
                           currentFurnitureDataSO != null &&
                           currentFurnitureDataSO.HasRecipe &&
                           descPanel.CanCraftCurrentItem();

        craftButton.interactable = shouldEnable;

        if (debugMode)
        {
            Debug.Log($"[CRAFT] Button state updated: {shouldEnable}");
            if (!shouldEnable)
            {
                Debug.Log($"  - Is Material Tab: {isMaterialTab}");
                Debug.Log($"  - Has Item: {currentItem != null}");
                Debug.Log($"  - Has Recipe: {currentFurnitureDataSO?.HasRecipe ?? false}");
                Debug.Log($"  - Can Craft: {descPanel?.CanCraftCurrentItem() ?? false}");
            }
        }
    }

    void Update()
    {
        if (isPlacingItem)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseInventory();
        }

        if (isOpen && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUIElement())
            {
                cardManager?.DeselectAll();
            }
        }
    }

    bool IsPointerOverUIElement()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (inventoryPanel != null && result.gameObject.transform.IsChildOf(inventoryPanel.transform))
            {
                return true;
            }
        }
        return false;
    }

    public void ToggleInventory()
    {
        if (isPlacingItem)
            return;

        if (isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (isOpen) return;

        var animator = EnsurePanelAnimator();

        isOpen = true;
        UIPanelExclusionManager.Instance?.NotifyOpened(this);

        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(true);
        }

        if (tabContainer != null && !tabContainer.activeSelf)
        {
            tabContainer.SetActive(true);
        }

        animator?.PlayOpen();
        RefreshInventoryDisplay();

        DisablePlayerControl(true);
        NotifyCameraController(true);
        SetSceneInteractionActive(false);
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        var animator = EnsurePanelAnimator();

        isOpen = false;

        animator?.PlayClose();
        DisablePlayerControl(false);
        NotifyCameraController(false);

        if (!isPlacingItem)
        {
            SetSceneInteractionActive(true);
        }
    }

    void DisablePlayerControl(bool disable)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = !disable;
        }
    }

    void CacheSceneInteractionComponents()
    {
        if (cachedSelectionManager == null)
        {
            cachedSelectionManager = FindFirstObjectByType<SelectionManager>();
            if (cachedSelectionManager != null)
            {
                selectionManagerWasEnabled = cachedSelectionManager.enabled;
                selectionManagerStateCached = true;
            }
        }

        if (cachedObjectManipulator == null)
        {
            cachedObjectManipulator = FindFirstObjectByType<ObjectManipulator>();
            if (cachedObjectManipulator != null)
            {
                objectManipulatorWasEnabled = cachedObjectManipulator.enabled;
                objectManipulatorStateCached = true;
            }
        }
    }

    void SetSceneInteractionActive(bool active)
    {
        CacheSceneInteractionComponents();

        if (!active)
        {
            if (cachedSelectionManager != null)
            {
                selectionManagerWasEnabled = cachedSelectionManager.enabled;
                selectionManagerStateCached = true;
                cachedSelectionManager.enabled = false;
            }

            if (cachedObjectManipulator != null)
            {
                objectManipulatorWasEnabled = cachedObjectManipulator.enabled;
                objectManipulatorStateCached = true;
                cachedObjectManipulator.enabled = false;
            }
        }
        else
        {
            if (cachedSelectionManager != null && selectionManagerStateCached)
            {
                cachedSelectionManager.enabled = selectionManagerWasEnabled;
            }

            if (cachedObjectManipulator != null && objectManipulatorStateCached)
            {
                cachedObjectManipulator.enabled = objectManipulatorWasEnabled;
            }
        }
    }


    void NotifyCameraController(bool inventoryOpen)
    {
        var cameraController = FindFirstObjectByType<OrthographicCameraController>();
        if (cameraController != null)
        {
            cameraController.NotifyInventoryStateChanged(inventoryOpen);
            if (debugMode) Debug.Log($"[UI] Notified camera controller - Inventory: {(inventoryOpen ? "Open" : "Closed")}");
        }
    }

    public void SetPlacingItem(bool placing)
    {
        isPlacingItem = placing;

        if (placing)
        {
            SetSceneInteractionActive(false);
        }
        else if (!isOpen)
        {
            SetSceneInteractionActive(true);
        }
    }

    public void SwitchTab(bool material)
    {
        SwitchTab(material ? InventoryTabType.Material : InventoryTabType.Furniture);
    }

    public void SwitchTab(InventoryTabType targetType)
    {
        isMaterialTab = targetType == InventoryTabType.Material;
        searchQuery = "";  // タブ切り替え時に検索をクリア

        UpdateTabToggleVisuals(targetType);

        materialManager?.ClearSelection();

        foreach (var binding in tabs)
        {
            if (binding?.content == null)
                continue;

            binding.content.SetActive(binding.type == targetType);
        }

        RefreshInventoryDisplay();
    }

    public void RefreshInventoryDisplay()
    {
        if (isMaterialTab)
        {
            RefreshMaterialDisplay();
        }
        else
        {
            RefreshFurnitureDisplay();
            UpdateCraftButtonState();
        }
    }

    void RefreshMaterialDisplay()
    {
        var items = GetSortedMaterialList();

        // 検索フィルター適用
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(item =>
            {
                var materialData = InventoryManager.Instance?.GetMaterialData(item.itemID);
                return materialData != null &&
                       materialData.materialName.ToLower().Contains(searchQuery.ToLower());
            }).ToList();
        }

        materialManager?.RefreshMaterialIcons(items);

        // Material用の説明エリア更新（必要に応じて）
        UpdateMaterialDescriptionArea();
    }

    void RefreshFurnitureDisplay()
    {
        var items = GetSortedFurnitureList();

        // 検索フィルター適用
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(item =>
            {
                return item.itemID.ToLower().Contains(searchQuery.ToLower());
            }).ToList();
        }

        if (debugMode) Debug.Log($"RefreshFurnitureDisplay - Total items: {items.Count}, Craftable filter: {showOnlyCraftable}, Favorite filter: {showOnlyFavorites}");

        cardManager?.RefreshFurnitureCards(items);
    }

    // Material用の説明エリア更新（新規追加）
    void UpdateMaterialDescriptionArea()
    {
        // シンプルな説明エリアの更新処理
        // 拡張しやすいように基本構造だけ用意
        if (materialDescriptionArea != null)
        {
            var descPanel = materialDescriptionArea.GetComponent<MaterialDescriptionPanel>();
            if (descPanel != null)
            {
                // 選択されたアイテムがあれば表示を更新
                // この部分は InventoryMaterialManager からのコールバックで処理
            }
        }
    }

    List<InventoryItem> GetSortedMaterialList()
    {
        if (debugMode) Debug.Log($"GetSortedMaterialList - Sort: {currentSortType}, Favorites: {showOnlyFavorites}, Ascending: {sortAscending}");
        return InventoryManager.Instance?.GetMaterialList(currentSortType, showOnlyFavorites, sortAscending)
               ?? new List<InventoryItem>();
    }

    List<InventoryItem> GetSortedFurnitureList()
    {
        if (debugMode) Debug.Log($"GetSortedFurnitureList - Sort: {currentSortType}, Craftable: {showOnlyCraftable}, Favorites: {showOnlyFavorites}, Ascending: {sortAscending}");
        var list = InventoryManager.Instance?.GetFurnitureList(currentSortType, showOnlyCraftable, showOnlyFavorites, sortAscending)
                   ?? new List<InventoryItem>();

        // デバッグ用：取得したリストの内容を確認（エディター専用）
#if UNITY_EDITOR
        if (debugMode)
        {
            foreach (var item in list.Take(3)) // 最初の3つだけログ出力
            {
                Debug.Log($"  Item: {item.itemID}, Craftable: {item.canCraft}, Favorite: {item.isFavorite}");
            }
        }
#endif

        return list;
    }

    public bool IsPointerOverInventoryWindow(Vector2 screenPosition, Camera camera = null)
    {
        GameObject targetWindow = inventoryWindow != null ? inventoryWindow : inventoryPanel;

        if (targetWindow != null)
        {
            RectTransform rect = targetWindow.GetComponent<RectTransform>();
            if (rect != null)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
            }
        }

        return false;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventoryDisplay;
        }

        cardManager?.Cleanup();
        materialManager?.Cleanup();

        foreach (var pair in tabToggleListeners)
        {
            if (pair.Key != null)
            {
                pair.Key.onValueChanged.RemoveListener(pair.Value);
            }
        }

        tabToggleListeners.Clear();
    }
}
