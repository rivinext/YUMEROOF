using UnityEngine;
using UnityEngine.UI;

public class IndependentMaterialColorController : MonoBehaviour
{
    [SerializeField, Tooltip("Renderer whose material color will be controlled. Drag the target Renderer here in the Inspector.")]
    private Renderer targetRenderer;

    [SerializeField, Tooltip("Index of the material to edit on the target Renderer. Confirm the index when the Renderer has multiple materials.")]
    private int materialIndex;
    [SerializeField] private HueRingSelector hueSelector;
    [SerializeField] private SaturationValuePalette svPalette;
    [SerializeField] private string prefsKeyPrefix = "independent_mat";

    [SerializeField, Range(0f, 1f)] private float hue = 0f;
    [SerializeField, Range(0f, 1f)] private float saturation = 1f;
    [SerializeField, Range(0f, 1f)] private float value = 1f;

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
        materialIndex = 0;
    }

    private void Awake()
    {
        EnsurePrefsPrefix();
        EnsureUIReferences();
    }

    private void Start()
    {
        LoadValuesFromPrefs();
        ApplySelectors();
        ApplyColor();

        if (hueSelector != null)
        {
            hueSelector.OnHueChanged.AddListener(UpdateHue);
        }

        if (svPalette != null)
        {
            svPalette.OnSaturationChanged.AddListener(UpdateSaturation);
            svPalette.OnValueChanged.AddListener(UpdateValue);
        }
    }

    public void UpdateHue(float newHue)
    {
        SetHSV(newHue, saturation, value, saveToPrefs: true);
    }

    public void UpdateSaturation(float newSaturation)
    {
        SetHSV(hue, newSaturation, value, saveToPrefs: true);
    }

    public void UpdateValue(float newValue)
    {
        SetHSV(hue, saturation, newValue, saveToPrefs: true);
    }

    public void SetHSV(float newHue, float newSaturation, float newValue, bool saveToPrefs = false)
    {
        EnsurePrefsPrefix();

        newHue = Mathf.Repeat(newHue, 1f);
        newSaturation = Mathf.Clamp01(newSaturation);
        newValue = Mathf.Clamp01(newValue);

        bool changed = !Mathf.Approximately(hue, newHue) ||
                       !Mathf.Approximately(saturation, newSaturation) ||
                       !Mathf.Approximately(value, newValue);

        hue = newHue;
        saturation = newSaturation;
        value = newValue;

        ApplySelectors();
        ApplyColor();

        if (saveToPrefs && changed)
        {
            SaveValuesToPrefs();
        }
    }

    public void SetTarget(Renderer renderer, int index)
    {
        EnsurePrefsPrefix();

        SaveValuesToPrefs();

        targetRenderer = renderer;
        materialIndex = index;

        LoadValuesFromPrefs();
        ApplySelectors();
        ApplyColor();
    }

    private void LoadValuesFromPrefs()
    {
        if (TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue))
        {
            hue = savedHue;
            saturation = savedSaturation;
            value = savedValue;
        }
    }

    private void SaveValuesToPrefs()
    {
        string hueKey = GetHueKey();
        string satKey = GetSaturationKey();
        string valKey = GetValueKey();

        PlayerPrefs.SetFloat(hueKey, hue);
        PlayerPrefs.SetFloat(satKey, saturation);
        PlayerPrefs.SetFloat(valKey, value);
        PlayerPrefs.Save();
    }

    private bool TryGetSavedValues(out float savedHue, out float savedSaturation, out float savedValue)
    {
        EnsurePrefsPrefix();

        savedHue = hue;
        savedSaturation = saturation;
        savedValue = value;

        string hueKey = GetHueKey();
        string satKey = GetSaturationKey();
        string valKey = GetValueKey();

        bool hasSavedValue = false;

        if (PlayerPrefs.HasKey(hueKey))
        {
            savedHue = PlayerPrefs.GetFloat(hueKey);
            hasSavedValue = true;
        }

        if (PlayerPrefs.HasKey(satKey))
        {
            savedSaturation = PlayerPrefs.GetFloat(satKey);
            hasSavedValue = true;
        }

        if (PlayerPrefs.HasKey(valKey))
        {
            savedValue = PlayerPrefs.GetFloat(valKey);
            hasSavedValue = true;
        }

        return hasSavedValue;
    }

    private void ApplySelectors()
    {
        if (hueSelector != null)
        {
            hueSelector.SetHue(hue);
        }

        if (svPalette != null)
        {
            svPalette.SetHue(hue);
            svPalette.SetValues(saturation, value);
        }
    }

    private void ApplyColor()
    {
        if (!TryGetTargetMaterial(out Material material))
        {
            return;
        }

        material.color = Color.HSVToRGB(hue, saturation, value);
    }

    private bool TryGetTargetMaterial(out Material material)
    {
        material = null;

        if (targetRenderer == null)
        {
            return false;
        }

        Material[] materials = targetRenderer.materials;
        if (materials == null || materialIndex < 0 || materialIndex >= materials.Length)
        {
            return false;
        }

        material = materials[materialIndex];
        return material != null;
    }

    private void EnsurePrefsPrefix()
    {
        if (string.IsNullOrEmpty(prefsKeyPrefix))
        {
            prefsKeyPrefix = "independent_mat";
        }
    }

    private void EnsureUIReferences()
    {
        if (hueSelector == null)
        {
            hueSelector = GetComponentInChildren<HueRingSelector>(includeInactive: true);
        }

        if (svPalette == null)
        {
            svPalette = GetComponentInChildren<SaturationValuePalette>(includeInactive: true);
        }

        Transform uiRoot = null;

        if (hueSelector == null || svPalette == null)
        {
            uiRoot = GetOrCreateUIRoot();
        }

        if (hueSelector == null)
        {
            hueSelector = CreateHueSelector(uiRoot);
        }

        if (svPalette == null)
        {
            svPalette = CreateSaturationValuePalette(uiRoot);
        }
    }

    private Transform GetOrCreateUIRoot()
    {
        Canvas existingCanvas = GetComponentInChildren<Canvas>(includeInactive: true);
        if (existingCanvas != null)
        {
            return existingCanvas.transform;
        }

        GameObject canvasObject = new GameObject("IndependentColorUI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        return canvasObject.transform;
    }

    private HueRingSelector CreateHueSelector(Transform parent)
    {
        GameObject hueObject = new GameObject("HueSelector", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HueRingSelector));
        hueObject.transform.SetParent(parent, false);

        RectTransform rectTransform = hueObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100f, 100f);

        Image ringImage = hueObject.GetComponent<Image>();
        ringImage.raycastTarget = true;

        HueRingSelector selector = hueObject.GetComponent<HueRingSelector>();

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(hueObject.transform, false);

        selector.GetType().GetField("handleRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(selector, handleObject.GetComponent<RectTransform>());
        selector.GetType().GetField("ringRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(selector, rectTransform);
        selector.GetType().GetField("ringGraphic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(selector, ringImage);

        return selector;
    }

    private SaturationValuePalette CreateSaturationValuePalette(Transform parent)
    {
        GameObject paletteObject = new GameObject("SaturationValuePalette", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(SaturationValuePalette));
        paletteObject.transform.SetParent(parent, false);

        RectTransform rectTransform = paletteObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150f, 150f);

        RawImage paletteImage = paletteObject.GetComponent<RawImage>();
        paletteImage.raycastTarget = true;

        SaturationValuePalette palette = paletteObject.GetComponent<SaturationValuePalette>();

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(paletteObject.transform, false);

        palette.GetType().GetField("handleRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(palette, handleObject.GetComponent<RectTransform>());
        palette.GetType().GetField("paletteRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(palette, rectTransform);
        palette.GetType().GetField("paletteImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(palette, paletteImage);

        return palette;
    }

    private string GetHueKey()
    {
        return $"{prefsKeyPrefix}_{GetTargetKey()}_h";
    }

    private string GetSaturationKey()
    {
        return $"{prefsKeyPrefix}_{GetTargetKey()}_s";
    }

    private string GetValueKey()
    {
        return $"{prefsKeyPrefix}_{GetTargetKey()}_v";
    }

    private string GetTargetKey()
    {
        if (targetRenderer == null)
        {
            return "no_target";
        }

        string rendererName = targetRenderer.gameObject != null ? targetRenderer.gameObject.name : targetRenderer.name;
        return $"{rendererName}_{materialIndex}";
    }

    private void OnValidate()
    {
        EnsurePrefsPrefix();

        hue = Mathf.Repeat(hue, 1f);
        saturation = Mathf.Clamp01(saturation);
        value = Mathf.Clamp01(value);

        if (targetRenderer != null)
        {
            int maxIndex = Mathf.Max(0, targetRenderer.materials.Length - 1);
            materialIndex = Mathf.Clamp(materialIndex, 0, maxIndex);
        }

        ApplySelectors();
        ApplyColor();
    }
}
