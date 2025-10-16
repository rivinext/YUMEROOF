using UnityEngine;
using UnityEngine.UI;

public class MaterialHueController : MonoBehaviour
{
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
        if (targetMaterial == null)
        {
            return;
        }

        Color.RGBToHSV(targetMaterial.color, out var h, out var s, out value);
        h = 0f;
        s = 0f;
        targetMaterial.color = Color.HSVToRGB(0f, 0f, value);
        hue = 0f;
        saturation = 0f;

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
