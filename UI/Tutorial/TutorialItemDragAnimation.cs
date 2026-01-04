using System.Collections;
using UnityEngine;

public class TutorialItemDragAnimation : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform targetRect;

    [Header("Positions")]
    [SerializeField] private RectTransform startAnchor;
    [SerializeField] private RectTransform endAnchor;
    [SerializeField] private Vector2 startPosition;
    [SerializeField] private Vector2 endPosition;

    [Header("Timing")]
    [SerializeField] private float stayAtStartSeconds = 1.0f;
    [SerializeField] private float moveSeconds = 1.0f;
    [SerializeField] private float stayAtEndSeconds = 1.0f;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;

    private Coroutine loopCoroutine;

    void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
        }

        if (targetRect == null)
        {
            Debug.LogWarning("[TutorialItemDragAnimation] Target RectTransform is not assigned.");
            return;
        }

        loopCoroutine = StartCoroutine(LoopAnimation());
    }

    public void Stop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    Vector2 ResolveStartPosition()
    {
        if (startAnchor != null)
        {
            return startAnchor.anchoredPosition;
        }

        return startPosition;
    }

    Vector2 ResolveEndPosition()
    {
        if (endAnchor != null)
        {
            return endAnchor.anchoredPosition;
        }

        return endPosition;
    }

    IEnumerator LoopAnimation()
    {
        while (true)
        {
            Vector2 start = ResolveStartPosition();
            Vector2 end = ResolveEndPosition();

            targetRect.anchoredPosition = start;

            if (stayAtStartSeconds > 0f)
            {
                yield return new WaitForSeconds(stayAtStartSeconds);
            }

            if (moveSeconds <= 0f)
            {
                targetRect.anchoredPosition = end;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < moveSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveSeconds);
                    targetRect.anchoredPosition = Vector2.Lerp(start, end, t);
                    yield return null;
                }
            }

            if (stayAtEndSeconds > 0f)
            {
                yield return new WaitForSeconds(stayAtEndSeconds);
            }
        }
    }
}
