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
    [SerializeField] private InitialState initialState = InitialState.Unchanged;
    [SerializeField] private bool applyInitialStateOnEnable = false;

    private Coroutine animationRoutine;
    private bool isOpen;
    private bool hasAppliedInitialState;

    public enum InitialState
    {
        Unchanged = 0,
        Closed = 1,
        Open = 2
    }

    private void Awake()
    {
        WarnIfTargetMissing();
        ApplyInitialState(forceApply: true);
    }

    private void OnEnable()
    {
        ApplyInitialState(forceApply: applyInitialStateOnEnable);
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

    public RectTransform Target => target;

    public void Open()
    {
        StartScaleRoutine(true);
    }

    public void Close()
    {
        StartScaleRoutine(false);
    }

    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
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

    public void SnapOpen()
    {
        ApplyOpenScale();
    }

    public void SnapClosed()
    {
        ApplyClosedScale();
    }

    public void SetTarget(RectTransform targetTransform)
    {
        target = targetTransform;
    }

    private void ApplyOpenScale()
    {
        if (target != null)
        {
            target.localScale = openedScale;
            isOpen = true;
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

    private void WarnIfTargetMissing()
    {
        if (target == null)
        {
            Debug.LogWarning($"PanelScaleAnimator on {name} is missing a target RectTransform reference. Assign the content container in the inspector.");
        }
    }

    private void ApplyInitialState(bool forceApply)
    {
        if (initialState == InitialState.Unchanged)
        {
            return;
        }

        if (!forceApply && hasAppliedInitialState)
        {
            return;
        }

        switch (initialState)
        {
            case InitialState.Open:
                ApplyOpenScale();
                break;
            case InitialState.Closed:
                ApplyClosedScale();
                break;
        }

        hasAppliedInitialState = true;
    }
}
