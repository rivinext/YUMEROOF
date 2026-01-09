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
        if (!ShouldShowPanel())
        {
            return;
        }

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

        HasSeenOpeningPanel = true;
        SaveGameManager.Instance?.SaveCurrentSlot();

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
    }
}
