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
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine animationRoutine;
    private bool isOpen;

    private void Awake()
    {
        EnsureTarget();
        EnsureCanvasGroup();
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

        EnsureCanvasGroup();
        ApplyCanvasGroupState(opening);

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
            ApplyCanvasGroupState(opening);
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
        ApplyCanvasGroupState(opening);
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

    private void EnsureTarget()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
        {
            return;
        }

        if (target != null)
        {
            canvasGroup = target.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
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
