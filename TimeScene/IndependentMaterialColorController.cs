using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マテリアルの色を Hue Ring と Saturation/Value パレットで操作するコントローラー。
/// </summary>
public class IndependentMaterialColorController : MonoBehaviour
{
    [Header("保存設定")]
    [SerializeField, Tooltip("セーブデータ内で識別するためのキー。インスタンスごとに重複しない値を設定してください。")]
    private string saveIdentifier = "independent_material";

    [SerializeField, Tooltip("現在のセーブスロット ID（SaveGameManager のキーなど）")]
    private string currentSlotId;

    [SerializeField, Tooltip("セーブデータへの読み書きを行うアクセサ")]
    private MonoBehaviour saveAccessorBehaviour;

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

    private IIndependentMaterialColorSaveAccessor SaveAccessor => saveAccessorBehaviour as IIndependentMaterialColorSaveAccessor;

    private void Awake()
    {
        currentHue = Mathf.Repeat(initialHue, 1f);
        currentSaturation = Mathf.Clamp01(initialSaturation);
        currentValue = Mathf.Clamp01(initialValue);

        ApplyInitialLoad();
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

    public void SetHSV(float hue, float saturation, float value, bool saveToData = true)
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

        if (hasChanged && saveToData)
        {
            SaveValues();
        }
    }

    private void SaveValues()
    {
        if (SaveAccessor == null || string.IsNullOrEmpty(currentSlotId))
        {
            return;
        }

        SaveAccessor.SaveColor(currentSlotId, GetNamespacedIdentifier(), new HSVColor(currentHue, currentSaturation, currentValue));
    }

    private void ApplyInitialLoad()
    {
        if (!TryApplySavedValues())
        {
            SetHSV(initialHue, initialSaturation, initialValue, saveToData: false);
        }
    }

    private bool TryApplySavedValues()
    {
        if (SaveAccessor == null || string.IsNullOrEmpty(currentSlotId))
        {
            return false;
        }

        if (SaveAccessor.TryGetColor(currentSlotId, GetNamespacedIdentifier(), out var savedColor))
        {
            SetHSV(savedColor.H, savedColor.S, savedColor.V, saveToData: false);
            return true;
        }

        return false;
    }

    private string GetNamespacedIdentifier()
    {
        return string.IsNullOrWhiteSpace(saveIdentifier) ? name : saveIdentifier.Trim();
    }

    public void SetSaveContext(string slotId, IIndependentMaterialColorSaveAccessor accessor)
    {
        currentSlotId = slotId;
        saveAccessorBehaviour = accessor as MonoBehaviour;
        ApplyInitialLoad();
        ApplySelectors();
        ApplyColor();
    }

    public static void SetSaveContextForAllControllers(string slotId, IIndependentMaterialColorSaveAccessor accessor)
    {
        if (string.IsNullOrEmpty(slotId) || accessor == null)
        {
            return;
        }

        foreach (var controller in Resources.FindObjectsOfTypeAll<IndependentMaterialColorController>())
        {
            if (controller == null)
            {
                continue;
            }

            controller.SetSaveContext(slotId, accessor);
        }
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
