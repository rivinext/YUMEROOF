using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages scene transitions using a sliding panel animation.
/// </summary>
public class SlideTransitionManager : MonoBehaviour
{
    public static SlideTransitionManager Instance { get; private set; }
    public event Action SlideOutCompleted;

    [SerializeField] private UISlidePanel slidePanel;
    [SerializeField] private UISlidePanel secondarySlidePanel;
    [SerializeField, Min(0f)] private float panelSequenceDelaySeconds = 2f;
    [Header("Loading Indicator")]
    [SerializeField] private GameObject loadingIndicatorRoot;
    [SerializeField] private TMP_Text loadingStatusText;
    [SerializeField] private Slider loadingProgressSlider;
    [SerializeField, Range(0f, 1f)] private float sceneProgressWeight = 0.5f;
    [SerializeField] private string sceneLoadingMessage = "Loading scene...";
    [SerializeField] private string furnitureLoadingMessage = "Placing furniture...";
    [SerializeField] private string finishingLoadingMessage = "Ready!";

    private readonly List<UISlidePanel> orderedSlidePanels = new(2);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Loads a scene with a slide in/out transition.
    /// </summary>
    public void LoadSceneWithSlide(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Slides out the panel and waits for completion.
    /// </summary>
    public IEnumerator RunSlideOut()
    {
        yield return RunSlideSequence(slideIn: false);
        Debug.Log("[SlideTransitionManager] SlideOutCompleted invoked.");
        SlideOutCompleted?.Invoke();
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return RunSlideSequence(slideIn: true);

        SaveGameManager.Instance.SaveCurrentSlot();

        string slotKey = UIMenuManager.SelectedSlotKey;
        if (string.IsNullOrEmpty(slotKey))
        {
            slotKey = SaveGameManager.Instance?.CurrentSlotKey;
        }
        GameSessionInitializer.CreateIfNeeded(slotKey);
        UIMenuManager.ClearSelectedSlot();

        ShowLoadingIndicator(sceneLoadingMessage);
        float clampedSceneWeight = Mathf.Clamp01(sceneProgressWeight);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            UpdateLoadingIndicator(Mathf.Lerp(0f, clampedSceneWeight, op.progress));
            yield return null;
        }
        UpdateLoadingIndicator(clampedSceneWeight);

        if (SceneManager.GetActiveScene().name != "MainMenu" &&
            FindFirstObjectByType<GameClock>() == null)
        {
            new GameObject("GameClock").AddComponent<GameClock>();
        }

        yield return WaitForFurnitureLoad(clampedSceneWeight);
        HideLoadingIndicator();

        var spawnManager = FindFirstObjectByType<RandomSceneSpawnManager>();
        spawnManager?.SpawnOnce();

        var mgr = SlideTransitionManager.Instance;
        if (mgr != null && mgr != this)
        {
            yield return mgr.RunSlideOut();
        }
        else
        {
            yield return RunSlideOut();
        }
    }

    void ShowLoadingIndicator(string message)
    {
        if (loadingIndicatorRoot != null)
            loadingIndicatorRoot.SetActive(true);

        UpdateLoadingIndicator(0f, message);
    }

    void UpdateLoadingIndicator(float progress, string message = null)
    {
        if (loadingProgressSlider != null)
            loadingProgressSlider.value = Mathf.Clamp01(progress);

        if (!string.IsNullOrEmpty(message) && loadingStatusText != null)
            loadingStatusText.text = message;
    }

    void HideLoadingIndicator()
    {
        if (loadingIndicatorRoot != null)
            loadingIndicatorRoot.SetActive(false);
    }

