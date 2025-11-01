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
    [SerializeField, Tooltip("Time (in seconds) spent sitting before transitioning to the sit sleep state.")]
    private float sitSleepDelay = 5f;
    [SerializeField, Tooltip("Cross-fade duration (in seconds) when switching between idle/sleep states.")]
    private float crossFadeDuration = 0.25f;

    private int idleNormalStateHash = -1;
    private int idleSleepStateHash = -1;
    private int sitNormalStateHash = -1;
    private int sitSleepStateHash = -1;

    private float inactivityTimer = 0f;
    private float sitElapsedTimer = 0f;
    private bool isSleeping = false;
    private bool isCurrentlySitting = false;
    private bool wasSittingPreviousFrame = false;
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
    /// <param name="isSitting">Whether the player is currently sitting.</param>
    public void NotifyInactive(float deltaTime, bool isSitting)
    {
        isCurrentlySitting = isSitting;

        bool startedSittingThisFrame = isSitting && !wasSittingPreviousFrame;
        if (startedSittingThisFrame)
        {
            sitElapsedTimer = 0f;
        }
        else if (isSitting)
        {
            sitElapsedTimer += deltaTime;
        }
        else
        {
            sitElapsedTimer = 0f;
        }

        wasSittingPreviousFrame = isSitting;

        if (isSleeping)
        {
            EnsureSleepStateMatchesPosture();
            return;
        }

        if (isSitting && sitSleepDelay > 0f && sitElapsedTimer >= sitSleepDelay)
        {
            isSleeping = true;
            CrossFadeToState(sitSleepStateHash);
            return;
        }

        inactivityTimer += deltaTime;
        if (inactivityTimer >= inactiveToSleepDelay)
        {
            isSleeping = true;
            CrossFadeToState(isSitting ? sitSleepStateHash : idleSleepStateHash);
        }
    }

    /// <summary>
    /// Resets inactivity tracking and returns to the normal idle/sit animation.
    /// </summary>
    /// <param name="isSitting">Whether the player is currently sitting.</param>
    public void NotifyActive(bool isSitting)
    {
        isCurrentlySitting = isSitting;
        wasSittingPreviousFrame = isSitting;
        inactivityTimer = 0f;
        
        if (isSleeping)
        {
            isSleeping = false;
        }

        if (!isSitting)
        {
            sitElapsedTimer = 0f;
        }
        CrossFadeToState(isSitting ? sitNormalStateHash : idleNormalStateHash);
    }

    /// <summary>
    /// Immediately forces the animation back to the appropriate baseline state and resets timers.
    /// </summary>
    /// <param name="isSitting">Whether the player should be in the sit baseline state.</param>
    public void ForceState(bool isSitting)
    {
        isCurrentlySitting = isSitting;
        wasSittingPreviousFrame = isSitting;
        inactivityTimer = 0f;
        isSleeping = false;
        if (!isSitting)
        {
            sitElapsedTimer = 0f;
        }
        CrossFadeToState(isSitting ? sitNormalStateHash : idleNormalStateHash);
    }

    private void EnsureSleepStateMatchesPosture()
    {
        int targetSleepState = isCurrentlySitting ? sitSleepStateHash : idleSleepStateHash;
        CrossFadeToState(targetSleepState);
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
