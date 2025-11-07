using UnityEngine;

/// <summary>
/// Controls transitions between idle/sit baseline animations and their sleep variants
/// based on player inactivity.
/// </summary>
public class PlayerIdleSleepController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string idleSleepBoolName = "IsIdleSleeping";
    [SerializeField] private string sitSleepBoolName = "IsSitSleeping";

    [Header("Timing")]
    [SerializeField, Tooltip("Inactive duration (in seconds) before transitioning to the sleep state.")]
    private float inactiveToSleepDelay = 10f;
    private bool animatorIdleSleepValue;
    private bool animatorSitSleepValue;

    private float inactivityTimer = 0f;
    private bool isSleeping = false;
    private bool isSitting = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        animatorIdleSleepValue = false;
        animatorSitSleepValue = false;
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
            SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, true);
            return;
        }

        inactivityTimer += deltaTime;
        if (inactivityTimer >= inactiveToSleepDelay)
        {
            isSleeping = true;
            SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, true);
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
            SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
            return;
        }

        isSleeping = false;
        SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
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
            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
            SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
            return;
        }

        isSleeping = false;
        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
        SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
    }

    public void NotifySitState(bool isSitting, bool isSleeping)
    {
        this.isSitting = isSitting;

        if (!this.isSitting)
        {
            this.isSleeping = false;
            SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, false);
            SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
            return;
        }

        inactivityTimer = 0f;
        this.isSleeping = isSleeping;

        SetAnimatorBool(ref animatorSitSleepValue, sitSleepBoolName, this.isSleeping);
        SetAnimatorBool(ref animatorIdleSleepValue, idleSleepBoolName, false);
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
