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
    [SerializeField] public bool allowXAxisOverride;
    [SerializeField] public bool allowYAxisOverride;

    private Light targetLight;
    private GameClock clock;

    void Awake()
    {
        targetLight = GetComponent<Light>();
    }

    void OnEnable()
    {
        TrySubscribeToClock();

        if (clock != null)
        {
            UpdateLighting(clock.NormalizedTime);
        }
    }

    void OnDisable()
    {
        UnsubscribeFromClock();
    }

    void Update()
    {
        if (clock == null)
        {
            TrySubscribeToClock();
            if (clock != null)
            {
                UpdateLighting(clock.NormalizedTime);
            }
        }
    }

    private void TrySubscribeToClock()
    {
        if (clock != null)
        {
            return;
        }

        clock = GameClock.Instance;
        if (clock == null)
        {
            return;
        }

        clock.OnTimeUpdated += UpdateLighting;
    }

    private void UnsubscribeFromClock()
    {
        if (clock != null)
        {
            clock.OnTimeUpdated -= UpdateLighting;
            clock = null;
        }
    }

    private void UpdateLighting(float t)
    {
        if (targetLight == null)
        {
            return;
        }

        targetLight.color = colorOverDay.Evaluate(t);
        targetLight.intensity = intensityOverDay.Evaluate(t);
        Vector3 currentEuler = transform.rotation.eulerAngles;
        float xRotation = currentEuler.x;
        float yRotation = currentEuler.y;
        if (!allowXAxisOverride)
        {
            xRotation = xAxisRotationOverDay.Evaluate(t);
        }
        if (!allowYAxisOverride)
        {
            yRotation = yAxisRotationOverDay.Evaluate(t);
        }
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
