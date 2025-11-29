using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HueSyncCoordinator : MonoBehaviour
{
    [Header("HSV Values")]
    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f, 1f)]
    [SerializeField] private float value;

    [Header("Selectors")]
    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private List<HueRingSelector> hueRingSelectors = new();
    [SerializeField] private SaturationValuePalette saturationValuePalette;
    [SerializeField] private List<SaturationValuePalette> saturationValuePalettes = new();

    [Header("Targets")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private List<Material> targetMaterials = new();
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private List<Graphic> previewGraphics = new();

    [SerializeField] private UnityEvent<float> onHueUpdated = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> onSaturationUpdated = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> onValueUpdated = new UnityEvent<float>();
    [SerializeField] private UnityEvent<Color> onColorApplied = new UnityEvent<Color>();

    public UnityEvent<float> OnHueUpdated => onHueUpdated;
    public UnityEvent<float> OnSaturationUpdated => onSaturationUpdated;
    public UnityEvent<float> OnValueUpdated => onValueUpdated;
    public UnityEvent<Color> OnColorApplied => onColorApplied;

    public float Hue => hue;
    public float Saturation => saturation;
    public float Value => value;
    public Color CurrentColor => Color.HSVToRGB(hue, saturation, value);

    private void Start()
    {
        RegisterSelectorListeners();
        SyncSelectors();
        ApplyColor();
    }

    public void InitializeSelectors()
    {
        RegisterSelectorListeners();
        SyncSelectors();
        ApplyColor();
    }

    public void SetColorValues(float newHue, float newSaturation, float newValue)
    {
        hue = Mathf.Repeat(newHue, 1f);
        saturation = Mathf.Clamp01(newSaturation);
        value = Mathf.Clamp01(newValue);

        SyncSelectors();
        ApplyColor();
    }

    public void SetHue(float newHue)
    {
        hue = Mathf.Repeat(newHue, 1f);
        SyncHueSelectors();
        SyncPaletteHues();
        ApplyColor();
        onHueUpdated.Invoke(hue);
    }

    public void SetSaturation(float newSaturation)
    {
        saturation = Mathf.Clamp01(newSaturation);
        SyncPalettesSaturation();
        ApplyColor();
        onSaturationUpdated.Invoke(saturation);
    }

    public void SetValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);
        SyncPalettesValue();
        ApplyColor();
        onValueUpdated.Invoke(value);
    }

    public void ApplyColor()
    {
        ApplyColor(CurrentColor);
    }

    public void ApplyColor(Color color)
    {
        ApplyColorToTargets(color);
        onColorApplied.Invoke(color);
    }

    public void SyncSelectors()
    {
        SyncHueSelectors();
        SyncPaletteHues();
        SyncPalettesValues();
    }

    public void SyncHueSelectors()
    {
        foreach (HueRingSelector selector in EnumerateHueRingSelectors())
        {
            selector.SetHue(hue);
        }
    }

    public void SyncPaletteHues()
    {
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetHue(hue);
        }
    }

    public void SyncPalettesSaturation()
    {
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetSaturation(saturation);
        }
    }

    public void SyncPalettesValue()
    {
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetValue(value);
        }
    }

    private void SyncPalettesValues()
    {
        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.SetValues(saturation, value);
        }
    }

    private void RegisterSelectorListeners()
    {
        foreach (HueRingSelector selector in EnumerateHueRingSelectors())
        {
            selector.OnHueChanged.RemoveListener(SetHue);
            selector.OnHueChanged.AddListener(SetHue);
        }

        foreach (SaturationValuePalette palette in EnumerateSaturationValuePalettes())
        {
            palette.OnSaturationChanged.RemoveListener(SetSaturation);
            palette.OnSaturationChanged.AddListener(SetSaturation);
            palette.OnValueChanged.RemoveListener(SetValue);
            palette.OnValueChanged.AddListener(SetValue);
        }
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

        SyncSelectors();
        ApplyColor();
    }
}
