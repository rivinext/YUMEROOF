using System.Collections;
using UnityEngine;

public class InventoryPanelAnimator : MonoBehaviour
{
    private RectTransform tabContainerRect;
    private CanvasGroup tabCanvasGroup;
    private float closedPositionX;
    private float openPositionX;
    private float anchoredY;
    private float slideDuration;
    private AnimationCurve slideInCurve;
    private AnimationCurve slideOutCurve;
    private Coroutine slideCoroutine;

    public void Initialize(
        RectTransform rectTransform,
        CanvasGroup canvasGroup,
        float closedX,
        float openX,
        float anchoredYValue,
        float duration,
        AnimationCurve inCurve,
        AnimationCurve outCurve)
    {
        tabContainerRect = rectTransform;
        tabCanvasGroup = canvasGroup;
        closedPositionX = closedX;
        openPositionX = openX;
        anchoredY = anchoredYValue;
        slideDuration = duration;
        slideInCurve = inCurve;
        slideOutCurve = outCurve;
    }

    public void SnapToInitialPosition()
    {
        if (tabContainerRect == null)
        {
            return;
        }

        AnimationCurve initialCurve = slideOutCurve != null ? slideOutCurve : slideInCurve;
        float startNormalized = initialCurve != null && initialCurve.length > 0
            ? initialCurve.Evaluate(0f)
            : 0f;
        float resolvedY = ResolveAnchoredY();
        float startX = Mathf.LerpUnclamped(closedPositionX, openPositionX, startNormalized);
        tabContainerRect.anchoredPosition = new Vector2(startX, resolvedY);
    }

    public void PlayOpen()
    {
        StopRunningAnimation();

        if (tabContainerRect == null)
        {
            return;
        }

        float resolvedY = ResolveAnchoredY();
        float startNormalized = slideInCurve != null && slideInCurve.length > 0
            ? slideInCurve.Evaluate(0f)
            : 0f;
        float startX = Mathf.LerpUnclamped(closedPositionX, openPositionX, startNormalized);
        tabContainerRect.anchoredPosition = new Vector2(startX, resolvedY);

        slideCoroutine = StartCoroutine(SlideTabContainer(true));
    }

    public void PlayClose()
    {
        StopRunningAnimation();

        if (tabContainerRect == null)
        {
            return;
        }

        float resolvedY = ResolveAnchoredY();
        AnimationCurve closingCurve = slideOutCurve != null ? slideOutCurve : slideInCurve;
        if (TryGetSlideParameters(closingCurve, out float durationValue) && closingCurve != null)
        {
            float startNormalized = closingCurve.Evaluate(durationValue);
            float startX = Mathf.LerpUnclamped(closedPositionX, openPositionX, startNormalized);
            tabContainerRect.anchoredPosition = new Vector2(startX, resolvedY);
        }
        else
        {
            tabContainerRect.anchoredPosition = new Vector2(openPositionX, resolvedY);
        }

        slideCoroutine = StartCoroutine(SlideTabContainer(false));
    }

