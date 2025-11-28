using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ColorPreset
{
    public string name;
    [Range(0f, 1f)] public float hue;
    [Range(0f, 1f)] public float saturation;
    [Range(0f, 1f)] public float value;
}
public class MaterialHueController : MonoBehaviour
{
    private const string HueKey = "material_hue";
    private const string SaturationKey = "material_saturation";
    private const string ValueKey = "material_value";
    private const string UserPresetsKey = "material_user_presets";
    private const int MaxUserPresets = 20;

    [SerializeField] private Material targetMaterial;
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f,1f)] [SerializeField] private float value;

    [SerializeField] private List<ColorPreset> defaultPresets = new List<ColorPreset>();
    [SerializeField] private Dropdown presetDropdown;
    [SerializeField] private InputField presetNameInput;
    [SerializeField] private Button savePresetButton;

    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private SaturationValuePalette saturationValuePalette;

    private readonly List<ColorPreset> userPresets = new List<ColorPreset>();
    private readonly List<ColorPreset> combinedPresets = new List<ColorPreset>();
    private bool isInitializingDropdown;

    private void Start()
    {
        LoadSavedValues();
        LoadUserPresetsFromPrefs();

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

        InitializePresetDropdown();
        HookupSavePresetButton();

        ApplyColor();
    }

    public void UpdateHue(float newHue)
    {
        bool hasChanged = !Mathf.Approximately(hue, newHue);
        hue = newHue;
        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        ApplyColor();

        if (hasChanged)
        {
            PlayerPrefs.SetFloat(HueKey, hue);
            PlayerPrefs.Save();
        }
    }

    public void UpdateSaturation(float newSat)
    {
        bool hasChanged = !Mathf.Approximately(saturation, newSat);
        saturation = newSat;
        saturationValuePalette?.SetSaturation(saturation);
        ApplyColor();

        if (hasChanged)
        {
            PlayerPrefs.SetFloat(SaturationKey, saturation);
            PlayerPrefs.Save();
        }
    }

    public void UpdateValue(float newVal)
    {
        bool hasChanged = !Mathf.Approximately(value, newVal);
        value = newVal;
        saturationValuePalette?.SetValue(value);
        ApplyColor();

        if (hasChanged)
        {
            PlayerPrefs.SetFloat(ValueKey, value);
            PlayerPrefs.Save();
        }
    }

    private void LoadSavedValues()
    {
        if (PlayerPrefs.HasKey(HueKey))
        {
            hue = PlayerPrefs.GetFloat(HueKey);
        }

        if (PlayerPrefs.HasKey(SaturationKey))
        {
            saturation = PlayerPrefs.GetFloat(SaturationKey);
        }

        if (PlayerPrefs.HasKey(ValueKey))
        {
            value = PlayerPrefs.GetFloat(ValueKey);
        }
    }

    public void SaveUserPresets()
    {
        ColorPresetCollection collection = new ColorPresetCollection { presets = new List<ColorPreset>(userPresets) };
        string json = JsonUtility.ToJson(collection);
        PlayerPrefs.SetString(UserPresetsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadUserPresets()
    {
        LoadUserPresetsFromPrefs();
        InitializePresetDropdown();
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

        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        saturationValuePalette?.SetValues(saturation, value);
        ApplyColor();
    }

    private void InitializePresetDropdown()
    {
        if (presetDropdown == null)
        {
            return;
        }

        combinedPresets.Clear();
        combinedPresets.Add(CreateCurrentValuePreset());
        combinedPresets.AddRange(defaultPresets.Where(p => p != null));
        combinedPresets.AddRange(userPresets.Where(p => p != null));

        isInitializingDropdown = true;
        presetDropdown.options = combinedPresets
            .Select(p => new Dropdown.OptionData(string.IsNullOrWhiteSpace(p.name) ? "Unnamed Preset" : p.name))
            .ToList();
        presetDropdown.RefreshShownValue();
        presetDropdown.onValueChanged.RemoveListener(OnPresetSelected);
        presetDropdown.onValueChanged.AddListener(OnPresetSelected);
        isInitializingDropdown = false;

        int matchingIndex = FindPresetIndexByValue(hue, saturation, value);
        if (matchingIndex >= 0)
        {
            presetDropdown.SetValueWithoutNotify(matchingIndex);
        }
    }

    private void HookupSavePresetButton()
    {
        if (savePresetButton == null)
        {
            return;
        }

        savePresetButton.onClick.RemoveListener(SavePresetFromUI);
        savePresetButton.onClick.AddListener(SavePresetFromUI);
    }

    private void SavePresetFromUI()
    {
        string presetName = presetNameInput != null ? presetNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(presetName))
        {
            Debug.LogWarning("Preset name cannot be empty.");
            return;
        }

        if (defaultPresets.Any(p => string.Equals(p.name, presetName, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.LogWarning($"A default preset named '{presetName}' already exists. Please choose another name.");
            return;
        }

        ColorPreset existingPreset = userPresets.FirstOrDefault(p => string.Equals(p.name, presetName, StringComparison.OrdinalIgnoreCase));
        if (existingPreset != null)
        {
            existingPreset.hue = hue;
            existingPreset.saturation = saturation;
            existingPreset.value = value;
        }
        else
        {
            if (userPresets.Count >= MaxUserPresets)
            {
                Debug.LogWarning($"Reached maximum preset count ({MaxUserPresets}). Oldest preset will be removed.");
                userPresets.RemoveAt(0);
            }

            userPresets.Add(new ColorPreset
            {
                name = presetName,
                hue = hue,
                saturation = saturation,
                value = value
            });
        }

        SaveUserPresets();
        InitializePresetDropdown();

        int index = combinedPresets.FindIndex(p => string.Equals(p.name, presetName, StringComparison.OrdinalIgnoreCase));
        if (index >= 0 && presetDropdown != null)
        {
            presetDropdown.SetValueWithoutNotify(index);
        }
    }

    private void OnPresetSelected(int index)
    {
        if (isInitializingDropdown || index < 0 || index >= combinedPresets.Count)
        {
            return;
        }

        ColorPreset selectedPreset = combinedPresets[index];
        ApplyPreset(selectedPreset);
    }

    private void ApplyPreset(ColorPreset preset)
    {
        if (preset == null)
        {
            return;
        }

        UpdateHue(preset.hue);
        UpdateSaturation(preset.saturation);
        UpdateValue(preset.value);
    }

    private void LoadUserPresetsFromPrefs()
    {
        userPresets.Clear();

        if (!PlayerPrefs.HasKey(UserPresetsKey))
        {
            return;
        }

        try
        {
            string json = PlayerPrefs.GetString(UserPresetsKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            ColorPresetCollection collection = JsonUtility.FromJson<ColorPresetCollection>(json);
            if (collection?.presets != null)
            {
                foreach (ColorPreset preset in collection.presets.Where(p => !string.IsNullOrEmpty(p.name)))
                {
                    userPresets.Add(new ColorPreset
                    {
                        name = preset.name,
                        hue = Mathf.Clamp01(preset.hue),
                        saturation = Mathf.Clamp01(preset.saturation),
                        value = Mathf.Clamp01(preset.value)
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load user presets. Falling back to defaults. {e.Message}");
            userPresets.Clear();
            PlayerPrefs.DeleteKey(UserPresetsKey);
            ApplyDefaultPresetFallback();
        }
    }

    private void ApplyDefaultPresetFallback()
    {
        ColorPreset fallbackPreset = defaultPresets.FirstOrDefault();
        if (fallbackPreset != null)
        {
            ApplyPreset(fallbackPreset);
        }
    }

    private ColorPreset CreateCurrentValuePreset()
    {
        return new ColorPreset
        {
            name = "Current (Saved)",
            hue = hue,
            saturation = saturation,
            value = value
        };
    }

    private int FindPresetIndexByValue(float targetHue, float targetSaturation, float targetValue)
    {
        for (int i = 0; i < combinedPresets.Count; i++)
        {
            ColorPreset preset = combinedPresets[i];
            if (ApproximatelyEqual(preset.hue, targetHue) &&
                ApproximatelyEqual(preset.saturation, targetSaturation) &&
                ApproximatelyEqual(preset.value, targetValue))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ApproximatelyEqual(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.001f;
    }
}

[Serializable]
public class ColorPresetCollection
{
    public List<ColorPreset> presets = new List<ColorPreset>();
}
