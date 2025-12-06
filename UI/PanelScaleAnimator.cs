using System;
using System.Collections;
using UnityEngine;

public class PanelScaleAnimator : MonoBehaviour
{
    private enum AnimationMode
    {
        Fade,
        Scale
    }

    [SerializeField] private RectTransform target;
    [SerializeField] private AnimationMode animationMode = AnimationMode.Fade;
    [SerializeField] private AnimationCurve openFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector3 closedScale = Vector3.zero;

    public Action OnOpenComplete;
    public Action OnCloseComplete;

    private Coroutine animationRoutine;
    private bool isOpen;
    private Vector3 initialScale = Vector3.one;

    private void Awake()
    {
        EnsureTarget();
        CacheInitialScale();
        EnsureCanvasGroup();
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
        StartFadeRoutine(true);
    }

    public void Close()
    {
        StartFadeRoutine(false);
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

    private void StartFadeRoutine(bool opening)
    {
        EnsureReferences();

        if (animationMode == AnimationMode.Fade && canvasGroup == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(FadeRoutine(opening));
    }

    private IEnumerator FadeRoutine(bool opening)
    {
        float duration = Mathf.Max(0f, opening ? openDuration : closeDuration);
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        Vector3 startScale = opening ? closedScale : initialScale;
        Vector3 endScale = opening ? initialScale : closedScale;

        if (duration <= 0f)
        {
            ApplyFinalState(endAlpha, endScale);
            isOpen = opening;
            ApplyCanvasGroupState(opening);
            animationRoutine = null;
            InvokeCompletion(opening);
            yield break;
        }

        float elapsed = 0f;
        ApplyInitialState(startAlpha, startScale, opening);

        while (elapsed < duration)
        {
            float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            AnimationCurve curve = opening ? openFadeCurve : closeFadeCurve;
            float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            ApplyAnimatedValue(startAlpha, endAlpha, startScale, endScale, curveValue);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyFinalState(endAlpha, endScale);
        isOpen = opening;
        ApplyCanvasGroupState(opening);
        animationRoutine = null;

        InvokeCompletion(opening);
    }

    private void ApplyInitialState(float startAlpha, Vector3 startScale, bool opening)
    {
        if (animationMode == AnimationMode.Fade)
        {
            ApplyAlpha(startAlpha);
        }
        else
        {
            ApplyScale(startScale);
        }

        if (!opening)
        {
            ApplyCanvasGroupState(false);
        }
    }

    private void ApplyAnimatedValue(float startAlpha, float endAlpha, Vector3 startScale, Vector3 endScale, float curveValue)
    {
        if (animationMode == AnimationMode.Fade)
        {
            float alphaValue = Mathf.LerpUnclamped(startAlpha, endAlpha, curveValue);
            ApplyAlpha(alphaValue);
        }
        else
        {
            Vector3 scaleValue = Vector3.LerpUnclamped(startScale, endScale, curveValue);
            ApplyScale(scaleValue);
        }
    }

    private void ApplyFinalState(float endAlpha, Vector3 endScale)
    {
        if (animationMode == AnimationMode.Fade)
        {
            ApplyAlpha(endAlpha);
        }
        else
        {
            ApplyScale(endScale);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
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
        ApplyOpenState();
    }

    public void SnapClosed()
    {
        ApplyClosedState();
    }

    private void ApplyOpenState()
    {
        if (animationMode == AnimationMode.Fade)
        {
            ApplyAlpha(1f);
        }
        else
        {
            ApplyScale(initialScale);
        }

        ApplyCanvasGroupState(true);
        isOpen = true;
    }

    private void ApplyClosedState()
    {
        if (animationMode == AnimationMode.Fade)
        {
            ApplyAlpha(0f);
        }
        else
        {
            ApplyScale(closedScale);
        }

        ApplyCanvasGroupState(false);
        isOpen = false;
    }

    private void EnsureTarget()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    private void CacheInitialScale()
    {
        if (target != null)
        {
            initialScale = target.localScale;
        }
    }

    private void EnsureReferences()
    {
        EnsureTarget();
        if (animationMode == AnimationMode.Fade)
        {
            EnsureCanvasGroup();
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
        {
            EnsureTarget();
            if (target != null)
            {
                canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }

    private void ApplyCanvasGroupState(bool opening)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = opening;
        canvasGroup.blocksRaycasts = opening;
    }

    private void InvokeCompletion(bool opening)
    {
        if (opening)
        {
            OnOpenComplete?.Invoke();
        }
        else
        {
            OnCloseComplete?.Invoke();
        }
    }
}
