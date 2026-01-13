using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overrides the Y axis rotation of a DayNightLighting-controlled light using a UI slider.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightLightingYAxisOverride : MonoBehaviour
{
    [SerializeField] private Slider yAxisSlider;
    [SerializeField] private Vector2 sliderRange = new Vector2(0f, 360f);

    private float sliderValue;

    private void OnEnable()
    {
        if (yAxisSlider == null)
        {
            return;
        }

        yAxisSlider.minValue = sliderRange.x;
        yAxisSlider.maxValue = sliderRange.y;
        sliderValue = yAxisSlider.value;
        yAxisSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        if (yAxisSlider == null)
        {
            return;
        }

        yAxisSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void LateUpdate()
    {
        if (yAxisSlider == null)
        {
            return;
        }

        var eulerAngles = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerAngles.x, sliderValue, eulerAngles.z);
    }

    private void OnSliderValueChanged(float value)
    {
        sliderValue = value;
    }
}
