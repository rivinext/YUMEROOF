using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles player interactions with beds, including tracking when the player enters
/// a bed's trigger area and coordinating animation triggers and positioning when
/// starting or ending a sleep sequence.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class BedInteractionController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string bedEnterTriggerName = "BedEnter";
    [FormerlySerializedAs("bedExitTriggerName")]
    [SerializeField] private string bedWakeTriggerName = "BedWake";
    [SerializeField] private string bedSleepTriggerName = "BedSleep";

    [Header("Positioning")]
    [Tooltip("Optional root transform used when aligning the player to a bed anchor. Defaults to this transform.")]
    [SerializeField] private Transform snapRoot;

    [Header("Movement")]
    [SerializeField] private BedEntryMovementAnimator bedEntryMovementAnimator;

    private Rigidbody cachedRigidbody;
    private PlayerController playerController;
    private BedTrigger currentBed;
    private bool isPlayerWithinBedRange;
    private bool isSleeping;
    private bool isWakingUp;
    private bool cachedGlobalInputState = true;
    private bool cachedLocalInputState = true;
    private bool didDisableGlobalInput;
    private bool didDisableLocalInput;

    /// <summary>
    /// True while the player remains inside the trigger volume of the current bed.
    /// </summary>
    public bool IsPlayerWithinBedRange => isPlayerWithinBedRange;

    /// <summary>
    /// The most recently detected bed trigger.
    /// </summary>
    public BedTrigger CurrentBed => currentBed;

    /// <summary>
    /// Whether the sleep animation has been initiated.
    /// </summary>
    public bool IsSleeping => isSleeping;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        cachedRigidbody = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        bedEntryMovementAnimator = GetComponent<BedEntryMovementAnimator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        cachedRigidbody = GetComponent<Rigidbody>();
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (snapRoot == null)
        {
            snapRoot = transform;
        }

        if (bedEntryMovementAnimator == null)
        {
            bedEntryMovementAnimator = GetComponent<BedEntryMovementAnimator>();
        }

        if (bedEntryMovementAnimator != null && bedEntryMovementAnimator.MovementRoot == null && snapRoot != null)
        {
            bedEntryMovementAnimator.MovementRoot = snapRoot;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        BedTrigger bed = other.GetComponentInParent<BedTrigger>();
        if (bed == null)
        {
            return;
        }

        currentBed = bed;
        isPlayerWithinBedRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        BedTrigger bed = other.GetComponentInParent<BedTrigger>();
        if (bed == null)
        {
            return;
        }

        if (bed != currentBed)
        {
            return;
        }

        isPlayerWithinBedRange = false;
        if (!isSleeping)
        {
            currentBed = null;
        }
    }

    /// <summary>
    /// Attempts to start the sleep sequence at the specified bed. If <paramref name="bed"/>
    /// is null the most recently entered bed is used.
    /// </summary>
    /// <param name="bed">Bed trigger initiating the sleep interaction.</param>
    public bool BeginSleepSequence(BedTrigger bed)
    {
        if (bed != null)
        {
            currentBed = bed;
            isPlayerWithinBedRange = true;
        }

        if (currentBed == null)
        {
            Debug.LogWarning("BedInteractionController: Cannot begin sleep sequence because no bed is assigned.", this);
            return false;
        }

        isSleeping = true;
        isWakingUp = false;
        DisablePlayerInput();

        BedTrigger entryBed = currentBed;
        Transform anchor = entryBed.SleepAnchor;

        void CompleteEntry()
        {
            AlignWithBedAnchor(entryBed);
            TriggerAnimator(bedWakeTriggerName, false);
            TriggerAnimator(bedSleepTriggerName, false);
            TriggerAnimator(bedEnterTriggerName, true);
        }

        if (bedEntryMovementAnimator != null && anchor != null)
        {
            bedEntryMovementAnimator.PlayMovementSequence(anchor, CompleteEntry);
        }
        else
        {
            CompleteEntry();
        }

        return true;
    }

    /// <summary>
    /// Ends the current sleep sequence and optionally triggers an exit animation.
    /// </summary>
    /// <param name="bed">Bed trigger completing the sleep interaction.</param>
    public void EndSleepSequence(BedTrigger bed)
    {
        if (!isSleeping)
        {
            return;
        }

        if (bed != null && currentBed != null && bed != currentBed)
        {
            return;
        }

        TriggerAnimator(bedEnterTriggerName, false);
        TriggerAnimator(bedSleepTriggerName, false);
        TriggerAnimator(bedWakeTriggerName, true);

        isWakingUp = true;
    }

    private void AlignWithBedAnchor(BedTrigger bed)
    {
        Transform anchor = bed != null ? bed.SleepAnchor : null;
        if (anchor == null)
        {
            return;
        }

        Vector3 position = anchor.position;
        Quaternion rotation = anchor.rotation;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.MovePosition(position);
            cachedRigidbody.MoveRotation(rotation);
        }

        Transform root = snapRoot != null ? snapRoot : transform;
        root.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Called via animation event when the bed entry animation has fully transitioned
    /// into the sleeping pose. Triggers the sleep state within the animator.
    /// </summary>
    public void HandleBedEntryCompleted()
    {
        if (!isSleeping || isWakingUp)
        {
            return;
        }

        TriggerAnimator(bedEnterTriggerName, false);
        TriggerAnimator(bedSleepTriggerName, true);
    }

    /// <summary>
    /// Called when the wake-up animation has fully finished playing. Restores the
    /// player's ability to move and clears the sleep state.
    /// </summary>
    public void HandleWakeUpCompleted()
    {
        if (!isSleeping && !isWakingUp)
        {
            return;
        }

        TriggerAnimator(bedSleepTriggerName, false);
        TriggerAnimator(bedWakeTriggerName, false);

        isSleeping = false;
        isWakingUp = false;

        if (!isPlayerWithinBedRange)
        {
            currentBed = null;
        }

        RestorePlayerInput();
    }

    private void DisablePlayerInput()
    {
        cachedGlobalInputState = PlayerController.GlobalInputEnabled;
        if (cachedGlobalInputState)
        {
            PlayerController.SetGlobalInputEnabled(false);
            didDisableGlobalInput = true;
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController != null)
        {
            cachedLocalInputState = playerController.IsInputEnabled;
            if (cachedLocalInputState)
            {
                playerController.SetInputEnabled(false);
                didDisableLocalInput = true;
            }
        }
    }

    private void RestorePlayerInput()
    {
        if (didDisableGlobalInput)
        {
            PlayerController.SetGlobalInputEnabled(cachedGlobalInputState);
            didDisableGlobalInput = false;
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController != null && didDisableLocalInput)
        {
            playerController.SetInputEnabled(cachedLocalInputState);
            didDisableLocalInput = false;
        }
    }

    private void TriggerAnimator(string triggerName, bool set)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        if (set)
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
        }
        else
        {
            animator.ResetTrigger(triggerName);
        }
    }
}
