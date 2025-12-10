using UnityEngine;
using UnityEngine.UI;

public class MaterialHueController : MonoBehaviour
{
    // PlayerPrefs のキー用プレフィックス（インスタンスごとに変えられる）
    [SerializeField] private string playerPrefsKeyPrefix = "material";

    [Header("Persistence")]
    [SerializeField] private bool loadFromPrefsOnStart = false;

    private string HueKey => $"{playerPrefsKeyPrefix}_hue";
    private string SaturationKey => $"{playerPrefsKeyPrefix}_saturation";
    private string ValueKey => $"{playerPrefsKeyPrefix}_value";

    [Header("Targets")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Image previewImage;
    [SerializeField] private RawImage previewRawImage;

    [Header("HSV Values")]
    [Range(0f, 1f)]
    [SerializeField] private float hue;

    [Range(0f, 1f)]
    [SerializeField] private float saturation;

    [Range(0f, 1f)]
    [SerializeField] private float value;

    [Header("Applied HSV Values")]
    [SerializeField] private float appliedHue;

    [Range(0f, 1f)]
    [SerializeField] private float appliedSaturation;

    [Range(0f, 1f)]
    [SerializeField] private float appliedValue;

    [Header("Selectors")]
    [SerializeField] private HueRingSelector hueRingSelector;
    [SerializeField] private SaturationValuePalette saturationValuePalette;

    // 外部（プリセットマネージャなど）から参照する用
    public float Hue => hue;
    public float Saturation => saturation;
    public float Value => value;
    public float AppliedHue => appliedHue;
    public float AppliedSaturation => appliedSaturation;
    public float AppliedValue => appliedValue;
    public Color CurrentColor => Color.HSVToRGB(hue, saturation, value);
    public Color AppliedColor => Color.HSVToRGB(appliedHue, appliedSaturation, appliedValue);

    private void Awake()
    {
        SyncAppliedToPreview();
    }

    private void Start()
    {
        if (loadFromPrefsOnStart)
        {
            ApplySavedValuesFromPrefs();
        }

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

    /// <summary>
    /// Hueリング用のリスナー
    /// </summary>
    public void UpdateHue(float newHue)
    {
        SetHSV(newHue, saturation, value, saveToPrefs: true);
    }

    /// <summary>
    /// Saturation用のリスナー
    /// </summary>
    public void UpdateSaturation(float newSat)
    {
        SetHSV(hue, newSat, value, saveToPrefs: true);
    }

    /// <summary>
    /// Value用のリスナー
    /// </summary>
    public void UpdateValue(float newVal)
    {
        SetHSV(hue, saturation, newVal, saveToPrefs: true);
    }

    /// <summary>
    /// 外部からまとめて HSV を設定するための API
    /// saveToPrefs = true にすると PlayerPrefs にも保存される
    /// applyToMaterial = false にするとプレビュー用 UI のみ更新し、マテリアルは変更しない
    /// </summary>
    public void SetHSV(float newHue, float newSaturation, float newValue, bool saveToPrefs = false, bool applyToMaterial = true)
    {
        newHue = Mathf.Repeat(newHue, 1f);
        newSaturation = Mathf.Clamp01(newSaturation);
        newValue = Mathf.Clamp01(newValue);

        bool previewChanged =
            !Mathf.Approximately(hue, newHue) ||
            !Mathf.Approximately(saturation, newSaturation) ||
            !Mathf.Approximately(value, newValue);

        hue = newHue;
        saturation = newSaturation;
        value = newValue;

        if (applyToMaterial)
        {
            appliedHue = hue;
            appliedSaturation = saturation;
            appliedValue = value;
        }

        // セレクタ側も同期
        if (hueRingSelector != null)
        {
            hueRingSelector.SetHue(hue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.SetHue(hue);
            saturationValuePalette.SetValues(saturation, value);
        }

        ApplyColor(applyToMaterial);

        if (saveToPrefs && applyToMaterial && previewChanged)
        {
            SaveValues();
        }
    }

    public void SetPreviewHSV(float newHue, float newSaturation, float newValue)
    {
        SetHSV(newHue, newSaturation, newValue, saveToPrefs: false, applyToMaterial: false);
    }

    public bool ApplySavedValuesFromPrefs()
    {
        if (!TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue))
        {
            return false;
        }

        SetHSV(savedHue, savedSaturation, savedValue);
        return true;
    }

    private void SaveValues()
    {
        PlayerPrefs.SetFloat(HueKey, appliedHue);
        PlayerPrefs.SetFloat(SaturationKey, appliedSaturation);
        PlayerPrefs.SetFloat(ValueKey, appliedValue);
        PlayerPrefs.Save();
    }

    private bool TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue)
    {
        bool hasSavedValue = false;

        savedHue = appliedHue;
        savedSaturation = appliedSaturation;
        savedValue = appliedValue;

        if (PlayerPrefs.HasKey(HueKey))
        {
            savedHue = PlayerPrefs.GetFloat(HueKey);
            hasSavedValue = true;
        }

        if (PlayerPrefs.HasKey(SaturationKey))
        {
            savedSaturation = PlayerPrefs.GetFloat(SaturationKey);
            hasSavedValue = true;
        }

        if (PlayerPrefs.HasKey(ValueKey))
        {
            savedValue = PlayerPrefs.GetFloat(ValueKey);
            hasSavedValue = true;
        }

        return hasSavedValue;
    }

    private void ApplyColor(bool applyToMaterial = true)
    {
        Color currentColor = CurrentColor;

        if (applyToMaterial && targetMaterial != null)
        {
            targetMaterial.color = AppliedColor;
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

        SyncAppliedToPreview();

        // エディタ上で値をいじったときも UI & マテリアルに反映
        if (hueRingSelector != null)
        {
            hueRingSelector.SetHue(hue);
        }

        if (saturationValuePalette != null)
        {
            saturationValuePalette.SetHue(hue);
            saturationValuePalette.SetValues(saturation, value);
        }

        ApplyColor();
    }

    private void SyncAppliedToPreview()
    {
        appliedHue = hue;
        appliedSaturation = saturation;
        appliedValue = value;
    }
}
