using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class SceneOnceSlidePanelController : MonoBehaviour
{
    [SerializeField] private UISlidePanel slidePanel;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private string localizationTableName = "StandardText";
    [SerializeField] private string fieldName = "StairDialog";
    [SerializeField] private DynamicLocalizer dynamicLocalizer;
    [SerializeField] private List<string> pages = new();
    [SerializeField] private float showDelaySeconds = 0.7f;

    private bool waitingForSlideOut;
    private bool hasShown;
    private int currentPageIndex;
    private Coroutine showRoutine;

    void OnEnable()
    {
        Debug.Log($"[{nameof(SceneOnceSlidePanelController)}] OnEnable start. slidePanel={(slidePanel == null ? "null" : slidePanel.name)} exitButton={(exitButton == null ? "null" : exitButton.name)}");
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(ShowPreviousPage);
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextPage);
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
        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(ShowPreviousPage);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
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

        BeginShowAfterDelay();
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
        currentPageIndex = 0;
        UpdatePageDisplay();
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
        if (prevButton != null)
        {
            prevButton.interactable = false;
        }
        if (nextButton != null)
        {
            nextButton.interactable = false;
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
        if (prevButton != null)
        {
            prevButton.interactable = false;
        }
        if (nextButton != null)
        {
            nextButton.interactable = false;
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

    void BeginShowAfterDelay()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }
        showRoutine = StartCoroutine(ShowPanelAfterDelay());
    }

    IEnumerator ShowPanelAfterDelay()
    {
        if (showDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(showDelaySeconds);
        }

        showRoutine = null;
        ShowPanel();
    }

    void ShowPreviousPage()
    {
        SetPage(currentPageIndex - 1);
    }

    void ShowNextPage()
    {
        SetPage(currentPageIndex + 1);
    }

    void SetPage(int index)
    {
        if (pages == null || pages.Count == 0)
        {
            currentPageIndex = 0;
            UpdatePageDisplay();
            return;
        }

        currentPageIndex = Mathf.Clamp(index, 0, pages.Count - 1);
        UpdatePageDisplay();
    }

    void UpdatePageDisplay()
    {
        var pageKey = pages != null && pages.Count > 0
            ? pages[Mathf.Clamp(currentPageIndex, 0, pages.Count - 1)]
            : string.Empty;

        if (pageText != null)
        {
            if (!string.IsNullOrEmpty(pageKey))
            {
                if (dynamicLocalizer != null)
                {
                    dynamicLocalizer.SetFieldByName(fieldName, pageKey);
                }
                else
                {
                    var localizeEvent = pageText.GetComponent<LocalizeStringEvent>();
                    if (localizeEvent == null)
                    {
                        localizeEvent = pageText.gameObject.AddComponent<LocalizeStringEvent>();
                    }

                    localizeEvent.StringReference = new LocalizedString(localizationTableName, pageKey);
                    EnsureUpdateStringListener(localizeEvent, pageText);
                    localizeEvent.enabled = true;
                    localizeEvent.RefreshString();
                }
            }
            else
            {
                if (dynamicLocalizer != null)
                {
                    dynamicLocalizer.ClearField(fieldName);
                }
                else
                {
                    var localizeEvent = pageText.GetComponent<LocalizeStringEvent>();
                    if (localizeEvent != null)
                    {
                        localizeEvent.StringReference.Clear();
                        localizeEvent.RefreshString();
                    }

                    pageText.text = string.Empty;
                }
            }
        }

        bool hasPages = pages != null && pages.Count > 0;
        if (prevButton != null)
        {
            prevButton.interactable = hasPages && currentPageIndex > 0;
        }
        if (nextButton != null)
        {
            nextButton.interactable = hasPages && currentPageIndex < pages.Count - 1;
        }
    }

    private void EnsureUpdateStringListener(LocalizeStringEvent localizeEvent, TMP_Text targetText)
    {
        if (localizeEvent == null || targetText == null)
        {
            return;
        }

        localizeEvent.OnUpdateString.RemoveListener(targetText.SetText);
        localizeEvent.OnUpdateString.AddListener(targetText.SetText);
    }
}
