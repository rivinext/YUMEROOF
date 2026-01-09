using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneOnceSlidePanelController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName;

    [Header("Slide Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private UISlidePanel slidePanel;
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector2 openPosition;
    [SerializeField] private Vector2 closePosition;
    [SerializeField, Min(0f)] private float slideInDelaySeconds;

    [Header("Page Content")]
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private List<string> pages = new();

    // SaveGameManager が ApplyManagers(Scene) でスロットごとに復元してくれる想定の値
    public bool HasSeenScenePanel { get; set; }

    private int currentPageIndex;
    private bool hasSavedSeenState;
    private bool isWaitingSlideOutCompletion;
    private Action cachedSlideOutComplete;
    private Coroutine slideInCoroutine;

    private void OnEnable()
    {
        Debug.Log(
            $"[SceneOnceSlidePanelController] OnEnable sceneName={SceneManager.GetActiveScene().name} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
        LogMissingReferences();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextPage);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(ShowPreviousPage);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitPanel);
        }

        hasSavedSeenState = false;
        currentPageIndex = 0;
        ApplySlideCurves();
        ApplyPanelPositions();
        UpdatePageDisplay();

        StartCoroutine(ShowAfterOneFrame());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitPanel);
        }

        ClearSlideOutHandler();

        if (slideInCoroutine != null)
        {
            StopCoroutine(slideInCoroutine);
            slideInCoroutine = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(
            $"[SceneOnceSlidePanelController] HandleSceneLoaded sceneName={scene.name} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
        TryShowForScene(scene.name);
    }

    private System.Collections.IEnumerator ShowAfterOneFrame()
    {
        yield return null;
        var activeSceneName = SceneManager.GetActiveScene().name;
        TryShowForScene(activeSceneName);
    }

    private void LogMissingReferences()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning(
                "[SceneOnceSlidePanelController] panelRoot is not assigned. Ensure the controller is active and panelRoot is assigned in the inspector.");
        }

        if (slidePanel == null)
        {
            Debug.LogWarning(
                "[SceneOnceSlidePanelController] slidePanel is not assigned. Ensure the controller is active and slidePanel is assigned in the inspector.");
        }
    }

    private void TryShowForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log(
                $"[SceneOnceSlidePanelController] TryShowForScene return=missingSceneName sceneName={sceneName} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
            return;
        }

        if (!sceneName.Equals(targetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log(
                $"[SceneOnceSlidePanelController] TryShowForScene return=sceneMismatch sceneName={sceneName} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
            return;
        }

        if (!ShouldShowPanel())
        {
            Debug.Log(
                $"[SceneOnceSlidePanelController] TryShowForScene return=shouldNotShow sceneName={sceneName} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
            return;
        }

        Debug.Log(
            $"[SceneOnceSlidePanelController] TryShowForScene show sceneName={sceneName} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
        ShowPanel();
    }

    private bool ShouldShowPanel()
    {
        Debug.Log(
            $"[SceneOnceSlidePanelController] ShouldShowPanel sceneName={SceneManager.GetActiveScene().name} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
        return !HasSeenScenePanel;
    }

    private void ShowPanel()
    {
        Debug.Log(
            $"[SceneOnceSlidePanelController] ShowPanel sceneName={SceneManager.GetActiveScene().name} targetSceneName={targetSceneName} HasSeenScenePanel={HasSeenScenePanel} panelRootIsNull={panelRoot == null} slidePanelIsNull={slidePanel == null}");
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        ApplySlideCurves();
        PlayerController.SetGlobalInputEnabled(false);
        StartSlideIn();
        currentPageIndex = 0;
        UpdatePageDisplay();
    }

    private void ExitPanel()
    {
        if (slidePanel == null || isWaitingSlideOutCompletion)
        {
            return;
        }

        isWaitingSlideOutCompletion = true;
        cachedSlideOutComplete = slidePanel.OnSlideOutComplete;
        slidePanel.OnSlideOutComplete = HandleSlideOutComplete;
        slidePanel.SlideOut();
    }

    private void HandleSlideOutComplete()
    {
        ClearSlideOutHandler();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        PlayerController.SetGlobalInputEnabled(true);
        SaveSeenStateOnce();
    }

    private void ClearSlideOutHandler()
    {
        if (!isWaitingSlideOutCompletion || slidePanel == null)
        {
            return;
        }

        slidePanel.OnSlideOutComplete = cachedSlideOutComplete;
        cachedSlideOutComplete = null;
        isWaitingSlideOutCompletion = false;
    }

    private void SaveSeenStateOnce()
    {
        if (hasSavedSeenState)
        {
            return;
        }

        hasSavedSeenState = true;
        HasSeenScenePanel = true;

        var slotKey = SaveGameManager.Instance != null ? SaveGameManager.Instance.CurrentSlotKey : null;
        if (string.IsNullOrEmpty(slotKey))
        {
            return;
        }

        SaveGameManager.Instance.SaveCurrentSlot();
    }

    private void ShowNextPage()
    {
        if (pages == null || pages.Count == 0)
        {
            return;
        }

        if (currentPageIndex >= pages.Count - 1)
        {
            return;
        }

        currentPageIndex++;
        UpdatePageDisplay();
    }

    private void ShowPreviousPage()
    {
        if (pages == null || pages.Count == 0)
        {
            return;
        }

        if (currentPageIndex <= 0)
        {
            return;
        }

        currentPageIndex--;
        UpdatePageDisplay();
    }

    private void UpdatePageDisplay()
    {
        if (bodyText != null)
        {
            if (pages == null || pages.Count == 0)
            {
                bodyText.text = string.Empty;
            }
            else
            {
                currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pages.Count - 1);
                bodyText.text = pages[currentPageIndex];
            }
        }

        if (prevButton != null)
        {
            prevButton.interactable = pages != null && pages.Count > 0 && currentPageIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = pages != null && pages.Count > 0 && currentPageIndex < pages.Count - 1;
        }
    }

    private void ApplySlideCurves()
    {
        if (slidePanel == null)
        {
            return;
        }

        slidePanel.SetSlideCurves(slideInCurve, slideOutCurve);
    }

    private void ApplyPanelPositions()
    {
        if (slidePanel == null)
        {
            return;
        }

        slidePanel.SetPositions(openPosition, closePosition);
        slidePanel.CloseImmediate();
    }

    private void StartSlideIn()
    {
        if (slideInCoroutine != null)
        {
            StopCoroutine(slideInCoroutine);
        }

        slideInCoroutine = StartCoroutine(DelayedSlideIn());
    }

    private System.Collections.IEnumerator DelayedSlideIn()
    {
        if (slideInDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(slideInDelaySeconds);
        }

        slidePanel?.SlideIn();
        slideInCoroutine = null;
    }
}
