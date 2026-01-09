using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StairRoomSlidePanelController : MonoBehaviour
{
    private const string TargetSceneName = "StairRoom";

    [SerializeField] private UISlidePanel slidePanel;

    private bool isWaitingForSlideOutStart;
    private bool hasShownForScene;
    private string currentSceneName;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        currentSceneName = SceneManager.GetActiveScene().name;
        hasShownForScene = false;
        TryShowForScene(currentSceneName);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeFromSlideOutStarted();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        hasShownForScene = false;
        TryShowForScene(scene.name);
    }

    private void TryShowForScene(string sceneName)
    {
        if (hasShownForScene)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName) ||
            !sceneName.Equals(TargetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            if (slideManager.IsAnyPanelOpen() || slideManager.IsSlideOutInProgress)
            {
                ShowPanel();
                return;
            }

            StartSlideOutStartWait(slideManager);
            return;
        }

        ShowPanel();
    }

    private void StartSlideOutStartWait(SlideTransitionManager slideManager)
    {
        if (isWaitingForSlideOutStart)
        {
            return;
        }

        slideManager.SlideOutStarted += HandleSlideOutStarted;
        isWaitingForSlideOutStart = true;
    }

    private void HandleSlideOutStarted()
    {
        UnsubscribeFromSlideOutStarted();
        ShowPanel();
    }

    private void UnsubscribeFromSlideOutStarted()
    {
        if (!isWaitingForSlideOutStart)
        {
            return;
        }

        var slideManager = SlideTransitionManager.Instance;
        if (slideManager != null)
        {
            slideManager.SlideOutStarted -= HandleSlideOutStarted;
        }

        isWaitingForSlideOutStart = false;
    }

    private void ShowPanel()
    {
        if (hasShownForScene)
        {
            return;
        }

        if (!string.Equals(currentSceneName, TargetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        hasShownForScene = true;
        slidePanel?.SlideIn();
    }
}
