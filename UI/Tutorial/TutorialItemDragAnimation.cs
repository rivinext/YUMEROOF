using System.Collections;
using UnityEngine;

public class TutorialItemDragAnimation : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform targetRect;

    [Header("Positions")]
    [SerializeField] private RectTransform startAnchor;
    [SerializeField] private RectTransform endAnchor;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float stayAtStartSeconds = 1.0f;
    [SerializeField] private float moveSeconds = 1.0f;
    [SerializeField] private float stayAtEndSeconds = 1.0f;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

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
        Stop();

        if (targetRect == null)
        {
            Debug.LogWarning("[TutorialItemDragAnimation] Target RectTransform is not assigned.");
            return;
        }

        if (startAnchor == null || endAnchor == null)
        {
            Debug.LogWarning("[TutorialItemDragAnimation] Start/End anchors are not assigned.");
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

    IEnumerator LoopAnimation()
    {
        while (true)
        {
            Vector2 start = startAnchor.anchoredPosition;
            Vector2 end = endAnchor.anchoredPosition;
            AnimationCurve curve = moveCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);

            targetRect.anchoredPosition = start;

            if (stayAtStartSeconds > 0f)
            {
                yield return WaitForSecondsRoutine(stayAtStartSeconds);
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
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveSeconds);
                    targetRect.anchoredPosition = Vector2.Lerp(start, end, curve.Evaluate(t));
                    yield return null;
                }
            }

            if (stayAtEndSeconds > 0f)
            {
                yield return WaitForSecondsRoutine(stayAtEndSeconds);
            }
        }
    }

    IEnumerator WaitForSecondsRoutine(float seconds)
    {
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
