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

    [Header("Bed Animation")]
    [SerializeField] private BedAnimationDriver bedAnimationDriver;

    private PlayerController player;
    private PlayerBedStateController playerBedStateController;

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

        if (bedAnimationDriver == null)
            bedAnimationDriver = GetComponent<BedAnimationDriver>();
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
        EnsurePlayerReference();

        if (bedAnimationDriver != null)
        {
            void OnBedInCompleted()
            {
                BeginBedIdleState();
                OpenPanel();
                SetupPanelButtons();
                UpdateSleepButtonState();
            }

            bedAnimationDriver.PlayBedIn(OnBedInCompleted);
        }
        else
        {
            BeginBedIdleState();
            OpenPanel();
            SetupPanelButtons();
            UpdateSleepButtonState();
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
    }

    public void ClosePanel()
    {
        isPanelOpen = false;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        ShowPromptIfNearby();

        PlayerController.SetGlobalInputEnabled(true);
    }

    void EnsurePlayerReference()
    {
        if (player != null)
            return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            playerBedStateController = playerObj.GetComponent<PlayerBedStateController>();
        }
    }

    void BeginBedIdleState()
    {
        if (player == null)
        {
            return;
        }

        Transform bedAnchor = null;
        if (bedAnimationDriver != null && bedAnimationDriver.AnchorPoint != null)
        {
            bedAnchor = bedAnimationDriver.AnchorPoint;
        }

        if (bedAnchor == null)
        {
            bedAnchor = transform;
        }

        if (playerBedStateController == null)
        {
            playerBedStateController = player.GetComponent<PlayerBedStateController>();
        }

        if (playerBedStateController != null)
        {
            playerBedStateController.BeginBedIdle(bedAnchor, bedAnimationDriver);
        }
        else
        {
            Debug.LogWarning("PlayerBedStateController not found on player when trying to begin bed idle state.");
        }
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
