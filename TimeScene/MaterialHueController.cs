using UnityEngine;
using UnityEngine.UI;

public class MaterialHueController : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;

    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float value = 0.5f;

    [SerializeField] private Slider hueSlider;
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private Slider valueSlider;

    private void Start()
    {
        if (targetMaterial == null)
        {
            return;
        }

        const float defaultHue = 0f;
        const float defaultSaturation = 0.5f;
        const float defaultValue = 0.5f;

        hue = defaultHue;
        saturation = defaultSaturation;
        value = defaultValue;

        targetMaterial.color = Color.HSVToRGB(hue, saturation, value);

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
    }

    public void UpdateHue(float newHue)
    {
        hue = newHue;
        ApplyColor();
    }

    public void UpdateSaturation(float newSat)
    {
        saturation = newSat;
        ApplyColor();
    }

    public void UpdateValue(float newVal)
    {
        value = newVal;
        ApplyColor();
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
        ApplyColor();
    }
}
