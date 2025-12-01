using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マテリアルの色を Hue Ring と Saturation/Value パレットで操作するコントローラー。
/// </summary>
public class IndependentMaterialColorController : MonoBehaviour
{
    [Header("保存設定")]
    [SerializeField, Tooltip("PlayerPrefs に保存するときのキー プレフィックス。インスタンスごとに変えてください。")]
    private string playerPrefsKeyPrefix = "independent_material";

    [SerializeField, Tooltip("起動時に PlayerPrefs から HSV を自動で読み込むかどうか。")]
    private bool loadFromPrefsOnAwake = true;

    private string HueKey => $"{playerPrefsKeyPrefix}_hue";
    private string SaturationKey => $"{playerPrefsKeyPrefix}_saturation";
    private string ValueKey => $"{playerPrefsKeyPrefix}_value";

    [Header("対象マテリアル")]
    [SerializeField, Tooltip("色を変更したいマテリアルをアタッチしてください。")]
    private Material targetMaterial;

    [Header("カラー UI 参照")]
    [SerializeField, Tooltip("Hue Ring Selector をアタッチしてください。")]
    private HueRingSelector hueRingSelector;

    [SerializeField, Tooltip("Saturation Value Palette をアタッチしてください。")]
    private SaturationValuePalette saturationValuePalette;

    [Header("確認用 Raw Image")]
    [SerializeField, Tooltip("調整後の色を表示したい Raw Image をアタッチしてください。")]
    private RawImage previewRawImage;

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

        if (loadFromPrefsOnAwake)
        {
            ApplySavedValuesFromPrefs();
        }

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

    public void SetHSV(float hue, float saturation, float value, bool saveToPrefs = true)
    {
        float clampedHue = Mathf.Repeat(hue, 1f);
        float clampedSaturation = Mathf.Clamp01(saturation);
        float clampedValue = Mathf.Clamp01(value);

        bool hasChanged =
            !Mathf.Approximately(currentHue, clampedHue) ||
            !Mathf.Approximately(currentSaturation, clampedSaturation) ||
            !Mathf.Approximately(currentValue, clampedValue);

        currentHue = clampedHue;
        currentSaturation = clampedSaturation;
        currentValue = clampedValue;

        ApplySelectors();
        ApplyColor();

        if (hasChanged && saveToPrefs)
        {
            SaveValues();
        }
    }

    private void SaveValues()
    {
        PlayerPrefs.SetFloat(HueKey, currentHue);
        PlayerPrefs.SetFloat(SaturationKey, currentSaturation);
        PlayerPrefs.SetFloat(ValueKey, currentValue);
        PlayerPrefs.Save();
    }

    private void ApplySavedValuesFromPrefs()
    {
        if (TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue))
        {
            SetHSV(savedHue, savedSaturation, savedValue, saveToPrefs: false);
        }
    }

    private bool TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue)
    {
        bool hasSaved = false;

        savedHue = currentHue;
        savedSaturation = currentSaturation;
        savedValue = currentValue;

        if (PlayerPrefs.HasKey(HueKey))
        {
            savedHue = PlayerPrefs.GetFloat(HueKey);
            hasSaved = true;
        }

        if (PlayerPrefs.HasKey(SaturationKey))
        {
            savedSaturation = PlayerPrefs.GetFloat(SaturationKey);
            hasSaved = true;
        }

        if (PlayerPrefs.HasKey(ValueKey))
        {
            savedValue = PlayerPrefs.GetFloat(ValueKey);
            hasSaved = true;
        }

        return hasSaved;
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

        if (previewRawImage != null)
        {
            previewRawImage.color = targetMaterial.color;
        }
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
