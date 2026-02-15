using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// ビルおばけのインタラクション処理。
/// フォーカス状態に応じて会話を開始し、HintSystem をフォールバックとして利用します。
/// </summary>
[RequireComponent(typeof(GhostFloatAnimator))]
public class BuildingGhostInteractable : MonoBehaviour, IFocusableInteractable
{
    [Header("ヒント設定")]
    [SerializeField] private TriggerType hintTriggerType = TriggerType.StatusCheck;
    [SerializeField] private string defaultTextID = "hint_default_greeting";

    [Header("フォーカス設定")]
    [SerializeField] private bool autoFadePanelOnFocus = true;
    [SerializeField] private bool autoStartDialogueOnFocus = true;
    [SerializeField] private bool requireMovementInputToClose = true;
    [SerializeField] private string fallbackSpeakerName = "Building Ghost";
    [SerializeField] private UnityEvent onFocused;
    [SerializeField] private UnityEvent onBlurred;

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private string currentHintID = "";
    [SerializeField] private string currentTextID = "";
    [SerializeField] private string localizedText = "";

    [Header("会話ローカライズ設定")]
    [SerializeField] private string ghostDialogueTableName = "GhostDialogues";

    private HintSystem.HintData cachedHint;
    private string cachedLocalizedText;
    private bool hintInitialized = false;

    private readonly string localizationTableName = "Hints";

    private PlayerRayInteractor currentInteractor;
    private InteractionUIController cachedInteractionUI;
    private bool isFocused;
    private bool dialogueActive;

    public event Action<string> HintTextLoaded;

    void Start()
    {
        if (HintSystem.Instance == null)
        {
            Debug.LogError("[BuildingGhostInteractable] HintSystem.Instance is null!");
        }

        InitializeHint();
    }

    void OnDestroy()
    {
        StopListeningToController();
    }

    public void Interact()
    {
        if (debugMode)
        {
            Debug.Log("[BuildingGhostInteractable] Interact called (no action)");
        }
    }

    public void OnFocus(PlayerRayInteractor interactor)
    {
        if (interactor == null)
            return;

        currentInteractor = interactor;
        cachedInteractionUI = interactor.InteractionUI;
        if (isFocused)
            return;

        isFocused = true;
        onFocused?.Invoke();

        if (autoFadePanelOnFocus && cachedInteractionUI != null && !cachedInteractionUI.IsPanelOpen)
        {
            cachedInteractionUI.ShowPanelOnly();
        }

        if (autoStartDialogueOnFocus)
        {
            TryStartDialogue();
        }
    }

    public void OnBlur(PlayerRayInteractor interactor)
    {
        if (!isFocused)
            return;

        isFocused = false;
        onBlurred?.Invoke();

        StopListeningToController();

        if (dialogueActive && cachedInteractionUI != null && cachedInteractionUI.CurrentTarget == this)
        {
            cachedInteractionUI.CloseInteraction();
        }
        else
        {
            cachedInteractionUI?.HidePanelOnly();
        }

        dialogueActive = false;
        cachedInteractionUI = null;
        currentInteractor = null;
    }

    void InitializeHint()
    {
        if (hintInitialized)
            return;

        if (HintSystem.Instance == null)
        {
            Debug.LogWarning("[BuildingGhostInteractable] HintSystem not available yet");
            return;
        }

        cachedHint = HintSystem.Instance.RequestHint(hintTriggerType);

        if (cachedHint != null)
        {
            currentHintID = cachedHint.id;
            currentTextID = cachedHint.textID;
            StartCoroutine(LoadLocalizedText(cachedHint.textID));
        }
        else
        {
            currentTextID = defaultTextID;
            StartCoroutine(LoadLocalizedText(defaultTextID));

            if (debugMode)
            {
                Debug.Log($"[BuildingGhostInteractable] No hint found, using default: {defaultTextID}");
            }
        }

        hintInitialized = true;
    }

