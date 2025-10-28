using System.Collections;
using UnityEngine;

public class PlacementScaleAnimator : MonoBehaviour
{
    [SerializeField]
    private float animationDuration = 0.5f;

    [SerializeField]
    private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(0.2f, 0.85f, 0f, 0f),
        new Keyframe(0.5f, 1.15f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f));

    private Coroutine animationRoutine;
    private Vector3 originalScale;
    private bool initialized;
    private Bounds combinedBounds;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        originalScale = transform.localScale;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }
        }
        else
        {
            combinedBounds = new Bounds(transform.position, Vector3.zero);
        }

        initialized = true;
    }

    public void Play()
    {
        EnsureInitialized();

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            ResetScale();
        }

        animationRoutine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            float normalizedTime = animationDuration > 0f ? Mathf.Clamp01(elapsed / animationDuration) : 1f;
            float multiplier = scaleCurve.Evaluate(normalizedTime);
            ApplyScale(multiplier);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetScale();
        animationRoutine = null;
    }

    private void ApplyScale(float multiplier)
    {
        transform.localScale = originalScale * multiplier;
    }

    private void ResetScale()
    {
        transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        ResetScale();
    }

    public Bounds CombinedBounds => combinedBounds;
}
