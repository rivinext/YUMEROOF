using UnityEngine;

/// <summary>
/// Updates the skybox material parameters based on the normalized time of day.
/// </summary>
public class SkyboxTimeOfDayController : MonoBehaviour
{
    [SerializeField] private string transitionPropertyName = "_CubemapTransition";
    [SerializeField] private string exposurePropertyName = "_CubemapExposure";
    [SerializeField] private AnimationCurve transitionCurve = CreateDefaultTransitionCurve();
    [SerializeField] private AnimationCurve exposureCurve = CreateDefaultExposureCurve();

    private Material skyboxInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindObjectOfType<SkyboxTimeOfDayController>() != null)
        {
            return;
        }

        var controllerObject = new GameObject("Skybox Time Of Day Controller");
        controllerObject.AddComponent<SkyboxTimeOfDayController>();
    }

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

    private static AnimationCurve CreateDefaultTransitionCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.35f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.75f, 0.35f),
            new Keyframe(1f, 0f)
        );

        SmoothCurveTangents(curve);
        return curve;
    }

    private static AnimationCurve CreateDefaultExposureCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.25f, 0.6f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.75f, 0.6f),
            new Keyframe(1f, 0.2f)
        );

        SmoothCurveTangents(curve);
        return curve;
    }

    private static void SmoothCurveTangents(AnimationCurve curve)
    {
        if (curve == null)
        {
            return;
        }

        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }
    }
}