    public void StopRunningAnimation()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
    }

    private IEnumerator SlideTabContainer(bool slideIn)
    {
        if (tabContainerRect == null)
        {
            slideCoroutine = null;
            yield break;
        }

        if (tabCanvasGroup != null)
        {
            tabCanvasGroup.blocksRaycasts = false;
            tabCanvasGroup.interactable = false;
        }

        float anchoredYValue = ResolveAnchoredY();
        AnimationCurve activeCurve = GetSlideCurve(slideIn);
        bool hasCurve = TryGetSlideParameters(activeCurve, out float duration);
        float endTime = slideIn ? duration : 0f;
        float targetX = slideIn ? openPositionX : closedPositionX;

        if (!hasCurve)
        {
            tabContainerRect.anchoredPosition = new Vector2(targetX, anchoredYValue);
            if (slideIn && tabCanvasGroup != null)
            {
                tabCanvasGroup.blocksRaycasts = true;
                tabCanvasGroup.interactable = true;
            }
            slideCoroutine = null;
            yield break;
        }

        float currentTime = Mathf.Clamp(
            GetTimeForPosition(tabContainerRect.anchoredPosition.x, duration, closedPositionX, openPositionX, activeCurve),
            0f,
            duration);
        float direction = slideIn ? 1f : -1f;

        tabContainerRect.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(currentTime)),
            anchoredYValue);

        while ((direction > 0f && currentTime < endTime) || (direction < 0f && currentTime > endTime))
        {
            currentTime += Time.unscaledDeltaTime * direction;
            currentTime = Mathf.Clamp(currentTime, 0f, duration);

            float normalized = activeCurve.Evaluate(currentTime);
            float lerpedX = Mathf.LerpUnclamped(closedPositionX, openPositionX, normalized);
            tabContainerRect.anchoredPosition = new Vector2(lerpedX, anchoredYValue);
            yield return null;
        }

        tabContainerRect.anchoredPosition = new Vector2(
            Mathf.LerpUnclamped(closedPositionX, openPositionX, activeCurve.Evaluate(endTime)),
            anchoredYValue);

        if (slideIn && tabCanvasGroup != null)
        {
            tabCanvasGroup.blocksRaycasts = true;
            tabCanvasGroup.interactable = true;
        }

        slideCoroutine = null;
    }

    private AnimationCurve GetSlideCurve(bool slideIn)
    {
        if (slideIn)
        {
            return slideInCurve != null ? slideInCurve : slideOutCurve;
        }

        return slideOutCurve != null ? slideOutCurve : slideInCurve;
    }

    private bool TryGetSlideParameters(AnimationCurve curve, out float duration)
    {
        duration = slideDuration;

        if (curve == null)
        {
            return false;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return false;
        }

        if (duration <= 0f)
        {
            Keyframe lastKey = curve.keys[keyCount - 1];
            duration = lastKey.time;
            if (duration <= 0f)
            {
                return false;
            }
        }

        return keyCount >= 2;
    }

    private float GetTimeForPosition(float positionX, float duration, float closedX, float openX, AnimationCurve curve)
    {
        if (curve == null)
        {
            return 0f;
        }

        int keyCount = curve.length;
        if (keyCount == 0)
        {
            return 0f;
        }

        Keyframe[] keys = curve.keys;
        if (keyCount == 1)
        {
            return keys[0].time;
        }

        float denominator = openX - closedX;
        float normalized = denominator != 0f ? (positionX - closedX) / denominator : 0f;

        for (int i = 0; i < keyCount - 1; i++)
        {
            float startValue = keys[i].value;
            float endValue = keys[i + 1].value;

            if (Mathf.Approximately(startValue, endValue))
            {
                if (Mathf.Approximately(normalized, startValue))
                {
                    return Mathf.Lerp(keys[i].time, keys[i + 1].time, 0.5f);
                }
                continue;
            }

            bool between = (normalized >= startValue && normalized <= endValue) ||
                           (normalized <= startValue && normalized >= endValue);
            if (between)
            {
                float segmentProgress = (normalized - startValue) / (endValue - startValue);
                return Mathf.Lerp(keys[i].time, keys[i + 1].time, segmentProgress);
            }
        }

        float startValueAtZero = curve.Evaluate(0f);
        float endValueAtDuration = curve.Evaluate(duration);
        return Mathf.Abs(normalized - startValueAtZero) <= Mathf.Abs(normalized - endValueAtDuration) ? 0f : duration;
    }

    private float ResolveAnchoredY()
    {
        if (tabContainerRect == null)
        {
            return anchoredY;
        }

        if (!Mathf.Approximately(anchoredY, 0f))
        {
            return anchoredY;
        }

        return tabContainerRect.anchoredPosition.y;
    }
}