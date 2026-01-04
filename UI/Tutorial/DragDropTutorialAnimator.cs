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
    [SerializeField] private float moveBackSeconds = 1.5f;

    private Coroutine animationCoroutine;

    private void OnEnable()
    {
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

    private void StartAnimationIfReady()
    {
        if (draggableCard == null || startPosition == null || endPosition == null)
        {
            return;
        }

        animationCoroutine = StartCoroutine(AnimateLoop());
    }

    private IEnumerator AnimateLoop()
    {
        while (true)
        {
            draggableCard.anchoredPosition = startPosition.anchoredPosition;
            yield return WaitSeconds(startWaitSeconds);

            yield return MoveTo(endPosition, moveToEndSeconds);
            yield return WaitSeconds(endWaitSeconds);

            yield return MoveTo(startPosition, moveBackSeconds);
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
            draggableCard.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
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
}
