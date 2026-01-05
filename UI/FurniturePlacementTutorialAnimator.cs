using System.Collections;
using UnityEngine;

public class FurniturePlacementTutorialAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Positions")]
    [SerializeField] private Vector2 startPosition;
    [SerializeField] private Vector2 endPosition;

    [Header("Timing")]
    [SerializeField] private float startWaitSeconds = 0.5f;
    [SerializeField] private float transitionSeconds = 1.5f;
    [SerializeField] private float endWaitSeconds = 0.5f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine playRoutine;

    void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        Stop();
        playRoutine = StartCoroutine(PlayLoop());
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    IEnumerator PlayLoop()
    {
        while (true)
        {
            if (target == null)
            {
                yield break;
            }

            target.anchoredPosition = startPosition;

            if (startWaitSeconds > 0f)
            {
                yield return new WaitForSeconds(startWaitSeconds);
            }

            if (transitionSeconds <= 0f)
            {
                target.anchoredPosition = endPosition;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < transitionSeconds)
                {
                    float t = Mathf.Clamp01(elapsed / transitionSeconds);
                    float curved = moveCurve != null ? moveCurve.Evaluate(t) : t;
                    target.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, curved);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                target.anchoredPosition = endPosition;
            }

            if (endWaitSeconds > 0f)
            {
                yield return new WaitForSeconds(endWaitSeconds);
            }
        }
    }
}
