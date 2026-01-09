using System;
using UnityEngine;
using UnityEngine.UI;

public class StoryOpeningPanelOnceController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    public bool HasSeenOpeningPanel { get; set; }

    private bool isWaitingForSlideOut;

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        TryShowPanelWhenReady();
    }

    void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        UnsubscribeFromSlideOut();
    }

    private void TryShowPanelWhenReady()
    {
        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        Debug.Log($"[StoryOpeningPanelOnceController] Before ShouldShowPanel: slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel}");

        if (!ShouldShowPanel())
        {
            Debug.Log($"[StoryOpeningPanelOnceController] After ShouldShowPanel: slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel}, willShow=false");
            return;
        }

        Debug.Log($"[StoryOpeningPanelOnceController] After ShouldShowPanel: slotKey='{slotKey}', hasSeenOpeningPanel={HasSeenOpeningPanel}, willShow=true");
        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted += HandleSlideOutCompleted;
            isWaitingForSlideOut = true;
        }
        else
        {
            ShowPanelAndSave();
        }
    }

    private void HandleSlideOutCompleted()
    {
        ShowPanelAndSave();
    }

    private void UnsubscribeFromSlideOut()
    {
        if (!isWaitingForSlideOut)
        {
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted -= HandleSlideOutCompleted;
        }

        isWaitingForSlideOut = false;
    }

    private bool ShouldShowPanel()
    {
        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        if (string.IsNullOrEmpty(slotKey) || !slotKey.StartsWith("Story", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !HasSeenOpeningPanel;
    }

    private void ShowPanelAndSave()
    {
        UnsubscribeFromSlideOut();

        if (!ShouldShowPanel())
        {
            return;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    private void ClosePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        Debug.Log($"[StoryOpeningPanelOnceController] Before setting hasSeenOpeningPanel=true (current={HasSeenOpeningPanel})");
        HasSeenOpeningPanel = true;
        Debug.Log($"[StoryOpeningPanelOnceController] After setting hasSeenOpeningPanel=true (current={HasSeenOpeningPanel})");

        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        if (string.IsNullOrEmpty(slotKey))
        {
            Debug.Log("[StoryOpeningPanelOnceController] CurrentSlotKey is empty. Skipping SaveCurrentSlot.");
            return;
        }

        Debug.Log($"[StoryOpeningPanelOnceController] CurrentSlotKey='{slotKey}'. Saving current slot.");
        SaveGameManager.Instance?.SaveCurrentSlot();
    }
}
