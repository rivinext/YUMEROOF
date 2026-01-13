using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Allows a UI slider to override the day/night lighting Y-axis rotation (sun azimuth).
/// </summary>
[RequireComponent(typeof(DayNightLighting))]
public class DayNightYAxisRotationOverride : MonoBehaviour
{
    [SerializeField] private Slider yAxisRotationSlider;
    [SerializeField] private Vector2 yAxisRange = new Vector2(0f, 360f);

    private float currentYRotation;

    void OnEnable()
    {
        if (yAxisRotationSlider != null)
        {
            yAxisRotationSlider.minValue = yAxisRange.x;
            yAxisRotationSlider.maxValue = yAxisRange.y;
            currentYRotation = transform.rotation.eulerAngles.y;
            yAxisRotationSlider.SetValueWithoutNotify(currentYRotation);
            yAxisRotationSlider.onValueChanged.AddListener(HandleSliderChanged);
        }
        else
        {
            currentYRotation = transform.rotation.eulerAngles.y;
        }
    }

    void OnDisable()
    {
        if (yAxisRotationSlider != null)
        {
            yAxisRotationSlider.onValueChanged.RemoveListener(HandleSliderChanged);
        }
    }

    void LateUpdate()
    {
        ApplyYRotation();
    }

    private void HandleSliderChanged(float value)
    {
        currentYRotation = value;
        ApplyYRotation();
    }

    private void ApplyYRotation()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        if (Mathf.Approximately(euler.y, currentYRotation))
        {
            return;
        }

        transform.rotation = Quaternion.Euler(euler.x, currentYRotation, euler.z);
    }
}
