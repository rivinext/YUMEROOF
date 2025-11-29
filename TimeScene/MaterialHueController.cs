using UnityEngine;
using UnityEngine.UI;
public class MaterialHueController : MonoBehaviour
{
    private const string HueKey = "material_hue";
    private const string SaturationKey = "material_saturation";
    private const string ValueKey = "material_value";

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
}
