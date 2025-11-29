using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialPresetUIController : MonoBehaviour
{
    [Header("Material Selection UI")]
    [SerializeField] private Transform materialSelectionContainer;
    [SerializeField] private GameObject materialSelectionButtonPrefab;

    [Header("Preset Category UI")]
    [SerializeField] private Button defaultCategoryButton;
    [SerializeField] private Button userCategoryButton;

    [Header("Preset Slot UI")]
    [SerializeField] private Transform slotButtonContainer;
    [SerializeField] private GameObject slotButtonPrefab;

    [Header("Preset Action Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private GameObject saveButtonRoot;
    [SerializeField] private GameObject loadButtonRoot;

    private readonly List<Button> slotButtons = new();
    private readonly List<int> slotButtonGlobalIndices = new();
    private readonly List<Button> materialButtons = new();

    private PresetCategory currentCategory;
    private int currentSlotIndex;
    private int currentMaterialIndex;
    private PresetSlotInfo slotInfo;

    private MaterialPresetService presetService;
    private HueSyncCoordinator hueCoordinator;
    private List<HueSyncCoordinator> hueCoordinators = new();
    public void Initialize(MaterialPresetService service, HueSyncCoordinator coordinator, int initialPresetIndex)
    {
        Initialize(service, coordinator != null ? new List<HueSyncCoordinator> { coordinator } : new List<HueSyncCoordinator>(), initialPresetIndex);
    }

    public void Initialize(MaterialPresetService service, List<HueSyncCoordinator> coordinators, int initialPresetIndex)
    {
        presetService = service;
        hueCoordinators = coordinators?.Where(c => c != null).Distinct().ToList() ?? new List<HueSyncCoordinator>();
        currentMaterialIndex = Mathf.Clamp(currentMaterialIndex, 0, Mathf.Max(hueCoordinators.Count - 1, 0));
        hueCoordinator = GetCurrentMaterialCoordinator();
        slotInfo = presetService != null ? presetService.GetSlotInfo() : new PresetSlotInfo();
        currentCategory = slotInfo.HasDefaultSlots ? PresetCategory.Default : PresetCategory.User;
        currentSlotIndex = 0;

        RefreshMaterialSelection();
        RegisterCategoryButtons();
        RegisterActionButtons();
        RefreshSlotButtonsForCategory(currentCategory);

        int totalSlots = slotInfo.TotalSlotCount;
        if (totalSlots > 0)
        {
            SelectPresetByGlobalIndex(initialPresetIndex);
        }
        else
        {
            SyncCurrentMaterialSelectors();
            UpdateCategoryButtons();
            UpdateActionButtons();
        }
    }

    public void SelectPresetByGlobalIndex(int globalIndex)
    {
        if (slotInfo.TotalSlotCount <= 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(globalIndex, 0, slotInfo.TotalSlotCount - 1);
        (PresetCategory category, int slotIndex) = GetCategoryAndSlotFromGlobalIndex(clampedIndex);
        currentCategory = category;
        currentSlotIndex = slotIndex;

        ApplyPresetFromSelection();
        FinalizePresetSelection(clampedIndex);
    }

    public void LoadCurrentPreset()
    {
        ApplyPresetFromSelection();
    }

    public void SaveCurrentPreset()
    {
        if (presetService == null)
        {
            return;
        }

        MaterialColorSet currentSet = BuildCurrentColorSet();
        if (currentSet == null || currentSet.Colors.Count == 0)
        {
            return;
        }

        bool saved = presetService.SavePreset(currentCategory, currentSlotIndex, currentSet);
        if (!saved)
        {
            return;
        }

        UpdateActionButtons();
    }

    private void ApplyPresetFromSelection()
    {
        if (presetService == null || slotInfo.TotalSlotCount <= 0)
        {
            return;
        }

        MaterialColorSet fallbackSet = BuildCurrentColorSet();
        bool loaded = presetService.LoadPreset(currentCategory, currentSlotIndex, fallbackSet, out MaterialColorSet presetSet);
        if (!loaded)
        {
            return;
        }

        ApplyPresetToCoordinators(presetSet, fallbackSet);
        SyncCurrentMaterialSelectors();
    }

    private void FinalizePresetSelection(int selectedGlobalIndex)
    {
        EnsureSlotButtonsForCategory(currentCategory);
        UpdateActionButtons();
        UpdateCategoryButtons();
        UpdateSlotButtonSelection(selectedGlobalIndex);
    }

    private void RegisterCategoryButtons()
    {
        if (defaultCategoryButton != null)
        {
            defaultCategoryButton.onClick.RemoveListener(SelectDefaultCategory);
            defaultCategoryButton.onClick.AddListener(SelectDefaultCategory);
        }

        if (userCategoryButton != null)
        {
            userCategoryButton.onClick.RemoveListener(SelectUserCategory);
            userCategoryButton.onClick.AddListener(SelectUserCategory);
        }
    }

    private MaterialColorSet BuildCurrentColorSet()
    {
        List<MaterialColor> colors = new List<MaterialColor>();

        foreach (HueSyncCoordinator coordinator in EnumerateCoordinators())
        {
            MaterialColor materialColor = coordinator.CreateMaterialColor();
            if (materialColor != null)
            {
                colors.Add(materialColor);
            }
        }

        return new MaterialColorSet(colors);
    }

    private void ApplyPresetToCoordinators(MaterialColorSet presetSet, MaterialColorSet fallbackSet)
    {
        foreach (HueSyncCoordinator coordinator in EnumerateCoordinators())
        {
            string materialId = coordinator.GetMaterialIdentifier();

            if (presetSet != null && presetSet.TryGetColor(materialId, out MaterialColor color))
            {
                coordinator.ApplyMaterialColor(color);
                continue;
            }

            if (fallbackSet != null && fallbackSet.TryGetColor(materialId, out MaterialColor fallbackColor))
            {
                coordinator.ApplyMaterialColor(fallbackColor);
            }
        }
    }

    private void RegisterActionButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveCurrentPreset);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadCurrentPreset);
        }
    }

    private void SelectDefaultCategory()
    {
        SetPresetCategory(PresetCategory.Default);
    }

    private void SelectUserCategory()
    {
        SetPresetCategory(PresetCategory.User);
    }

    private void SetPresetCategory(PresetCategory category)
    {
        if (!HasSlotsForCategory(category))
        {
            Debug.LogWarning($"{category} presets are not available.");
            return;
        }

        int slotCount = GetSlotCountForCategory(category);
        int slotIndex = Mathf.Clamp(currentCategory == category ? currentSlotIndex : 0, 0, slotCount - 1);
        currentCategory = category;
        currentSlotIndex = slotIndex;

        int globalIndex = GetGlobalSlotIndex(category, slotIndex);
        RefreshSlotButtonsForCategory(category);
        ApplyPresetFromSelection();
        FinalizePresetSelection(globalIndex);
    }

    private void RefreshSlotButtonsForCategory(PresetCategory category)
    {
        if (slotButtonContainer == null || slotButtonPrefab == null)
        {
            return;
        }

        slotButtonGlobalIndices.Clear();
        slotButtons.Clear();

        for (int i = slotButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(slotButtonContainer.GetChild(i).gameObject);
        }

        int slotCount = GetSlotCountForCategory(category);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject buttonInstance = Instantiate(slotButtonPrefab, slotButtonContainer);
            Button button = buttonInstance.GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            int globalIndex = GetGlobalSlotIndex(category, i);
            button.onClick.AddListener(() => SelectPresetByGlobalIndex(globalIndex));
            UpdateSlotButtonLabel(buttonInstance, globalIndex);
            slotButtons.Add(button);
            slotButtonGlobalIndices.Add(globalIndex);
        }
    }

    private void EnsureSlotButtonsForCategory(PresetCategory category)
    {
        int expectedCount = GetSlotCountForCategory(category);
        if (slotButtons.Count != expectedCount)
        {
            RefreshSlotButtonsForCategory(category);
        }
    }

    private IEnumerable<HueSyncCoordinator> EnumerateCoordinators()
    {
        HashSet<HueSyncCoordinator> seen = new HashSet<HueSyncCoordinator>();

        if (hueCoordinators != null)
        {
            foreach (HueSyncCoordinator coordinator in hueCoordinators)
            {
                if (coordinator != null && seen.Add(coordinator))
                {
                    yield return coordinator;
                }
            }
        }

        if (hueCoordinator != null && seen.Add(hueCoordinator))
        {
            yield return hueCoordinator;
        }
    }

    private int GetSlotCountForCategory(PresetCategory category)
    {
        return category == PresetCategory.User ? slotInfo.UserSlotCount : slotInfo.DefaultSlotCount;
    }

    private bool HasSlotsForCategory(PresetCategory category)
    {
        return category == PresetCategory.User ? slotInfo.HasUserSlots : slotInfo.HasDefaultSlots;
    }

    private int GetGlobalSlotIndex(PresetCategory category, int slotIndex)
    {
        return category == PresetCategory.User ? slotInfo.DefaultSlotCount + slotIndex : slotIndex;
    }

    private (PresetCategory category, int slotIndex) GetCategoryAndSlotFromGlobalIndex(int globalIndex)
    {
        bool isUserSlot = slotInfo.HasUserSlots && globalIndex >= slotInfo.DefaultSlotCount;
        return isUserSlot
            ? (PresetCategory.User, globalIndex - slotInfo.DefaultSlotCount)
            : (PresetCategory.Default, globalIndex);
    }

    private void UpdateSlotButtonLabel(GameObject buttonObject, int globalIndex)
    {
        if (buttonObject == null)
        {
            return;
        }

        string label = BuildSlotLabel(globalIndex);

        TMP_Text tmpLabel = buttonObject.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = label;
            return;
        }

        Text uiText = buttonObject.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    private string BuildSlotLabel(int globalIndex)
    {
        bool isUserSlot = slotInfo.HasUserSlots && globalIndex >= slotInfo.DefaultSlotCount;

        if (isUserSlot)
        {
            int userIndex = globalIndex - slotInfo.DefaultSlotCount + 1;
            return $"User {userIndex}";
        }

        int defaultIndex = globalIndex + 1;
        return $"Default {defaultIndex}";
    }

    private void UpdateSlotButtonSelection(int selectedGlobalIndex)
    {
        if (slotButtons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < slotButtons.Count; i++)
        {
            Button button = slotButtons[i];
            if (button == null)
            {
                continue;
            }

            bool isSelected = slotButtonGlobalIndices.Count > i && slotButtonGlobalIndices[i] == selectedGlobalIndex;
            button.interactable = !isSelected;
        }
    }

    private void UpdateActionButtons()
    {
        bool hasDefaultSlots = slotInfo.HasDefaultSlots;
        bool hasUserSlots = slotInfo.HasUserSlots;
        bool isUser = currentCategory == PresetCategory.User;

        bool showSave = isUser && hasUserSlots;
        bool showLoad = (isUser && hasUserSlots) || (!isUser && hasDefaultSlots);

        SetButtonVisibility(saveButton, saveButtonRoot, showSave);
        SetButtonVisibility(loadButton, loadButtonRoot, showLoad);

        if (saveButton != null)
        {
            saveButton.interactable = showSave;
        }

        if (loadButton != null)
        {
            loadButton.interactable = showLoad;
        }
    }

    private void UpdateCategoryButtons()
    {
        UpdateCategoryButton(defaultCategoryButton, PresetCategory.Default);
        UpdateCategoryButton(userCategoryButton, PresetCategory.User);
    }

    private void UpdateCategoryButton(Button button, PresetCategory category)
    {
        if (button == null)
        {
            return;
        }

        bool hasSlots = HasSlotsForCategory(category);
        button.gameObject.SetActive(hasSlots);
        button.interactable = hasSlots && currentCategory != category;
    }

    private void SetButtonVisibility(Button button, GameObject buttonRoot, bool isVisible)
    {
        if (buttonRoot != null)
        {
            buttonRoot.SetActive(isVisible);
            return;
        }

        if (button != null)
        {
            button.gameObject.SetActive(isVisible);
        }
    }

    private HueSyncCoordinator GetCurrentMaterialCoordinator()
    {
        if (hueCoordinators == null || hueCoordinators.Count == 0)
        {
            return hueCoordinator;
        }

        if (currentMaterialIndex < 0 || currentMaterialIndex >= hueCoordinators.Count)
        {
            currentMaterialIndex = 0;
        }

        return hueCoordinators[currentMaterialIndex];
    }

    private void RefreshMaterialSelection()
    {
        materialButtons.Clear();

        if (materialSelectionContainer == null || materialSelectionButtonPrefab == null)
        {
            SelectMaterialByIndex(currentMaterialIndex);
            return;
        }

        for (int i = materialSelectionContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(materialSelectionContainer.GetChild(i).gameObject);
        }

        List<HueSyncCoordinator> coordinators = hueCoordinators?.Where(c => c != null).ToList() ?? new List<HueSyncCoordinator>();
        for (int i = 0; i < coordinators.Count; i++)
        {
            HueSyncCoordinator coordinator = coordinators[i];
            GameObject buttonInstance = Instantiate(materialSelectionButtonPrefab, materialSelectionContainer);
            Button button = buttonInstance.GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            int index = i;
            button.onClick.AddListener(() => SelectMaterialByIndex(index));
            UpdateMaterialButtonLabel(buttonInstance, coordinator, index);
            materialButtons.Add(button);
        }

        SelectMaterialByIndex(currentMaterialIndex);
    }

    public void SelectMaterialByIndex(int materialIndex)
    {
        if (hueCoordinators == null || hueCoordinators.Count == 0)
        {
            hueCoordinator = null;
            UpdateMaterialButtonSelection();
            return;
        }

        int clampedIndex = Mathf.Clamp(materialIndex, 0, hueCoordinators.Count - 1);
        currentMaterialIndex = clampedIndex;
        hueCoordinator = GetCurrentMaterialCoordinator();

        SyncCurrentMaterialSelectors();
        UpdateMaterialButtonSelection();
        UpdateActionButtons();
    }

    private void SyncCurrentMaterialSelectors()
    {
        HueSyncCoordinator coordinator = GetCurrentMaterialCoordinator();
        if (coordinator == null)
        {
            return;
        }

        coordinator.SyncSelectors();
        coordinator.ApplyColor();
    }

    private void UpdateMaterialButtonSelection()
    {
        if (materialButtons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < materialButtons.Count; i++)
        {
            Button button = materialButtons[i];
            if (button == null)
            {
                continue;
            }

            bool isSelected = i == currentMaterialIndex;
            button.interactable = !isSelected;
        }
    }

    private void UpdateMaterialButtonLabel(GameObject buttonObject, HueSyncCoordinator coordinator, int index)
    {
        if (buttonObject == null)
        {
            return;
        }

        string label = BuildMaterialLabel(coordinator, index);

        TMP_Text tmpLabel = buttonObject.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = label;
            return;
        }

        Text uiText = buttonObject.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    private string BuildMaterialLabel(HueSyncCoordinator coordinator, int index)
    {
        if (coordinator == null)
        {
            return $"Material {index + 1}";
        }

        string identifier = coordinator.GetMaterialIdentifier();
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return $"Material {index + 1}";
        }

        return identifier;
    }
}
