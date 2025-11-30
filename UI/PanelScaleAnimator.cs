using System.Collections;
using UnityEngine;

public class PanelScaleAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private AnimationCurve openFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake()
    {
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
        EnsureCanvasGroup();

        if (canvasGroup == null)
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

        if (duration <= 0f)
        {
            ApplyAlpha(endAlpha);
            isOpen = opening;
            ApplyCanvasGroupState(opening);
            animationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        ApplyAlpha(startAlpha);
        if (!opening)
        {
            ApplyCanvasGroupState(false);
        }

        while (elapsed < duration)
        {
            float normalizedTime = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            AnimationCurve curve = opening ? openFadeCurve : closeFadeCurve;
            float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;

            float alphaValue = Mathf.LerpUnclamped(startAlpha, endAlpha, curveValue);
            ApplyAlpha(alphaValue);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyAlpha(endAlpha);
        isOpen = opening;
        ApplyCanvasGroupState(opening);
        animationRoutine = null;
    }

    private void ApplyAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
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
        ApplyAlpha(1f);
        ApplyCanvasGroupState(true);
        isOpen = true;
    }

    private void ApplyClosedState()
    {
        ApplyAlpha(0f);
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
}
