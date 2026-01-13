using UnityEngine;

/// <summary>
/// Overrides the Y-axis rotation for a sun light after DayNightLighting updates.
/// </summary>
[DefaultExecutionOrder(100)]
public class SunYAxisOverride : MonoBehaviour
{
    [SerializeField] private float yAxisRotation;

    void LateUpdate()
    {
        float currentX = transform.rotation.eulerAngles.x;
        float normalizedY = Mathf.Repeat(yAxisRotation, 360f);
        transform.rotation = Quaternion.Euler(currentX, normalizedY, 0f);
    }
}
