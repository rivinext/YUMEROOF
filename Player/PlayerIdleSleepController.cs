using UnityEngine;

/// <summary>
/// Controls transitions between idle/sit baseline animations and their sleep variants
/// based on player inactivity.
/// </summary>
public class PlayerIdleSleepController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Idle States")]
    [SerializeField] private string idleNormalStateName = "Idle";
    [SerializeField] private string idleSleepStateName = "IdleSleep";

    [Header("Sit States")]
    [SerializeField] private string sitNormalStateName = "SitIdle";
    [SerializeField] private string sitSleepStateName = "SitSleep";

    [Header("Timing")]
    [SerializeField, Tooltip("Inactive duration (in seconds) before transitioning to the sleep state.")]
    private float inactiveToSleepDelay = 10f;
    [SerializeField, Tooltip("Cross-fade duration (in seconds) when switching between idle/sleep states.")]
    private float crossFadeDuration = 0.25f;

    private int idleNormalStateHash = -1;
    private int idleSleepStateHash = -1;
    private int sitNormalStateHash = -1;
    private int sitSleepStateHash = -1;

    private float inactivityTimer = 0f;
    private bool isSleeping = false;
    private bool isSitting = false;
    private int currentStateHash = -1;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (!string.IsNullOrEmpty(idleNormalStateName))
        {
            idleNormalStateHash = Animator.StringToHash(idleNormalStateName);
        }

        if (!string.IsNullOrEmpty(idleSleepStateName))
        {
            idleSleepStateHash = Animator.StringToHash(idleSleepStateName);
        }

        if (!string.IsNullOrEmpty(sitNormalStateName))
        {
            sitNormalStateHash = Animator.StringToHash(sitNormalStateName);
        }

        if (!string.IsNullOrEmpty(sitSleepStateName))
        {
            sitSleepStateHash = Animator.StringToHash(sitSleepStateName);
        }
    }

    /// <summary>
    /// Accumulates inactivity time and transitions to sleep states when the delay is exceeded.
    /// </summary>
    /// <param name="deltaTime">Frame delta time.</param>
    public void NotifyInactive(float deltaTime)
    {
        if (isSitting)
        {
            return;
        }

        if (isSleeping)
        {
            CrossFadeToState(idleSleepStateHash);
            return;
        }

        inactivityTimer += deltaTime;
        if (inactivityTimer >= inactiveToSleepDelay)
        {
            isSleeping = true;
            CrossFadeToState(idleSleepStateHash);
        }
    }

    /// <summary>
    /// Resets inactivity tracking and returns to the normal idle/sit animation.
    /// </summary>
    /// <param name="isSitting">Whether the player is currently sitting.</param>
    public void NotifyActive(bool isSitting)
    {
        inactivityTimer = 0f;
        this.isSitting = isSitting;

        if (this.isSitting)
        {
            isSleeping = false;
            return;
        }

        isSleeping = false;
        CrossFadeToState(idleNormalStateHash);
    }

    /// <summary>
    /// Immediately forces the animation back to the appropriate baseline state and resets timers.
    /// </summary>
    /// <param name="isSitting">Whether the player should be in the sit baseline state.</param>
    public void ForceState(bool isSitting)
    {
        inactivityTimer = 0f;
        this.isSitting = isSitting;

        if (this.isSitting)
        {
            isSleeping = false;
            CrossFadeToState(sitNormalStateHash);
            return;
        }

        isSleeping = false;
        CrossFadeToState(idleNormalStateHash);
    }

    public void NotifySitState(bool isSitting, bool isSleeping)
    {
        this.isSitting = isSitting;

        if (!this.isSitting)
        {
            this.isSleeping = false;
            return;
        }

        inactivityTimer = 0f;
        this.isSleeping = isSleeping;

        CrossFadeToState(this.isSleeping ? sitSleepStateHash : sitNormalStateHash);
    }

    private void CrossFadeToState(int stateHash)
    {
        if (animator != null && stateHash != -1 && currentStateHash != stateHash)
        {
            animator.CrossFade(stateHash, crossFadeDuration, 0, 0f);
            currentStateHash = stateHash;
        }
    }
}
