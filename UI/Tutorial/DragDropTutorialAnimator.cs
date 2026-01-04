using System.Collections;
using UnityEngine;

public class DragDropTutorialAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform draggableCard;
    [SerializeField] private RectTransform startPosition;
    [SerializeField] private RectTransform endPosition;

    [SerializeField] private float startWaitSeconds = 0.5f;
    [SerializeField] private float moveToEndSeconds = 1.5f;
    [SerializeField] private float endWaitSeconds = 0.5f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Coroutine animationCoroutine;
    private bool loggedMissingReferences;

    private void OnEnable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        StartAnimationIfReady();
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StartAnimationIfReady();
    }

    public void TryStartAnimation()
    {
        StartAnimationIfReady();
    }

    private void StartAnimationIfReady()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            LogMissingReferences();
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        animationCoroutine = StartCoroutine(AnimateLoop());
    }

    private IEnumerator AnimateLoop()
    {
        while (true)
        {
            if (!HasRequiredReferences())
            {
                LogMissingReferences();
                yield return null;
                continue;
            }

            loggedMissingReferences = false;

            try
            {
                draggableCard.anchoredPosition = startPosition.anchoredPosition;
                yield return WaitSeconds(startWaitSeconds);

                yield return MoveTo(endPosition, moveToEndSeconds);
                yield return WaitSeconds(endWaitSeconds);
                draggableCard.anchoredPosition = startPosition.anchoredPosition;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"{nameof(DragDropTutorialAnimator)} animation loop interrupted, retrying. {exception}", this);
                yield return null;
            }
        }
    }

    private IEnumerator MoveTo(RectTransform destination, float duration)
    {
        if (destination == null)
        {
            yield break;
        }

        Vector2 start = draggableCard.anchoredPosition;
        Vector2 end = destination.anchoredPosition;

        if (duration <= 0f)
        {
            draggableCard.anchoredPosition = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;
            draggableCard.anchoredPosition = Vector2.LerpUnclamped(start, end, curvedT);
            yield return null;
        }

        draggableCard.anchoredPosition = end;
    }

    private static IEnumerator WaitSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }

    private bool HasRequiredReferences()
    {
        return draggableCard != null && startPosition != null && endPosition != null;
    }

    private void LogMissingReferences()
    {
        if (loggedMissingReferences)
        {
            return;
        }

        loggedMissingReferences = true;
        Debug.LogWarning($"{nameof(DragDropTutorialAnimator)} is missing references and cannot start. Assign draggableCard/startPosition/endPosition.", this);
    }
}
