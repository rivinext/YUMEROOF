using UnityEngine;
using UnityEngine.UI;

public class SunYAxisSliderController : MonoBehaviour
{
    [SerializeField] private Slider yAxisSlider;
    [SerializeField] private SunYAxisOverride sunYAxisOverride;

    private void Awake()
    {
        ConfigureSlider();
    }

    private void OnEnable()
    {
        if (yAxisSlider != null)
        {
            yAxisSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        }

        SyncSliderWithOverride();
    }

    private void OnDisable()
    {
        if (yAxisSlider != null)
        {
            yAxisSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }
    }

    private void ConfigureSlider()
    {
        if (yAxisSlider == null)
            return;

        yAxisSlider.minValue = 0f;
        yAxisSlider.maxValue = 360f;
        yAxisSlider.wholeNumbers = false;
    }

    private void SyncSliderWithOverride()
    {
        if (yAxisSlider == null || sunYAxisOverride == null)
            return;

        float normalizedValue = Mathf.Repeat(sunYAxisOverride.YAxisRotation, 360f);
        yAxisSlider.SetValueWithoutNotify(normalizedValue);
    }

    private void HandleSliderValueChanged(float value)
    {
        if (sunYAxisOverride == null)
            return;

        sunYAxisOverride.YAxisRotation = value;
    }
}
