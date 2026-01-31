using System.Collections;
using UnityEngine;
using System;

public class PlayerSitStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private PlayerBlinkController blinkController;
    [SerializeField] private PlayerIdleSleepController sleepController;
    [SerializeField] private PlayerEmoteController emoteController;

    [Header("Animator Parameters")]
    [SerializeField] private string sitTriggerName = "SitDown";
    [SerializeField] private string standTriggerName = "StandUp";
    [SerializeField] private string sitBoolName = "IsSitting";
    [SerializeField] private string sitSleepBoolName = "IsSitSleeping";

    [Header("Timing")]
    [SerializeField, Tooltip("Delay in seconds before transitioning from sit idle to sit sleep.")]
    private float sitSleepDelay = 5f;
    [Header("Input")]
    [SerializeField, Tooltip("Input magnitude below this value is ignored when checking for stand up requests.")]
    private float inputDeadZone = 0.1f;

    [Header("Seat Movement")]
    [SerializeField] private AnimationCurve seatMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve standUpForwardOffsetCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);
    [SerializeField, Tooltip("If true, only XZ are manually interpolated when moving to the seat. If false, Y is also lerped.")]
    private bool seatMoveLerpXZOnly = false;
    [SerializeField, Tooltip("If true, keeps the final Y position from root motion instead of snapping to the anchor.")]
    private bool seatMoveLockFinalY = false;
    [SerializeField, Tooltip("Allows vertical matching when using MatchTarget during seat moves.")]
    private bool enableVerticalSeatMatch = true;

    public event Action StandUpRequested;

    public bool IsSitting => isSitting;
    public bool IsMovingToSeat => isMovingToSeat;
    public bool IsStandingUp => isStandingUp;
    public bool IsSeatIdle => isSitting && !isMovingToSeat && !isStandingUp;
    public Transform CurrentSeatAnchor => seatAnchor;

    private Transform seatAnchor;
    private Collider seatCollider;
    private bool isSitting;
    private bool isMovingToSeat;
    private bool isStandingUp;
    private bool hasEnteredSleepState;
    private float sitTimer;
    private bool animatorSitValue;
    private bool animatorSitSleepValue;

    private Coroutine seatMoveRoutine;
    private Coroutine standMoveRoutine;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }

        if (blinkController == null)
        {
            blinkController = GetComponent<PlayerBlinkController>();
        }

        if (sleepController == null)
        {
            sleepController = GetComponent<PlayerIdleSleepController>();
        }

        if (emoteController == null)
        {
            emoteController = GetComponent<PlayerEmoteController>();
        }

        animatorSitValue = false;
        animatorSitSleepValue = false;
    }

    public void Configure(PlayerBlinkController blink, PlayerIdleSleepController sleep, PlayerEmoteController emote)
    {
        if (blink != null)
        {
            blinkController = blink;
        }

        if (sleep != null)
        {
            sleepController = sleep;
        }

        if (emote != null)
        {
            emoteController = emote;
        }
    }

    void OnDisable()
    {
        ResetCoroutines();
    }

    public void Tick()
    {
        if (!IsSeatIdle)
        {
            return;
        }

        if (TryRequestStandUp())
        {
            return;
        }

        sitTimer += Time.deltaTime;

        bool shouldEnterSleep = sitSleepDelay > 0f && sitTimer >= sitSleepDelay;
        UpdateSitSleepState(shouldEnterSleep);

        NotifyControllersIdle(Time.deltaTime);
    }

    public void Sit(Transform anchor, Collider seatCol)
    {
        if (anchor == null)
        {
            return;
        }

        ResetCoroutines();

        seatAnchor = anchor;
        seatCollider = seatCol;
        isMovingToSeat = true;
        isStandingUp = false;
        isSitting = false;
        hasEnteredSleepState = false;
        sitTimer = 0f;

        if (playerCollider != null && seatCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, seatCollider, true);
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayerController.SetGlobalInputEnabled(false);

        if (animator != null)
        {
            animator.applyRootMotion = true;
            if (!string.IsNullOrEmpty(standTriggerName))
            {
                animator.ResetTrigger(standTriggerName);
            }

            if (!string.IsNullOrEmpty(sitTriggerName))
            {
                animator.SetTrigger(sitTriggerName);
            }

            SetAnimatorBool(ref animatorSitValue, sitBoolName, true);
            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
        }

        UpdateBlinkControl(false);
        seatMoveRoutine = StartCoroutine(MoveToSeat(anchor));
    }

    public void StandUp()
    {
        if (isStandingUp || (!isSitting && !isMovingToSeat))
        {
            return;
        }

        ResetSitTimer();
        hasEnteredSleepState = false;

        UpdateBlinkControl(false);

        if (animator != null)
        {
            animator.applyRootMotion = true;
            if (!string.IsNullOrEmpty(sitTriggerName))
            {
                animator.ResetTrigger(sitTriggerName);
            }

            if (!string.IsNullOrEmpty(standTriggerName))
            {
                animator.SetTrigger(standTriggerName);
            }

            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
            SetAnimatorBool(ref animatorSitValue, sitBoolName, false);
        }

        isStandingUp = true;
        isSitting = false;
        isMovingToSeat = false;

        if (seatMoveRoutine != null)
        {
            StopCoroutine(seatMoveRoutine);
            seatMoveRoutine = null;
        }

        StartStandUpMove();
    }

    public void ForceStandUpImmediate()
    {
        ResetCoroutines();

        if (playerCollider != null && seatCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, seatCollider, false);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (!string.IsNullOrEmpty(sitTriggerName))
            {
                animator.ResetTrigger(sitTriggerName);
            }

            if (!string.IsNullOrEmpty(standTriggerName))
            {
                animator.ResetTrigger(standTriggerName);
            }

            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
            SetAnimatorBool(ref animatorSitValue, sitBoolName, false);
        }

        seatAnchor = null;
        seatCollider = null;
        isStandingUp = false;
        isMovingToSeat = false;
        isSitting = false;
        hasEnteredSleepState = false;
        sitTimer = 0f;

        PlayerController.SetGlobalInputEnabled(true);

        if (sleepController != null)
        {
            sleepController.ForceState(false);
            sleepController.NotifySitState(false, false);
            sleepController.NotifyActive(false);
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }

        UpdateBlinkControl(true);
    }

    public void StartSeatMove()
    {
        if (seatAnchor != null && !isMovingToSeat)
        {
            isMovingToSeat = true;
            seatMoveRoutine = StartCoroutine(MoveToSeat(seatAnchor));
        }
    }

    public void StartSeatMove(float normalizedStartTime)
    {
        if (seatAnchor == null)
        {
            return;
        }

        if (seatMoveRoutine != null)
        {
            StopCoroutine(seatMoveRoutine);
        }

        isMovingToSeat = true;
        seatMoveRoutine = StartCoroutine(MoveToSeat(seatAnchor, normalizedStartTime));
    }

    public void StartStandUpMove()
    {
        if (standMoveRoutine != null)
        {
            StopCoroutine(standMoveRoutine);
        }

        standMoveRoutine = StartCoroutine(MoveFromSeat());
    }

    public void NotifySeatAnimationStart()
    {
        ResetSitTimer();
        hasEnteredSleepState = false;
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
    }

    private IEnumerator MoveToSeat(Transform anchor, float normalizedStartTime = -1f)
    {
        if (anchor == null)
        {
            yield break;
        }

        isMovingToSeat = true;

        if (normalizedStartTime >= 0f && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < normalizedStartTime)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 targetPos = anchor.position;
        Quaternion targetRot = anchor.rotation;

        float duration = seatMoveCurve.keys[^1].time;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = seatMoveCurve.Evaluate(elapsed);
            Vector3 lerpedPosition = Vector3.Lerp(startPos, targetPos, t);
            Vector3 positionToApply = seatMoveLerpXZOnly
                ? new Vector3(lerpedPosition.x, transform.position.y, lerpedPosition.z)
                : lerpedPosition;
            transform.position = positionToApply;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (animator != null)
            {
                animator.MatchTarget(targetPos, targetRot, AvatarTarget.Root,
                    new MatchTargetWeightMask(new Vector3(1f, enableVerticalSeatMatch ? 1f : 0f, 1f), 1f), 0f, 1f, true);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalT = seatMoveCurve.Evaluate(duration);
        Vector3 finalPosition = Vector3.Lerp(startPos, targetPos, finalT);
        Vector3 finalPositionToApply = seatMoveLerpXZOnly
            ? new Vector3(finalPosition.x, seatMoveLockFinalY ? transform.position.y : finalPosition.y, finalPosition.z)
            : finalPosition;
        transform.position = finalPositionToApply;
        transform.rotation = Quaternion.Slerp(startRot, targetRot, finalT);

        isMovingToSeat = false;
        isSitting = true;
        sitTimer = 0f;
        hasEnteredSleepState = false;
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);

        UpdateBlinkControl(true);

        if (sleepController != null)
        {
            sleepController.ForceState(true);
            sleepController.NotifyActive(true);
            sleepController.NotifySitState(true, false);
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }
    }

    private IEnumerator MoveFromSeat()
    {
        if (seatAnchor == null)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            PlayerController.SetGlobalInputEnabled(true);
            isStandingUp = false;

            if (sleepController != null)
            {
                sleepController.ForceState(false);
                sleepController.NotifySitState(false, false);
                sleepController.NotifyActive(false);
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            yield break;
        }

        Vector3 startPos = seatAnchor.position;
        Quaternion targetRot = seatAnchor.rotation;

        float duration = standUpForwardOffsetCurve.keys[^1].time;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offset = standUpForwardOffsetCurve.Evaluate(elapsed);
            transform.position = startPos + seatAnchor.forward * offset;
            transform.rotation = targetRot;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos + seatAnchor.forward * standUpForwardOffsetCurve.Evaluate(duration);
        transform.rotation = targetRot;

        if (playerCollider != null && seatCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, seatCollider, false);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        PlayerController.SetGlobalInputEnabled(true);
        seatAnchor = null;
        seatCollider = null;
        isStandingUp = false;

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (sleepController != null)
        {
            sleepController.ForceState(false);
            sleepController.NotifySitState(false, false);
            sleepController.NotifyActive(false);
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }

        UpdateBlinkControl(false);
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
    }

    private void ResetSitTimer()
    {
        sitTimer = 0f;
    }

    private void ResetCoroutines()
    {
        if (seatMoveRoutine != null)
        {
            StopCoroutine(seatMoveRoutine);
            seatMoveRoutine = null;
        }

        if (standMoveRoutine != null)
        {
            StopCoroutine(standMoveRoutine);
            standMoveRoutine = null;
        }
    }

    private bool TryRequestStandUp()
    {
        bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
        bool interactPressed = Input.GetKeyDown(KeyCode.E);
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 moveInput = new Vector2(horizontal, vertical);
        bool movementRequested = moveInput.magnitude > inputDeadZone;

        if (!escapePressed && !interactPressed && !movementRequested)
        {
            return false;
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }

        if (sleepController != null)
        {
            sleepController.NotifyActive(true);
            sleepController.NotifySitState(true, false);
        }

        ResetSitTimer();
        hasEnteredSleepState = false;
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);

        if (StandUpRequested != null)
        {
            StandUpRequested.Invoke();
        }
        else
        {
            StandUp();
        }

        return true;
    }

    private void NotifyControllersIdle(float deltaTime)
    {
        bool canControlBlink = blinkController != null && (emoteController == null || !emoteController.IsBlinkLocked);
        if (canControlBlink)
        {
            blinkController.SetBlinkingEnabled(true);
            blinkController.NotifyInactive(deltaTime);
        }

    }

    private void NotifyControllersSleepStarted()
    {
        bool canControlBlink = blinkController != null && (emoteController == null || !emoteController.IsBlinkLocked);
        if (canControlBlink)
        {
            blinkController.ForceEyesClosed();
        }
    }

    private void UpdateBlinkControl(bool shouldEnableBlinking)
    {
        if (blinkController == null)
        {
            return;
        }

        if (emoteController != null && emoteController.IsBlinkLocked)
        {
            return;
        }

        blinkController.SetBlinkingEnabled(shouldEnableBlinking);
    }

    private void UpdateSitSleepState(bool shouldSleep)
    {
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, shouldSleep);

        if (hasEnteredSleepState == shouldSleep)
        {
            return;
        }

        hasEnteredSleepState = shouldSleep;
        sleepController?.NotifySitState(true, shouldSleep);

        if (shouldSleep)
        {
            NotifyControllersSleepStarted();
        }
    }

    private void SetAnimatorBool(ref bool cachedValue, string parameterName, bool value)
    {
        if (cachedValue == value)
        {
            return;
        }

        cachedValue = value;

        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }
}
