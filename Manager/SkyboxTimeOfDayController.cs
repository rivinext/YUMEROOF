using UnityEngine;

/// <summary>
/// Updates the skybox material parameters based on the normalized time of day.
/// </summary>
public class SkyboxTimeOfDayController : MonoBehaviour
{
    [SerializeField] private string transitionPropertyName = "_CubemapTransition";
    [SerializeField] private string exposurePropertyName = "_Exposure";
    [SerializeField] private AnimationCurve transitionCurve = CreateDefaultTransitionCurve();
    [SerializeField] private AnimationCurve exposureCurve = CreateDefaultExposureCurve();

    private Material skyboxInstance;
    private GameClock clock;
    private bool hasLoggedMissingTransitionProperty;
    private bool hasLoggedMissingExposureProperty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<SkyboxTimeOfDayController>() != null)
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

    private void OnEnable()
    {
        TrySubscribeToClock();

        if (clock != null)
        {
            UpdateSkybox(clock.NormalizedTime);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromClock();
    }

    private void Update()
    {
        if (clock == null)
        {
            TrySubscribeToClock();
            if (clock != null)
            {
                UpdateSkybox(clock.NormalizedTime);
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

        clock.OnTimeUpdated += UpdateSkybox;
    }

    private void UnsubscribeFromClock()
    {
        if (clock != null)
        {
            clock.OnTimeUpdated -= UpdateSkybox;
            clock = null;
        }
    }

    private void UpdateSkybox(float normalizedTime)
    {
        if (skyboxInstance == null)
        {
            return;
        }

        normalizedTime = Mathf.Repeat(normalizedTime, 1f);

        if (!string.IsNullOrEmpty(transitionPropertyName))
        {
            if (skyboxInstance.HasProperty(transitionPropertyName))
            {
                float transitionValue = transitionCurve.Evaluate(normalizedTime);
                skyboxInstance.SetFloat(transitionPropertyName, transitionValue);
            }
            else if (!hasLoggedMissingTransitionProperty)
            {
                Debug.LogWarning($"SkyboxTimeOfDayController: Skybox material is missing transition property '{transitionPropertyName}'.");
                hasLoggedMissingTransitionProperty = true;
            }
        }

        if (!string.IsNullOrEmpty(exposurePropertyName))
        {
            if (skyboxInstance.HasProperty(exposurePropertyName))
            {
                float exposureValue = exposureCurve.Evaluate(normalizedTime);
                skyboxInstance.SetFloat(exposurePropertyName, exposureValue);
            }
            else if (!hasLoggedMissingExposureProperty)
            {
                Debug.LogWarning($"SkyboxTimeOfDayController: Skybox material is missing exposure property '{exposurePropertyName}'.");
                hasLoggedMissingExposureProperty = true;
            }
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
