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
        if (hasShown)
        {
            return;
        }

        if (slidePanel != null && !slidePanel.gameObject.activeSelf)
        {
            slidePanel.gameObject.SetActive(true);
        }

        if (HasSeenPanel())
        {
            HidePanelImmediate();
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null && slideManager.IsAnyPanelOpen)
        {
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
            return;
        }

        if (!slidePanel.gameObject.activeSelf)
        {
            slidePanel.gameObject.SetActive(true);
        }

        hasShown = true;
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

        waitingForSlideOut = false;
        TryShowPanel();
    }

    void HidePanelImmediate()
    {
        if (slidePanel != null)
        {
            slidePanel.CloseImmediate();
        }

        if (exitButton != null)
        {
            exitButton.interactable = false;
        }
    }

    void ClosePanel()
    {
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
