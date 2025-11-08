using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trigger for sleeping in the bed. Allows sleeping only during specified hours
/// and advances the day when sleep is triggered.
/// Advancing the day fires <see cref="GameClock.OnDayChanged"/>, which is
/// used by <see cref="MaterialSpawnManager"/> to regenerate random materials.
/// A separate sleep-specific event <see cref="GameClock.OnSleepAdvancedDay"/>
/// is invoked after the day advances so systems can react only to sleeping.
/// </summary>
public class BedTrigger : MonoBehaviour, IInteractable, IInteractionPromptDataProvider
{
    [Header("Interaction Prompt")]
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float promptOffset = 1f;
    [SerializeField] private string promptLocalizationKey = string.Empty;

    [Header("Interaction UI")]
    public GameObject interactionPanel;
    public bool isPlayerNearby = false;
    bool isPanelOpen = false;
    bool isWaitingForSleepAnimation = false;
    bool isWaitingForStandUpAnimation = false;
    bool isInBedIdleState = false;
    Canvas cachedPanelCanvas;

    private PlayerController cachedPlayerController;
    private bool cachedPlayerInputState = true;
    private bool hasCachedPlayerInputState;
    private bool didDisablePlayerInput;

    private SharedInteractionPromptController promptController;

    [Header("Sleep Settings")]
    public int sleepStartMinutes = 20 * 60; // 8:00 PM
    public int sleepEndMinutes = 6 * 60;   // 6:00 AM
    public GameClock clock;

    [Header("Sleep Animation")]
    [SerializeField] private Animator sleepAnimator;
    [SerializeField] private string sleepStateName = string.Empty;
    [SerializeField, Range(0f, 1f)] private float sleepStateNormalizedTimeThreshold = 0.99f;
    [SerializeField, Tooltip("Animator trigger name(s) used to start the sleep animation.")]
    private string sleepTriggerName = string.Empty;
    [SerializeField, Tooltip("Optional additional animator triggers that should be fired together.")]
    private string[] additionalSleepTriggerNames = System.Array.Empty<string>();
    [SerializeField, Tooltip("Animator trigger name used to start the stand-up animation once the bed idle pose is active.")]
    private string wakeTriggerName = string.Empty;
    [SerializeField, Tooltip("Optional additional animator triggers fired when starting the stand-up animation.")]
    private string[] additionalWakeTriggerNames = System.Array.Empty<string>();
    [SerializeField, Tooltip("Animator state that represents the stand-up animation used to determine when the player regains control.")]
    private string wakeStateName = string.Empty;
    [SerializeField, Range(0f, 1f)] private float wakeStateNormalizedTimeThreshold = 0.99f;

    private bool hasWarnedMissingSleepTrigger;
    private bool hasWarnedMissingWakeTrigger;

    [Header("Player Positioning")]
    [SerializeField, Tooltip("Player anchor used when initiating the sleep animation. Defaults to this transform if unset.")]
    private Transform sleepAnchor;

    [Header("Panel Buttons")]
    public Button sleepButton;
    public Button cancelButton;

    [Header("Panel Area")]
    [Tooltip("Main clickable area of the interaction panel. Clicking outside this area will close the panel.")]
    public RectTransform panelContentArea;
    [Tooltip("Optional camera used for UI raycasts. Leave empty for Screen Space Overlay canvases.")]
    public Camera uiCamera;

    [Header("Transition UI")]
    public SleepTransitionUIManager transitionUI;

    public Transform SleepAnchor => sleepAnchor != null ? sleepAnchor : transform;

    void Start()
    {
        clock = GameClock.Instance;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        if (panelContentArea != null)
            cachedPanelCanvas = panelContentArea.GetComponentInParent<Canvas>();

        if (promptAnchor == null)
            promptAnchor = transform;

        promptController = SharedInteractionPromptController.Instance;

        if (sleepAnchor == null)
        {
            sleepAnchor = transform;
        }

        ResolveSleepAnimator();
        ResolvePlayerController();
    }

    void Update()
    {
        if (isPanelOpen)
        {
            UpdateSleepButtonState();
            HandlePanelInput();
        }
    }

