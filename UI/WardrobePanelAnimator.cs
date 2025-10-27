using System.Collections;
using UnityEngine;

public class WardrobePanelAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Animation")]
    [SerializeField] private Vector2 hiddenAnchoredPosition;
    [SerializeField] private Vector2 visibleAnchoredPosition;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine animationCoroutine;
    private bool isVisible;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        EnsureReferences();
    }

    public void SnapOpen()
    {
        SetPanelState(true);
    }

    public void SnapClosed()
    {
        SetPanelState(false);
    }

    public void PlayOpen()
    {
        if (isVisible)
        {
            return;
        }

        PlayAnimation(true);
    }

    public void PlayClose()
    {
        if (!isVisible)
        {
            return;
        }

        PlayAnimation(false);
    }

    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private void PlayAnimation(bool opening)
    {
        EnsureReferences();
        StopAnimation();

        if (panelRectTransform == null)
        {
            return;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        animationCoroutine = StartCoroutine(AnimatePanel(opening));
    }

    private IEnumerator AnimatePanel(bool opening)
    {
        Vector2 startPosition = opening ? hiddenAnchoredPosition : visibleAnchoredPosition;
        Vector2 endPosition = opening ? visibleAnchoredPosition : hiddenAnchoredPosition;
        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;
        AnimationCurve activeCurve = opening ? showCurve : hideCurve;
        bool hasCurve = activeCurve != null && activeCurve.length > 0;
        float duration = Mathf.Max(0f, animationDuration);

        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = startPosition;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = startAlpha;
        }

        if (duration <= Mathf.Epsilon)
        {
            CompleteAnimation(opening, endPosition, endAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float normalized = hasCurve ? activeCurve.Evaluate(t) : t;

            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, normalized);
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, normalized);
            }

            yield return null;
        }

        CompleteAnimation(opening, endPosition, endAlpha);
    }

    private void CompleteAnimation(bool opening, Vector2 endPosition, float endAlpha)
    {
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = endPosition;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = endAlpha;
            panelCanvasGroup.interactable = opening;
            panelCanvasGroup.blocksRaycasts = opening;
        }

        isVisible = opening;
        animationCoroutine = null;
    }

    private void SetPanelState(bool visible)
    {
        EnsureReferences();
        StopAnimation();

        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = visible ? visibleAnchoredPosition : hiddenAnchoredPosition;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
        }

        isVisible = visible;
    }

    private void EnsureReferences()
    {
        if (panelRectTransform == null)
        {
            panelRectTransform = GetComponent<RectTransform>();
        }

        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif
}
