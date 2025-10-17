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

    private Texture originalMainTexture;
    private Texture2D baseTexture;
    private Texture2D runtimeTexture;

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

        originalMainTexture = targetMaterial.mainTexture;

        var originalTexture = originalMainTexture as Texture2D;
        if (originalTexture != null)
        {
            baseTexture = Instantiate(originalTexture);
            baseTexture.name = originalTexture.name + "_Base";
            runtimeTexture = Instantiate(baseTexture);
            runtimeTexture.name = originalTexture.name + "_Runtime";
            targetMaterial.mainTexture = runtimeTexture;
        }

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

        if (baseTexture != null && runtimeTexture != null)
        {
            var pixels = baseTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color.RGBToHSV(pixels[i], out var pixelHue, out var pixelSaturation, out var pixelValue);
                pixelHue = hue;
                pixelSaturation = saturation;
                pixelValue = value;
                pixels[i] = Color.HSVToRGB(pixelHue, pixelSaturation, pixelValue);
            }

            runtimeTexture.SetPixels(pixels);
            runtimeTexture.Apply();
            targetMaterial.mainTexture = runtimeTexture;
        }

        targetMaterial.color = Color.HSVToRGB(hue, saturation, value);
    }

    private void OnValidate()
    {
        ApplyColor();
    }

    private void OnDestroy()
    {
        if (targetMaterial != null && originalMainTexture != null)
        {
            targetMaterial.mainTexture = originalMainTexture;
        }

        if (runtimeTexture != null)
        {
            Destroy(runtimeTexture);
            runtimeTexture = null;
        }

        if (baseTexture != null)
        {
            Destroy(baseTexture);
            baseTexture = null;
        }
        originalMainTexture = null;
    }
}
