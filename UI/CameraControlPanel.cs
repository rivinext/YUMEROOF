using System.Globalization;
using TMPro;
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

    [Header("Value Labels")]
    [SerializeField] private TextMeshProUGUI fovValueText;
    [SerializeField] private TextMeshProUGUI distanceValueText;
    [SerializeField] private TextMeshProUGUI focusDistanceValueText;
    [SerializeField] private TextMeshProUGUI focalLengthValueText;
    [SerializeField] private TextMeshProUGUI apertureValueText;

    [Header("Panel Controls")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private CameraControlPanelAnimator panelAnimator;
    [SerializeField] private bool startOpen = false;

    [Header("Slider Ranges")]
    [SerializeField] private Vector2 fovRange = new Vector2(10f, 120f);
    [SerializeField] private Vector2 distanceRange = new Vector2(5f, 50f);
    [SerializeField] private Vector2 focusDistanceRange = new Vector2(0.1f, 25f);
    [SerializeField] private Vector2 focalLengthRange = new Vector2(1f, 300f);
    [SerializeField] private Vector2 apertureRange = new Vector2(1f, 32f);

    private DepthOfField depthOfField;

    private void Awake()
    {
        InitializeReferences();
        ConfigureSliders();
        ApplyCameraControllerLimits();
        CacheDepthOfField();
        InitializePanelAnimator();
    }

    private void OnEnable()
    {
        EnsurePanelAnimatorReference();
        RegisterCallbacks();
        RegisterCameraControllerEvents();
        RegisterPanelToggle();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnregisterCameraControllerEvents();
        UnregisterPanelToggle();
        UnregisterCallbacks();
    }

    private void InitializePanelAnimator()
    {
        EnsurePanelAnimatorReference();

        if (panelAnimator == null)
        {
            return;
        }

        if (startOpen)
        {
            panelAnimator.SnapOpen();
        }
        else
        {
            panelAnimator.SnapClosed();
        }
    }

    private void EnsurePanelAnimatorReference()
    {
        if (panelAnimator == null)
        {
            panelAnimator = GetComponent<CameraControlPanelAnimator>();
        }
    }

    private void RegisterPanelToggle()
    {
        if (toggleButton == null || panelAnimator == null)
        {
            return;
        }

        toggleButton.onClick.AddListener(panelAnimator.TogglePanel);
    }

    private void UnregisterPanelToggle()
    {
        if (toggleButton == null || panelAnimator == null)
        {
            return;
        }

        toggleButton.onClick.RemoveListener(panelAnimator.TogglePanel);
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

    private void ApplyCameraControllerLimits()
    {
        if (cameraController == null)
        {
            return;
        }

        if (fovSlider != null)
        {
            cameraController.SetFieldOfViewLimits(fovSlider.minValue, fovSlider.maxValue);
        }

        if (distanceSlider != null)
        {
            cameraController.SetDistanceLimits(distanceSlider.minValue, distanceSlider.maxValue);
        }
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
    }

    private void RegisterCameraControllerEvents()
    {
        UnregisterCameraControllerEvents();

        if (cameraController == null)
        {
            return;
        }

        cameraController.FieldOfViewChanged += HandleCameraControllerFieldOfViewChanged;
        cameraController.DistanceChanged += HandleCameraControllerDistanceChanged;

        ApplyCameraControllerLimits();
    }

    private void UnregisterCameraControllerEvents()
    {
        if (cameraController == null)
        {
            return;
        }

        cameraController.FieldOfViewChanged -= HandleCameraControllerFieldOfViewChanged;
        cameraController.DistanceChanged -= HandleCameraControllerDistanceChanged;
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
        RefreshSliderValueTexts();
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
        UpdateSliderValueText(focusDistanceValueText, focusDistanceSlider);
        UpdateSliderValueText(focalLengthValueText, focalLengthSlider);
        UpdateSliderValueText(apertureValueText, apertureSlider);
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

        UpdateSliderValueText(fovValueText, fovSlider, clampedValue);
    }

    private void HandleDistanceChanged(float value)
    {
        if (cameraController == null)
        {
            return;
        }

        float clampedValue = distanceSlider != null ? Mathf.Clamp(value, distanceSlider.minValue, distanceSlider.maxValue) : Mathf.Max(value, 0f);
        cameraController.SetDistance(clampedValue, true);
        UpdateSliderValueText(distanceValueText, distanceSlider, clampedValue);
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
        RefreshSliderValueTexts();
    }

    private void HandleFocusDistanceChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = focusDistanceSlider != null ? Mathf.Clamp(value, focusDistanceSlider.minValue, focusDistanceSlider.maxValue) : value;
        depthOfField.focusDistance.value = clampedValue;
        UpdateSliderValueText(focusDistanceValueText, focusDistanceSlider, clampedValue);
    }

    private void HandleFocalLengthChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = focalLengthSlider != null ? Mathf.Clamp(value, focalLengthSlider.minValue, focalLengthSlider.maxValue) : value;
        depthOfField.focalLength.value = clampedValue;
        UpdateSliderValueText(focalLengthValueText, focalLengthSlider, clampedValue);
    }

    private void HandleApertureChanged(float value)
    {
        if (!EnsureDepthOfFieldReference())
        {
            return;
        }

        float clampedValue = apertureSlider != null ? Mathf.Clamp(value, apertureSlider.minValue, apertureSlider.maxValue) : value;
        depthOfField.aperture.value = clampedValue;
        UpdateSliderValueText(apertureValueText, apertureSlider, clampedValue);
    }

    private bool EnsureDepthOfFieldReference()
    {
        if (depthOfField == null)
        {
            CacheDepthOfField();
        }

        return depthOfField != null;
    }

    private void RefreshSliderValueTexts()
    {
        UpdateSliderValueText(fovValueText, fovSlider);
        UpdateSliderValueText(distanceValueText, distanceSlider);
        UpdateSliderValueText(focusDistanceValueText, focusDistanceSlider);
        UpdateSliderValueText(focalLengthValueText, focalLengthSlider);
        UpdateSliderValueText(apertureValueText, apertureSlider);
    }

    private void HandleCameraControllerFieldOfViewChanged(float value)
    {
        if (fovSlider == null)
        {
            return;
        }

        float clampedValue = Mathf.Clamp(value, fovSlider.minValue, fovSlider.maxValue);
        fovSlider.SetValueWithoutNotify(clampedValue);
        UpdateSliderValueText(fovValueText, fovSlider, clampedValue);
    }

    private void HandleCameraControllerDistanceChanged(float value)
    {
        if (distanceSlider == null)
        {
            return;
        }

        float clampedValue = Mathf.Clamp(value, distanceSlider.minValue, distanceSlider.maxValue);
        distanceSlider.SetValueWithoutNotify(clampedValue);
        UpdateSliderValueText(distanceValueText, distanceSlider, clampedValue);
    }

    private void UpdateSliderValueText(TextMeshProUGUI textField, Slider slider)
    {
        if (textField == null || slider == null)
        {
            return;
        }

        UpdateSliderValueText(textField, slider, slider.value);
    }

    private void UpdateSliderValueText(TextMeshProUGUI textField, Slider slider, float value)
    {
        if (textField == null)
        {
            return;
        }

        bool useWholeNumbers = slider != null && slider.wholeNumbers;
        textField.text = FormatSliderValue(value, useWholeNumbers);
    }

    private string FormatSliderValue(float value, bool useWholeNumbers)
    {
        if (useWholeNumbers)
        {
            return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
