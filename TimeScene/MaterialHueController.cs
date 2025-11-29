using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MaterialHueController : MonoBehaviour
{
    private const string HueKey = "material_hue";
    private const string SaturationKey = "material_saturation";
    private const string ValueKey = "material_value";

    [System.Serializable]
    public struct HsvColorData
    {
        public float Hue;
        public float Saturation;
        public float Value;
    }

    [SerializeField] private string controllerId;
    [SerializeField] private string uniqueId;
    [SerializeField] private int slotNumber;
    [SerializeField] private int defaultSlotCount = 1;
    [SerializeField] private int fixedSlotCount;
    [SerializeField] private bool disableLocalPersistence;
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

    private void Start()
    {
        LoadSavedValues();

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

    public HsvColorData GetDefaultPreset(int index)
    {
        return default;
    }

    public void ApplyDefaultPreset(int index, bool saveToPlayerPrefs = true)
    {
        ApplyColorData(GetDefaultPreset(index), saveToPlayerPrefs);
    }

    public HsvColorData GetColorData()
    {
        return new HsvColorData
        {
            Hue = hue,
            Saturation = saturation,
            Value = value
        };
    }

    public HsvColorData GetCurrentColorData()
    {
        return GetColorData();
    }

    public string ControllerId => string.IsNullOrWhiteSpace(uniqueId)
        ? string.IsNullOrWhiteSpace(controllerId)
            ? gameObject.name
            : controllerId
        : uniqueId;

    public int SlotNumber => slotNumber;

    public int DefaultSlotCount => Mathf.Max(0, defaultSlotCount);

    public int FixedSlotCount => Mathf.Max(0, fixedSlotCount);

    public void ApplyColorData(HsvColorData colorData, bool saveToPlayerPrefs = true)
    {
        hue = Mathf.Repeat(colorData.Hue, 1f);
        saturation = Mathf.Clamp01(colorData.Saturation);
        value = Mathf.Clamp01(colorData.Value);

        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        saturationValuePalette?.SetValues(saturation, value);

        ApplyColor();

        if (saveToPlayerPrefs && ShouldUseLocalPersistence())
        {
            SaveValues();
        }
    }

    public void ApplyPresetColors(IList<Color> colors, bool saveToPlayerPrefs = true)
    {
        if (colors == null)
        {
            return;
        }

        int totalSlots = DefaultSlotCount + FixedSlotCount;
        if (totalSlots <= 0)
        {
            throw new InvalidOperationException("No available slots configured for MaterialHueController.");
        }

        if (colors.Count > totalSlots)
        {
            Debug.LogWarning($"Preset contains more colors ({colors.Count}) than available slots ({totalSlots}); extra colors will be ignored.");
        }

        if (slotNumber < 0 || slotNumber >= totalSlots)
        {
            Debug.LogWarning($"Slot number {slotNumber} is out of range for configured slots (0-{totalSlots - 1}).");
            return;
        }

        if (slotNumber >= colors.Count)
        {
            Debug.LogWarning($"No preset color exists for slot {slotNumber}; preset contains only {colors.Count} colors.");
            return;
        }

        Color sourceColor = colors[slotNumber];
        Color.RGBToHSV(sourceColor, out float newHue, out float newSaturation, out float newValue);
        ApplyColorData(new HsvColorData
        {
            Hue = newHue,
            Saturation = newSaturation,
            Value = newValue,
        }, saveToPlayerPrefs);
    }

    public void UpdateHue(float newHue)
    {
        bool hasChanged = !Mathf.Approximately(hue, newHue);
        hue = newHue;
        hueRingSelector?.SetHue(hue);
        saturationValuePalette?.SetHue(hue);
        ApplyColor();

        if (hasChanged && ShouldUseLocalPersistence())
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

        if (hasChanged && ShouldUseLocalPersistence())
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

        if (hasChanged && ShouldUseLocalPersistence())
        {
            PlayerPrefs.SetFloat(ValueKey, value);
            PlayerPrefs.Save();
        }
    }

    public Color GetCurrentColor()
    {
        return Color.HSVToRGB(hue, saturation, value);
    }

    private void LoadSavedValues()
    {
        if (!ShouldUseLocalPersistence())
        {
            return;
        }

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

    private bool HasSavedValues()
    {
        if (!ShouldUseLocalPersistence())
        {
            return false;
        }

        return PlayerPrefs.HasKey(HueKey) || PlayerPrefs.HasKey(SaturationKey) || PlayerPrefs.HasKey(ValueKey);
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

    private void SaveValues()
    {
        if (!ShouldUseLocalPersistence())
        {
            return;
        }

        PlayerPrefs.SetFloat(HueKey, hue);
        PlayerPrefs.SetFloat(SaturationKey, saturation);
        PlayerPrefs.SetFloat(ValueKey, value);
        PlayerPrefs.Save();
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

        if (string.IsNullOrWhiteSpace(controllerId))
        {
            controllerId = gameObject.name;
        }

        if (string.IsNullOrWhiteSpace(uniqueId))
        {
            uniqueId = controllerId;
        }

        if (slotNumber < 0)
        {
            slotNumber = 0;
        }

        if (defaultSlotCount < 0)
        {
            defaultSlotCount = 0;
        }

        if (fixedSlotCount < 0)
        {
            fixedSlotCount = 0;
        }
    }

    private bool ShouldUseLocalPersistence()
    {
        return !disableLocalPersistence;
    }
}
