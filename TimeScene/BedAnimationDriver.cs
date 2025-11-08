using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives "bed in" and "bed out" animations for the player character.
/// Moves the configured transform toward the bed anchor when entering the bed
/// and back toward the recorded start or an optional exit point when leaving.
/// </summary>
public class BedAnimationDriver : MonoBehaviour
{
    [Header("Animation Targets")]
    [SerializeField] private Transform animatedTransform;
    [SerializeField] private Transform anchorPoint;
    [SerializeField, Tooltip("Optional exit point. Leave empty to return to the recorded start position.")]
    private Transform exitPoint;

    [Header("Bed In Animation")]
    [SerializeField] private AnimationCurve bedInMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float bedInDuration = 1.5f;

    [Header("Bed Out Animation")]
    [SerializeField] private AnimationCurve bedOutMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float bedOutDuration = 1.25f;

    public event Action BedInCompleted;
    public event Action BedOutCompleted;

    public Transform AnchorPoint => anchorPoint;
    public Transform ExitPoint => exitPoint;

    Coroutine currentRoutine;
    Vector3 recordedStartPosition;
    Quaternion recordedStartRotation;
    bool hasRecordedStart;

    void Awake()
    {
        if (animatedTransform == null)
        {
            animatedTransform = transform;
        }
    }

    public void SetAnchorPoint(Transform anchor)
    {
        anchorPoint = anchor;
    }

    public void SetExitPoint(Transform exit)
    {
        exitPoint = exit;
    }

    public void SetAnimatedTransform(Transform animated)
    {
        animatedTransform = animated;
    }

    public void PlayBedIn(Action onCompleted = null)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(PlayBedInRoutine(onCompleted));
    }

    public void PlayBedOut(Action onCompleted = null)
    {
        StopCurrentRoutine();
        currentRoutine = StartCoroutine(PlayBedOutRoutine(onCompleted));
    }

    void StopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
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

        hasRecordedStart = true;
        recordedStartPosition = animatedTransform.position;
        recordedStartRotation = animatedTransform.rotation;

        Vector3 targetPosition = anchorPoint != null ? anchorPoint.position : animatedTransform.position;
        Quaternion targetRotation = anchorPoint != null ? anchorPoint.rotation : animatedTransform.rotation;

        float elapsed = 0f;
        float resolvedDuration = Mathf.Max(0.01f, bedInDuration);

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / resolvedDuration);
            float curveValue = bedInMovementCurve != null ? bedInMovementCurve.Evaluate(normalizedTime) : normalizedTime;

            animatedTransform.position = Vector3.LerpUnclamped(recordedStartPosition, targetPosition, curveValue);
            animatedTransform.rotation = Quaternion.SlerpUnclamped(recordedStartRotation, targetRotation, curveValue);

            yield return null;
        }

        animatedTransform.position = targetPosition;
        animatedTransform.rotation = targetRotation;

        onCompleted?.Invoke();
        BedInCompleted?.Invoke();

        currentRoutine = null;
    }

    IEnumerator PlayBedOutRoutine(Action onCompleted)
    {
        if (animatedTransform == null)
        {
            onCompleted?.Invoke();
            BedOutCompleted?.Invoke();
            currentRoutine = null;
            yield break;
        }

        Vector3 startPosition = animatedTransform.position;
        Quaternion startRotation = animatedTransform.rotation;
        Vector3 targetPosition = ResolveExitPosition(startPosition);
        Quaternion targetRotation = ResolveExitRotation(startRotation);

        float elapsed = 0f;
        float resolvedDuration = Mathf.Max(0.01f, bedOutDuration);

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / resolvedDuration);
            float curveValue = bedOutMovementCurve != null ? bedOutMovementCurve.Evaluate(normalizedTime) : normalizedTime;

            animatedTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
            animatedTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, curveValue);

            yield return null;
        }

        animatedTransform.position = targetPosition;
        animatedTransform.rotation = targetRotation;

        onCompleted?.Invoke();
        BedOutCompleted?.Invoke();

        currentRoutine = null;
        hasRecordedStart = false;
    }

    Vector3 ResolveExitPosition(Vector3 fallback)
    {
        if (exitPoint != null)
        {
            return exitPoint.position;
        }

        if (hasRecordedStart)
        {
            return recordedStartPosition;
        }

        return fallback;
    }

    Quaternion ResolveExitRotation(Quaternion fallback)
    {
        if (exitPoint != null)
        {
            return exitPoint.rotation;
        }

        if (hasRecordedStart)
        {
            return recordedStartRotation;
        }

        return fallback;
    }
}
