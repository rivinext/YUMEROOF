using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class TimeSliderController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private GameClock clock;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TMP_Text timeText;

    private bool _isDragging;
    private float _cachedTimeScale = 1f;
    private bool _createdUI;

    private void Awake()
    {
        EnsureUIBuilt();
        ConfigureSliderRange();
    }

    private void OnEnable()
    {
        EnsureClockReference();
        SubscribeEvents();
        SyncWithClock();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        RestoreTimeScaleIfNeeded();
    }

    private void EnsureClockReference()
    {
        if (clock != null)
            return;

        clock = GameClock.Instance ?? FindFirstObjectByType<GameClock>(FindObjectsInactive.Include);
    }

    private void SubscribeEvents()
    {
        if (timeSlider != null)
        {
            timeSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        }

        if (clock != null)
        {
            clock.OnTimeUpdated += HandleClockTimeUpdated;
        }
    }

    private void UnsubscribeEvents()
    {
        if (timeSlider != null)
        {
            timeSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }

        if (clock != null)
        {
            clock.OnTimeUpdated -= HandleClockTimeUpdated;
        }
    }

    private void HandleSliderValueChanged(float value)
    {
        if (clock == null)
            return;

        clock.SetTime(Mathf.RoundToInt(value));
        UpdateTimeText();
    }

    private void HandleClockTimeUpdated(float normalizedTime)
    {
        if (clock == null || timeSlider == null || _isDragging)
            return;

        timeSlider.SetValueWithoutNotify(normalizedTime * 1440f);
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (timeText == null || clock == null)
            return;

        timeText.text = clock.GetFormattedTime();
    }

    private void SyncWithClock()
    {
        if (clock == null || timeSlider == null)
            return;

        timeSlider.SetValueWithoutNotify(clock.currentMinutes);
        UpdateTimeText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        BeginDrag();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndDrag();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag();
    }

    private void BeginDrag()
    {
        if (_isDragging || clock == null)
            return;

        _isDragging = true;
        _cachedTimeScale = clock.CurrentScale;
        clock.SetTimeScale(0f);
    }

    private void EndDrag()
    {
        if (!_isDragging || clock == null)
            return;

        _isDragging = false;
        clock.SetTimeScale(_cachedTimeScale);
        UpdateTimeText();
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (_isDragging && clock != null)
        {
            clock.SetTimeScale(_cachedTimeScale);
            _isDragging = false;
        }
    }

    private void ConfigureSliderRange()
    {
        if (timeSlider == null)
            return;

        timeSlider.minValue = 0f;
        timeSlider.maxValue = 1440f;
    }

    private void EnsureUIBuilt()
    {
        if (timeSlider != null && timeText != null)
            return;

        var canvasGameObject = new GameObject("TimeSliderCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGameObject.transform.SetParent(transform, false);
        _createdUI = true;

        var canvas = canvasGameObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGameObject.layer = LayerMask.NameToLayer("UI");

        var scaler = canvasGameObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("TimeSliderPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGameObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(600f, 120f);
        panelRect.anchoredPosition = new Vector2(0f, 60f);

        timeSlider = CreateSlider(panel.transform);
        timeText = CreateTimeText(panel.transform);
    }

    private Slider CreateSlider(Transform parent)
    {
        var sliderGO = new GameObject("TimeSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);

        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(0f, 30f);

        var backgroundImage = sliderGO.GetComponent<Image>();
        backgroundImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        backgroundImage.type = Image.Type.Sliced;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.GetComponent<Image>();
        fillImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;

        var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderGO.transform, false);
        var handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleSlideArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 20f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.type = Image.Type.Sliced;

        var slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.value = 0f;

        return slider;
    }

    private TMP_Text CreateTimeText(Transform parent)
    {
        var textGO = new GameObject("TimeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent, false);

        var rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 10f);
        rectTransform.sizeDelta = new Vector2(300f, 40f);

        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.text = "00:00";

        return text;
    }
}
