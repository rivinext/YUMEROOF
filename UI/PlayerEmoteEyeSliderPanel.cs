using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages manual eye control sliders shown during player emote playback.
/// </summary>
public class PlayerEmoteEyeSliderPanel : MonoBehaviour
{
    [SerializeField] private PlayerEyeBlendShapeController eyeBlendShapeController;
    [SerializeField] private Slider leftEyeSlider;
    [SerializeField] private Slider rightEyeSlider;
    [SerializeField, Tooltip("Optional root object that is toggled alongside slider visibility.")]
    private GameObject contentRoot;
    [SerializeField, Tooltip("Optional CanvasGroup used to fade/enable the panel when manual control is active.")]
    private CanvasGroup canvasGroup;

    private bool initialized;

    private void Awake()
    {
        Initialize();
        SetEmoteModeActive(false);
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (leftEyeSlider != null)
        {
            leftEyeSlider.onValueChanged.RemoveListener(OnLeftSliderChanged);
        }

        if (rightEyeSlider != null)
        {
            rightEyeSlider.onValueChanged.RemoveListener(OnRightSliderChanged);
        }
    }

    /// <summary>
    /// Shows or hides the manual eye sliders.
    /// </summary>
    public void SetEmoteModeActive(bool active)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = active;
        }

        if (contentRoot != null)
        {
            contentRoot.SetActive(active);
        }
        else
        {
            if (leftEyeSlider != null)
            {
                leftEyeSlider.gameObject.SetActive(active);
            }

            if (rightEyeSlider != null)
            {
                rightEyeSlider.gameObject.SetActive(active);
            }
        }

        if (leftEyeSlider != null)
        {
            leftEyeSlider.interactable = active;
        }

        if (rightEyeSlider != null)
        {
            rightEyeSlider.interactable = active;
        }

        if (!active)
        {
            ResetSliders();
        }
    }

    /// <summary>
    /// Updates the slider positions to match the provided manual weights.
    /// </summary>
    public void RefreshSliderValues(float leftWeight, float rightWeight)
    {
        if (leftEyeSlider != null)
        {
            leftEyeSlider.SetValueWithoutNotify(leftWeight);
        }

        if (rightEyeSlider != null)
        {
            rightEyeSlider.SetValueWithoutNotify(rightWeight);
        }
    }

    /// <summary>
    /// Resets both slider values to zero without triggering change events.
    /// </summary>
    public void ResetSliders()
    {
        if (leftEyeSlider != null)
        {
            leftEyeSlider.SetValueWithoutNotify(0f);
        }

        if (rightEyeSlider != null)
        {
            rightEyeSlider.SetValueWithoutNotify(0f);
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (eyeBlendShapeController == null)
        {
            eyeBlendShapeController = GetComponentInParent<PlayerEyeBlendShapeController>();

            if (eyeBlendShapeController == null)
            {
                if (contentRoot != null)
                {
                    eyeBlendShapeController = contentRoot.GetComponentInChildren<PlayerEyeBlendShapeController>(true);
                }
                else if (transform.root != null)
                {
                    eyeBlendShapeController = transform.root.GetComponentInChildren<PlayerEyeBlendShapeController>(true);
                }

                if (eyeBlendShapeController == null)
                {
                    eyeBlendShapeController = FindFirstObjectByType<PlayerEyeBlendShapeController>();
                }

                if (eyeBlendShapeController == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerEmoteEyeSliderPanel)}] Could not locate {nameof(PlayerEyeBlendShapeController)} in parents, content root, scene root, or scene search. Manual eye sliders will be disabled.", this);
                }
            }
        }

        if (leftEyeSlider != null)
        {
            leftEyeSlider.onValueChanged.AddListener(OnLeftSliderChanged);
        }

        if (rightEyeSlider != null)
        {
            rightEyeSlider.onValueChanged.AddListener(OnRightSliderChanged);
        }
    }

    private void OnLeftSliderChanged(float value)
    {
        eyeBlendShapeController?.SetLeftEyeWeight(value);
    }

    private void OnRightSliderChanged(float value)
    {
        eyeBlendShapeController?.SetRightEyeWeight(value);
    }
}
