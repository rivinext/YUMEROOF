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

    private void OnEnable()
    {
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
        UpdatePageDisplay();

        var activeSceneName = SceneManager.GetActiveScene().name;
        TryShowForScene(activeSceneName);
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
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryShowForScene(scene.name);
    }

    private void TryShowForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(targetSceneName))
        {
            return;
        }

        if (!sceneName.Equals(targetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!ShouldShowPanel())
        {
            return;
        }

        ShowPanel();
    }

    private bool ShouldShowPanel()
    {
        return !HasSeenScenePanel;
    }

    private void ShowPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        ApplySlideCurves();
        slidePanel?.SlideIn();
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
}
