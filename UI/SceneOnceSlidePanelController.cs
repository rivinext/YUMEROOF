using UnityEngine;
using UnityEngine.UI;

public class SceneOnceSlidePanelController : MonoBehaviour
{
    [SerializeField] private UISlidePanel slidePanel;
    [SerializeField] private Button exitButton;

    private bool waitingForSlideOut;
    private bool hasShown;

    void OnEnable()
    {
        Debug.Log("[SceneOnceSlidePanelController] OnEnable");
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted += HandleSlideOutCompleted;
        }

        TryShowPanel();
    }

    void OnDisable()
    {
        Debug.Log("[SceneOnceSlidePanelController] OnDisable");
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ClosePanel);
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutCompleted -= HandleSlideOutCompleted;
        }
    }

    void TryShowPanel()
    {
        Debug.Log("[SceneOnceSlidePanelController] TryShowPanel");
        if (hasShown)
        {
            Debug.Log("[SceneOnceSlidePanelController] Panel already shown, skipping.");
            return;
        }

        if (HasSeenPanel())
        {
            Debug.Log("[SceneOnceSlidePanelController] Panel already seen in save. Hiding immediately.");
            HidePanelImmediate();
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null && slideManager.IsAnyPanelOpen)
        {
            Debug.Log("[SceneOnceSlidePanelController] SlideTransitionManager is busy. Waiting for slide out.");
            waitingForSlideOut = true;
            return;
        }

        ShowPanel();
    }

    bool HasSeenPanel()
    {
        var saveGameManager = SaveGameManager.Instance;
        return saveGameManager != null && saveGameManager.HasSeenSceneOnceSlidePanel;
    }

    void ShowPanel()
    {
        if (slidePanel == null)
        {
            Debug.LogWarning("[SceneOnceSlidePanelController] Slide panel reference is missing.");
            return;
        }

        if (!slidePanel.gameObject.activeSelf)
        {
            slidePanel.gameObject.SetActive(true);
        }

        hasShown = true;
        Debug.Log("[SceneOnceSlidePanelController] Sliding in panel.");
        slidePanel.SlideIn();

        if (exitButton != null)
        {
            exitButton.interactable = true;
        }
    }

    void HandleSlideOutCompleted()
    {
        if (!waitingForSlideOut)
        {
            return;
        }

        Debug.Log("[SceneOnceSlidePanelController] Slide out completed. Attempting to show panel.");
        waitingForSlideOut = false;
        TryShowPanel();
    }

    void HidePanelImmediate()
    {
        if (slidePanel != null)
        {
            Debug.Log("[SceneOnceSlidePanelController] CloseImmediate called.");
            slidePanel.CloseImmediate();
        }

        if (exitButton != null)
        {
            exitButton.interactable = false;
        }
    }

    void ClosePanel()
    {
        Debug.Log("[SceneOnceSlidePanelController] ClosePanel called.");
        var saveGameManager = SaveGameManager.Instance;
        if (saveGameManager != null)
        {
            saveGameManager.HasSeenSceneOnceSlidePanel = true;
            var slotKey = saveGameManager.CurrentSlotKey;
            if (!string.IsNullOrEmpty(slotKey))
            {
                saveGameManager.Save(slotKey);
            }
        }

        if (exitButton != null)
        {
            exitButton.interactable = false;
        }

        if (slidePanel != null)
        {
            slidePanel.SlideOut();
        }
    }
}
