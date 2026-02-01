using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger for sleeping in the bed. Allows sleeping only during specified hours
/// and advances the day when sleep is triggered.
/// Advancing the day fires <see cref="GameClock.OnDayChanged"/>, which is
/// used by <see cref="MaterialSpawnManager"/> to regenerate random materials.
/// A separate sleep-specific event <see cref="GameClock.OnSleepAdvancedDay"/>
/// is invoked after the day advances so systems can react only to sleeping.
/// </summary>
public class BedTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction UI")]
    [SerializeField] private SleepPromptSlidePanel sleepPrompt;
    public bool isPlayerNearby = false;
    bool isPanelOpen = false;

    [Header("Player Control")]
    [SerializeField] private Transform bedAnchor;
    [SerializeField] private AnimationCurve bedInHeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private float bedTransitionDuration = 1f;
    [SerializeField] private string bedInTriggerName = "BedIn";
    [SerializeField] private string bedOutTriggerName = "BedOut";
    [SerializeField] private string bedIdleStateName = "BedIdle";
    [SerializeField] private string movementIdleStateName = "Idle";

    [Header("Bed Idle Sleep")]
    [SerializeField, Tooltip("Delay in seconds before the player's eyes close while idling in bed.")]
    private float bedIdleSleepDelay = 5f;

    private PlayerController playerController;
    private Animator playerAnimator;
    private PlayerBlinkController playerBlinkController;
    private Vector3 cachedPlayerPosition;
    private Quaternion cachedPlayerRotation;
    private bool isPlayerInBed;
    private bool isTransitioning;

    private float bedIdleSleepTimer;
    private bool hasClosedEyesInBed;

    [Header("Sleep Settings")]
    public int sleepStartMinutes = 20 * 60; // 8:00 PM
    public int sleepEndMinutes = 6 * 60;   // 6:00 AM
    public GameClock clock;

    [Header("Emote Controls")]
    [SerializeField] private PlayerEmoteButtonBinder playerEmoteButtonBinder;

    [Header("Transition UI")]
    public SleepTransitionUIManager transitionUI;

    void Reset()
    {
        AutoAssignReferences();
    }

    void Awake()
    {
        AutoAssignReferences();
    }

    void OnValidate()
    {
        AutoAssignReferences();
    }

    void Start()
    {
        clock = GameClock.Instance;

        TryResolveEmoteButtonBinder();

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
            playerAnimator = playerObject.GetComponent<Animator>();
            playerBlinkController = playerObject.GetComponent<PlayerBlinkController>();
            if (playerController == null)
                Debug.LogWarning("BedTrigger could not find PlayerController on the player object.");
            if (playerAnimator == null)
                Debug.LogWarning("BedTrigger could not find Animator on the player object.");
            if (playerBlinkController == null)
                Debug.LogWarning("BedTrigger could not find PlayerBlinkController on the player object.");
        }
        else
        {
            Debug.LogWarning("BedTrigger could not find a GameObject with the 'Player' tag.");
        }

        if (bedAnchor == null)
            Debug.LogWarning("BedTrigger: bedAnchor is not assigned.");

        if (bedInHeightCurve == null)
            Debug.LogWarning("BedTrigger: bedInHeightCurve is not assigned. Player movement will not include a height offset.");
    }

    void Update()
    {
        if (isPanelOpen)
        {
            UpdateSleepButtonState();
        }

        if (!isPlayerInBed || isTransitioning)
        {
            ResetBedIdleSleepTimer(true);
            return;
        }

        if (playerController == null || playerAnimator == null)
        {
            ResetBedIdleSleepTimer(true);
            return;
        }

        if (!string.IsNullOrEmpty(bedIdleStateName))
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(bedIdleStateName))
            {
                ResetBedIdleSleepTimer(true);
                return;
            }
            UpdateBedIdleSleep(Time.deltaTime);
        }
        else
        {
            UpdateBedIdleSleep(Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            if (isPanelOpen)
                ClosePanel();

            StartCoroutine(ExitBedSequence(playerController, playerAnimator));
        }
    }

    private void UpdateBedIdleSleep(float deltaTime)
    {
        if (playerBlinkController == null)
        {
            return;
        }

        if (bedIdleSleepDelay <= 0f)
        {
            if (!hasClosedEyesInBed)
            {
                playerBlinkController.ForceEyesClosed();
                hasClosedEyesInBed = true;
            }
            return;
        }

        bedIdleSleepTimer += deltaTime;

        if (!hasClosedEyesInBed && bedIdleSleepTimer >= bedIdleSleepDelay)
        {
            playerBlinkController.ForceEyesClosed();
            hasClosedEyesInBed = true;
        }
    }

    private void ResetBedIdleSleepTimer(bool reopenEyes)
    {
        bedIdleSleepTimer = 0f;

        if (hasClosedEyesInBed && reopenEyes && playerBlinkController != null)
        {
            playerBlinkController.NotifyActive();
        }

        hasClosedEyesInBed = false;
    }

    public void Interact()
    {
        if (isTransitioning)
            return;

        if (!isPlayerInBed)
        {
            if (playerController == null || playerAnimator == null || bedAnchor == null)
            {
                Debug.LogWarning("BedTrigger: Missing references required to enter the bed.");
                return;
            }

            StartCoroutine(EnterBedSequence(playerController, playerAnimator));
        }
        else
        {
            if (isPanelOpen)
                ClosePanel();

            if (playerController == null || playerAnimator == null || bedAnchor == null)
            {
                Debug.LogWarning("BedTrigger: Missing references required to exit the bed.");
                return;
            }

            StartCoroutine(ExitBedSequence(playerController, playerAnimator));
        }
    }

    void OpenPanel()
    {
        if (sleepPrompt == null)
        {
            Debug.LogWarning("BedTrigger: SleepPromptSlidePanel is not assigned.");
            return;
        }

        isPanelOpen = true;
        sleepPrompt.ShowPrompt(StartSleep, ClosePanel);
        UpdateSleepButtonState();
    }

    void ClosePanel()
    {
        if (!isPanelOpen)
            return;

        isPanelOpen = false;
        sleepPrompt?.Hide();
    }

    IEnumerator EnterBedSequence(PlayerController controller, Animator animator)
    {
        if (controller == null || animator == null || bedAnchor == null)
            yield break;

        isTransitioning = true;
        ResetBedIdleSleepTimer(true);

        SetEmoteButtonsTemporarilyDisabled(true);

        cachedPlayerPosition = controller.transform.position;
        cachedPlayerRotation = controller.transform.rotation;

        PlayerController.SetGlobalInputEnabled(false);

        if (!string.IsNullOrEmpty(bedInTriggerName))
            animator.SetTrigger(bedInTriggerName);

        float duration = Mathf.Max(0.01f, bedTransitionDuration);
        Vector3 targetPosition = bedAnchor.position;
        Quaternion targetRotation = bedAnchor.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 basePosition = Vector3.Lerp(cachedPlayerPosition, targetPosition, t);
            float heightOffset = bedInHeightCurve != null ? bedInHeightCurve.Evaluate(t) : 0f;
            Vector3 nextPosition = basePosition + Vector3.up * heightOffset;
            controller.transform.position = nextPosition;
            controller.transform.rotation = Quaternion.Slerp(cachedPlayerRotation, targetRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalHeightOffset = bedInHeightCurve != null ? bedInHeightCurve.Evaluate(1f) : 0f;
        Vector3 finalPosition = targetPosition + Vector3.up * finalHeightOffset;
        finalPosition.x = bedAnchor.position.x;
        controller.transform.position = finalPosition;
        controller.transform.rotation = targetRotation;

        while (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool reachedIdle = !string.IsNullOrEmpty(bedIdleStateName) && stateInfo.IsName(bedIdleStateName);
            if (reachedIdle || (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f))
            {
                AlignPlayerWithBedAnchorX(force: true);
                break;
            }
            AlignPlayerWithBedAnchorX(force: true);
            yield return null;
        }

        isPlayerInBed = true;
        isTransitioning = false;

        OpenPanel();
    }

    IEnumerator ExitBedSequence(PlayerController controller, Animator animator)
    {
        if (controller == null || animator == null || bedAnchor == null)
        {
            SetEmoteButtonsTemporarilyDisabled(false);
            yield break;
        }

        isTransitioning = true;
        ResetBedIdleSleepTimer(true);

        SetEmoteButtonsTemporarilyDisabled(true);

        if (!string.IsNullOrEmpty(bedOutTriggerName))
            animator.SetTrigger(bedOutTriggerName);

        Vector3 startPosition = controller.transform.position;
        Quaternion startRotation = controller.transform.rotation;
        Vector3 targetPosition = cachedPlayerPosition;
        Quaternion targetRotation = cachedPlayerRotation;
        float duration = Mathf.Max(0.01f, bedTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, t);
            float heightOffset = bedInHeightCurve != null ? bedInHeightCurve.Evaluate(1f - t) : 0f;
            controller.transform.position = basePosition + Vector3.up * heightOffset;
            controller.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        controller.transform.position = targetPosition;
        controller.transform.rotation = targetRotation;

        while (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool finishedExit = !animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f;
            bool leftBedIdleState = string.IsNullOrEmpty(bedIdleStateName) || !stateInfo.IsName(bedIdleStateName);
            bool reachedMovementIdleState = !string.IsNullOrEmpty(movementIdleStateName) && stateInfo.IsName(movementIdleStateName);

            if ((finishedExit && leftBedIdleState) || reachedMovementIdleState)
                break;

            yield return null;
        }

        PlayerController.SetGlobalInputEnabled(true);
        isPlayerInBed = false;
        isTransitioning = false;

        SetEmoteButtonsTemporarilyDisabled(false);
    }

    void StartSleep()
    {
        if (clock == null) return;
        StartCoroutine(SleepRoutine());
    }

    IEnumerator SleepRoutine()
    {
        if (!CanSleep())
        {
            Debug.Log("It's not time to sleep yet.");
            yield break;
        }

        // Pause the game clock while the player sleeps
        clock.SetTimeScale(0f);

        // Remove any dropped materials that were not collected.
        // MaterialSpawnManager listens for sleep-based day advancement to spawn
        // new drops after this cleanup.
        DropMaterialSaveManager.Instance?.ClearDropsForScene(SceneManager.GetActiveScene().name);

        ClosePanel();
        if (transitionUI != null)
        {
            void OnDayShownHandler()
            {
                // Advance the day and notify sleep-specific listeners.
                clock.SetTimeAndAdvanceDay(sleepEndMinutes);
                clock.TriggerSleepAdvancedDay();
                AcquireRecipes();
                transitionUI.OnDayShown -= OnDayShownHandler;
            }

            transitionUI.OnDayShown += OnDayShownHandler;
            transitionUI.PlayTransition(clock.currentDay + 1);
        }
        else
        {
            // Advance the day and notify sleep-specific listeners.
            clock.SetTimeAndAdvanceDay(sleepEndMinutes);
            clock.TriggerSleepAdvancedDay();
            AcquireRecipes();
        }

        if (transitionUI != null)
            yield return new WaitUntil(() => !transitionUI.IsTransitionRunning);

        // Resume the game clock before re-enabling player input
        float resumeScale = 1f;
        if (clock.timeScales != null && clock.timeScales.Length > 0)
            resumeScale = clock.timeScales[0];

        clock.SetTimeScale(resumeScale);

        if (playerController != null && playerAnimator != null && bedAnchor != null)
        {
            yield return ExitBedSequence(playerController, playerAnimator);
        }
        else
        {
            PlayerController.SetGlobalInputEnabled(true);
            isPlayerInBed = false;
            isTransitioning = false;
            SetEmoteButtonsTemporarilyDisabled(false);
        }
    }

    bool CanSleep()
    {
        float minutes = clock.currentMinutes;
        return minutes >= sleepStartMinutes || minutes < sleepEndMinutes;
    }

    void UpdateSleepButtonState()
    {
        if (sleepPrompt != null)
            sleepPrompt.SetYesButtonInteractable(CanSleep());
    }

    void LateUpdate()
    {
        AlignPlayerWithBedAnchorX();
    }

    private void AlignPlayerWithBedAnchorX(bool force = false)
    {
        if (playerController == null || bedAnchor == null)
            return;

        if (!force && (!isPlayerInBed || isTransitioning))
            return;

        Transform playerTransform = playerController.transform;
        Vector3 position = playerTransform.position;
        float anchorX = bedAnchor.position.x;

        if (!Mathf.Approximately(position.x, anchorX))
        {
            position.x = anchorX;
            playerTransform.position = position;
        }
    }

    void AcquireRecipes()
    {
        // TODO: Implement recipe acquisition based on Cozy/Nature values, day count, and milestones.
        // This placeholder keeps the logic extendable as described in MD/_Concept.md.
    }

    void OnDisable()
    {
        SetEmoteButtonsTemporarilyDisabled(false);
    }

    public void ForceExitBedImmediate()
    {
        if (!isPlayerInBed && !isTransitioning)
        {
            return;
        }

        StopAllCoroutines();
        ResetBedIdleSleepTimer(true);
        ClosePanel();

        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(bedInTriggerName))
            {
                playerAnimator.ResetTrigger(bedInTriggerName);
            }

            if (!string.IsNullOrEmpty(bedOutTriggerName))
            {
                playerAnimator.ResetTrigger(bedOutTriggerName);
            }
        }

        isPlayerInBed = false;
        isTransitioning = false;

        PlayerController.SetGlobalInputEnabled(true);
        SetEmoteButtonsTemporarilyDisabled(false);

        if (playerBlinkController != null)
        {
            playerBlinkController.NotifyActive();
        }
    }

    private void TryResolveEmoteButtonBinder()
    {
        if (playerEmoteButtonBinder != null)
            return;

#if UNITY_2023_1_OR_NEWER
        playerEmoteButtonBinder = FindFirstObjectByType<PlayerEmoteButtonBinder>();
#else
        playerEmoteButtonBinder = FindObjectOfType<PlayerEmoteButtonBinder>();
#endif
    }

    private void AutoAssignReferences()
    {
        if (sleepPrompt == null)
            sleepPrompt = FindFirstObjectByType<SleepPromptSlidePanel>(FindObjectsInactive.Include);

        if (transitionUI == null)
            transitionUI = FindFirstObjectByType<SleepTransitionUIManager>(FindObjectsInactive.Include);

        if (playerEmoteButtonBinder == null)
            playerEmoteButtonBinder = FindFirstObjectByType<PlayerEmoteButtonBinder>(FindObjectsInactive.Include);

        if (clock == null)
            clock = FindFirstObjectByType<GameClock>(FindObjectsInactive.Include);
    }

    private void SetEmoteButtonsTemporarilyDisabled(bool disabled)
    {
        TryResolveEmoteButtonBinder();
        playerEmoteButtonBinder?.SetButtonsTemporarilyDisabled(disabled);
    }
}