    IEnumerator LoadLocalizedText(string textID)
    {
        yield return LocalizationSettings.InitializationOperation;

        var localizedString = new LocalizedString(localizationTableName, textID);
        var loadHandle = localizedString.GetLocalizedStringAsync();

        yield return loadHandle;

        if (loadHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            cachedLocalizedText = loadHandle.Result;
            localizedText = cachedLocalizedText;
            HintTextLoaded?.Invoke(cachedLocalizedText);

            if (debugMode)
            {
                Debug.Log($"[BuildingGhostInteractable] Loaded text: {cachedLocalizedText}");
            }
        }
        else
        {
            cachedLocalizedText = textID;
            localizedText = textID;

            Debug.LogWarning($"[BuildingGhostInteractable] Failed to load localized text for: {textID}");
        }
    }

    public string GetCachedHintText()
    {
        if (!hintInitialized)
        {
            InitializeHint();
        }

        return string.IsNullOrEmpty(cachedLocalizedText) ? currentTextID : cachedLocalizedText;
    }

    public string GetCurrentTextID()
    {
        return currentTextID;
    }

    public bool IsHintReady()
    {
        return hintInitialized && !string.IsNullOrEmpty(cachedLocalizedText);
    }

    public void ReloadHint()
    {
        if (!string.IsNullOrEmpty(currentTextID))
        {
            StartCoroutine(LoadLocalizedText(currentTextID));
        }
    }

    [ContextMenu("Force Refresh Hint")]
    public void ForceRefreshHint()
    {
        hintInitialized = false;
        cachedHint = null;
        cachedLocalizedText = "";
        InitializeHint();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }

    private bool TryStartDialogue()
    {
        if (cachedInteractionUI == null || dialogueActive)
            return false;

        var queue = BuildDialogueQueue();
        if (queue == null || queue.Count == 0)
            return false;

        cachedInteractionUI.SetCloseOnMovementAfterComplete(requireMovementInputToClose);
        StopListeningToController();
        cachedInteractionUI.InteractionClosed += HandleInteractionClosed;
        cachedInteractionUI.BeginInteraction(this, queue);
        dialogueActive = true;
        return true;
    }

    private Queue<InteractionUIController.InteractionLine> BuildDialogueQueue()
    {
        Queue<InteractionUIController.InteractionLine> queue = new Queue<InteractionUIController.InteractionLine>();
        var dialogueManager = BuildingGhostDialogueManager.CreateIfNeeded();
        bool localizationReady = LocalizationSettings.InitializationOperation.IsDone;
        if (dialogueManager != null && dialogueManager.TrySelectDialogue(out var lines))
        {
            foreach (var line in lines)
            {
                string resolvedMessage = line.Message;

                if (localizationReady)
                {
                    string localizedMessage = LocalizationSettings.StringDatabase.GetLocalizedString(ghostDialogueTableName, line.Message);
                    if (!string.IsNullOrEmpty(localizedMessage))
                    {
                        resolvedMessage = localizedMessage;
                    }
                    else if (debugMode)
                    {
                        Debug.LogWarning($"[BuildingGhostInteractable] Ghost dialogue localization missing. table={ghostDialogueTableName}, key={line.Message}");
                    }
                }

                queue.Enqueue(new InteractionUIController.InteractionLine(line.Speaker, resolvedMessage));
            }
        }

        if (queue.Count == 0 && TryBuildFallbackLine(out var fallback))
        {
            queue.Enqueue(fallback);
        }

        return queue;
    }

    private bool TryBuildFallbackLine(out InteractionUIController.InteractionLine line)
    {
        string text = GetCachedHintText();
        if (string.IsNullOrEmpty(text))
        {
            line = default;
            return false;
        }

        line = new InteractionUIController.InteractionLine(fallbackSpeakerName, text);
        return true;
    }

    private void HandleInteractionClosed(IInteractable closedTarget)
    {
        if (closedTarget != this)
            return;

        dialogueActive = false;
        StopListeningToController();
        if (currentInteractor != null)
        {
            currentInteractor.ReleaseHighlightIfCurrent(this);
        }
    }

    private void StopListeningToController()
    {
        if (cachedInteractionUI != null)
        {
            cachedInteractionUI.InteractionClosed -= HandleInteractionClosed;
        }
    }
}
