using System.Collections;
using UnityEngine;

public class FurniturePlacementTutorialAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Vector2 startPosition;
    [SerializeField] private float startWaitSeconds = 0.5f;
    [SerializeField] private Vector2 endPosition;
    [SerializeField] private float endWaitSeconds = 0.5f;
    [SerializeField] private float transitionSeconds = 1f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private GameObject[] additionalTargets;
    [SerializeField] private CanvasGroup visibilityGroup;

    private Coroutine moveRoutine;
    private bool resetPositionOnStart;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (visibilityGroup == null)
        {
            visibilityGroup = GetComponent<CanvasGroup>();
        }

        resetPositionOnStart = true;
    }

    private void OnEnable()
    {
        StartLoop();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    public void Play()
    {
        StartLoop();
    }

    public void Stop()
    {
        StopLoop();
    }

    private void StartLoop()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (target != null)
        {
            target.gameObject.SetActive(true);
            if (resetPositionOnStart)
            {
                target.anchoredPosition = startPosition;
                resetPositionOnStart = false;
            }
        }

        SetVisibility(true);
        SetAdditionalTargetsActive(true);

        if (!isActiveAndEnabled)
        {
            enabled = true;
        }

        if (moveRoutine == null && isActiveAndEnabled)
        {
            moveRoutine = StartCoroutine(MoveLoop());
        }
    }

    private void StopLoop()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (target != null)
        {
            target.anchoredPosition = startPosition;
            resetPositionOnStart = true;
        }

        SetVisibility(false);
    }

    private IEnumerator MoveLoop()
    {
        if (target == null)
        {
            yield break;
        }

        while (true)
        {
            // anchoredPosition is relative to the RectTransform's anchors.
            // If the Canvas uses a scaler, verify the motion distance matches design at runtime.
            target.anchoredPosition = startPosition;
            if (startWaitSeconds > 0f)
            {
                yield return new WaitForSeconds(startWaitSeconds);
            }

            float duration = Mathf.Max(0f, transitionSeconds);
            if (duration <= 0f)
            {
                target.anchoredPosition = endPosition;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float normalized = Mathf.Clamp01(elapsed / duration);
                    float curveValue = moveCurve != null ? moveCurve.Evaluate(normalized) : normalized;
                    target.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, curveValue);
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

    private void SetAdditionalTargetsActive(bool isActive)
    {
        if (additionalTargets == null || additionalTargets.Length == 0)
        {
            return;
        }

        foreach (GameObject additionalTarget in additionalTargets)
        {
            if (additionalTarget == null)
            {
                continue;
            }

            additionalTarget.SetActive(isActive);
        }
    }

    private void SetVisibility(bool isVisible)
    {
        if (visibilityGroup == null)
        {
            return;
        }

        visibilityGroup.alpha = isVisible ? 1f : 0f;
        visibilityGroup.blocksRaycasts = isVisible;
        visibilityGroup.interactable = isVisible;
    }
}
