using UnityEngine;

/// <summary>
/// Handles idle blinking and closed-eye states for the player.
/// </summary>
public class PlayerBlinkController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    private float defaultAnimatorSpeed = 1f;

    [Header("States")]
    [SerializeField] private string blinkStateName = "IdleBlink";
    [SerializeField] private string closedEyesStateName = "IdleClosedEyes";

    [Header("Timing")]
    [SerializeField, Tooltip("Idle duration (in seconds) before transitioning to the closed-eyes state.")]
    private float idleToClosedEyesDelay = 5f;
    [SerializeField, Tooltip("Cross-fade duration (in seconds) when switching between blink-related states.")]
    private float crossFadeDuration = 0.1f;

    private int blinkStateHash = -1;
    private int closedEyesStateHash = -1;

    private float idleTimer = 0f;
    private bool isEyesClosed = false;
    private bool blinkingEnabled = true;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            defaultAnimatorSpeed = animator.speed;
        }

        if (!string.IsNullOrEmpty(blinkStateName))
        {
            blinkStateHash = Animator.StringToHash(blinkStateName);
        }

        if (!string.IsNullOrEmpty(closedEyesStateName))
        {
            closedEyesStateHash = Animator.StringToHash(closedEyesStateName);
        }
    }

    /// <summary>
    /// Enables or disables the blinking logic. When disabled the eyes are forced open.
    /// </summary>
    public void SetBlinkingEnabled(bool enabled)
    {
        if (blinkingEnabled == enabled)
        {
            return;
        }

        blinkingEnabled = enabled;

        if (animator == null)
        {
            return;
        }

        if (!blinkingEnabled)
        {
            idleTimer = 0f;
            isEyesClosed = false;

            if (blinkStateHash != -1)
            {
                animator.Play(blinkStateHash, 0, 0f);
                animator.Update(0f);
            }

            animator.speed = 0f;
        }
        else
        {
            animator.speed = defaultAnimatorSpeed;
            ResetBlinkState();
        }
    }

    /// <summary>
    /// Called while the player is idle. Accumulates time until the closed-eyes state should play.
    /// </summary>
    public void NotifyInactive(float deltaTime)
    {
        if (!blinkingEnabled || isEyesClosed)
        {
            return;
        }

        idleTimer += deltaTime;
        if (idleTimer >= idleToClosedEyesDelay)
        {
            CrossFadeToState(closedEyesStateHash);
            isEyesClosed = true;
        }
    }

    /// <summary>
    /// Called when player input resumes. Resets timers and plays the blink state.
    /// </summary>
    public void NotifyActive()
    {
        if (!blinkingEnabled)
        {
            return;
        }

        idleTimer = 0f;
        bool shouldCrossFade = false;

        if (isEyesClosed)
        {
            shouldCrossFade = true;
        }

        isEyesClosed = false;

        if (!shouldCrossFade && animator != null && blinkStateHash != -1)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            bool isBlinkStateActive = currentState.shortNameHash == blinkStateHash || currentState.IsName(blinkStateName);
            if (!isBlinkStateActive)
            {
                shouldCrossFade = true;
            }
        }

        if (shouldCrossFade)
        {
            CrossFadeToState(blinkStateHash);
        }
    }

    /// <summary>
    /// Resets the blink timers and ensures the blink state is active.
    /// </summary>
    public void ResetBlinkState()
    {
        idleTimer = 0f;
        if (isEyesClosed)
        {
            CrossFadeToState(blinkStateHash);
            isEyesClosed = false;
        }
    }

    private void CrossFadeToState(int stateHash)
    {
        if (animator != null && stateHash != -1)
        {
            animator.CrossFade(stateHash, crossFadeDuration, 0, 0f);
        }
    }
}
