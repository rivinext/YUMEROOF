using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the "bed in" animation using an animation curve.
/// Moves the configured transform toward the anchor position and notifies
/// listeners when the animation has finished.
/// </summary>
public class BedInAnimationDriver : MonoBehaviour
{
    [Header("Animation Targets")]
    [SerializeField] private Transform animatedTransform;
    [SerializeField] private Transform anchorPoint;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float duration = 1.5f;

    public event Action BedInCompleted;

    Coroutine currentRoutine;

    void Awake()
    {
        if (animatedTransform == null)
            animatedTransform = transform;
    }

    public void PlayBedIn(Action onCompleted = null)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(PlayBedInRoutine(onCompleted));
    }

    IEnumerator PlayBedInRoutine(Action onCompleted)
    {
        if (animatedTransform == null)
        {
            onCompleted?.Invoke();
            BedInCompleted?.Invoke();
            currentRoutine = null;
            yield break;
        }

        Vector3 startPosition = animatedTransform.position;
        Vector3 targetPosition = anchorPoint != null ? anchorPoint.position : animatedTransform.position;

        float elapsed = 0f;
        float resolvedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / resolvedDuration);
            float curveValue = movementCurve != null ? movementCurve.Evaluate(normalizedTime) : normalizedTime;
            animatedTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
            yield return null;
        }

        animatedTransform.position = targetPosition;

        onCompleted?.Invoke();
        BedInCompleted?.Invoke();

        currentRoutine = null;
    }
}
