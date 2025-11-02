using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages manual mouth control sliders shown during player emote playback.
/// </summary>
public class PlayerEmoteMouthSliderPanel : MonoBehaviour
{
    [SerializeField] private PlayerMouthBlendShapeController mouthBlendShapeController;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private Slider horizontalSlider;
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
        if (verticalSlider != null)
        {
            verticalSlider.onValueChanged.RemoveListener(OnVerticalSliderChanged);
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.onValueChanged.RemoveListener(OnHorizontalSliderChanged);
        }
    }

    /// <summary>
    /// Shows or hides the manual mouth sliders.
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
            if (verticalSlider != null)
            {
                verticalSlider.gameObject.SetActive(active);
            }

            if (horizontalSlider != null)
            {
                horizontalSlider.gameObject.SetActive(active);
            }
        }

        if (verticalSlider != null)
        {
            verticalSlider.interactable = active;
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.interactable = active;
        }

        if (!active)
        {
            ResetSliders();
        }
    }

    /// <summary>
    /// Updates the slider positions to match the provided manual weights.
    /// </summary>
    public void RefreshSliderValues(float verticalWeight, float horizontalWeight)
    {
        if (verticalSlider != null)
        {
            verticalSlider.SetValueWithoutNotify(verticalWeight);
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.SetValueWithoutNotify(horizontalWeight);
        }
    }

    /// <summary>
    /// Resets both slider values to zero without triggering change events.
    /// </summary>
    public void ResetSliders()
    {
        if (verticalSlider != null)
        {
            verticalSlider.SetValueWithoutNotify(0f);
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.SetValueWithoutNotify(0f);
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (mouthBlendShapeController == null)
        {
            mouthBlendShapeController = GetComponentInParent<PlayerMouthBlendShapeController>();
        }

        if (verticalSlider != null)
        {
            verticalSlider.onValueChanged.AddListener(OnVerticalSliderChanged);
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.onValueChanged.AddListener(OnHorizontalSliderChanged);
        }
    }

    private void OnVerticalSliderChanged(float value)
    {
        mouthBlendShapeController?.SetVerticalWeight(value);
    }

    private void OnHorizontalSliderChanged(float value)
    {
        mouthBlendShapeController?.SetHorizontalWeight(value);
    }
}
