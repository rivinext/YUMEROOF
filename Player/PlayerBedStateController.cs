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

    private Transform bedAnchor;
    private BedAnimationDriver activeDriver;
    private BedTrigger activeTrigger;
    private bool isBedIdle;
    private bool isBedOutInProgress;
    private bool cachedRootMotionState;
    private bool hasCachedRootMotion;

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
        if (activeDriver != null)
        {
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        }

        if (isBedIdle)
        {
            RestoreInputState();
        }

        ResetState();
    }

    private void OnDestroy()
    {
        if (activeDriver != null)
        {
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        }
    }

    /// <summary>
    /// Starts the idle-in-bed state after the bed-in animation completes.
    /// </summary>
    /// <param name="bedAnchor">Anchor transform representing the bed idle pose.</param>
    /// <param name="driver">Animation driver responsible for bed in/out animations.</param>
    public void BeginBedIdle(Transform bedAnchor, BedAnimationDriver driver)
    {
        if (playerController == null || bedAnchor == null)
        {
            return;
        }

        if (activeDriver != null)
        {
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        }

        this.bedAnchor = bedAnchor;
        activeDriver = driver;
        activeTrigger = ResolveBedTrigger(driver, bedAnchor);

        isBedIdle = true;
        isBedOutInProgress = false;

        if (animator != null)
        {
            cachedRootMotionState = animator.applyRootMotion;
            hasCachedRootMotion = true;
            animator.applyRootMotion = false;
        }

        PlayerController.SetGlobalInputEnabled(false);
        playerController.SetInputEnabled(false);

        SnapToAnchor();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (activeDriver != null)
        {
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
            activeDriver.BedOutCompleted += HandleBedOutCompleted;
        }
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
            activeDriver.PlayBedOut(HandleBedOutCompleted);
            return;
        }

        HandleBedOutCompleted();
    }

    private void HandleBedOutCompleted()
    {
        isBedOutInProgress = false;

        if (!isBedIdle)
        {
            return;
        }

        if (activeDriver != null)
        {
            activeDriver.BedOutCompleted -= HandleBedOutCompleted;
        }

        RestoreInputState();

        activeTrigger?.ClosePanel();

        ResetState();
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
}
