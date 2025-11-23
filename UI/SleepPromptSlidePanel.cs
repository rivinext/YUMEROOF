using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wraps an <see cref="InteractionSlidePanel"/> to present a simple Yes/No prompt
/// asking whether the player wants to sleep.
/// </summary>
public class SleepPromptSlidePanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private InteractionSlidePanel slidePanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private GameObject backgroundDimmer;
    [SerializeField] private Button backgroundDismissButton;

    [Header("Prompt Text")]
    [SerializeField, TextArea]
    private string defaultPrompt = "今日はもう寝る？";

    [Header("Input Handling")]
    [SerializeField] private bool closeOnEscape = true;

    private Action yesAction;
    private Action noAction;
    private bool promptVisible;

    /// <summary>
    /// True when the prompt is currently visible.
    /// </summary>
    public bool IsVisible => promptVisible && slidePanel != null && slidePanel.IsOpen;

    void Reset()
    {
        if (slidePanel == null)
        {
            slidePanel = GetComponentInChildren<InteractionSlidePanel>();
        }
    }

    void Awake()
    {
        if (slidePanel == null)
        {
            slidePanel = GetComponentInChildren<InteractionSlidePanel>();
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(HandleYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(HandleNoClicked);
        }

        if (backgroundDismissButton != null)
        {
            backgroundDismissButton.onClick.AddListener(HandleNoClicked);
        }

        SetBackgroundDimmer(false);
    }

    void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(HandleYesClicked);

        if (noButton != null)
            noButton.onClick.RemoveListener(HandleNoClicked);

        if (backgroundDismissButton != null)
            backgroundDismissButton.onClick.RemoveListener(HandleNoClicked);
    }

    void Update()
    {
        if (!promptVisible || !closeOnEscape)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleNoClicked();
        }
    }

    /// <summary>
    /// Displays the prompt with optional custom text and callbacks.
    /// </summary>
    public void ShowPrompt(Action onYes, Action onNo, string customPrompt = null)
    {
        yesAction = onYes;
        noAction = onNo;

        if (promptLabel != null)
        {
            promptLabel.text = string.IsNullOrWhiteSpace(customPrompt) ? defaultPrompt : customPrompt;
        }

        promptVisible = true;
        SetBackgroundDimmer(true);
        slidePanel?.SlideIn();
    }

    /// <summary>
    /// Hides the prompt, clearing any cached callbacks.
    /// </summary>
    public void Hide()
    {
        promptVisible = false;
        SetBackgroundDimmer(false);
        slidePanel?.SlideOut();
        yesAction = null;
        noAction = null;
    }

    /// <summary>
    /// Enables or disables the Yes button.
    /// </summary>
    public void SetYesButtonInteractable(bool interactable)
    {
        if (yesButton != null)
        {
            yesButton.interactable = interactable;
        }
    }

    /// <summary>
    /// Overrides the displayed prompt text at runtime.
    /// </summary>
    public void SetPromptText(string text)
    {
        if (promptLabel == null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            promptLabel.text = defaultPrompt;
        }
        else
        {
            promptLabel.text = text;
        }
    }

    private void HandleYesClicked()
    {
        if (!promptVisible)
            return;

        yesAction?.Invoke();
    }

    private void HandleNoClicked()
    {
        if (!promptVisible)
            return;

        noAction?.Invoke();
    }

    private void SetBackgroundDimmer(bool visible)
    {
        if (backgroundDimmer != null)
        {
            backgroundDimmer.SetActive(visible);
        }
    }
}