    IEnumerator WaitForFurnitureLoad(float sceneWeight)
    {
        FurnitureSaveManager furnitureMgr = FurnitureSaveManager.Instance;
        if (furnitureMgr == null)
        {
            UpdateLoadingIndicator(1f, finishingLoadingMessage);
            yield break;
        }

        float remainingWeight = Mathf.Clamp01(1f - sceneWeight);
        bool started = false;
        bool completed = false;

        void HandleStarted()
        {
            started = true;
            completed = false;
            UpdateLoadingIndicator(sceneWeight, furnitureLoadingMessage);
            SetLoadingMessage(furnitureLoadingMessage);
        }

        void HandleProgress(float progress)
        {
            float weighted = sceneWeight + remainingWeight * Mathf.Clamp01(progress);
            UpdateLoadingIndicator(weighted);
        }

        void HandleCompleted()
        {
            completed = true;
        }

        furnitureMgr.OnFurnitureLoadStarted += HandleStarted;
        furnitureMgr.OnFurnitureLoadProgress += HandleProgress;
        furnitureMgr.OnFurnitureLoadCompleted += HandleCompleted;

        bool immediateComplete = !furnitureMgr.IsFurnitureLoading && furnitureMgr.CurrentLoadProgress >= 0.999f;

        if (furnitureMgr.IsFurnitureLoading)
        {
            HandleStarted();
            HandleProgress(furnitureMgr.CurrentLoadProgress);
        }

        float waitTimer = 0f;
        const float startTimeout = 1.5f;

        while (!immediateComplete && !started && waitTimer < startTimeout)
        {
            if (furnitureMgr.IsFurnitureLoading)
            {
                HandleStarted();
                HandleProgress(furnitureMgr.CurrentLoadProgress);
                break;
            }

            if (furnitureMgr.CurrentLoadProgress >= 0.999f)
            {
                immediateComplete = true;
                break;
            }

            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!started && !immediateComplete)
        {
            // No furniture load triggered, treat as instantly complete.
            completed = true;
        }

        try
        {
            while (!immediateComplete && started && !completed)
            {
                yield return null;
            }
        }
        finally
        {
            furnitureMgr.OnFurnitureLoadStarted -= HandleStarted;
            furnitureMgr.OnFurnitureLoadProgress -= HandleProgress;
            furnitureMgr.OnFurnitureLoadCompleted -= HandleCompleted;
        }

        UpdateLoadingIndicator(1f, finishingLoadingMessage);
        SetLoadingMessage(finishingLoadingMessage);
    }

    void SetLoadingMessage(string message)
    {
        if (loadingStatusText != null && !string.IsNullOrEmpty(message))
        {
            loadingStatusText.text = message;
        }
    }

    private IEnumerator RunSlideSequence(bool slideIn)
    {
        var panels = CollectSlidePanels(slideIn);
        if (panels.Count == 0)
            yield break;

        var activeStates = new List<PanelSlideState>(panels.Count);

        try
        {
            bool hasStartedAnyPanel = false;

            foreach (var panel in panels)
            {
                if (!ShouldAnimatePanel(panel, slideIn))
                    continue;

                if (hasStartedAnyPanel && panelSequenceDelaySeconds > 0f)
                {
                    yield return new WaitForSecondsRealtime(panelSequenceDelaySeconds);
                }

                var state = StartPanelSlide(panel, slideIn);
                activeStates.Add(state);
                hasStartedAnyPanel = true;
            }

            foreach (var state in activeStates)
            {
                if (!state.Completed)
                {
                    yield return new WaitUntil(() => state.Completed);
                }
            }
        }
        finally
        {
            foreach (var state in activeStates)
            {
                state.UnsubscribeOnce();
            }
        }
    }

    private List<UISlidePanel> CollectSlidePanels(bool slideIn)
    {
        orderedSlidePanels.Clear();

        if (slidePanel != null)
            orderedSlidePanels.Add(slidePanel);

        if (secondarySlidePanel != null && !orderedSlidePanels.Contains(secondarySlidePanel))
            orderedSlidePanels.Add(secondarySlidePanel);

        if (slideIn && orderedSlidePanels.Count > 1)
        {
            orderedSlidePanels.Reverse();
        }

        return orderedSlidePanels;
    }

    private static bool ShouldAnimatePanel(UISlidePanel panel, bool slideIn)
    {
        if (panel == null)
            return false;

        return slideIn ? !panel.IsOpen : panel.IsOpen;
    }

    private PanelSlideState StartPanelSlide(UISlidePanel panel, bool slideIn)
    {
        var state = new PanelSlideState();

        void Handler()
        {
            state.Completed = true;
            state.UnsubscribeOnce();
        }

        if (slideIn)
        {
            state.Unsubscribe = () => panel.OnSlideInComplete -= Handler;
            panel.OnSlideInComplete += Handler;
            panel.SlideIn();
        }
        else
        {
            state.Unsubscribe = () => panel.OnSlideOutComplete -= Handler;
            panel.OnSlideOutComplete += Handler;
            panel.SlideOut();
        }

        return state;
    }

    private sealed class PanelSlideState
    {
        public bool Completed;
        public System.Action Unsubscribe;

        public void UnsubscribeOnce()
        {
            if (Unsubscribe == null)
                return;

            Unsubscribe.Invoke();
            Unsubscribe = null;
        }
    }
}
