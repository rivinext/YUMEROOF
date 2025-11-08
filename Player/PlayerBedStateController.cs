using System;
using UnityEngine;

/// <summary>
/// Manages the player's idle state while lying in bed. Disables movement input,
/// monitors for wake-up input, and coordinates with the bed animation driver to
/// play the exit animation.
/// </summary>
public class PlayerBedStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [Header("Animator Parameters")]
    [SerializeField] private string bedInTrigger = "BedIn";
    [SerializeField] private string bedIdleBool = "BedIdle";
    [SerializeField] private string bedOutTrigger = "BedOut";
    [SerializeField] private bool disableRootMotionDuringBedState;

    private Transform bedAnchor;
    private BedAnimationDriver activeDriver;
    private BedTrigger activeTrigger;
    private bool isBedIdle;
    private bool isBedOutInProgress;
    private bool cachedRootMotionState;
    private bool hasCachedRootMotion;
    private bool isWaitingForBedInCompletion;
    private Action pendingBedInCallback;

    public event Action BedInCompleted;
    public event Action BedOutCompleted;

    public BedTrigger ActiveTrigger => activeTrigger;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromDriverEvents();

        if (isBedIdle)
        {
            RestoreInputState();
        }

        ResetState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDriverEvents();
    }

    /// <summary>
    /// Begins the bed entry flow for the supplied trigger. This handles
    /// disabling player input, snapping to the bed anchor, firing any
    /// animator triggers, and coordinating with the animation driver if one
    /// is provided. Completion of the entry sequence is signalled via
    /// <see cref="BedInCompleted"/>.
    /// </summary>
    /// <param name="trigger">The trigger that initiated the bed entry.</param>
    /// <param name="onCompleted">Optional callback invoked when entry completes.</param>
    public void BeginBedEntry(BedTrigger trigger, Action onCompleted = null, bool forceSnapToAnchor = false)
    {
        if (playerController == null || trigger == null)
        {
            return;
        }

        if (isBedIdle || isWaitingForBedInCompletion || isBedOutInProgress)
        {
            return;
        }

        UnsubscribeFromDriverEvents();

        pendingBedInCallback = onCompleted;

        activeTrigger = trigger;
        activeDriver = trigger.AnimationDriver;
        bedAnchor = ResolveBedAnchor(trigger);

        PrepareForBedState(forceSnapToAnchor);

        isWaitingForBedInCompletion = true;

        SubscribeToDriverEvents();

        if (activeDriver != null)
        {
            activeDriver.PlayBedIn(null);
            return;
        }

        bool triggeredAnimation = TryTriggerBedInAnimation();

        if (!triggeredAnimation)
        {
            EnterBedIdleState();
        }
    }

    /// <summary>
    /// Starts the idle-in-bed state after the bed-in animation completes.
    /// </summary>
    /// <param name="bedAnchor">Anchor transform representing the bed idle pose.</param>
    /// <param name="driver">Animation driver responsible for bed in/out animations.</param>
    public void BeginBedIdle(Transform bedAnchor, BedAnimationDriver driver, bool forceSnapToAnchor = false)
    {
        if (playerController == null || bedAnchor == null)
        {
            return;
        }

        UnsubscribeFromDriverEvents();

        this.bedAnchor = bedAnchor;
        activeDriver = driver;
        activeTrigger = ResolveBedTrigger(driver, bedAnchor);

        PrepareForBedState(forceSnapToAnchor);
        EnterBedIdleState();
    }

    /// <summary>
    /// Called every frame from <see cref="PlayerController.Update"/> to monitor
    /// wake-up input while the player is lying in bed.
    /// </summary>
    public void Tick()
    {
        if (!isBedIdle || isBedOutInProgress)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            RequestWakeUp();
        }
    }

    /// <summary>
    /// Requests that the player exit the bed.
    /// </summary>
    public void RequestWakeUp()
    {
        if (!isBedIdle || isBedOutInProgress)
        {
            return;
        }

        isBedOutInProgress = true;

        if (activeDriver != null)
        {
            activeDriver.PlayBedOut(null);
            return;
        }

        bool triggeredAnimation = TryTriggerBedOutAnimation();

        if (!triggeredAnimation)
        {
            HandleBedOutCompleted();
        }
    }

    private void HandleBedOutCompleted()
    {
        isBedOutInProgress = false;

        SetBedIdleState(false);

        if (!isBedIdle)
        {
            return;
        }

        if (activeDriver != null)
        {
            activeDriver.BedInCompleted -= HandleDriverBedInCompleted;
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        }

        RestoreInputState();

        activeTrigger?.ClosePanel();

        ResetState();

        BedOutCompleted?.Invoke();
    }

    private void RestoreInputState()
    {
        playerController?.SetInputEnabled(true);
        PlayerController.SetGlobalInputEnabled(true);

        if (animator != null && hasCachedRootMotion)
        {
            animator.applyRootMotion = cachedRootMotionState;
        }

        hasCachedRootMotion = false;
    }

    private void ResetState()
    {
        isBedIdle = false;
        isBedOutInProgress = false;
        bedAnchor = null;
        activeDriver = null;
        activeTrigger = null;
        isWaitingForBedInCompletion = false;
        pendingBedInCallback = null;

        ClearAnimatorBedParameters();
    }

    private void SnapToAnchor()
    {
        if (bedAnchor == null || playerController == null)
        {
            return;
        }

        Transform playerTransform = playerController.transform;
        playerTransform.position = bedAnchor.position;
        playerTransform.rotation = bedAnchor.rotation;
    }

    private BedTrigger ResolveBedTrigger(BedAnimationDriver driver, Transform anchor)
    {
        if (driver != null)
        {
            BedTrigger trigger = driver.GetComponentInParent<BedTrigger>();
            if (trigger == null)
            {
                trigger = driver.GetComponent<BedTrigger>();
            }

            if (trigger != null)
            {
                return trigger;
            }
        }

        return anchor != null ? anchor.GetComponentInParent<BedTrigger>() : null;
    }

    private Transform ResolveBedAnchor(BedTrigger trigger)
    {
        if (trigger == null)
        {
            return null;
        }

        if (trigger.AnimationDriver != null && trigger.AnimationDriver.AnchorPoint != null)
        {
            return trigger.AnimationDriver.AnchorPoint;
        }

        return trigger.transform;
    }

    private void PrepareForBedState(bool forceSnapToAnchor)
    {
        if (animator != null)
        {
            cachedRootMotionState = animator.applyRootMotion;
            hasCachedRootMotion = true;

            if (ShouldDisableRootMotionForBedState())
            {
                animator.applyRootMotion = false;
            }
        }

        PlayerController.SetGlobalInputEnabled(false);
        playerController.SetInputEnabled(false);

        bool shouldSnapToAnchor = forceSnapToAnchor || activeDriver != null || disableRootMotionDuringBedState;

        if (shouldSnapToAnchor)
        {
            SnapToAnchor();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void EnterBedIdleState()
    {
        if (isBedIdle)
        {
            return;
        }

        isBedIdle = true;
        isBedOutInProgress = false;
        isWaitingForBedInCompletion = false;

        SetBedIdleState(true);

        if (activeDriver != null)
        {
            activeDriver.BedInCompleted -= HandleDriverBedInCompleted;
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
            activeDriver.BedOutCompleted += HandleBedOutCompleted;
        }

        var completed = BedInCompleted;
        completed?.Invoke();

        pendingBedInCallback?.Invoke();
        pendingBedInCallback = null;
    }

    private void HandleDriverBedInCompleted()
    {
        EnterBedIdleState();
    }

    private void SubscribeToDriverEvents()
    {
        if (activeDriver == null)
        {
            return;
        }

        activeDriver.BedInCompleted -= HandleDriverBedInCompleted;
        activeDriver.BedInCompleted += HandleDriverBedInCompleted;
        activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        activeDriver.BedOutCompleted += HandleBedOutCompleted;
    }

    private void UnsubscribeFromDriverEvents()
    {
        if (activeDriver == null)
        {
            return;
        }

        activeDriver.BedInCompleted -= HandleDriverBedInCompleted;
        activeDriver.BedOutCompleted -= HandleBedOutCompleted;
    }

    /// <summary>
    /// Animation event hook to signal that the bed-in animation finished.
    /// </summary>
    public void OnBedInAnimationComplete()
    {
        if (!isWaitingForBedInCompletion || activeDriver != null)
        {
            return;
        }

        SetBedIdleState(true);

        EnterBedIdleState();
    }

    /// <summary>
    /// Animation event hook to signal that the bed-out animation finished.
    /// </summary>
    public void OnBedOutAnimationComplete()
    {
        if (activeDriver != null)
        {
            return;
        }

        SetBedIdleState(false);

        HandleBedOutCompleted();
    }

    private bool TryTriggerBedInAnimation()
    {
        if (animator == null)
        {
            return false;
        }

        SetBedIdleState(false);

        if (!string.IsNullOrEmpty(bedOutTrigger))
        {
            animator.ResetTrigger(bedOutTrigger);
        }

        if (string.IsNullOrEmpty(bedInTrigger))
        {
            return false;
        }

        animator.SetTrigger(bedInTrigger);
        return true;
    }

    private bool TryTriggerBedOutAnimation()
    {
        if (animator == null)
        {
            return false;
        }

        SetBedIdleState(false);

        if (!string.IsNullOrEmpty(bedInTrigger))
        {
            animator.ResetTrigger(bedInTrigger);
        }

        if (string.IsNullOrEmpty(bedOutTrigger))
        {
            return false;
        }

        animator.SetTrigger(bedOutTrigger);
        return true;
    }

    private void SetBedIdleState(bool isIdle)
    {
        if (animator == null || string.IsNullOrEmpty(bedIdleBool))
        {
            return;
        }

        animator.SetBool(bedIdleBool, isIdle);
    }

    private void ClearAnimatorBedParameters()
    {
        SetBedIdleState(false);

        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(bedInTrigger))
        {
            animator.ResetTrigger(bedInTrigger);
        }

        if (!string.IsNullOrEmpty(bedOutTrigger))
        {
            animator.ResetTrigger(bedOutTrigger);
        }
    }

    private bool ShouldDisableRootMotionForBedState()
    {
        if (disableRootMotionDuringBedState)
        {
            return true;
        }

        return activeDriver != null;
    }
}
