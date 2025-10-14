using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraControlPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private OrthographicCameraController cameraController;
    [SerializeField] private Volume globalVolume;

    [Header("UI Elements")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private Slider distanceSlider;
    [SerializeField] private Toggle depthOfFieldToggle;
    [SerializeField] private Slider focusDistanceSlider;
    [SerializeField] private Slider focalLengthSlider;
    [SerializeField] private Slider apertureSlider;
    [SerializeField] private Button toggleButton;

    [Header("Panel Animation")]
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private bool openOnStart = false;
    [SerializeField] private float closedPositionX = 1030f;
    [SerializeField] private float openPositionX = 0f;
    [SerializeField] private float anchoredY = 0f;
    [SerializeField] private AnimationCurve slideInXCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve slideOutXCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Slider Ranges")]
    [SerializeField] private Vector2 fovRange = new Vector2(10f, 120f);
    [SerializeField] private Vector2 distanceRange = new Vector2(5f, 50f);
    [SerializeField] private Vector2 focusDistanceRange = new Vector2(0.1f, 25f);
    [SerializeField] private Vector2 focalLengthRange = new Vector2(1f, 300f);
    [SerializeField] private Vector2 apertureRange = new Vector2(1f, 32f);

    private DepthOfField depthOfField;
    private bool isPanelOpen;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        InitializePanelReferences();
        InitializeReferences();
        ConfigureSliders();
        CacheDepthOfField();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
        RefreshUI();
        SnapPanelToState();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
    }

    private void InitializePanelReferences()
    {
        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        isPanelOpen = openOnStart;
        SnapPanelToState();
    }

    private void InitializeReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (cameraController == null)
        {
            if (targetCamera != null)
            {
                cameraController = targetCamera.GetComponent<OrthographicCameraController>();
            }

            if (cameraController == null)
            {
                cameraController = FindObjectOfType<OrthographicCameraController>();
            }
        }

        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }
    }

    private void CacheDepthOfField()
    {
        depthOfField = null;

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out depthOfField);
        }
    }

    private void ConfigureSliders()
    {
        ConfigureSliderRange(fovSlider, fovRange);
        ConfigureSliderRange(distanceSlider, distanceRange);
        ConfigureSliderRange(focusDistanceSlider, focusDistanceRange);
        ConfigureSliderRange(focalLengthSlider, focalLengthRange);
        ConfigureSliderRange(apertureSlider, apertureRange);
    }

    private void ConfigureSliderRange(Slider slider, Vector2 range)
    {
        if (slider == null)
        {
            return;
        }

        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        slider.minValue = min;
        slider.maxValue = max;
    }

    private void RegisterCallbacks()
    {
        UnregisterCallbacks();

        if (fovSlider != null)
        {
            fovSlider.onValueChanged.AddListener(HandleFieldOfViewChanged);
        }

        if (distanceSlider != null)
        {
            distanceSlider.onValueChanged.AddListener(HandleDistanceChanged);
        }

        if (depthOfFieldToggle != null)
        {
            depthOfFieldToggle.onValueChanged.AddListener(HandleDepthOfFieldToggled);
        }

        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.onValueChanged.AddListener(HandleFocusDistanceChanged);
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.onValueChanged.AddListener(HandleFocalLengthChanged);
        }

        if (apertureSlider != null)
        {
            apertureSlider.onValueChanged.AddListener(HandleApertureChanged);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(HandleToggleButtonClicked);
        }
    }

    private void UnregisterCallbacks()
    {
        if (fovSlider != null)
        {
            fovSlider.onValueChanged.RemoveListener(HandleFieldOfViewChanged);
        }

        if (distanceSlider != null)
        {
            distanceSlider.onValueChanged.RemoveListener(HandleDistanceChanged);
        }

        if (depthOfFieldToggle != null)
        {
            depthOfFieldToggle.onValueChanged.RemoveListener(HandleDepthOfFieldToggled);
        }

        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.onValueChanged.RemoveListener(HandleFocusDistanceChanged);
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.onValueChanged.RemoveListener(HandleFocalLengthChanged);
        }

        if (apertureSlider != null)
        {
            apertureSlider.onValueChanged.RemoveListener(HandleApertureChanged);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(HandleToggleButtonClicked);
        }
    }

    private void RefreshUI()
    {
        if (cameraController != null && targetCamera == null)
        {
            targetCamera = cameraController.GetComponent<Camera>();
        }

        if (fovSlider != null)
        {
            float currentFov = targetCamera != null ? targetCamera.fieldOfView : (cameraController != null ? cameraController.CurrentFieldOfView : fovSlider.minValue);
            fovSlider.SetValueWithoutNotify(Mathf.Clamp(currentFov, fovSlider.minValue, fovSlider.maxValue));
        }

        if (distanceSlider != null && cameraController != null)
        {
            float currentDistance = Mathf.Clamp(cameraController.defaultDistance, distanceSlider.minValue, distanceSlider.maxValue);
            distanceSlider.SetValueWithoutNotify(currentDistance);
        }

        CacheDepthOfField();
        bool hasDepthOfField = depthOfField != null;
        bool isDepthOfFieldActive = hasDepthOfField && depthOfField.active;

        if (depthOfFieldToggle != null)
        {
            depthOfFieldToggle.interactable = hasDepthOfField;
            depthOfFieldToggle.SetIsOnWithoutNotify(isDepthOfFieldActive);
        }

        UpdateDepthOfFieldSliders();
        SetDepthOfFieldSliderInteractable(hasDepthOfField && isDepthOfFieldActive);
    }

    private void UpdateDepthOfFieldSliders()
    {
        if (depthOfField == null)
        {
            SetDepthOfFieldSliderInteractable(false);
            return;
        }

        SetSliderValueWithoutNotify(focusDistanceSlider, depthOfField.focusDistance.value);
        SetSliderValueWithoutNotify(focalLengthSlider, depthOfField.focalLength.value);
        SetSliderValueWithoutNotify(apertureSlider, depthOfField.aperture.value);
    }

    private void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        float clamped = Mathf.Clamp(value, slider.minValue, slider.maxValue);
        slider.SetValueWithoutNotify(clamped);
    }

    private void SetDepthOfFieldSliderInteractable(bool interactable)
    {
        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.interactable = interactable;
        }

        if (focalLengthSlider != null)
        {
            focalLengthSlider.interactable = interactable;
        }

        if (apertureSlider != null)
        {
            apertureSlider.interactable = interactable;
        }
    }

    private void HandleFieldOfViewChanged(float value)
    {
        float clampedValue = fovSlider != null ? Mathf.Clamp(value, fovSlider.minValue, fovSlider.maxValue) : value;

        if (cameraController != null)
        {
            cameraController.SetFieldOfView(clampedValue, true);
        }
        else if (targetCamera != null)
        {
            targetCamera.fieldOfView = clampedValue;
        }
    }

    private void HandleDistanceChanged(float value)
    {
        if (cameraController == null)
        {
            return;
        }

        float clampedValue = distanceSlider != null ? Mathf.Clamp(value, distanceSlider.minValue, distanceSlider.maxValue) : Mathf.Max(value, 0f);
        cameraController.SetDistance(clampedValue, true);
    }

    private void HandleDepthOfFieldToggled(bool isOn)
    {
        EnsureDepthOfFieldReference();

        if (depthOfField != null)
        {
            depthOfField.active = isOn;
            UpdateDepthOfFieldSliders();
        }

        SetDepthOfFieldSliderInteractable(isOn && depthOfField != null);
    }

    private void HandleFocusDistanceChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = focusDistanceSlider != null ? Mathf.Clamp(value, focusDistanceSlider.minValue, focusDistanceSlider.maxValue) : value;
        depthOfField.focusDistance.value = clampedValue;
    }

    private void HandleFocalLengthChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = focalLengthSlider != null ? Mathf.Clamp(value, focalLengthSlider.minValue, focalLengthSlider.maxValue) : value;
        depthOfField.focalLength.value = clampedValue;
    }

    private void HandleApertureChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = apertureSlider != null ? Mathf.Clamp(value, apertureSlider.minValue, apertureSlider.maxValue) : value;
        depthOfField.aperture.value = clampedValue;
    }

    private bool EnsureDepthOfFieldReference()
    {
        if (depthOfField == null)
        {
            CacheDepthOfField();
        }

        return depthOfField != null;
    }

    private void HandleToggleButtonClicked()
    {
        TogglePanel(!isPanelOpen);
    }

    private void TogglePanel(bool open)
    {
        if (panelRectTransform == null)
        {
            return;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        isPanelOpen = open;
        slideCoroutine = StartCoroutine(SlidePanelCoroutine(open));
    }

    private IEnumerator SlidePanelCoroutine(bool open)
    {
        if (panelRectTransform == null)
        {
            yield break;
        }

        AnimationCurve activeCurve = open ? slideInXCurve : slideOutXCurve;
        float targetX = open ? openPositionX : closedPositionX;
        float anchoredYValue = anchoredY;

        if (!TryGetSlideParameters(activeCurve, out float duration))
        {
            panelRectTransform.anchoredPosition = new Vector2(targetX, anchoredYValue);
            slideCoroutine = null;
            yield break;
        }

        float endTime = open ? duration : 0f;
        float direction = open ? 1f : -1f;
        float currentTime = Mathf.Clamp(
            GetTimeForPosition(panelRectTransform.anchoredPosition.x, closedPositionX, openPositionX, activeCurve, duration),
            0f,
            duration);

        panelRectTransform.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(currentTime)),
            anchoredYValue);

        while ((direction > 0f && currentTime < endTime) || (direction < 0f && currentTime > endTime))
        {
            currentTime += Time.unscaledDeltaTime * direction;
            currentTime = Mathf.Clamp(currentTime, 0f, duration);

            float normalized = activeCurve.Evaluate(currentTime);
            float lerpedX = Mathf.LerpUnclamped(closedPositionX, openPositionX, normalized);
            panelRectTransform.anchoredPosition = new Vector2(lerpedX, anchoredYValue);
            yield return null;
        }

        panelRectTransform.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(endTime)),
            anchoredYValue);
        slideCoroutine = null;
    }

    private void SnapPanelToState()
    {
        if (panelRectTransform == null)
        {
            return;
        }

        float targetX = isPanelOpen ? openPositionX : closedPositionX;
        panelRectTransform.anchoredPosition = new Vector2(targetX, anchoredY);
    }

    private bool TryGetSlideParameters(AnimationCurve curve, out float duration)
    {
        duration = 0f;

        if (curve == null)
        {
            return false;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return false;
        }

        duration = curve.keys[keyCount - 1].time;
        if (duration <= 0f)
        {
            return false;
        }

        return keyCount >= 2;
    }

    private float GetTimeForPosition(float positionX, float closedX, float openX, AnimationCurve curve, float duration)
    {
        if (curve == null)
        {
            return 0f;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return 0f;
        }

        Keyframe[] keys = curve.keys;
        if (keyCount == 1)
        {
            return keys[0].time;
        }

        float denominator = openX - closedX;
        float normalized = denominator != 0f ? (positionX - closedX) / denominator : 0f;

        for (int i = 0; i < keyCount - 1; i++)
        {
            float startValue = keys[i].value;
            float endValue = keys[i + 1].value;

            if (Mathf.Approximately(startValue, endValue))
            {
                if (Mathf.Approximately(normalized, startValue))
                {
                    return Mathf.Lerp(keys[i].time, keys[i + 1].time, 0.5f);
                }

                continue;
            }

            bool between = (normalized >= startValue && normalized <= endValue) ||
                           (normalized <= startValue && normalized >= endValue);
            if (between)
            {
                float segmentProgress = (normalized - startValue) / (endValue - startValue);
                return Mathf.Lerp(keys[i].time, keys[i + 1].time, segmentProgress);
            }
        }

        float startValueAtZero = curve.Evaluate(0f);
        float endValueAtDuration = curve.Evaluate(duration);
        return Mathf.Abs(normalized - startValueAtZero) <= Mathf.Abs(normalized - endValueAtDuration) ? 0f : duration;
    }
}
