using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles moving the player from their current position to a bed's sleep anchor
/// using an animation curve to interpolate both position and rotation.
/// </summary>
public class BedEntryMovementAnimator : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform movementRoot;
    [SerializeField, Min(0f)] private float moveDuration = 1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Optional rigidbody that will be driven alongside the movement root during the animation.")]
    [SerializeField] private Rigidbody movementRigidbody;

    private Coroutine movementRoutine;

    /// <summary>
    /// Transform that is animated when moving to the bed anchor. Defaults to this transform.
    /// </summary>
    public Transform MovementRoot
    {
        get => movementRoot;
        set => movementRoot = value;
    }

    /// <summary>
    /// Duration for the movement animation in seconds.
    /// </summary>
    public float MoveDuration
    {
        get => moveDuration;
        set => moveDuration = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Animation curve used when interpolating the movement from the player's
    /// current pose to the bed anchor.
    /// </summary>
    public AnimationCurve MovementCurve
    {
        get => movementCurve;
        set => movementCurve = value;
    }

    private void Reset()
    {
        movementRoot = transform;
        movementRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Begins animating the player towards the supplied destination. If a movement
    /// sequence is already running it is replaced with the new one.
    /// </summary>
    /// <param name="destination">Target transform representing the bed anchor.</param>
    /// <param name="onCompleted">Callback invoked once the animation finishes.</param>
    public void PlayMovementSequence(Transform destination, Action onCompleted)
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        if (destination == null)
        {
            onCompleted?.Invoke();
            return;
        }

        movementRoutine = StartCoroutine(AnimateMovement(destination, onCompleted));
    }

    private IEnumerator AnimateMovement(Transform destination, Action onCompleted)
    {
        Transform root = movementRoot != null ? movementRoot : transform;
        Rigidbody rigidbody = movementRigidbody;

        Vector3 startPosition = root.position;
        Quaternion startRotation = root.rotation;
        Vector3 targetPosition = destination.position;
        Quaternion targetRotation = destination.rotation;

        if (rigidbody != null)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        float duration = Mathf.Max(0f, moveDuration);
        float elapsed = 0f;

        if (duration <= Mathf.Epsilon)
        {
            ApplyTransform(root, rigidbody, targetPosition, targetRotation);
        }
        else
        {
            while (elapsed < duration)
            {
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float curveValue = EvaluateCurve(normalizedTime);

                Vector3 nextPosition = Vector3.LerpUnclamped(startPosition, targetPosition, curveValue);
                Quaternion nextRotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, curveValue);

                ApplyTransform(root, rigidbody, nextPosition, nextRotation);

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyTransform(root, rigidbody, targetPosition, targetRotation);
        }

        movementRoutine = null;
        onCompleted?.Invoke();
    }

    private float EvaluateCurve(float time)
    {
        if (movementCurve == null || movementCurve.length == 0)
        {
            return time;
        }

        float clampedTime = Mathf.Clamp01(time);
        return movementCurve.Evaluate(clampedTime);
    }

    private void ApplyTransform(Transform root, Rigidbody rigidbody, Vector3 position, Quaternion rotation)
    {
        if (rigidbody != null)
        {
            rigidbody.MovePosition(position);
            rigidbody.MoveRotation(rotation);
        }

        root.SetPositionAndRotation(position, rotation);
    }
}
