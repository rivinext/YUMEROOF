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
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f,1f)] [SerializeField] private float value;

    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private SaturationValuePalette saturationValuePalette;

    private PresetCategory currentCategory;
    private int currentSlotIndex;

    private void Start()
    {
        InitializePresetSelection();

        if (hueRingSelector != null)
        {
            hueRingSelector.SetHue(hue);
            hueRingSelector.OnHueChanged.AddListener(UpdateHue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.SetHue(hue);
            saturationValuePalette.SetValues(saturation, value);
            saturationValuePalette.OnSaturationChanged.AddListener(UpdateSaturation);
            saturationValuePalette.OnValueChanged.AddListener(UpdateValue);
        }

        ApplyColor();
    }

    public void UpdateHue(float newHue)
    {
        hue = newHue;
        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        ApplyColor();
    }

    public void UpdateSaturation(float newSat)
    {
        saturation = newSat;
        saturationValuePalette?.SetSaturation(saturation);
        ApplyColor();
    }

    public void UpdateValue(float newVal)
    {
        value = newVal;
        saturationValuePalette?.SetValue(value);
        ApplyColor();
    }

    public void SelectPresetByIndex(int slotIndex)
    {
        UpdatePresetSelectionFromIndex(slotIndex);
        ApplySelectedPreset();
    }

    public void SaveCurrentUserPreset()
    {
        if (currentCategory != PresetCategory.User)
        {
            Debug.LogWarning("Default presets cannot be saved. Switch to a user preset slot to save.");
            return;
        }

        SaveUserPreset(currentSlotIndex, hue, saturation, value);
        PlayerPrefs.Save();
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

    private void UpdatePresetSelectionFromIndex(int slotIndex)
    {
        int safeDefault = Mathf.Max(defaultPresetCount, 0);
        int safeUser = Mathf.Max(userPresetCount, 0);
        int totalSlots = Mathf.Max(safeDefault + safeUser, 1);

        int clampedIndex = Mathf.Clamp(slotIndex, 0, totalSlots - 1);
        bool isUserSlot = safeUser > 0 && clampedIndex >= safeDefault;

        currentCategory = isUserSlot ? PresetCategory.User : PresetCategory.Default;
        currentSlotIndex = isUserSlot ? clampedIndex - safeDefault : clampedIndex;
    }

    private void ApplyPresetColor(float presetHue, float presetSaturation, float presetValue)
    {
        hue = presetHue;
        saturation = presetSaturation;
        value = presetValue;

        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        saturationValuePalette?.SetValues(saturation, value);
        ApplyColor();
    }

    private int GetTotalSlotCount()
    {
        return Mathf.Max(defaultPresetCount, 0) + Mathf.Max(userPresetCount, 0);
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
        Color currentColor = Color.HSVToRGB(hue, saturation, value);

        if (targetMaterial != null)
        {
            targetMaterial.color = currentColor;
        }

        if (previewImage != null)
        {
            previewImage.color = currentColor;
        }

        if (previewRawImage != null)
        {
            previewRawImage.color = currentColor;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        UpdatePresetSelectionFromIndex(initialPresetIndex);
        ApplySelectedPreset();
    }
}
