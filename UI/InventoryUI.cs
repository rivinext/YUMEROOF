using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;


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

    [Serializable]
    private class CategoryDisplaySetting
    {
        public string categoryId;
        public string displayName;
        public Sprite icon;
        public bool useBackgroundColor;
        public Color backgroundColor = Color.white;
        public bool useCheckmarkColor;
        public Color checkmarkColor = Color.white;
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

    [Header("Material Tab Elements")]
    public GameObject materialContent;
    public GameObject materialDescriptionArea;
    public TMP_InputField materialSearchField;


    [Header("Furniture Tab Elements")]
    public GameObject furnitureContent;
    public GameObject furnitureScrollView;
    public GameObject furnitureDescriptionArea;
    public TMP_InputField furnitureSearchField;

    [Header("Furniture Virtualization")]
    [SerializeField] private ScrollRect furnitureScrollRect;
    [SerializeField] private ScrollRectVirtualizer furnitureVirtualizer;
    [SerializeField] private float furnitureItemHeight = 120f;
    [SerializeField] private float furnitureItemSpacing = 0f;
    [SerializeField] private float furniturePaddingTop = 0f;
    [SerializeField] private float furniturePaddingBottom = 0f;

    [Header("Furniture Category Tabs")]
    [SerializeField] private Transform furnitureCategoryTabContainer;
    [SerializeField] private ToggleGroup furnitureCategoryToggleGroup;
    [SerializeField] private GameObject furnitureCategoryTogglePrefab;
    [SerializeField] private Sprite defaultCategoryIcon;
    [SerializeField] private Color defaultCategoryColor = Color.white;
    [SerializeField] private Color defaultCategoryCheckmarkColor = Color.white;
    private const string StandardTextTableName = "StandardText";
    private static readonly Dictionary<string, string> CategoryLocalizationKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    [SerializeField] private string allCategoryKey = "ALL";
    [SerializeField] private string allCategoryLabel = "ALL";
    [SerializeField] private Sprite allCategoryIcon;
    [SerializeField] private List<CategoryDisplaySetting> categoryDisplaySettings = new List<CategoryDisplaySetting>();


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
    public Toggle wallPlacementToggle;
    public Toggle ceilingPlacementToggle;
    public Button craftButton;

    [Header("Audio")]
    [SerializeField] private AudioClip craftButtonSfx;
    [SerializeField] private AudioSource craftButtonAudioSource;
    [SerializeField, Range(0f, 1f)] private float craftButtonSfxVolume = 1f;
    [SerializeField] private bool autoCreateAudioSource = true;

    [Header("Auto Reopen")]
    public Toggle autoReopenToggle;


    [Header("Panel Animation")]
    [SerializeField] private PanelScaleAnimator panelScaleAnimator;


    // マネージャー
    private InventoryCardManager cardManager;
    private InventoryMaterialManager materialManager;
    private ShopConversationController shopConversationController;


    // 状態管理
    private bool isOpen = false;
    private bool isMaterialTab = true;
    private string currentSortType = "none";
    private bool sortAscending = true;
    private bool showOnlyCraftable = false;
    private bool showOnlyFavorites = false;
    private bool showOnlyWallPlacement = false;
    private bool showOnlyCeilingPlacement = false;
    private bool isPlacingItem = false;
    private string searchQuery = "";
    private bool isSearchEditing = false;
    private TMP_InputField currentSearchField;
    private InventoryItem selectedFurnitureItem;
    private string selectedFurnitureCategory;
    private bool autoReopenEnabled = false;
    private float currentSfxVolume = 1f;
    private const string AutoReopenPrefKey = "InventoryUI.AutoReopenEnabled";
    private readonly Dictionary<Toggle, UnityAction<bool>> tabToggleListeners = new Dictionary<Toggle, UnityAction<bool>>();
    private readonly List<FurnitureCategoryToggle> categoryToggles = new List<FurnitureCategoryToggle>();
    private Coroutine inventoryRefreshCoroutine;
    private bool inventoryRefreshQueued;
    private readonly List<InventoryItem> filteredFurnitureItems = new List<InventoryItem>();
    private float lastFurnitureScrollNormalized = 1f;
    private bool hasSavedFurnitureScrollPosition;
    private string lastFurnitureSearchQuery = "";
    private string lastFurnitureCategory = "";
    private UnityAction<Vector2> furnitureScrollListener;

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
        SetupFurnitureCategoryTabs();
        SetupFurnitureVirtualization();
        SetupCraftButton();
        SetupAutoReopenControl();
        RegisterEvents();
        InitializeTabState();

        CacheSceneInteractionComponents();
        UpdateAutoReopenVisual();
    }

    void OnEnable()
    {
        AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        HandleSfxVolumeChanged(AudioManager.CurrentSfxVolume);
    }

    void OnDisable()
    {
        AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
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

        // パネルをスケール0の初期状態に設定
        EnsurePanelScaleAnimator();
        panelScaleAnimator?.SnapClosed();

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

    private void EnsurePanelScaleAnimator()
    {
        if (panelScaleAnimator == null)
        {
            panelScaleAnimator = GetComponent<PanelScaleAnimator>();
        }
    }

    private void UpdateTabToggleVisuals(InventoryTabType targetType)
    {
        foreach (var binding in tabs)
        {
            if (binding == null || binding.toggle == null)
                continue;

            bool isActive = binding.type == targetType;
            binding.toggle.SetIsOnWithoutNotify(isActive);
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

    void ConfigureSearchField(TMP_InputField field, string debugLabel)
    {
        if (field == null) return;

        field.onValueChanged.RemoveAllListeners();
        field.onValueChanged.AddListener(value =>
        {
            if (debugMode) Debug.Log($"{debugLabel}: {value}");
            searchQuery = value;
            RefreshInventoryDisplay();
        });

        field.onSelect.RemoveAllListeners();
        field.onSelect.AddListener(_ => HandleSearchFieldSelected(field));

        field.onDeselect.RemoveAllListeners();
        field.onDeselect.AddListener(_ => HandleSearchFieldDeselected(field));

        field.onSubmit.RemoveAllListeners();
        field.onSubmit.AddListener(_ => HandleSearchFieldSubmitted(field));

        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(_ => HandleSearchFieldEndEdit(field));

        var placeholder = field.placeholder as TMP_Text;
        if (placeholder != null) placeholder.text = "Search...";
    }

    void SetupSearchFields()
    {
        ConfigureSearchField(furnitureSearchField, "Furniture search");
        ConfigureSearchField(materialSearchField, "Material search");
    }

    void HandleSearchFieldSelected(TMP_InputField field)
    {
        if (field == null)
            return;

        if (currentSearchField == field && isSearchEditing)
            return;

        currentSearchField = field;
        isSearchEditing = true;
        PlayerController.SetGlobalInputEnabled(false);
    }

    void HandleSearchFieldDeselected(TMP_InputField field)
    {
        if (currentSearchField != field)
            return;

        ClearSearchEditingState();
    }

    void HandleSearchFieldSubmitted(TMP_InputField field)
    {
        if (currentSearchField != field)
            return;

        currentSearchField.DeactivateInputField();
    }

    void HandleSearchFieldEndEdit(TMP_InputField field)
    {
        if (currentSearchField != field)
            return;

        ClearSearchEditingState();
    }

    void ClearSearchEditingState()
    {
        if (!isSearchEditing)
            return;

        isSearchEditing = false;
        currentSearchField = null;
        PlayerController.SetGlobalInputEnabled(true);
    }

    void SetupFilters()
    {
        ConfigureFilterToggle(craftableToggle, value => showOnlyCraftable = value, "Craftable toggle");
        ConfigureFilterToggle(favoriteToggle, value => showOnlyFavorites = value, "Favorite toggle");
        ConfigureFilterToggle(wallPlacementToggle, value => showOnlyWallPlacement = value, "Wall placement toggle");
        ConfigureFilterToggle(ceilingPlacementToggle, value => showOnlyCeilingPlacement = value, "Ceiling placement toggle");
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

    void SetupFurnitureCategoryTabs()
    {
        if (furnitureCategoryTabContainer == null || furnitureCategoryTogglePrefab == null || furnitureCategoryToggleGroup == null)
        {
            if (debugMode) Debug.LogWarning("[CATEGORY] Furniture category tab references are missing.");
            return;
        }

        ClearFurnitureCategoryTabs();

        var categories = GetFurnitureCategories().ToList();
        var orderedCategories = new List<string>();
        var addedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var displaySetting in categoryDisplaySettings)
        {
            if (displaySetting == null || string.IsNullOrEmpty(displaySetting.categoryId))
                continue;

            var matchedCategory = categories.FirstOrDefault(category =>
                string.Equals(category, displaySetting.categoryId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchedCategory) && addedCategories.Add(matchedCategory))
            {
                orderedCategories.Add(matchedCategory);
            }
        }

        foreach (var category in categories)
        {
            if (addedCategories.Add(category))
            {
                orderedCategories.Add(category);
            }
        }
        CreateFurnitureCategoryToggle(allCategoryKey, allCategoryLabel, allCategoryIcon ?? defaultCategoryIcon);

        foreach (var category in orderedCategories)
        {
            CreateFurnitureCategoryToggle(category, ResolveCategoryDisplayName(category), ResolveCategoryIcon(category));
        }

        string previousCategory = selectedFurnitureCategory;
        string targetCategory = allCategoryKey;
        FurnitureCategoryToggle targetToggle = null;

        if (!string.IsNullOrEmpty(previousCategory))
        {
            targetToggle = categoryToggles
                .FirstOrDefault(toggle => string.Equals(toggle.CategoryId, previousCategory, StringComparison.OrdinalIgnoreCase));
            if (targetToggle != null)
            {
                targetCategory = targetToggle.CategoryId;
            }
        }

        SelectFurnitureCategory(targetCategory, false);
        if (targetToggle == null)
        {
            targetToggle = categoryToggles
                .FirstOrDefault(toggle => string.Equals(toggle.CategoryId, targetCategory, StringComparison.OrdinalIgnoreCase))
                ?? categoryToggles.FirstOrDefault();
        }

        targetToggle?.SetIsOn(true, false);

        if (!string.IsNullOrEmpty(previousCategory))
        {
            RefreshFurnitureDisplay();
        }
    }

    void ClearFurnitureCategoryTabs()
    {
        foreach (var toggle in categoryToggles)
        {
            if (toggle != null)
            {
                Destroy(toggle.gameObject);
            }
        }

        categoryToggles.Clear();
    }

    IEnumerable<string> GetFurnitureCategories()
    {
        var categories = FurnitureDataManager.Instance?.GetFurnitureCategories();
        return categories ?? Array.Empty<string>();
    }

    void CreateFurnitureCategoryToggle(string categoryId, string displayName, Sprite icon)
    {
        if (string.IsNullOrEmpty(categoryId) || furnitureCategoryTogglePrefab == null || furnitureCategoryTabContainer == null)
        {
            return;
        }

        var toggleObj = Instantiate(furnitureCategoryTogglePrefab, furnitureCategoryTabContainer);
        var categoryToggle = toggleObj.GetComponent<FurnitureCategoryToggle>();
        if (categoryToggle == null)
        {
            categoryToggle = toggleObj.AddComponent<FurnitureCategoryToggle>();
        }

        bool showLabel = string.Equals(categoryId, allCategoryKey, StringComparison.OrdinalIgnoreCase);
        bool useBackgroundColor = true;
        Color backgroundColor = ResolveCategoryBackgroundColor(categoryId);
        bool useCheckmarkColor = true;
        Color checkmarkColor = ResolveCategoryCheckmarkColor(categoryId);

        categoryToggle.Initialize(categoryId, displayName, icon, furnitureCategoryToggleGroup, selectedCategory =>
        {
            SelectFurnitureCategory(selectedCategory);
        }, showLabel, useBackgroundColor, backgroundColor, useCheckmarkColor, checkmarkColor);

        string localizationKey = ResolveCategoryLocalizationKey(categoryId);
        if (!string.IsNullOrEmpty(localizationKey))
        {
            categoryToggle.SetLabelLocalization(StandardTextTableName, localizationKey);
        }

        categoryToggles.Add(categoryToggle);
    }

    string ResolveCategoryDisplayName(string categoryId)
    {
        var setting = FindCategoryDisplaySetting(categoryId);
        return setting != null && !string.IsNullOrEmpty(setting.displayName) ? setting.displayName : categoryId;
    }

    Sprite ResolveCategoryIcon(string categoryId)
    {
        var setting = FindCategoryDisplaySetting(categoryId);
        return setting != null && setting.icon != null ? setting.icon : defaultCategoryIcon;
    }

    string ResolveCategoryLocalizationKey(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
        {
            return string.Empty;
        }

        if (string.Equals(categoryId, allCategoryKey, StringComparison.OrdinalIgnoreCase))
        {
            return allCategoryKey;
        }

        if (CategoryLocalizationKeys.TryGetValue(categoryId, out string key) && !string.IsNullOrEmpty(key))
        {
            return key;
        }

        return categoryId;
    }

    Color ResolveCategoryBackgroundColor(string categoryId)
    {
        if (string.Equals(categoryId, allCategoryKey, StringComparison.OrdinalIgnoreCase))
        {
            return defaultCategoryColor;
        }

        var setting = FindCategoryDisplaySetting(categoryId);
        if (setting != null && setting.useBackgroundColor)
        {
            return setting.backgroundColor;
        }

        return defaultCategoryColor;
    }

    Color ResolveCategoryCheckmarkColor(string categoryId)
    {
        if (string.Equals(categoryId, allCategoryKey, StringComparison.OrdinalIgnoreCase))
        {
            return defaultCategoryCheckmarkColor;
        }

        var setting = FindCategoryDisplaySetting(categoryId);
        if (setting != null && setting.useCheckmarkColor)
        {
            return setting.checkmarkColor;
        }
        if (setting != null && setting.useBackgroundColor)
        {
            return setting.backgroundColor;
        }

        return defaultCategoryCheckmarkColor;
    }

    CategoryDisplaySetting FindCategoryDisplaySetting(string categoryId)
    {
        return categoryDisplaySettings
            .FirstOrDefault(setting => !string.IsNullOrEmpty(setting.categoryId) &&
                                       string.Equals(setting.categoryId, categoryId, StringComparison.OrdinalIgnoreCase));
    }

    void SelectFurnitureCategory(string categoryId, bool refresh = true)
    {
        selectedFurnitureCategory = categoryId;

        if (refresh)
        {
            RefreshFurnitureDisplay();
        }
    }

    void ResetFurnitureCategorySelection()
    {
        SelectFurnitureCategory(allCategoryKey, false);

        var toggle = categoryToggles
            .FirstOrDefault(ct => string.Equals(ct.CategoryId, allCategoryKey, StringComparison.OrdinalIgnoreCase));

        if (toggle != null)
        {
            toggle.SetIsOn(true, false);
        }
    }

    void SetupCraftButton()
    {
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() =>
            {
                if (debugMode) Debug.Log("=== CRAFT BUTTON CLICKED ===");
                PlayCraftButtonSfx();
                CraftSelectedItem();
            });

            craftButton.interactable = false;

            if (debugMode) Debug.Log("Craft button setup complete");
        }

        EnsureCraftButtonAudioSource();
    }

    void EnsureCraftButtonAudioSource()
    {
        if (craftButtonAudioSource == null && autoCreateAudioSource)
        {
            craftButtonAudioSource = GetComponent<AudioSource>();
            if (craftButtonAudioSource == null)
            {
                craftButtonAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (craftButtonAudioSource != null)
        {
            craftButtonAudioSource.playOnAwake = false;
            craftButtonAudioSource.loop = false;
            craftButtonAudioSource.spatialBlend = 0f;
            UpdateCraftButtonAudioSourceVolume();
        }
    }

    void HandleSfxVolumeChanged(float value)
    {
        currentSfxVolume = Mathf.Clamp01(value);
        UpdateCraftButtonAudioSourceVolume();
    }

    void UpdateCraftButtonAudioSourceVolume()
    {
        if (craftButtonAudioSource != null)
        {
            craftButtonAudioSource.volume = currentSfxVolume;
        }
    }

    void PlayCraftButtonSfx()
    {
        if (craftButtonSfx == null)
        {
            return;
        }

        EnsureCraftButtonAudioSource();

        if (craftButtonAudioSource == null)
        {
            return;
        }

        float volume = craftButtonSfxVolume * currentSfxVolume;
        craftButtonAudioSource.PlayOneShot(craftButtonSfx, volume);
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
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    void SetupFurnitureVirtualization()
    {
        if (furnitureScrollRect == null && furnitureScrollView != null)
        {
            furnitureScrollRect = furnitureScrollView.GetComponent<ScrollRect>();
        }

        if (furnitureVirtualizer == null && furnitureScrollView != null)
        {
            furnitureVirtualizer = furnitureScrollView.GetComponent<ScrollRectVirtualizer>();
        }

        if (furnitureScrollRect == null || furnitureVirtualizer == null)
        {
            return;
        }

        furnitureVirtualizer.OnCreateItem = HandleCreateFurnitureCard;
        furnitureVirtualizer.OnReleaseItem = HandleReleaseFurnitureCard;
        furnitureVirtualizer.OnBindItem = HandleBindFurnitureCard;
        furnitureVirtualizer.Initialize(furnitureScrollRect, furnitureItemHeight, furnitureItemSpacing, furniturePaddingTop, furniturePaddingBottom);
        if (furnitureScrollListener == null)
        {
            furnitureScrollListener = _ => SaveFurnitureScrollPosition();
        }
        furnitureScrollRect.onValueChanged.RemoveListener(furnitureScrollListener);
        furnitureScrollRect.onValueChanged.AddListener(furnitureScrollListener);
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
            RequestInventoryRefresh();

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

        if (isSearchEditing)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                currentSearchField?.DeactivateInputField();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !IsShopOpen() && !IsShopConversationActive())
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

    private bool IsShopOpen()
    {
        return ShopUIManager.Instance?.IsOpen ?? false;
    }

    private bool IsShopConversationActive()
    {
        if (shopConversationController == null)
        {
            shopConversationController = FindFirstObjectByType<ShopConversationController>();
        }

        return shopConversationController != null && shopConversationController.IsConversationActive;
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

        EnsurePanelScaleAnimator();

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

        panelScaleAnimator?.Open();
        RefreshInventoryDisplay();
        RestoreFurnitureScrollPosition();

        NotifyCameraController(true);
        SetSceneInteractionActive(false);
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        EnsurePanelScaleAnimator();

        if (isSearchEditing)
        {
            if (currentSearchField != null && currentSearchField.isFocused)
            {
                currentSearchField.DeactivateInputField();
            }

            ClearSearchEditingState();
        }

        SaveFurnitureScrollPosition();
        isOpen = false;

        panelScaleAnimator?.Close();
        NotifyCameraController(false);

        if (!isPlacingItem)
        {
            SetSceneInteractionActive(true);
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

        if (!isMaterialTab)
        {
            if (string.IsNullOrEmpty(selectedFurnitureCategory))
            {
                ResetFurnitureCategorySelection();
            }
        }

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

    void HandleInventoryChanged()
    {
        RequestInventoryRefresh();
    }

    void RequestInventoryRefresh()
    {
        if (inventoryRefreshQueued)
        {
            return;
        }

        inventoryRefreshQueued = true;

        if (inventoryRefreshCoroutine != null)
        {
            StopCoroutine(inventoryRefreshCoroutine);
        }

        inventoryRefreshCoroutine = StartCoroutine(DelayedInventoryRefresh());
    }

    IEnumerator DelayedInventoryRefresh()
    {
        yield return null;
        inventoryRefreshQueued = false;
        inventoryRefreshCoroutine = null;
        RefreshInventoryDisplay();
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

        // カテゴリフィルターを適用
        if (!string.IsNullOrEmpty(selectedFurnitureCategory) &&
            !string.Equals(selectedFurnitureCategory, allCategoryKey, StringComparison.OrdinalIgnoreCase))
        {
            items = items.Where(ItemMatchesSelectedCategory).ToList();
        }

        // 検索フィルター適用
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(item =>
            {
                return item.itemID.ToLower().Contains(searchQuery.ToLower());
            }).ToList();
        }

        if (debugMode) Debug.Log($"RefreshFurnitureDisplay - Total items: {items.Count}, Craftable filter: {showOnlyCraftable}, Favorite filter: {showOnlyFavorites}, Wall filter: {showOnlyWallPlacement}, Ceiling filter: {showOnlyCeilingPlacement}");

        filteredFurnitureItems.Clear();
        filteredFurnitureItems.AddRange(items);

        bool searchChanged = !string.Equals(searchQuery, lastFurnitureSearchQuery, StringComparison.Ordinal);
        bool categoryChanged = !string.Equals(selectedFurnitureCategory, lastFurnitureCategory, StringComparison.OrdinalIgnoreCase);
        bool resetScrollPosition = searchChanged || categoryChanged || !hasSavedFurnitureScrollPosition;

        if (furnitureVirtualizer != null && furnitureScrollRect != null)
        {
            cardManager?.ClearSelectionIfMissing(filteredFurnitureItems);
            furnitureVirtualizer.SetItemCount(filteredFurnitureItems.Count, resetScrollPosition);
            if (resetScrollPosition)
            {
                lastFurnitureScrollNormalized = 1f;
                hasSavedFurnitureScrollPosition = true;
                furnitureScrollRect.verticalNormalizedPosition = lastFurnitureScrollNormalized;
            }
            else
            {
                RestoreFurnitureScrollPosition();
            }
            furnitureVirtualizer.RefreshVisibleItems();
        }
        else
        {
            cardManager?.RefreshFurnitureCards(items);
        }

        lastFurnitureSearchQuery = searchQuery;
        lastFurnitureCategory = selectedFurnitureCategory;
    }

    RectTransform HandleCreateFurnitureCard()
    {
        return cardManager != null ? cardManager.CreateFurnitureCardForVirtualizer() : null;
    }

    void HandleReleaseFurnitureCard(RectTransform item)
    {
        cardManager?.ReleaseFurnitureCardFromVirtualizer(item);
    }

    void HandleBindFurnitureCard(int index, RectTransform item)
    {
        if (index < 0 || index >= filteredFurnitureItems.Count)
        {
            return;
        }

        cardManager?.BindFurnitureCardForVirtualizer(filteredFurnitureItems[index], item);
    }

    void SaveFurnitureScrollPosition()
    {
        if (furnitureScrollRect == null)
        {
            return;
        }

        lastFurnitureScrollNormalized = furnitureScrollRect.verticalNormalizedPosition;
        hasSavedFurnitureScrollPosition = true;
    }

    void RestoreFurnitureScrollPosition()
    {
        if (furnitureScrollRect == null || !hasSavedFurnitureScrollPosition)
        {
            return;
        }

        furnitureScrollRect.verticalNormalizedPosition = lastFurnitureScrollNormalized;
        furnitureVirtualizer?.RefreshVisibleItems();
    }

    bool ItemMatchesSelectedCategory(InventoryItem item)
    {
        var data = FurnitureDataManager.Instance?.GetFurnitureDataSO(item.itemID);
        if (data == null)
        {
            return false;
        }

        return FurnitureDataManager.SplitCategories(data.category)
            .Any(category => string.Equals(category, selectedFurnitureCategory, StringComparison.OrdinalIgnoreCase));
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
        var list = InventoryManager.Instance?.GetFurnitureList(
                       currentSortType,
                       showOnlyCraftable,
                       showOnlyFavorites,
                       sortAscending,
                       showOnlyWallPlacement,
                       showOnlyCeilingPlacement)
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
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
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

        ClearFurnitureCategoryTabs();
    }
}
