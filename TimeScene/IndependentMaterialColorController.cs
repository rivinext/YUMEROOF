using UnityEngine;

/// <summary>
/// マテリアルの色を Hue Ring と Saturation/Value パレットで操作するコントローラー。
/// </summary>
public class IndependentMaterialColorController : MonoBehaviour
{
    [Header("対象マテリアル")]
    [SerializeField, Tooltip("色を変更したいマテリアルをアタッチしてください。")]
    private Material targetMaterial;

    [Header("カラー UI 参照")]
    [SerializeField, Tooltip("Hue Ring Selector をアタッチしてください。")]
    private HueRingSelector hueRingSelector;

    [SerializeField, Tooltip("Saturation Value Palette をアタッチしてください。")]
    private SaturationValuePalette saturationValuePalette;

    [Header("初期 HSV 値")]
    [SerializeField, Range(0f, 1f)] private float initialHue = 0f;
    [SerializeField, Range(0f, 1f)] private float initialSaturation = 1f;
    [SerializeField, Range(0f, 1f)] private float initialValue = 1f;

    private float currentHue;
    private float currentSaturation;
    private float currentValue;

    private void Awake()
    {
        currentHue = Mathf.Repeat(initialHue, 1f);
        currentSaturation = Mathf.Clamp01(initialSaturation);
        currentValue = Mathf.Clamp01(initialValue);

        ApplySelectors();
        ApplyColor();
    }

    private void OnEnable()
    {
        if (hueRingSelector != null)
        {
            hueRingSelector.OnHueChanged.AddListener(UpdateHue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.OnSaturationChanged.AddListener(UpdateSaturation);
            saturationValuePalette.OnValueChanged.AddListener(UpdateValue);
        }
    }

    private void OnDisable()
    {
        if (hueRingSelector != null)
        {
            hueRingSelector.OnHueChanged.RemoveListener(UpdateHue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.OnSaturationChanged.RemoveListener(UpdateSaturation);
            saturationValuePalette.OnValueChanged.RemoveListener(UpdateValue);
        }
    }

    public void UpdateHue(float newHue)
    {
        SetHSV(newHue, currentSaturation, currentValue);
    }

    public void UpdateSaturation(float newSaturation)
    {
        SetHSV(currentHue, newSaturation, currentValue);
    }

    public void UpdateValue(float newValue)
    {
        SetHSV(currentHue, currentSaturation, newValue);
    }

    public void SetHSV(float hue, float saturation, float value)
    {
        currentHue = Mathf.Repeat(hue, 1f);
        currentSaturation = Mathf.Clamp01(saturation);
        currentValue = Mathf.Clamp01(value);

        ApplySelectors();
        ApplyColor();
    }

    private void ApplySelectors()
    {
        if (hueRingSelector != null)
        {
            hueRingSelector.SetHue(currentHue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.SetHue(currentHue);
            saturationValuePalette.SetValues(currentSaturation, currentValue);
        }
    }

    private void ApplyColor()
    {
        if (targetMaterial == null)
        {
            return;
        }

        targetMaterial.color = Color.HSVToRGB(currentHue, currentSaturation, currentValue);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        initialHue = Mathf.Repeat(initialHue, 1f);
        initialSaturation = Mathf.Clamp01(initialSaturation);
        initialValue = Mathf.Clamp01(initialValue);

        currentHue = initialHue;
        currentSaturation = initialSaturation;
        currentValue = initialValue;

        ApplySelectors();
        ApplyColor();
    }
#endif
}
