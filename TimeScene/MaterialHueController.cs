using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MaterialHueController : MonoBehaviour
{
    private enum PresetCategory
    {
        Default,
        User
    }

    private const string UserPresetKeyPrefix = "material_user_preset";

    [SerializeField] private int defaultPresetCount = 3;
    [SerializeField] private int userPresetCount = 5;
    [SerializeField] private Color[] defaultPresetColors;
    [SerializeField] private int initialPresetIndex;

    [SerializeField] private Material targetMaterial;
    [SerializeField] private List<Material> targetMaterials = new();
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private List<Graphic> previewGraphics = new();

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

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f,1f)] [SerializeField] private float value;

    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private List<HueRingSelector> hueRingSelectors = new();
    [SerializeField] private SaturationValuePalette saturationValuePalette;
    [SerializeField] private List<SaturationValuePalette> saturationValuePalettes = new();

    private PresetCategory currentCategory;
    private int currentSlotIndex;

    private readonly List<Button> slotButtons = new();
    private readonly List<int> slotButtonGlobalIndices = new();
    private PresetCategory lastSlotButtonCategory = (PresetCategory)(-1);

    private void Start()
    {
        BuildSlotButtons();
        RegisterCategoryButtons();
        RegisterActionButtons();
        InitializePresetSelection();

        foreach (HueRingSelector ringSelector in EnumerateHueRingSelectors())
        {
            ringSelector.SetHue(hue);
            ringSelector.OnHueChanged.AddListener(UpdateHue);
        }

        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetHue(hue);
            palette.SetValues(saturation, value);
            palette.OnSaturationChanged.AddListener(UpdateSaturation);
            palette.OnValueChanged.AddListener(UpdateValue);
        }

        ApplyColor();
    }

    private void RegisterActionButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveCurrentUserPreset);
        }
        else
        {
            Debug.LogWarning("Save button is not assigned in the inspector.");
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadCurrentPreset);
        }
        else
        {
            Debug.LogWarning("Load button is not assigned in the inspector.");
        }
    }

    public void UpdateHue(float newHue)
    {
        hue = newHue;
        foreach (HueRingSelector ringSelector in EnumerateHueRingSelectors())
        {
            ringSelector.SetHue(hue);
        }

        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetHue(hue);
        }

        ApplyColor();
    }

    public void UpdateSaturation(float newSat)
    {
        saturation = newSat;
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetSaturation(saturation);
        }

        ApplyColor();
    }

    public void UpdateValue(float newVal)
    {
        value = newVal;
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetValue(value);
        }

        ApplyColor();
    }

    public void SelectPresetByIndex(int slotIndex)
    {
        int clampedIndex = UpdatePresetSelectionFromIndex(slotIndex);
        FinalizePresetSelection(clampedIndex, true);
    }

    public void LoadCurrentPreset()
    {
        HandleLoad(currentCategory, currentSlotIndex);
    }

    public void SaveCurrentUserPreset()
    {
        HandleSave(currentCategory, currentSlotIndex);
    }

    private void InitializePresetSelection()
    {
        int totalSlots = GetTotalSlotCount();

        if (totalSlots <= 0)
        {
            ApplyColor();
            return;
        }

        SelectPresetByIndex(initialPresetIndex);
        UpdateActionButtons();
        UpdateCategoryButtons();
    }

    private void ApplySelectedPreset()
    {
        if (currentCategory == PresetCategory.User &&
            TryLoadUserPreset(currentSlotIndex, out float presetHue, out float presetSaturation, out float presetValue))
        {
            ApplyPresetColor(presetHue, presetSaturation, presetValue);
            return;
        }

        Color defaultColor = GetDefaultPresetColor(currentSlotIndex);
        Color.RGBToHSV(defaultColor, out float defaultHue, out float defaultSaturation, out float defaultValue);
        ApplyPresetColor(defaultHue, defaultSaturation, defaultValue);
    }

    private int UpdatePresetSelectionFromIndex(int slotIndex)
    {
        int safeDefault = Mathf.Max(defaultPresetCount, 0);
        int safeUser = Mathf.Max(userPresetCount, 0);
        int totalSlots = Mathf.Max(safeDefault + safeUser, 1);

        int clampedIndex = Mathf.Clamp(slotIndex, 0, totalSlots - 1);
        bool isUserSlot = safeUser > 0 && clampedIndex >= safeDefault;

        currentCategory = isUserSlot ? PresetCategory.User : PresetCategory.Default;
        currentSlotIndex = isUserSlot ? clampedIndex - safeDefault : clampedIndex;

        return clampedIndex;
    }

    private void ApplyPresetColor(float presetHue, float presetSaturation, float presetValue)
    {
        hue = presetHue;
        saturation = presetSaturation;
        value = presetValue;

        foreach (HueRingSelector ringSelector in EnumerateHueRingSelectors())
        {
            ringSelector.SetHue(hue);
        }

        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetHue(hue);
            palette.SetValues(saturation, value);
        }

        ApplyColor();
    }

    private IEnumerable<HueRingSelector> EnumerateHueRingSelectors()
    {
        if (hueRingSelectors != null)
        {
            foreach (HueRingSelector selector in hueRingSelectors)
            {
                if (selector != null)
                {
                    yield return selector;
                }
            }
        }

        if (hueRingSelector != null && (hueRingSelectors == null || !hueRingSelectors.Contains(hueRingSelector)))
        {
            yield return hueRingSelector;
        }
    }

    private IEnumerable<SaturationValuePalette> EnumerateSaturationValuePalettes()
    {
        if (saturationValuePalettes != null)
        {
            foreach (SaturationValuePalette palette in saturationValuePalettes)
            {
                if (palette != null)
                {
                    yield return palette;
                }
            }
        }

        if (saturationValuePalette != null && (saturationValuePalettes == null || !saturationValuePalettes.Contains(saturationValuePalette)))
        {
            yield return saturationValuePalette;
        }
    }

    private int GetTotalSlotCount()
    {
        return Mathf.Max(defaultPresetCount, 0) + Mathf.Max(userPresetCount, 0);
    }

    private int GetSlotCountForCategory(PresetCategory category)
    {
        return category == PresetCategory.User
            ? Mathf.Max(userPresetCount, 0)
            : Mathf.Max(defaultPresetCount, 0);
    }

    private int GetGlobalSlotIndex(PresetCategory category, int slotIndex)
    {
        int defaultOffset = Mathf.Max(defaultPresetCount, 0);
        return category == PresetCategory.User ? defaultOffset + slotIndex : slotIndex;
    }

    private Color GetDefaultPresetColor(int slotIndex)
    {
        if (defaultPresetColors == null || defaultPresetColors.Length == 0)
        {
            return Color.HSVToRGB(hue, saturation, value);
        }

        int clampedIndex = Mathf.Clamp(slotIndex, 0, defaultPresetColors.Length - 1);
        return defaultPresetColors[clampedIndex];
    }

    private bool TryLoadUserPreset(int slotIndex, out float presetHue, out float presetSaturation, out float presetValue)
    {
        string hueKey = GetUserHueKey(slotIndex);

        if (!PlayerPrefs.HasKey(hueKey))
        {
            presetHue = 0f;
            presetSaturation = 0f;
            presetValue = 0f;
            return false;
        }

        presetHue = PlayerPrefs.GetFloat(hueKey);
        presetSaturation = PlayerPrefs.GetFloat(GetUserSaturationKey(slotIndex));
        presetValue = PlayerPrefs.GetFloat(GetUserValueKey(slotIndex));
        return true;
    }

    private void SaveUserPreset(int slotIndex, float presetHue, float presetSaturation, float presetValue)
    {
        PlayerPrefs.SetFloat(GetUserHueKey(slotIndex), presetHue);
        PlayerPrefs.SetFloat(GetUserSaturationKey(slotIndex), presetSaturation);
        PlayerPrefs.SetFloat(GetUserValueKey(slotIndex), presetValue);
    }

    private string GetUserHueKey(int slotIndex)
    {
        return $"{UserPresetKeyPrefix}_{slotIndex}_hue";
    }

    private string GetUserSaturationKey(int slotIndex)
    {
        return $"{UserPresetKeyPrefix}_{slotIndex}_saturation";
    }

    private string GetUserValueKey(int slotIndex)
    {
        return $"{UserPresetKeyPrefix}_{slotIndex}_value";
    }

    private void ApplyColor()
    {
        ApplyColorToTargets(Color.HSVToRGB(hue, saturation, value));
    }

    private void ApplyColorToTargets(Color color)
    {
        if (targetMaterial != null)
        {
            targetMaterial.color = color;
        }

        foreach (Material material in targetMaterials)
        {
            if (material != null)
            {
                material.color = color;
            }
        }

        if (previewImage != null)
        {
            previewImage.color = color;
        }

        if (previewRawImage != null)
        {
            previewRawImage.color = color;
        }

        foreach (Graphic graphic in previewGraphics)
        {
            if (graphic != null)
            {
                graphic.color = color;
            }
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        int clampedIndex = UpdatePresetSelectionFromIndex(initialPresetIndex);
        FinalizePresetSelection(clampedIndex, false);
        ApplySelectedPreset();
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
        int slotCount = GetSlotCountForCategory(category);
        if (slotCount <= 0)
        {
            Debug.LogWarning($"{category} presets are not available.");
            return;
        }

        int slotIndex = Mathf.Clamp(currentCategory == category ? currentSlotIndex : 0, 0, slotCount - 1);
        currentCategory = category;
        currentSlotIndex = slotIndex;

        int globalIndex = GetGlobalSlotIndex(currentCategory, currentSlotIndex);
        RefreshSlotButtonsForCategory(currentCategory);
        FinalizePresetSelection(globalIndex, true);
    }

    private void HandleLoad(PresetCategory category, int slotIndex)
    {
        if (GetSlotCountForCategory(category) <= 0)
        {
            Debug.LogWarning("No preset slots available for the selected category.");
            return;
        }

        currentCategory = category;
        currentSlotIndex = Mathf.Clamp(slotIndex, 0, GetSlotCountForCategory(category) - 1);
        int globalIndex = GetGlobalSlotIndex(currentCategory, currentSlotIndex);
        FinalizePresetSelection(globalIndex, true);
    }

    private void HandleSave(PresetCategory category, int slotIndex)
    {
        if (category != PresetCategory.User)
        {
            Debug.LogWarning("Default presets cannot be saved. Switch to a user preset slot to save.");
            return;
        }

        if (GetSlotCountForCategory(PresetCategory.User) <= 0)
        {
            Debug.LogWarning("No user preset slots available to save.");
            return;
        }

        SaveUserPreset(slotIndex, hue, saturation, value);
        PlayerPrefs.Save();
    }

    private void BuildSlotButtons()
    {
        RefreshSlotButtonsForCategory(currentCategory);
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
            button.onClick.AddListener(() => SelectPresetByIndex(globalIndex));
            UpdateSlotButtonLabel(buttonInstance, globalIndex);
            slotButtons.Add(button);
            slotButtonGlobalIndices.Add(globalIndex);
        }

        lastSlotButtonCategory = category;
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
        int safeDefault = Mathf.Max(defaultPresetCount, 0);
        bool isUserSlot = safeDefault > 0 && globalIndex >= safeDefault;

        if (isUserSlot)
        {
            int userIndex = globalIndex - safeDefault + 1;
            return $"User {userIndex}";
        }

        int defaultIndex = globalIndex + 1;
        return $"Default {defaultIndex}";
    }

    private void FinalizePresetSelection(int selectedGlobalIndex, bool applyPreset)
    {
        if (applyPreset)
        {
            ApplySelectedPreset();
        }

        EnsureSlotButtonsForCategory(currentCategory);
        UpdateActionButtons();
        UpdateCategoryButtons();
        UpdateSlotButtonSelection(selectedGlobalIndex);
    }

    private void EnsureSlotButtonsForCategory(PresetCategory category)
    {
        if (lastSlotButtonCategory != category)
        {
            RefreshSlotButtonsForCategory(category);
        }
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
        bool hasDefaultSlots = GetSlotCountForCategory(PresetCategory.Default) > 0;
        bool hasUserSlots = GetSlotCountForCategory(PresetCategory.User) > 0;
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

        bool hasSlots = GetSlotCountForCategory(category) > 0;
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
}