    public void Interact()
    {
        ResolveSleepAnimator();

        if (isWaitingForSleepAnimation || isWaitingForStandUpAnimation)
            return;

        if (isInBedIdleState)
        {
            HandleStandUpInteraction();
            return;
        }

        bool shouldAwaitSleepAnimation = sleepAnimator != null && !string.IsNullOrEmpty(sleepStateName);

        if (shouldAwaitSleepAnimation)
        {
            bool hasTriggered = TryTriggerSleepAnimation();

            if (!hasTriggered && !HasConfiguredSleepTrigger() && !hasWarnedMissingSleepTrigger)
            {
                Debug.LogWarning($"{nameof(BedTrigger)} on '{name}' has a sleep animator configured but no trigger name. The sleep animation will not be started automatically.", this);
                hasWarnedMissingSleepTrigger = true;
            }

            StartCoroutine(WaitForSleepAnimation());
            return;
        }

        CompleteSleepInteraction();
    }

    void HandleStandUpInteraction()
    {
        ResolveSleepAnimator();

        bool shouldAwaitStandUpAnimation = sleepAnimator != null && !string.IsNullOrEmpty(wakeStateName);
        bool hasTriggered = TryTriggerStandUpAnimation();

        if (!hasTriggered && !HasConfiguredWakeTrigger() && !hasWarnedMissingWakeTrigger)
        {
            Debug.LogWarning($"{nameof(BedTrigger)} on '{name}' has a wake animator configured but no trigger name. The stand-up animation will not be started automatically.", this);
            hasWarnedMissingWakeTrigger = true;
        }

        if (hasTriggered)
        {
            isWaitingForStandUpAnimation = true;
            promptController?.HidePrompt(this);
        }

        if (shouldAwaitStandUpAnimation && hasTriggered)
        {
            StartCoroutine(WaitForStandUpAnimation());
        }
        else
        {
            ExitBedIdleState();
        }
    }

    IEnumerator WaitForSleepAnimation()
    {
        isWaitingForSleepAnimation = true;

        int layerIndex = 0;
        bool hasReachedState = false;

        while (sleepAnimator != null)
        {
            if (!sleepAnimator.isActiveAndEnabled)
                break;

            var stateInfo = sleepAnimator.GetCurrentAnimatorStateInfo(layerIndex);

            if (stateInfo.IsName(sleepStateName))
            {
                hasReachedState = true;

                if (!sleepAnimator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= sleepStateNormalizedTimeThreshold)
                    break;
            }
            else if (hasReachedState)
            {
                break;
            }

            yield return null;
        }

        isWaitingForSleepAnimation = false;
        EnterBedIdleState();
        CompleteSleepInteraction();
    }

    public void OnSleepAnimationComplete()
    {
        if (isPanelOpen)
            return;

        isWaitingForSleepAnimation = false;
        EnterBedIdleState();
        CompleteSleepInteraction();
    }

    IEnumerator WaitForStandUpAnimation()
    {
        isWaitingForStandUpAnimation = true;

        int layerIndex = 0;
        bool hasReachedState = false;

        while (sleepAnimator != null)
        {
            if (!sleepAnimator.isActiveAndEnabled)
                break;

            var stateInfo = sleepAnimator.GetCurrentAnimatorStateInfo(layerIndex);

            if (stateInfo.IsName(wakeStateName))
            {
                hasReachedState = true;

                if (!sleepAnimator.IsInTransition(layerIndex) && stateInfo.normalizedTime >= wakeStateNormalizedTimeThreshold)
                    break;
            }
            else if (hasReachedState)
            {
                break;
            }

            yield return null;
        }

        isWaitingForStandUpAnimation = false;
        ExitBedIdleState();
    }

    public void OnStandUpAnimationComplete()
    {
        if (!isInBedIdleState)
            return;

        isWaitingForStandUpAnimation = false;
        ExitBedIdleState();
    }

    bool HasConfiguredSleepTrigger()
    {
        if (!string.IsNullOrEmpty(sleepTriggerName))
            return true;

        if (additionalSleepTriggerNames == null)
            return false;

        for (int i = 0; i < additionalSleepTriggerNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(additionalSleepTriggerNames[i]))
                return true;
        }

