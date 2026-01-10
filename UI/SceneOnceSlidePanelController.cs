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
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] OnEnable start. slidePanel={(slidePanel == null ? "null" : slidePanel.name)} exitButton={(exitButton == null ? "null" : exitButton.name)}");
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Subscribing to SlideOutCompleted. IsAnyPanelOpen={slideManager.IsAnyPanelOpen}");
            slideManager.SlideOutCompleted += HandleSlideOutCompleted;
        }
        else
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] SlideTransitionManager.Instance is null.");
        }

        TryShowPanel();
    }

    void OnDisable()
    {
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] OnDisable.");
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
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] TryShowPanel start. hasShown={hasShown} waitingForSlideOut={waitingForSlideOut}");
        if (hasShown)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Panel already shown; skipping.");
            return;
        }

        if (slidePanel != null && !slidePanel.gameObject.activeSelf)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Activating slidePanel gameObject.");
            slidePanel.gameObject.SetActive(true);
        }
        else if (slidePanel == null)
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] slidePanel is null.");
        }

        var hasSeen = HasSeenPanel();
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] HasSeenPanel={hasSeen}.");
        if (hasSeen)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Panel already seen; hiding immediately.");
            HidePanelImmediate();
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null && slideManager.IsAnyPanelOpen)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Another panel is open; waiting for SlideOutCompleted.");
            waitingForSlideOut = true;
            return;
        }
        else if (slideManager == null)
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] SlideTransitionManager.Instance is null in TryShowPanel.");
        }

        ShowPanel();
    }

    bool HasSeenPanel()
    {
        var saveGameManager = SaveGameManager.Instance;
        if (saveGameManager == null)
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] SaveGameManager.Instance is null.");
        }
        return saveGameManager != null && saveGameManager.HasSeenSceneOnceSlidePanel;
    }

    void ShowPanel()
    {
        if (slidePanel == null)
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] ShowPanel called with null slidePanel.");
            return;
        }

        if (!slidePanel.gameObject.activeSelf)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Activating slidePanel gameObject before SlideIn.");
            slidePanel.gameObject.SetActive(true);
        }

        hasShown = true;
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] SlideIn triggered. hasShown={hasShown}");
        slidePanel.SlideIn();

        if (exitButton != null)
        {
            exitButton.interactable = true;
        }
        else
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] exitButton is null in ShowPanel.");
        }
    }

    void HandleSlideOutCompleted()
    {
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] HandleSlideOutCompleted. waitingForSlideOut={waitingForSlideOut}");
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
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] HidePanelImmediate closing panel immediately.");
            slidePanel.CloseImmediate();
        }
        else
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] HidePanelImmediate called with null slidePanel.");
        }

        if (exitButton != null)
        {
            exitButton.interactable = false;
        }
    }

    void ClosePanel()
    {
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] ClosePanel invoked.");
        var saveGameManager = SaveGameManager.Instance;
        if (saveGameManager != null)
        {
            saveGameManager.HasSeenSceneOnceSlidePanel = true;
            var slotKey = saveGameManager.CurrentSlotKey;
            if (!string.IsNullOrEmpty(slotKey))
            {
                Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] Saving slot '{slotKey}'.");
                saveGameManager.Save(slotKey);
            }
            else
            {
                Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] CurrentSlotKey is null or empty; skipping save.");
            }
        }
        else
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] SaveGameManager.Instance is null; cannot persist HasSeenSceneOnceSlidePanel.");
        }

        if (exitButton != null)
        {
            exitButton.interactable = false;
        }

        if (slidePanel != null)
        {
            Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] SlideOut triggered.");
            slidePanel.SlideOut();
        }
        else
        {
            Debug.LogWarning($"[{nameof(SceneOnceSlidePanelController)}] slidePanel is null in ClosePanel.");
        }
    }
}
