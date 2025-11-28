using UnityEngine;
using UnityEngine.UI;
public class MaterialHueController : MonoBehaviour
{
    [System.Serializable]
    public struct ColorPreset
    {
        [Range(0f, 1f)] public float hue;
        [Range(0f, 1f)] public float saturation;
        [Range(0f, 1f)] public float value;

        public static ColorPreset FromColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return new ColorPreset { hue = h, saturation = s, value = v };
        }
    }

    private const string HueKey = "material_hue";
    private const string SaturationKey = "material_saturation";
    private const string ValueKey = "material_value";
    private const string PresetKeyPattern = "material_preset_{0}_{1}";

    [SerializeField] private Material targetMaterial;
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;

    [Header("Presets")]
    [SerializeField] private ColorPreset[] builtInPresets = System.Array.Empty<ColorPreset>();
    [SerializeField] private ColorPreset[] userPresets = System.Array.Empty<ColorPreset>();

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f,1f)] [SerializeField] private float value;

    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private SaturationValuePalette saturationValuePalette;

    private void Start()
    {
        LoadUserPresets();
        LoadSavedValues();

        if (hueRingSelector != null)
        {
            hueRingSelector.OnHueChanged.AddListener(UpdateHue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.OnSaturationChanged.AddListener(UpdateSaturation);
            saturationValuePalette.OnValueChanged.AddListener(UpdateValue);
        }

        ApplyPresetToSelectors();
        ApplyColor();
    }

    public void UpdateHue(float newHue)
    {
        SetColorValues(newHue, saturation, value);
    }

    public void UpdateSaturation(float newSat)
    {
        SetColorValues(hue, newSat, value);
    }

    public void UpdateValue(float newVal)
    {
        SetColorValues(hue, saturation, newVal);
    }

    public void LoadPreset(int presetIndex)
    {
        if (!TryGetPreset(presetIndex, out ColorPreset preset))
        {
            Debug.LogWarning($"Preset index {presetIndex} is out of range.");
            return;
        }

        ApplyPreset(preset);
    }

    public void SavePreset(int presetIndex)
    {
        if (presetIndex < 0)
        {
            Debug.LogWarning($"Preset index {presetIndex} is invalid.");
            return;
        }

        int userIndex = presetIndex - (builtInPresets?.Length ?? 0);
        if (userIndex < 0 || userIndex >= userPresets.Length)
        {
            Debug.LogWarning($"Preset {presetIndex} is not a user preset and cannot be saved.");
            return;
        }

        ColorPreset currentPreset = new ColorPreset
        {
            hue = hue,
            saturation = saturation,
            value = value
        };

        userPresets[userIndex] = currentPreset;
        SaveUserPresetToPrefs(presetIndex, currentPreset);
    }

    public void SetBuiltInPreset(int presetIndex, ColorPreset preset)
    {
        if (builtInPresets == null || presetIndex < 0 || presetIndex >= builtInPresets.Length)
        {
            Debug.LogWarning($"Built-in preset index {presetIndex} is out of range.");
            return;
        }

        builtInPresets[presetIndex] = preset;
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

    private void LoadUserPresets()
    {
        int builtInCount = builtInPresets?.Length ?? 0;
        for (int i = 0; i < userPresets.Length; i++)
        {
            int presetIndex = builtInCount + i;
            if (TryLoadPresetFromPrefs(presetIndex, out ColorPreset savedPreset))
            {
                userPresets[i] = savedPreset;
            }
        }
    }

    private bool TryGetPreset(int presetIndex, out ColorPreset preset)
    {
        int builtInCount = builtInPresets?.Length ?? 0;
        if (presetIndex < 0)
        {
            preset = default;
            return false;
        }

        if (presetIndex < builtInCount)
        {
            preset = builtInPresets[presetIndex];
            return true;
        }

        int userIndex = presetIndex - builtInCount;
        if (userIndex >= 0 && userIndex < userPresets.Length)
        {
            preset = userPresets[userIndex];
            return true;
        }

        preset = default;
        return false;
    }

    private bool TryLoadPresetFromPrefs(int presetIndex, out ColorPreset preset)
    {
        string hueKey = GetPresetKey(presetIndex, "hue");
        string saturationKey = GetPresetKey(presetIndex, "saturation");
        string valueKey = GetPresetKey(presetIndex, "value");

        if (!PlayerPrefs.HasKey(hueKey) || !PlayerPrefs.HasKey(saturationKey) || !PlayerPrefs.HasKey(valueKey))
        {
            preset = default;
            return false;
        }

        preset = new ColorPreset
        {
            hue = PlayerPrefs.GetFloat(hueKey),
            saturation = PlayerPrefs.GetFloat(saturationKey),
            value = PlayerPrefs.GetFloat(valueKey)
        };
        return true;
    }

    private void SaveUserPresetToPrefs(int presetIndex, ColorPreset preset)
    {
        PlayerPrefs.SetFloat(GetPresetKey(presetIndex, "hue"), preset.hue);
        PlayerPrefs.SetFloat(GetPresetKey(presetIndex, "saturation"), preset.saturation);
        PlayerPrefs.SetFloat(GetPresetKey(presetIndex, "value"), preset.value);
        PlayerPrefs.Save();
    }

    private string GetPresetKey(int presetIndex, string propertyName)
    {
        return string.Format(PresetKeyPattern, presetIndex, propertyName);
    }

    private void ApplyPreset(ColorPreset preset)
    {
        SetColorValues(preset.hue, preset.saturation, preset.value);
    }

    private void SetColorValues(float newHue, float newSaturation, float newValue)
    {
        bool hueChanged = !Mathf.Approximately(hue, newHue);
        bool satChanged = !Mathf.Approximately(saturation, newSaturation);
        bool valChanged = !Mathf.Approximately(value, newValue);

        hue = Mathf.Repeat(newHue, 1f);
        saturation = Mathf.Clamp01(newSaturation);
        value = Mathf.Clamp01(newValue);

        ApplyPresetToSelectors();
        ApplyColor();

        if (hueChanged)
        {
            PlayerPrefs.SetFloat(HueKey, hue);
        }

        if (satChanged)
        {
            PlayerPrefs.SetFloat(SaturationKey, saturation);
        }

        if (valChanged)
        {
            PlayerPrefs.SetFloat(ValueKey, value);
        }

        if (hueChanged || satChanged || valChanged)
        {
            PlayerPrefs.Save();
        }
    }

    private void ApplyPresetToSelectors()
    {
        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        saturationValuePalette?.SetValues(saturation, value);
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

        ApplyPresetToSelectors();
        ApplyColor();
    }
}