        return false;
    }

    bool HasConfiguredWakeTrigger()
    {
        if (!string.IsNullOrEmpty(wakeTriggerName))
            return true;

        if (additionalWakeTriggerNames == null)
            return false;

        for (int i = 0; i < additionalWakeTriggerNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(additionalWakeTriggerNames[i]))
                return true;
        }

        return false;
    }

    bool TryTriggerSleepAnimation()
    {
        ResolveSleepAnimator();

        if (sleepAnimator == null)
            return false;

        bool hasTriggered = false;

        if (!string.IsNullOrEmpty(sleepTriggerName))
        {
            sleepAnimator.ResetTrigger(sleepTriggerName);
            sleepAnimator.SetTrigger(sleepTriggerName);
            hasTriggered = true;
        }

        if (additionalSleepTriggerNames != null)
        {
            for (int i = 0; i < additionalSleepTriggerNames.Length; i++)
            {
                string trigger = additionalSleepTriggerNames[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;

                sleepAnimator.ResetTrigger(trigger);
                sleepAnimator.SetTrigger(trigger);
                hasTriggered = true;
            }
        }

        return hasTriggered;
    }

    bool TryTriggerStandUpAnimation()
    {
        ResolveSleepAnimator();

        if (sleepAnimator == null)
            return false;

        bool hasTriggered = false;

        if (!string.IsNullOrEmpty(wakeTriggerName))
        {
            sleepAnimator.ResetTrigger(wakeTriggerName);
            sleepAnimator.SetTrigger(wakeTriggerName);
            hasTriggered = true;
        }

        if (additionalWakeTriggerNames != null)
        {
            for (int i = 0; i < additionalWakeTriggerNames.Length; i++)
            {
                string trigger = additionalWakeTriggerNames[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;

                sleepAnimator.ResetTrigger(trigger);
                sleepAnimator.SetTrigger(trigger);
                hasTriggered = true;
            }
        }

        return hasTriggered;
    }

    void EnterBedIdleState()
    {
        if (isInBedIdleState)
            return;

        isInBedIdleState = true;
        LockPlayerMovement();
    }

    void ExitBedIdleState()
    {
        if (!isInBedIdleState)
            return;

        isInBedIdleState = false;
        isWaitingForStandUpAnimation = false;
        UnlockPlayerMovement();
        ShowPromptIfNearby();
    }

    void LockPlayerMovement()
    {
        var player = ResolvePlayerController();
        if (player == null)
            return;

        if (!hasCachedPlayerInputState)
        {
            cachedPlayerInputState = player.IsInputEnabled;
            hasCachedPlayerInputState = true;
        }

        didDisablePlayerInput = false;

        if (player.IsInputEnabled)
        {
            player.SetInputEnabled(false);
            didDisablePlayerInput = true;
        }
    }

    void UnlockPlayerMovement()
    {
        if (!hasCachedPlayerInputState)
            return;

        var player = ResolvePlayerController();
        if (player != null && didDisablePlayerInput && cachedPlayerInputState && !player.IsInputEnabled)
            player.SetInputEnabled(true);

        hasCachedPlayerInputState = false;
        didDisablePlayerInput = false;
    }

    PlayerController ResolvePlayerController()
    {
        if (cachedPlayerController != null && cachedPlayerController.isActiveAndEnabled)
            return cachedPlayerController;

        cachedPlayerController = FindFirstObjectByType<PlayerController>();
        return cachedPlayerController;
    }

    void CompleteSleepInteraction()
    {
        if (isPanelOpen)
            return;

        OpenPanel();
        SetupPanelButtons();
        UpdateSleepButtonState();
    }

    void OpenPanel()
    {
        isPanelOpen = true;

        promptController?.HidePrompt(this);

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);

            // Disable player input while panel is open
            PlayerController.SetGlobalInputEnabled(false);
        }
    }

    void ClosePanel()
    {
        isPanelOpen = false;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        ShowPromptIfNearby();

        PlayerController.SetGlobalInputEnabled(true);
    }

    void HandlePanelInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverPanelContent())
        {
            ClosePanel();
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !IsPointerOverPanelContent(touch.position))
            {
                ClosePanel();
            }
        }
    }

    bool IsPointerOverPanelContent()
    {
        return IsPointerOverPanelContent(Input.mousePosition);
    }

    bool IsPointerOverPanelContent(Vector2 screenPosition)
    {
        if (panelContentArea == null)
            return true; // Without a defined area we assume the click is valid.

        Camera cameraToUse = uiCamera;
        if (cachedPanelCanvas != null)
        {
            if (cachedPanelCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cameraToUse = null;
            }
            else if (cameraToUse == null)
            {
                cameraToUse = cachedPanelCanvas.worldCamera;
            }
        }

        return RectTransformUtility.RectangleContainsScreenPoint(panelContentArea, screenPosition, cameraToUse);
    }

    void SetupPanelButtons()
    {
        if (sleepButton != null)
        {
            sleepButton.onClick.RemoveAllListeners();
            sleepButton.onClick.AddListener(StartSleep);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(ClosePanel);
        }
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
        DropMaterialSaveManager.Instance?.ClearAllDrops();

        ClosePanel();
        BedInteractionController bedController = null;

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
            transitionUI.BeginSleepSequence(this);
            transitionUI.PlayTransition(clock.currentDay + 1);
        }
        else
        {
            // Advance the day and notify sleep-specific listeners.
            clock.SetTimeAndAdvanceDay(sleepEndMinutes);
            clock.TriggerSleepAdvancedDay();
            AcquireRecipes();

            bedController = FindFirstObjectByType<BedInteractionController>();
            if (bedController != null)
            {
                bedController.BeginSleepSequence(this);
            }
        }

        PlayerController.SetGlobalInputEnabled(false);
        promptController?.HidePrompt(this);

        if (transitionUI != null)
            yield return new WaitUntil(() => !transitionUI.IsTransitionRunning);

        // Resume the game clock before re-enabling player input
        float resumeScale = 1f;
        if (clock.timeScales != null && clock.timeScales.Length > 0)
            resumeScale = clock.timeScales[0];

        clock.SetTimeScale(resumeScale);
        PlayerController.SetGlobalInputEnabled(true);
        ShowPromptIfNearby();

        if (transitionUI != null)
        {
            transitionUI.CompleteSleepSequence(this);
        }
        else
        {
            if (bedController != null)
            {
                bedController.EndSleepSequence(this);
            }
        }

        ExitBedIdleState();
    }

    bool CanSleep()
    {
        float minutes = clock.currentMinutes;
        return minutes >= sleepStartMinutes || minutes < sleepEndMinutes;
    }

    void UpdateSleepButtonState()
    {
        if (sleepButton != null)
            sleepButton.interactable = CanSleep();
    }

    void AcquireRecipes()
    {
        // TODO: Implement recipe acquisition based on Cozy/Nature values, day count, and milestones.
        // This placeholder keeps the logic extendable as described in MD/_Concept.md.
    }

    public bool TryGetInteractionPromptData(out InteractionPromptData data)
    {
        var anchor = promptAnchor != null ? promptAnchor : transform;
        data = new InteractionPromptData(anchor, promptOffset, promptLocalizationKey);
        return true;
    }

    private void ShowPromptIfNearby()
    {
        if (!isPlayerNearby || promptController == null)
            return;

        if (TryGetInteractionPromptData(out var promptData) && promptData.IsValid)
            promptController.ShowPrompt(this, promptData);
    }

    void OnDisable()
    {
        promptController?.HidePrompt(this);

        if (isInBedIdleState)
        {
            UnlockPlayerMovement();
            isInBedIdleState = false;
        }

        isWaitingForSleepAnimation = false;
        isWaitingForStandUpAnimation = false;
    }

    void ResolveSleepAnimator()
    {
        if (sleepAnimator != null)
            return;

        var bedController = FindFirstObjectByType<BedInteractionController>();
        if (bedController != null)
        {
            sleepAnimator = bedController.GetComponent<Animator>();

            if (sleepAnimator == null)
                sleepAnimator = bedController.GetComponentInChildren<Animator>();
        }

        if (sleepAnimator == null)
            sleepAnimator = GetComponent<Animator>();

        if (sleepAnimator == null)
            sleepAnimator = GetComponentInChildren<Animator>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (sleepAnimator == null)
        {
            ResolveSleepAnimator();

            if (sleepAnimator == null)
            {
                Debug.LogWarning($"{nameof(BedTrigger)} on '{name}' is missing a sleep animator reference.", this);
            }
        }
    }
#endif
}
