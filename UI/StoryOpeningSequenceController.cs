using System.Collections;
using TMPro;
using UnityEngine;

public class StoryOpeningSequenceController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup openingPanel;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pressAnyKeyText;

    [Header("Timings")]
    [SerializeField, Min(0f)] private float bodyFadeInDuration = 1f;
    [SerializeField, Min(0f)] private float bodyDisplaySeconds = 2f;
    [SerializeField, Min(0f)] private float pressAnyKeyFadeInDuration = 1f;
    [SerializeField, Min(0f)] private float textFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float panelFadeOutDuration = 0.75f;

    private OrthographicCameraController cameraController;
    private bool inputLocked;
    private bool cameraWasEnabled;

    private void Awake()
    {
        InitializeUIState();
    }

    private void OnEnable()
    {
        if (!StoryOpeningSequenceState.IsNewStorySession)
        {
            DisableSequence();
            return;
        }

        StartCoroutine(RunSequence());
    }

    private void OnDisable()
    {
        RestoreControlIfNeeded();
    }

    private void InitializeUIState()
    {
        if (openingPanel != null)
        {
            openingPanel.alpha = 0f;
            openingPanel.gameObject.SetActive(false);
        }

        SetTextAlpha(bodyText, 0f);
        SetTextAlpha(pressAnyKeyText, 0f);
    }

    private IEnumerator RunSequence()
    {
        yield return WaitForSlideOutComplete();

        ShowPanel();
        LockControl();

        yield return FadeText(bodyText, 0f, 1f, bodyFadeInDuration);
        if (bodyDisplaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(bodyDisplaySeconds);
        }

        yield return FadeText(pressAnyKeyText, 0f, 1f, pressAnyKeyFadeInDuration);

        yield return WaitForAnyKey();

        yield return FadeText(bodyText, GetTextAlpha(bodyText), 0f, textFadeOutDuration);
        yield return FadeText(pressAnyKeyText, GetTextAlpha(pressAnyKeyText), 0f, textFadeOutDuration);
        yield return FadeCanvasGroup(openingPanel, openingPanel != null ? openingPanel.alpha : 0f, 0f, panelFadeOutDuration);

        if (openingPanel != null)
        {
            openingPanel.gameObject.SetActive(false);
        }

        RestoreControlIfNeeded();
        StoryOpeningSequenceState.Reset();
        enabled = false;
    }

    private IEnumerator WaitForSlideOutComplete()
    {
        var transitionManager = SlideTransitionManager.Instance;
        if (transitionManager == null)
        {
            yield return null;
            yield break;
        }

        bool completed = false;
        void HandleCompleted() => completed = true;

        transitionManager.SlideOutCompleted += HandleCompleted;
        try
        {
            while (!completed)
            {
                yield return null;
            }
        }
        finally
        {
            transitionManager.SlideOutCompleted -= HandleCompleted;
        }
    }

    private void ShowPanel()
    {
        if (openingPanel == null)
            return;

        openingPanel.gameObject.SetActive(true);
        openingPanel.alpha = 1f;
        openingPanel.blocksRaycasts = true;
        openingPanel.interactable = true;
    }

    private void LockControl()
    {
        PlayerController.SetGlobalInputEnabled(false);
        inputLocked = true;

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<OrthographicCameraController>();
            if (cameraController != null)
            {
                cameraWasEnabled = cameraController.enabled;
                cameraController.enabled = false;
            }
        }
    }

    private void RestoreControlIfNeeded()
    {
        if (inputLocked)
        {
            PlayerController.SetGlobalInputEnabled(true);
            inputLocked = false;
        }

        if (cameraController != null)
        {
            cameraController.enabled = cameraWasEnabled;
            cameraController = null;
        }
    }

    private IEnumerator WaitForAnyKey()
    {
        while (!Input.anyKeyDown && !Input.GetMouseButtonDown(0))
        {
            yield return null;
        }
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = to;
    }

    private static IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null)
            yield break;

        if (duration <= 0f)
        {
            SetTextAlpha(text, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            SetTextAlpha(text, alpha);
            yield return null;
        }

        SetTextAlpha(text, to);
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        var color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private static float GetTextAlpha(TMP_Text text)
    {
        return text != null ? text.color.a : 0f;
    }

    private void DisableSequence()
    {
        if (openingPanel != null)
        {
            openingPanel.gameObject.SetActive(false);
        }

        enabled = false;
    }
}
