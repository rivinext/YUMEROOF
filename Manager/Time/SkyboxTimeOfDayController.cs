using UnityEngine;

/// <summary>
/// Updates the skybox material parameters based on the normalized time of day.
/// </summary>
public class SkyboxTimeOfDayController : MonoBehaviour
{
    [SerializeField] private string transitionPropertyName = "_CubemapTransition";
    [SerializeField] private string exposurePropertyName = "_CubemapExposure";
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve exposureCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private Material skyboxInstance;

    private void Awake()
    {
        if (RenderSettings.skybox == null)
        {
            Debug.LogWarning("SkyboxTimeOfDayController: RenderSettings.skybox is null. Controller disabled.");
            enabled = false;
            return;
        }

        skyboxInstance = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyboxInstance;
    }

    private void Update()
    {
        if (skyboxInstance == null || GameClock.Instance == null)
        {
            return;
        }

        float normalizedTime = Mathf.Repeat(GameClock.Instance.NormalizedTime, 1f);

        if (!string.IsNullOrEmpty(transitionPropertyName))
        {
            float transitionValue = transitionCurve.Evaluate(normalizedTime);
            skyboxInstance.SetFloat(transitionPropertyName, transitionValue);
        }

        if (!string.IsNullOrEmpty(exposurePropertyName))
        {
            float exposureValue = exposureCurve.Evaluate(normalizedTime);
            skyboxInstance.SetFloat(exposurePropertyName, exposureValue);
        }
    }
}
