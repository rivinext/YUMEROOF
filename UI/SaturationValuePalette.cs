using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SaturationValuePalette : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform paletteRectTransform;
    [SerializeField] private RectTransform handleRectTransform;
    [SerializeField] private RawImage paletteImage;
    [SerializeField] private int textureResolution = 64;

    [SerializeField] private string sessionKey = string.Empty;

    [SerializeField] private UnityEvent<float> onSaturationChanged = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private Texture2D paletteTexture;
    private float currentHue;
    private float saturation;
    private float value;

    // Editor quick check (prefab pivot changes):
    // 1. Open the palette prefab, adjust paletteRectTransform's pivot (e.g., bottom-left, center).
    // 2. Enter Play Mode and drag across the palette area to confirm the handle follows the pointer smoothly
    //    and hits the edges without sticking regardless of pivot settings or UI scaling.

    public string SessionKey => sessionKey;
    public UnityEvent<float> OnSaturationChanged => onSaturationChanged;
    public UnityEvent<float> OnValueChanged => onValueChanged;

    private RectTransform RectTransformCache => (RectTransform)transform;

    private void Awake()
    {
        if (paletteRectTransform == null)
        {
            paletteRectTransform = RectTransformCache;
        }
    }

    private void OnDestroy()
    {
        if (paletteTexture != null)
        {
            Destroy(paletteTexture);
        }
    }

    public void SetSessionKey(string key)
    {
        sessionKey = key ?? string.Empty;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateFromPointer(eventData);
    }

    public void SetHue(float hue)
    {
        currentHue = Mathf.Repeat(hue, 1f);
        UpdatePaletteTexture();
    }

    public void SetValues(float newSaturation, float newValue)
    {
        saturation = Mathf.Clamp01(newSaturation);
        value = Mathf.Clamp01(newValue);
        UpdateHandlePosition();
    }

    public void SetSaturation(float newSaturation)
    {
        saturation = Mathf.Clamp01(newSaturation);
        UpdateHandlePosition();
    }

    public void SetValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);
        UpdateHandlePosition();
    }

    private void UpdateFromPointer(PointerEventData eventData)
    {
        if (paletteRectTransform == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(paletteRectTransform, eventData.position,
                eventData.pressEventCamera, out var localPoint))
        {
            return;
        }

        Rect rect = paletteRectTransform.rect;
        float normalizedSaturation = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x));
        float normalizedValue = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

        bool satChanged = !Mathf.Approximately(saturation, normalizedSaturation);
        bool valChanged = !Mathf.Approximately(value, normalizedValue);

        saturation = normalizedSaturation;
        value = normalizedValue;
        UpdateHandlePosition();

        if (satChanged)
        {
            onSaturationChanged.Invoke(saturation);
        }

        if (valChanged)
        {
            onValueChanged.Invoke(value);
        }
    }

    private void UpdatePaletteTexture()
    {
        if (paletteImage == null || textureResolution <= 1)
        {
            return;
        }

        if (paletteTexture == null)
        {
            paletteTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            paletteTexture.name = "SVPalette";
            paletteImage.texture = paletteTexture;
        }

        for (int y = 0; y < textureResolution; y++)
        {
            float v = (float)y / (textureResolution - 1);
            for (int x = 0; x < textureResolution; x++)
            {
                float s = (float)x / (textureResolution - 1);
                Color color = Color.HSVToRGB(currentHue, s, v);
                paletteTexture.SetPixel(x, y, color);
            }
        }

        paletteTexture.Apply();
    }

    private void UpdateHandlePosition()
    {
        if (handleRectTransform == null || paletteRectTransform == null)
        {
            return;
        }

        Rect rect = paletteRectTransform.rect;
        float x = (saturation - 0.5f) * rect.width;
        float y = (value - 0.5f) * rect.height;
        handleRectTransform.anchoredPosition = new Vector2(x, y);
    }
}
