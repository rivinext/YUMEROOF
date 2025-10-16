using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls light color, intensity, and orientation over the course of a day using curves for each axis.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightLighting : MonoBehaviour
{
    [SerializeField] private Gradient colorOverDay;
    [SerializeField] private AnimationCurve intensityOverDay;
    [FormerlySerializedAs("elevationOverDay")]
    [SerializeField] private AnimationCurve xAxisRotationOverDay;
    [FormerlySerializedAs("azimuthOverDay")]
    [SerializeField] private AnimationCurve yAxisRotationOverDay;

    private Light targetLight;

    void Awake()
    {
        targetLight = GetComponent<Light>();
    }

    void Update()
    {
        var clock = GameClock.Instance;
        if (clock == null || targetLight == null) return;
        float t = clock.NormalizedTime;
        targetLight.color = colorOverDay.Evaluate(t);
        targetLight.intensity = intensityOverDay.Evaluate(t);
        float xRotation = xAxisRotationOverDay.Evaluate(t);
        float yRotation = yAxisRotationOverDay.Evaluate(t);
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
