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

    [Header("Animator States")]
    [SerializeField] private string sitIdleStateName = "SitIdle";
    [SerializeField] private string sitSleepStateName = "SitSleep";

    [Header("Timing")]
    [SerializeField, Tooltip("Delay in seconds before transitioning from sit idle to sit sleep.")]
    private float sitSleepDelay = 5f;
    [SerializeField, Tooltip("Cross fade duration used for sit idle/sleep transitions.")]
    private float crossFadeDuration = 0.1f;

    [Header("Input")]
    [SerializeField, Tooltip("Input magnitude below this value is ignored when checking for stand up requests.")]
    private float inputDeadZone = 0.1f;

    [Header("Seat Movement")]
    [SerializeField] private AnimationCurve seatMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve standUpForwardOffsetCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);

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
    private int currentStateHash = -1;
    private int sitIdleStateHash = -1;
    private int sitSleepStateHash = -1;

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

        if (!string.IsNullOrEmpty(sitIdleStateName))
        {
            sitIdleStateHash = Animator.StringToHash(sitIdleStateName);
        }

        if (!string.IsNullOrEmpty(sitSleepStateName))
        {
            sitSleepStateHash = Animator.StringToHash(sitSleepStateName);
        }
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
        if (shouldEnterSleep && !hasEnteredSleepState)
        {
            hasEnteredSleepState = true;
            CrossFadeToState(sitSleepStateHash);
            NotifyControllersSleepStarted();
        }
        else if (!shouldEnterSleep && hasEnteredSleepState)
        {
            hasEnteredSleepState = false;
            CrossFadeToState(sitIdleStateHash);
        }
        else if (!hasEnteredSleepState)
        {
            CrossFadeToState(sitIdleStateHash);
        }

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
        currentStateHash = -1;

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

            if (!string.IsNullOrEmpty(sitBoolName))
            {
                animator.SetBool(sitBoolName, true);
            }
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

            if (!string.IsNullOrEmpty(sitBoolName))
            {
                animator.SetBool(sitBoolName, false);
            }
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
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (animator != null)
            {
                animator.MatchTarget(targetPos, targetRot, AvatarTarget.Root,
                    new MatchTargetWeightMask(Vector3.one, 1f), 0f, 1f, true);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalT = seatMoveCurve.Evaluate(duration);
        transform.position = Vector3.Lerp(startPos, targetPos, finalT);
        transform.rotation = Quaternion.Slerp(startRot, targetRot, finalT);

        isMovingToSeat = false;
        isSitting = true;
        sitTimer = 0f;
        hasEnteredSleepState = false;
        currentStateHash = -1;

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        UpdateBlinkControl(true);

        if (sleepController != null)
        {
            sleepController.ForceState(true);
            sleepController.NotifyActive(true);
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }

        CrossFadeToState(sitIdleStateHash);
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
            sleepController.NotifyActive(false);
        }

        if (blinkController != null)
        {
            blinkController.NotifyActive();
        }

        UpdateBlinkControl(false);
    }

    private void ResetSitTimer()
    {
        sitTimer = 0f;
        currentStateHash = -1;
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
        }

        ResetSitTimer();
        hasEnteredSleepState = false;

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

        sleepController?.NotifyInactive(deltaTime, true);
    }

    private void NotifyControllersSleepStarted()
    {
        bool canControlBlink = blinkController != null && (emoteController == null || !emoteController.IsBlinkLocked);
        if (canControlBlink)
        {
            blinkController.NotifyInactive(0f);
        }

        sleepController?.NotifyInactive(0f, true);
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

    private void CrossFadeToState(int stateHash)
    {
        if (animator == null || stateHash == -1)
        {
            return;
        }

        if (currentStateHash == stateHash)
        {
            return;
        }

        animator.CrossFade(stateHash, crossFadeDuration, 0, 0f);
        currentStateHash = stateHash;
    }
}
