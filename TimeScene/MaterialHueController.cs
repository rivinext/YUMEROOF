using UnityEngine;
using UnityEngine.UI;

public class MaterialHueController : MonoBehaviour
{
    private const string HueKey = "material_hue";
    private const string SaturationKey = "material_saturation";
    private const string ValueKey = "material_value";

    [SerializeField] private Material targetMaterial;

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f,1f)] [SerializeField] private float value;

    [SerializeField] private Slider hueSlider;
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private Slider valueSlider;

    private void Start()
    {
        LoadSavedValues();

        if (hueSlider != null)
        {
            hueSlider.value = hue;
            hueSlider.onValueChanged.AddListener(UpdateHue);
        }
        if (saturationSlider != null)
        {
            saturationSlider.value = saturation;
            saturationSlider.onValueChanged.AddListener(UpdateSaturation);
        }
        if (valueSlider != null)
        {
            valueSlider.value = value;
            valueSlider.onValueChanged.AddListener(UpdateValue);
        }

        ApplyColor();
    }

    public void UpdateHue(float newHue)
    {
        bool hasChanged = !Mathf.Approximately(hue, newHue);
        hue = newHue;
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
        if (targetMaterial == null)
        {
            return;
        }

        targetMaterial.color = Color.HSVToRGB(hue, saturation, value);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        ApplyColor();
    }
}
