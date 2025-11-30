using System.Collections;
using UnityEngine;

public class PanelScaleAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform backgroundTarget;
    [SerializeField] private RectTransform contentTarget;
    [SerializeField] private AnimationCurve openScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve openFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Vector3 closedScale = Vector3.zero;
    [SerializeField] private Vector3 openedScale = Vector3.one;
    [SerializeField] private CanvasGroup contentCanvasGroup;

    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake()
    {
        EnsureTargets();
        EnsureContentCanvasGroup();
        ApplyClosedState();
    }

    private void OnEnable()
    {
        ApplyClosedState();
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
        StartAnimationRoutine(true);
    }

    public void Close()
    {
        StartAnimationRoutine(false);
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

    private void StartAnimationRoutine(bool opening)
    {
        EnsureTargets();
        EnsureContentCanvasGroup();

        if (contentCanvasGroup == null && backgroundTarget == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(Animate(opening));
    }

    private IEnumerator Animate(bool opening)
    {
        float duration = Mathf.Max(0f, opening ? openDuration : closeDuration);
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        Vector3 startScale = opening ? closedScale : openedScale;
        Vector3 endScale = opening ? openedScale : closedScale;

        if (duration <= 0f)
        {
            ApplyBackgroundScale(endScale);
            ApplyContentAlpha(endAlpha);
            isOpen = opening;
            ApplyCanvasGroupState(opening);
            animationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        ApplyBackgroundScale(startScale);
        ApplyContentAlpha(startAlpha);
        if (!opening)
        {
            ApplyCanvasGroupState(false);
        }

        while (elapsed < duration)
        {
            float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            AnimationCurve scaleCurve = opening ? openScaleCurve : closeScaleCurve;
            AnimationCurve fadeCurve = opening ? openFadeCurve : closeFadeCurve;
            float scaleCurveValue = scaleCurve != null ? scaleCurve.Evaluate(normalizedTime) : normalizedTime;
            float fadeCurveValue = fadeCurve != null ? fadeCurve.Evaluate(normalizedTime) : normalizedTime;

            Vector3 scaledValue = new Vector3(
                Mathf.LerpUnclamped(startScale.x, endScale.x, scaleCurveValue),
                Mathf.LerpUnclamped(startScale.y, endScale.y, scaleCurveValue),
                Mathf.LerpUnclamped(startScale.z, endScale.z, scaleCurveValue));
            float alphaValue = Mathf.LerpUnclamped(startAlpha, endAlpha, fadeCurveValue);

            ApplyBackgroundScale(scaledValue);
            ApplyContentAlpha(alphaValue);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyBackgroundScale(endScale);
        ApplyContentAlpha(endAlpha);
        isOpen = opening;
        ApplyCanvasGroupState(opening);
        animationRoutine = null;
    }

    private void ApplyBackgroundScale(Vector3 scale)
    {
        if (backgroundTarget != null)
        {
            backgroundTarget.localScale = scale;
        }
    }

    private void ApplyContentAlpha(float alpha)
    {
        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    public void SnapOpen()
    {
        ApplyOpenState();
    }

    public void SnapClosed()
    {
        ApplyClosedState();
    }

    private void ApplyOpenState()
    {
        ApplyBackgroundScale(openedScale);
        ApplyContentAlpha(1f);
        ApplyCanvasGroupState(true);
        isOpen = true;
    }

    private void ApplyClosedState()
    {
        ApplyBackgroundScale(closedScale);
        ApplyContentAlpha(0f);
        ApplyCanvasGroupState(false);
        isOpen = false;
    }

    private void EnsureTargets()
    {
        if (backgroundTarget == null)
        {
            backgroundTarget = GetComponent<RectTransform>();
        }

        if (contentTarget == null)
        {
            contentTarget = transform as RectTransform;
        }
    }

    private void EnsureContentCanvasGroup()
    {
        if (contentCanvasGroup != null)
        {
            return;
        }

        if (contentTarget != null)
        {
            contentCanvasGroup = contentTarget.GetComponent<CanvasGroup>();
            if (contentCanvasGroup == null)
            {
                contentCanvasGroup = contentTarget.gameObject.AddComponent<CanvasGroup>();
            }
        }
        else
        {
            contentCanvasGroup = GetComponent<CanvasGroup>();
            if (contentCanvasGroup == null)
            {
                contentCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void ApplyCanvasGroupState(bool opening)
    {
        if (contentCanvasGroup == null)
        {
            return;
        }

        contentCanvasGroup.interactable = opening;
        contentCanvasGroup.blocksRaycasts = opening;
    }
}
