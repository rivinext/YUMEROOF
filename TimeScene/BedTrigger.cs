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
    Canvas cachedPanelCanvas;

    private SharedInteractionPromptController promptController;

    [Header("Player Control")]
    [SerializeField] private Transform bedAnchor;
    [SerializeField] private AnimationCurve bedInHeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private float bedTransitionDuration = 1f;
    [SerializeField] private string bedInTriggerName = "BedIn";
    [SerializeField] private string bedOutTriggerName = "BedOut";
    [SerializeField] private string bedIdleStateName = "BedIdle";

    private PlayerController playerController;
    private Animator playerAnimator;
    private Vector3 cachedPlayerPosition;
    private Quaternion cachedPlayerRotation;
    private bool isPlayerInBed;
    private bool isTransitioning;

    [Header("Sleep Settings")]
    public int sleepStartMinutes = 20 * 60; // 8:00 PM
    public int sleepEndMinutes = 6 * 60;   // 6:00 AM
    public GameClock clock;

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

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
            playerAnimator = playerObject.GetComponent<Animator>();
            if (playerController == null)
                Debug.LogWarning("BedTrigger could not find PlayerController on the player object.");
            if (playerAnimator == null)
                Debug.LogWarning("BedTrigger could not find Animator on the player object.");
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
            HandlePanelInput();
        }
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
        isPanelOpen = true;

        promptController?.HidePrompt(this);

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);

            // Disable player input while panel is open
            PlayerController.SetGlobalInputEnabled(false);
        }

        SetupPanelButtons();
        UpdateSleepButtonState();
    }

    void ClosePanel()
    {
        isPanelOpen = false;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);
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

    IEnumerator EnterBedSequence(PlayerController controller, Animator animator)
    {
        if (controller == null || animator == null || bedAnchor == null)
            yield break;

        isTransitioning = true;

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
            controller.transform.position = basePosition + Vector3.up * heightOffset;
            controller.transform.rotation = Quaternion.Slerp(cachedPlayerRotation, targetRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalHeightOffset = bedInHeightCurve != null ? bedInHeightCurve.Evaluate(1f) : 0f;
        controller.transform.position = targetPosition + Vector3.up * finalHeightOffset;
        controller.transform.rotation = targetRotation;

        while (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool reachedIdle = !string.IsNullOrEmpty(bedIdleStateName) && stateInfo.IsName(bedIdleStateName);
            if (reachedIdle || (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f))
                break;
            yield return null;
        }

        isPlayerInBed = true;
        isTransitioning = false;

        OpenPanel();
    }

    IEnumerator ExitBedSequence(PlayerController controller, Animator animator)
    {
        if (controller == null || animator == null || bedAnchor == null)
            yield break;

        isTransitioning = true;

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
            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.normalizedTime >= 1f)
                    break;
            }

            yield return null;
        }

        PlayerController.SetGlobalInputEnabled(true);
        isPlayerInBed = false;
        isTransitioning = false;

        ShowPromptIfNearby();
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
            ShowPromptIfNearby();
        }
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
    }
}
