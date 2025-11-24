using System.Collections;
using UnityEngine;

public class PanelScaleAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Vector3 closedScale = Vector3.zero;
    [SerializeField] private Vector3 openedScale = Vector3.one;

    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake()
    {
        EnsureTarget();
        ApplyClosedScale();
    }

    private void OnEnable()
    {
        ApplyClosedScale();
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    public bool IsOpen => isOpen;

    public void Open()
    {
        StartScaleRoutine(true);
    }

    public void Close()
    {
        StartScaleRoutine(false);
    }

    private void StartScaleRoutine(bool opening)
    {
        if (target == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(ScaleRoutine(opening));
    }

    private IEnumerator ScaleRoutine(bool opening)
    {
        float duration = Mathf.Max(0f, opening ? openDuration : closeDuration);
        Vector3 startScale = opening ? closedScale : openedScale;
        Vector3 endScale = opening ? openedScale : closedScale;

        if (duration <= 0f)
        {
            ApplyScale(endScale);
            isOpen = opening;
            animationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float curveValue = scaleCurve != null ? scaleCurve.Evaluate(normalizedTime) : normalizedTime;

            Vector3 scaledValue = new Vector3(
                Mathf.LerpUnclamped(startScale.x, endScale.x, curveValue),
                Mathf.LerpUnclamped(startScale.y, endScale.y, curveValue),
                Mathf.LerpUnclamped(startScale.z, endScale.z, curveValue));

            ApplyScale(scaledValue);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyScale(endScale);
        isOpen = opening;
        animationRoutine = null;
    }

    private void ApplyScale(Vector3 scale)
    {
        if (target != null)
        {
            target.localScale = scale;
        }
    }

    private void ApplyClosedScale()
    {
        if (target != null)
        {
            target.localScale = closedScale;
            isOpen = false;
        }
    }

    private void EnsureTarget()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }
}
